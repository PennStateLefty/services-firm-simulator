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
        Assert.NotNull(employee.PersonalInfo);
        Assert.NotNull(employee.EmploymentInfo);
        Assert.NotNull(employee.Compensation);
        Assert.NotNull(employee.Metadata);
        Assert.InRange(employee.Metadata.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        Assert.InRange(employee.Metadata.UpdatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
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
        employee.PersonalInfo = new PersonalInfo
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "555-1234",
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701"
            }
        };
        employee.EmploymentInfo = new EmploymentInfo
        {
            HireDate = testDate,
            JobTitle = "Software Engineer",
            Department = "Engineering",
            ManagerId = "mgr-123",
            EmploymentType = EmploymentType.FullTime,
            Status = EmploymentStatus.Active,
            TerminationDate = testDate.AddDays(365)
        };
        employee.Compensation = new Compensation
        {
            SalaryType = SalaryType.Annual,
            CurrentSalary = 100000.00m,
            Currency = "USD",
            BonusTarget = 10000.00m
        };
        employee.Metadata = new Metadata
        {
            CreatedAt = testDate,
            UpdatedAt = testDate.AddDays(1),
            CreatedBy = "admin",
            LastModifiedBy = "manager"
        };

        // Assert
        Assert.Equal("test-id-123", employee.Id);
        Assert.Equal("EMP-1001", employee.EmployeeNumber);
        Assert.Equal("John", employee.PersonalInfo.FirstName);
        Assert.Equal("Doe", employee.PersonalInfo.LastName);
        Assert.Equal("john.doe@example.com", employee.PersonalInfo.Email);
        Assert.Equal("555-1234", employee.PersonalInfo.PhoneNumber);
        Assert.NotNull(employee.PersonalInfo.Address);
        Assert.Equal("123 Main St", employee.PersonalInfo.Address.Street);
        Assert.Equal("Engineering", employee.EmploymentInfo.Department);
        Assert.Equal("Software Engineer", employee.EmploymentInfo.JobTitle);
        Assert.Equal("mgr-123", employee.EmploymentInfo.ManagerId);
        Assert.Equal(EmploymentType.FullTime, employee.EmploymentInfo.EmploymentType);
        Assert.Equal(testDate, employee.EmploymentInfo.HireDate);
        Assert.Equal(testDate.AddDays(365), employee.EmploymentInfo.TerminationDate);
        Assert.Equal(EmploymentStatus.Active, employee.EmploymentInfo.Status);
        Assert.Equal(SalaryType.Annual, employee.Compensation.SalaryType);
        Assert.Equal(100000.00m, employee.Compensation.CurrentSalary);
        Assert.Equal(10000.00m, employee.Compensation.BonusTarget);
        Assert.Equal(testDate, employee.Metadata.CreatedAt);
        Assert.Equal(testDate.AddDays(1), employee.Metadata.UpdatedAt);
    }

    [Fact]
    public void Employee_TerminationDate_IsNullable()
    {
        // Arrange & Act
        var employee = new Employee
        {
            EmploymentInfo = new EmploymentInfo
            {
                TerminationDate = null
            }
        };

        // Assert
        Assert.Null(employee.EmploymentInfo.TerminationDate);
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
    public void PersonalInfo_FirstName_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.FirstName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void PersonalInfo_FirstName_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.FirstName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(100, attribute.MaximumLength);
    }

    [Fact]
    public void PersonalInfo_LastName_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.LastName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void PersonalInfo_LastName_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.LastName));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(100, attribute.MaximumLength);
    }

    [Fact]
    public void PersonalInfo_Email_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.Email));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void PersonalInfo_Email_EmailAddressAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(PersonalInfo).GetProperty(nameof(PersonalInfo.Email));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(EmailAddressAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void EmploymentInfo_JobTitle_StringLengthAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(EmploymentInfo).GetProperty(nameof(EmploymentInfo.JobTitle));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .FirstOrDefault() as StringLengthAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(200, attribute.MaximumLength);
    }

    [Fact]
    public void Compensation_CurrentSalary_RangeAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(Compensation).GetProperty(nameof(Compensation.CurrentSalary));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RangeAttribute), false)
            .FirstOrDefault() as RangeAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(0.0, Convert.ToDouble(attribute.Minimum));
        Assert.Equal(10000000.0, Convert.ToDouble(attribute.Maximum));
    }

    [Fact]
    public void EmploymentInfo_HireDate_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(EmploymentInfo).GetProperty(nameof(EmploymentInfo.HireDate));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void EmploymentInfo_Status_RequiredAttribute_IsPresent()
    {
        // Arrange
        var property = typeof(EmploymentInfo).GetProperty(nameof(EmploymentInfo.Status));

        // Act
        var attribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void EmploymentInfo_Status_UsesSharedEmploymentStatusEnum()
    {
        // Arrange
        var property = typeof(EmploymentInfo).GetProperty(nameof(EmploymentInfo.Status));

        // Act & Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(EmploymentStatus), property.PropertyType);
    }

    [Fact]
    public void EmploymentInfo_Status_CanBeSetToAllEnumValues()
    {
        // Arrange & Act & Assert
        var employmentInfo = new EmploymentInfo();

        employmentInfo.Status = EmploymentStatus.Pending;
        Assert.Equal(EmploymentStatus.Pending, employmentInfo.Status);

        employmentInfo.Status = EmploymentStatus.Active;
        Assert.Equal(EmploymentStatus.Active, employmentInfo.Status);

        employmentInfo.Status = EmploymentStatus.OnLeave;
        Assert.Equal(EmploymentStatus.OnLeave, employmentInfo.Status);

        employmentInfo.Status = EmploymentStatus.Terminated;
        Assert.Equal(EmploymentStatus.Terminated, employmentInfo.Status);
    }

    [Fact]
    public void Employee_Validation_ValidEmployee_PassesValidation()
    {
        // Arrange
        var employee = new Employee
        {
            EmployeeNumber = "EMP-1001",
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            },
            EmploymentInfo = new EmploymentInfo
            {
                HireDate = DateTime.UtcNow,
                JobTitle = "Software Engineer",
                Department = "Engineering",
                EmploymentType = EmploymentType.FullTime,
                Status = EmploymentStatus.Active
            },
            Compensation = new Compensation
            {
                SalaryType = SalaryType.Annual,
                CurrentSalary = 100000.00m,
                Currency = "USD"
            }
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
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            },
            EmploymentInfo = new EmploymentInfo
            {
                HireDate = DateTime.UtcNow,
                JobTitle = "Software Engineer",
                Department = "Engineering",
                EmploymentType = EmploymentType.FullTime,
                Status = EmploymentStatus.Active
            },
            Compensation = new Compensation
            {
                SalaryType = SalaryType.Annual,
                CurrentSalary = 100000.00m,
                Currency = "USD"
            }
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
    public void PersonalInfo_Validation_InvalidEmail_FailsValidation()
    {
        // Arrange
        var personalInfo = new PersonalInfo
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email"
        };

        var validationContext = new ValidationContext(personalInfo);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(personalInfo, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(PersonalInfo.Email)));
    }

    [Fact]
    public void Compensation_Validation_NegativeSalary_FailsValidation()
    {
        // Arrange
        var compensation = new Compensation
        {
            SalaryType = SalaryType.Annual,
            CurrentSalary = -1000.00m,
            Currency = "USD"
        };

        var validationContext = new ValidationContext(compensation);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(compensation, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.MemberNames.Contains(nameof(Compensation.CurrentSalary)));
    }

    [Fact]
    public void Employee_NestedStructures_AreInitialized()
    {
        // Arrange & Act
        var employee = new Employee();

        // Assert
        Assert.NotNull(employee.PersonalInfo);
        Assert.NotNull(employee.EmploymentInfo);
        Assert.NotNull(employee.Compensation);
        Assert.NotNull(employee.Metadata);
    }

    [Fact]
    public void Employee_ManagerId_CanReferenceAnotherEmployee()
    {
        // Arrange & Act
        var manager = new Employee { Id = "mgr-001" };
        var employee = new Employee
        {
            EmploymentInfo = new EmploymentInfo
            {
                ManagerId = manager.Id
            }
        };

        // Assert
        Assert.Equal("mgr-001", employee.EmploymentInfo.ManagerId);
    }
}
