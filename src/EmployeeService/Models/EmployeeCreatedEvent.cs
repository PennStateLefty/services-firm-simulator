namespace EmployeeService.Models;

/// <summary>
/// Event published when a new employee is created
/// </summary>
public class EmployeeCreatedEvent
{
    /// <summary>
    /// The unique identifier of the employee
    /// </summary>
    public required string EmployeeId { get; set; }

    /// <summary>
    /// The email address of the employee
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The department ID the employee belongs to
    /// </summary>
    public required string DepartmentId { get; set; }

    /// <summary>
    /// The hire date of the employee
    /// </summary>
    public required DateTime HireDate { get; set; }
}
