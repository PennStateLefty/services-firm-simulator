using Shared.Models;

namespace EmployeeService.Models;

public class Department
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public int Headcount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
}

public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? ManagerId { get; set; }
}
