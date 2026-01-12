using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace EmployeeService.Models;

// Employee model matching OpenAPI spec structure
public class Employee
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    public PersonalInfo PersonalInfo { get; set; } = new();

    [Required]
    public EmploymentInfo EmploymentInfo { get; set; } = new();

    [Required]
    public Compensation Compensation { get; set; } = new();

    [Required]
    public Metadata Metadata { get; set; } = new();
}

// Nested structures matching OpenAPI spec
public class PersonalInfo
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public Address? Address { get; set; }
}

public class EmploymentInfo
{
    [Required]
    public DateTime HireDate { get; set; }

    [Required]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    public string? ManagerId { get; set; }

    [Required]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    [Required]
    public EmploymentStatus Status { get; set; }

    public DateTime? TerminationDate { get; set; }
}

public class Compensation
{
    [Required]
    public SalaryType SalaryType { get; set; }

    [Required]
    [Range(0.0, 10000000.0)]
    public decimal CurrentSalary { get; set; }

    [Required]
    public string Currency { get; set; } = "USD";

    public decimal? BonusTarget { get; set; }
}

public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}

public class Metadata
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
}

// Request models matching OpenAPI spec
public class CreateEmployeeRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public Address? Address { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    [Required]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    public string? ManagerId { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    [Required]
    public SalaryType SalaryType { get; set; }

    [Required]
    [Range(0.0, 10000000.0)]
    public decimal CurrentSalary { get; set; }

    public decimal? BonusTarget { get; set; }
}

public class UpdateEmployeeRequest
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public Address? Address { get; set; }

    [StringLength(200)]
    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public string? ManagerId { get; set; }

    public EmploymentType? EmploymentType { get; set; }
}
