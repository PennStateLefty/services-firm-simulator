using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace EmployeeService.Models;

public class Employee
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string DepartmentId { get; set; } = string.Empty;

    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Level { get; set; }

    [Range(0.0, 10000000.0)]
    public decimal Salary { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    [Required]
    public EmploymentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CreateEmployeeRequest
{
    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string DepartmentId { get; set; } = string.Empty;

    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Level { get; set; }

    [Range(0.0, 10000000.0)]
    public decimal Salary { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    [Required]
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Pending;
}

public class UpdateEmployeeRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? DepartmentId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    [Range(1, 10)]
    public int? Level { get; set; }

    [Range(0.0, 10000000.0)]
    public decimal? Salary { get; set; }

    public DateTime? TerminationDate { get; set; }

    public EmploymentStatus? Status { get; set; }
}
