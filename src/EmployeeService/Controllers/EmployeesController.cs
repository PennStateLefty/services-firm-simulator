using Microsoft.AspNetCore.Mvc;
using EmployeeService.Models;
using EmployeeService.Services;
using EmployeeService.Exceptions;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Controllers;

[ApiController]
[Route("v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating employee with email: {Email}", request.Email);

            // Validate required fields
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                return BadRequest(ErrorResponse.ValidationError(errors, HttpContext.TraceIdentifier));
            }

            // Validate department exists (request.Department is now a string name)
            var departments = await _departmentService.GetAllAsync(cancellationToken);
            var department = departments.FirstOrDefault(d => 
                string.Equals(d.Name, request.Department, StringComparison.OrdinalIgnoreCase));
            
            if (department == null)
            {
                _logger.LogWarning("Department not found: {Department}", request.Department);
                return BadRequest(ErrorResponse.ValidationError(
                    new Dictionary<string, string[]>
                    {
                        ["Department"] = new[] { $"Department '{request.Department}' does not exist" }
                    },
                    HttpContext.TraceIdentifier
                ));
            }

            // Create employee
            var employee = await _employeeService.CreateAsync(request, cancellationToken);

            _logger.LogInformation("Employee created successfully: {EmployeeId}", employee.Id);

            // Return 201 Created with Location header
            return CreatedAtAction(
                nameof(GetEmployee),
                new { employeeId = employee.Id },
                employee
            );
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Email already exists: {Email}", ex.Email);
            return Conflict(ErrorResponse.Conflict(ex.Message, HttpContext.TraceIdentifier));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during employee creation");
            return BadRequest(ErrorResponse.Create(400, "Validation failed", ex.Message, HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeListResponse>> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? department = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting employees - Page: {Page}, PageSize: {PageSize}, Status: {Status}, Department: {Department}",
                page, pageSize, status, department);

            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(ErrorResponse.Create(
                    400, 
                    "Invalid parameter", 
                    "Page must be greater than or equal to 1", 
                    HttpContext.TraceIdentifier));
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(ErrorResponse.Create(
                    400, 
                    "Invalid parameter", 
                    "PageSize must be between 1 and 100", 
                    HttpContext.TraceIdentifier));
            }

            // Validate and map status if provided
            // OpenAPI spec uses: Onboarding, Active, Inactive
            // Internal enum has: Pending, Active, OnLeave, Terminated
            string? mappedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                mappedStatus = status switch
                {
                    "Onboarding" => "Pending",
                    "Active" => "Active",
                    "Inactive" => "Terminated",
                    _ => null
                };

                if (mappedStatus == null)
                {
                    return BadRequest(ErrorResponse.Create(
                        400,
                        "Invalid parameter",
                        "Status must be one of: Onboarding, Active, Inactive",
                        HttpContext.TraceIdentifier));
                }
            }

            // Convert department name to department ID if provided
            string? departmentId = null;
            if (!string.IsNullOrWhiteSpace(department))
            {
                var departments = await _departmentService.GetAllAsync(cancellationToken);
                var matchingDept = departments.FirstOrDefault(d => 
                    string.Equals(d.Name, department, StringComparison.OrdinalIgnoreCase));
                
                if (matchingDept != null)
                {
                    departmentId = matchingDept.Id;
                }
                else
                {
                    // No matching department found, return empty list
                    return Ok(new EmployeeListResponse
                    {
                        Employees = new List<EmployeeSummary>(),
                        Pagination = new PaginationMetadata
                        {
                            Page = page,
                            PageSize = pageSize,
                            TotalCount = 0,
                            TotalPages = 0
                        }
                    });
                }
            }

            var result = await _employeeService.GetEmployeesAsync(
                page, 
                pageSize, 
                mappedStatus, 
                departmentId, 
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees");
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }

    [HttpGet("{employeeId}")]
    public async Task<ActionResult<Employee>> GetEmployee(string employeeId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting employee: {EmployeeId}", employeeId);
            
            var employee = await _employeeService.GetByIdAsync(employeeId, cancellationToken);
            
            if (employee == null)
            {
                return NotFound(ErrorResponse.NotFound("Employee", HttpContext.TraceIdentifier));
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee: {EmployeeId}", employeeId);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext.TraceIdentifier));
        }
    }
}
