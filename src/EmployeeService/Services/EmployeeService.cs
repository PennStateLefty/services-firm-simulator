using EmployeeService.Infrastructure;
using EmployeeService.Models;
using EmployeeService.Exceptions;
using Shared.Models;
using System.ComponentModel.DataAnnotations;
using Dapr.Client;

namespace EmployeeService.Services;

public interface IEmployeeService
{
    Task<Employee?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Employee> UpdateAsync(string id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> QueryAsync(string filter, CancellationToken cancellationToken = default);
    Task<EmployeeListResponse> GetEmployeesAsync(
        int page, 
        int pageSize, 
        string? status, 
        string? departmentId, 
        CancellationToken cancellationToken = default);
}

public class EmployeeServiceImpl : IEmployeeService
{
    private readonly IDaprStateStore _stateStore;
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<EmployeeServiceImpl> _logger;
    private readonly DaprClient _daprClient;
    private const string EmployeePrefix = "employee:";
    private const string EmailIndexPrefix = "email-index:";
    private const string EmployeeCounterPrefix = "employee-counter:";
    private const string CompensationHistoryPrefix = "compensation-history:";
    private const string EmployeeEventsTopic = "employee-events";
    private const string PubSubName = "pubsub";

    public EmployeeServiceImpl(
        IDaprStateStore stateStore, 
        IDepartmentService departmentService,
        ILogger<EmployeeServiceImpl> logger,
        DaprClient daprClient)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
    }

    public async Task<Employee?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting employee by ID: {Id}", id);
        var employee = await _stateStore.GetStateAsync<Employee>($"{EmployeePrefix}{id}", cancellationToken);
        
        if (employee == null)
        {
            _logger.LogWarning("Employee not found: {Id}", id);
        }
        
        return employee;
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all employees");
        
        var employees = await _stateStore.QueryStateAsync<Employee>(
            "{}", 
            cancellationToken
        );
        
