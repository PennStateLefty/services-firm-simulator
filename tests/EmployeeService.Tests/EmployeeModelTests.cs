using System.ComponentModel.DataAnnotations;
using EmployeeService.Models;
using Shared.Models;

namespace EmployeeService.Tests;

public class EmployeeModelTests
{
    [Fact]
    public void Employee_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var employee = new Employee();

        // Assert
        Assert.NotNull(employee.Id);
        Assert.NotEmpty(employee.Id);
        Assert.Empty(employee.EmployeeNumber);
        Assert.Empty(employee.FirstName);
        Assert.Empty(employee.LastName);
        Assert.Empty(employee.Email);
        Assert.Empty(employee.DepartmentId);
        Assert.Empty(employee.Title);
        Assert.Equal(0, employee.Level);
        Assert.Equal(0, employee.Salary);
        Assert.Null(employee.TerminationDate);
        Assert.InRange(employee.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        Assert.InRange(employee.UpdatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void Employee_Id_IsGeneratedAsGuid()
    {
        // Arrange & Act
        var employee = new Employee();

        // Assert
        Assert.True(Guid.TryParse(employee.Id, out _));
    }

    [Fact]
    public void Employee_AllProperties_CanBeSet()
    {
        // Arrange
        var employee = new Employee();
        var testDate = DateTime.UtcNow;

        // Act
        employee.Id = "test-id-123";
        employee.EmployeeNumber = "EMP-1001";
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.Email = "john.doe@example.com";
        employee.DepartmentId = "dept-123";
        employee.Title = "Software Engineer";
        employee.Level = 5;
        employee.Salary = 100000.00m;
        employee.HireDate = testDate;
        employee.TerminationDate = testDate.AddDays(365);
        employee.Status = EmploymentStatus.Active;
        employee.CreatedAt = testDate;
        employee.UpdatedAt = testDate.AddDays(1);

        // Assert
        Assert.Equal("test-id-123", employee.Id);
        Assert.Equal("EMP-1001", employee.EmployeeNumber);
        Assert.Equal("John", employee.FirstName);
        Assert.Equal("Doe", employee.LastName);
        Assert.Equal("john.doe@example.com", employee.Email);
        Assert.Equal("dept-123", employee.DepartmentId);
        Assert.Equal("Software Engineer", employee.Title);
        Assert.Equal(5, employee.Level);
        Assert.Equal(100000.00m, employee.Salary);
        Assert.Equal(testDate, employee.HireDate);
        Assert.Equal(testDate.AddDays(365), employee.TerminationDate);
        Assert.Equal(EmploymentStatus.Active, employee.Status);
        Assert.Equal(testDate, employee.CreatedAt);
        Assert.Equal(testDate.AddDays(1), employee.UpdatedAt);
    }

    [Fact]
    public void Employee_TerminationDate_IsNullable()
    {
        // Arrange & Act
        var employee = new Employee
        {
            TerminationDate = null
        };

        // Assert
        Assert.Null(employee.TerminationDate);
    }

    [Fact]
    public void Employee_EmployeeNumber_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.EmployeeNumber));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_FirstName_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.FirstName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_FirstName_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.FirstName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(100, attribute.MaximumLength);
    }

    [Fact]
    public void Employee_LastName_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.LastName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_LastName_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.LastName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(100, attribute.MaximumLength);
    }

    [Fact]
    public void Employee_Email_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Email));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_Email_EmailAddressAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Email));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(EmailAddressAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_DepartmentId_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.DepartmentId));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_Title_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Title));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(200, attribute.MaximumLength);
    }

    [Fact]
    public void Employee_Level_RangeAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Level));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RangeAttribute), false)
            .FirstOrDefault() as RangeAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(1, attribute.Minimum);
        Assert.Equal(10, attribute.Maximum);
    }

    [Fact]
    public void Employee_Salary_RangeAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Salary));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RangeAttribute), false)
            .FirstOrDefault() as RangeAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(0.0, Convert.ToDouble(attribute.Minimum));
    }

    [Fact]
    public void Employee_HireDate_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.HireDate));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_Status_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Status));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Employee_Status_UsesSharedEmploymentStatusEnum()
    {
        // Arrange
        var property = typeof(Employee).GetProperty(nameof(Employee.Status));

        // Act & Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(EmploymentStatus), property.PropertyType);
    }

    [Fact]
    public void Employee_Status_CanBeSetToAllEnumValues()
    {
        // Arrange & Act & Assert
        var employee = new Employee();

        employee.Status = EmploymentStatus.Pending;
        Assert.Equal(EmploymentStatus.Pending, employee.Status);

        employee.Status = EmploymentStatus.Active;
        Assert.Equal(EmploymentStatus.Active, employee.Status);

        employee.Status = EmploymentStatus.OnLeave;
        Assert.Equal(EmploymentStatus.OnLeave, employee.Status);

        employee.Status = EmploymentStatus.Terminated;
        Assert.Equal(EmploymentStatus.Terminated, employee.Status);
    }

    [Fact]
    public void Employee_Validation_ValidEmployee_PassesValidation()
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Active
        };

        var validationContext = new ValidationContext(employee);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(employee, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Employee_Validation_MissingEmployeeNumber_FailsValidation(string? employeeNumber)
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber!,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Active
        };

        var validationContext = new ValidationContext(employee);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(employee, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(Employee.EmployeeNumber)));
    }

    [Fact]
    public void Employee_Validation_InvalidEmail_FailsValidation()
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Active
        };

        var validationContext = new ValidationContext(employee);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(employee, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(Employee.Email)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Employee_Validation_InvalidLevel_FailsValidation(int level)
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = level,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Active
        };

        var validationContext = new ValidationContext(employee);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(employee, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(Employee.Level)));
    }

    [Fact]
    public void Employee_Validation_NegativeSalary_FailsValidation()
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = -1000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Active
        };

        var validationContext = new ValidationContext(employee);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(employee, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(Employee.Salary)));
    }
}
