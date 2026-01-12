using EmployeeService.Infrastructure;
using EmployeeService.Models;

namespace EmployeeService.Services;

public interface IDepartmentService
{
    Task<Department?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Department>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Department> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<Department> UpdateAsync(string id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public class DepartmentService : IDepartmentService
{
    private readonly IDaprStateStore _stateStore;
    private readonly ILogger<DepartmentService> _logger;
    private const string DepartmentPrefix = "department:";

    public DepartmentService(IDaprStateStore stateStore, ILogger<DepartmentService> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Department?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting department by ID: {Id}", id);
        var department = await _stateStore.GetStateAsync<Department>($"{DepartmentPrefix}{id}", cancellationToken);
        
        if (department == null)
        {
            _logger.LogWarning("Department not found: {Id}", id);
        }
        
        return department;
    }

    public async Task<IEnumerable<Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all departments");
        
        // Note: Dapr state store query would be used in production
        // For simplicity, we'll return an empty list for now
        // This will be enhanced when implementing the full query functionality
        var departments = await _stateStore.QueryStateAsync<Department>(
            "{}", 
            cancellationToken
        );
        
        return departments;
    }

    public async Task<Department> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating department: {Name}", request.Name);
        
        var department = new Department
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            ManagerId = request.ManagerId,
            Headcount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _stateStore.SaveStateAsync($"{DepartmentPrefix}{department.Id}", department, cancellationToken);
        
        _logger.LogInformation("Department created: {Id}", department.Id);
        return department;
    }

    public async Task<Department> UpdateAsync(string id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating department: {Id}", id);
        
        var department = await GetByIdAsync(id, cancellationToken);
        if (department == null)
        {
            throw new KeyNotFoundException($"Department with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            department.Name = request.Name;
        }

        if (request.ManagerId != null)
        {
            department.ManagerId = request.ManagerId;
        }

        department.UpdatedAt = DateTime.UtcNow;

        await _stateStore.SaveStateAsync($"{DepartmentPrefix}{department.Id}", department, cancellationToken);
        
        _logger.LogInformation("Department updated: {Id}", department.Id);
        return department;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting department: {Id}", id);
        
        var department = await GetByIdAsync(id, cancellationToken);
        if (department == null)
        {
            throw new KeyNotFoundException($"Department with ID {id} not found");
        }

        await _stateStore.DeleteStateAsync($"{DepartmentPrefix}{id}", cancellationToken);
        
        _logger.LogInformation("Department deleted: {Id}", id);
    }
}