        return employees;
    }

    public async Task<Employee> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating employee");
        
        // Validate the request
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
            _logger.LogError("Validation failed for employee creation: {Errors}", errors);
            throw new ValidationException($"Validation failed: {errors}");
        }
        
        // Check if email already exists
        var emailIndexKey = $"{EmailIndexPrefix}{request.Email.ToLowerInvariant()}";
        var existingEmployeeId = await _stateStore.GetStateAsync<string>(emailIndexKey, cancellationToken);
        if (existingEmployeeId != null)
        {
            _logger.LogWarning("Email already exists: {Email}", request.Email);
            throw new EmailAlreadyExistsException(request.Email);
        }
        
        // Auto-generate employee number
        var employeeNumber = await GenerateEmployeeNumberAsync(cancellationToken);
        _logger.LogInformation("Generated employee number: {EmployeeNumber}", employeeNumber);
        
        var now = DateTime.UtcNow;
        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeNumber = employeeNumber,
            PersonalInfo = new PersonalInfo
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address
            },
            EmploymentInfo = new EmploymentInfo
            {
                HireDate = request.HireDate,
                JobTitle = request.JobTitle,
                Department = request.Department,
                ManagerId = request.ManagerId,
                EmploymentType = request.EmploymentType,
                Status = EmploymentStatus.Pending
            },
            Compensation = new Compensation
            {
                SalaryType = request.SalaryType,
                CurrentSalary = request.CurrentSalary,
                Currency = "USD",
                BonusTarget = request.BonusTarget
            },
            Metadata = new Metadata
            {
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = null,
                LastModifiedBy = null
            }
        };

        // Create compensation history entry for the hire
        var compensationHistory = new CompensationHistory
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = employee.Id,
            EffectiveDate = request.HireDate,
            PreviousSalary = null,
            NewSalary = request.CurrentSalary,
            ChangeType = CompensationChangeType.Hire,
            ChangeReason = "Initial hire",
            ApprovedBy = null,
            CreatedAt = now
        };

        // Save employee, email index, and compensation history atomically using Dapr state transaction
        // This ensures either all three operations succeed or all fail, maintaining consistency
        var operations = new List<(string key, object value)>
        {
            ($"{EmployeePrefix}{employee.Id}", employee),
            (emailIndexKey, employee.Id),
            ($"{CompensationHistoryPrefix}{compensationHistory.Id}", compensationHistory)
        };
        
        await _stateStore.ExecuteStateTransactionAsync(operations, cancellationToken);
        
        _logger.LogInformation("Employee created: {Id} with compensation history: {CompensationHistoryId}", employee.Id, compensationHistory.Id);
        
        // Publish EmployeeCreated event
        await PublishEmployeeCreatedEventAsync(employee, cancellationToken);
        
        return employee;
    }

    public async Task<Employee> UpdateAsync(string id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating employee: {Id}", id);
        
        // Validate the request
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
            _logger.LogError("Validation failed for employee update: {Errors}", errors);
            throw new ValidationException($"Validation failed: {errors}");
        }
        
        var employee = await GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            _logger.LogError("Employee not found for update: {Id}", id);
            throw new KeyNotFoundException($"Employee with ID {id} not found");
        }

        // Track old email for index cleanup
        string? oldEmail = null;

        // Update only the provided fields in PersonalInfo
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            employee.PersonalInfo.FirstName = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            employee.PersonalInfo.LastName = request.LastName;
        }

        if (request.PhoneNumber != null)
        {
            employee.PersonalInfo.PhoneNumber = request.PhoneNumber;
        }

        if (request.Address != null)
        {
            employee.PersonalInfo.Address = request.Address;
        }

        // Update EmploymentInfo fields
        if (!string.IsNullOrWhiteSpace(request.JobTitle))
        {
            employee.EmploymentInfo.JobTitle = request.JobTitle;
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            employee.EmploymentInfo.Department = request.Department;
        }

        if (request.ManagerId != null)
        {
            employee.EmploymentInfo.ManagerId = request.ManagerId;
        }

        if (request.EmploymentType.HasValue)
        {
            employee.EmploymentInfo.EmploymentType = request.EmploymentType.Value;
        }

        employee.Metadata.UpdatedAt = DateTime.UtcNow;

        // Save employee
        await _stateStore.SaveStateAsync($"{EmployeePrefix}{employee.Id}", employee, cancellationToken);
        
        // Update email index if email changed
        if (oldEmail != null)
        {
            // Delete old email index
            var oldEmailIndexKey = $"{EmailIndexPrefix}{oldEmail.ToLowerInvariant()}";
            await _stateStore.DeleteStateAsync(oldEmailIndexKey, cancellationToken);
            
            // Create new email index
            var newEmailIndexKey = $"{EmailIndexPrefix}{employee.PersonalInfo.Email.ToLowerInvariant()}";
            await _stateStore.SaveStateAsync(newEmailIndexKey, employee.Id, cancellationToken);
            
            _logger.LogInformation("Email index updated for employee {Id}: {OldEmail} -> {NewEmail}", employee.Id, oldEmail, employee.PersonalInfo.Email);
        }
        
        _logger.LogInformation("Employee updated: {Id}", employee.Id);
        return employee;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting employee (soft delete): {Id}", id);
        
        var employee = await GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            _logger.LogError("Employee not found for deletion: {Id}", id);
            throw new KeyNotFoundException($"Employee with ID {id} not found");
        }

        // Soft delete: update status to Terminated
        employee.EmploymentInfo.Status = EmploymentStatus.Terminated;
        employee.EmploymentInfo.TerminationDate = DateTime.UtcNow;
        employee.Metadata.UpdatedAt = DateTime.UtcNow;

        await _stateStore.SaveStateAsync($"{EmployeePrefix}{employee.Id}", employee, cancellationToken);
        
        // Note: Email index is NOT deleted on soft delete to prevent email reuse
        // For hard delete implementation, add:
        // var emailIndexKey = $"{EmailIndexPrefix}{employee.PersonalInfo.Email.ToLowerInvariant()}";
        // await _stateStore.DeleteStateAsync(emailIndexKey, cancellationToken);
        
        _logger.LogInformation("Employee soft deleted: {Id}", employee.Id);
    }

    public async Task<IEnumerable<Employee>> QueryAsync(string filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying employees with filter: {Filter}", filter);
        
        var employees = await _stateStore.QueryStateAsync<Employee>(filter, cancellationToken);
        
        _logger.LogInformation("Query returned {Count} employees", employees.Count());
        return employees;
    }

    public async Task<EmployeeListResponse> GetEmployeesAsync(
        int page, 
        int pageSize, 
        string? status, 
        string? departmentId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting employees - Page: {Page}, PageSize: {PageSize}, Status: {Status}, DepartmentId: {DepartmentId}", 
            page, pageSize, status, departmentId);

        // Get all employees
        var allEmployees = await GetAllAsync(cancellationToken);
        
        // Apply filters
        var filteredEmployees = allEmployees.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<EmploymentStatus>(status, true, out var statusEnum))
            {
                filteredEmployees = filteredEmployees.Where(e => e.EmploymentInfo.Status == statusEnum);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            filteredEmployees = filteredEmployees.Where(e => 
                string.Equals(e.EmploymentInfo.Department, departmentId, StringComparison.OrdinalIgnoreCase));
        }
        
        // Sort by LastName, FirstName ascending
        var sortedEmployees = filteredEmployees
            .OrderBy(e => e.PersonalInfo.LastName)
            .ThenBy(e => e.PersonalInfo.FirstName)
            .ToList();
        
        var totalCount = sortedEmployees.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        // Apply pagination
        var pagedEmployees = sortedEmployees
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        // Create EmployeeSummary list
        // Map internal status to OpenAPI spec status
        var employeeSummaries = pagedEmployees.Select(employee => new EmployeeSummary
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            FullName = $"{employee.PersonalInfo.FirstName} {employee.PersonalInfo.LastName}",
            Email = employee.PersonalInfo.Email,
            JobTitle = employee.EmploymentInfo.JobTitle,
            Department = employee.EmploymentInfo.Department,
            Status = employee.EmploymentInfo.Status switch
            {
                EmploymentStatus.Pending => "Onboarding",
                EmploymentStatus.Active => "Active",
                EmploymentStatus.Terminated => "Inactive",
                EmploymentStatus.OnLeave => "Active", // Map OnLeave to Active for API
                _ => employee.EmploymentInfo.Status.ToString()
            }
        }).ToList();
        
        var response = new EmployeeListResponse
        {
            Employees = employeeSummaries,
            Pagination = new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
        
        _logger.LogInformation(
            "Returning {Count} employees out of {TotalCount} total", 
            employeeSummaries.Count, totalCount);
        
        return response;
    }

    private async Task<string> GenerateEmployeeNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var counterKey = $"{EmployeeCounterPrefix}{year}";
        
        var counter = await _stateStore.IncrementCounterAsync(counterKey, cancellationToken);
        
        // Format: EMP{year}{sequential:000000}
        // Example: EMP2026001, EMP2026002, etc.
        return $"EMP{year}{counter:D6}";
    }

    private async Task PublishEmployeeCreatedEventAsync(Employee employee, CancellationToken cancellationToken)
    {
        var employeeCreatedEvent = new EmployeeCreatedEvent
        {
            EmployeeId = employee.Id,
            Email = employee.PersonalInfo.Email,
            DepartmentId = employee.EmploymentInfo.Department,
            HireDate = employee.EmploymentInfo.HireDate
        };

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Publishing EmployeeCreated event for employee {EmployeeId} to topic {Topic} (attempt {Attempt}/{MaxRetries})",
                    employee.Id, EmployeeEventsTopic, attempt, maxRetries);

                await _daprClient.PublishEventAsync(
                    PubSubName,
                    EmployeeEventsTopic,
                    employeeCreatedEvent,
                    cancellationToken);

                _logger.LogInformation(
                    "Successfully published EmployeeCreated event for employee {EmployeeId}",
                    employee.Id);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Failed to publish EmployeeCreated event for employee {EmployeeId} (attempt {Attempt}/{MaxRetries}). Retrying...",
                    employee.Id, attempt, maxRetries);

                await Task.Delay(retryDelay * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish EmployeeCreated event for employee {EmployeeId} after {MaxRetries} attempts. Event will not be published.",
                    employee.Id, maxRetries);

                // Don't throw - we don't want to fail employee creation if event publishing fails
                // The employee has already been created in state store
                return;
            }
        }
    }
}
