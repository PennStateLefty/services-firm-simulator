namespace EmployeeService.Models;

public class EmployeeSummary
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class EmployeeListResponse
{
    public List<EmployeeSummary> Employees { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

// DTOs matching OpenAPI spec structure
public class EmployeeDto
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public PersonalInfo PersonalInfo { get; set; } = new();
    public EmploymentInfo EmploymentInfo { get; set; } = new();
    public Compensation Compensation { get; set; } = new();
    public Metadata Metadata { get; set; } = new();
}

public class PersonalInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
}

public class EmploymentInfo
{
    public DateTime HireDate { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public string EmploymentType { get; set; } = "FullTime";
    public string Status { get; set; } = string.Empty;
}

public class Compensation
{
    public string SalaryType { get; set; } = "Annual";
    public decimal CurrentSalary { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
}
