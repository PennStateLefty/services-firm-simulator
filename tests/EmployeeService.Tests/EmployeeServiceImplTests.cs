using EmployeeService.Infrastructure;
using EmployeeService.Models;
using EmployeeService.Services;
using EmployeeService.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Tests;

public class EmployeeServiceTests
{
    private readonly Mock<IDaprStateStore> _mockStateStore;
    private readonly Mock<ILogger<EmployeeServiceImpl>> _mockLogger;
    private readonly IEmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _mockStateStore = new Mock<IDaprStateStore>();
        _mockLogger = new Mock<ILogger<EmployeeServiceImpl>>();
        _employeeService = new EmployeeServiceImpl(_mockStateStore.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEmployee_ReturnsEmployee()
    {
        // Arrange
        var employeeId = "test-id";
        var expectedEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEmployee);

        // Act
        var result = await _employeeService.GetByIdAsync(employeeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employeeId, result.Id);
        Assert.Equal("EMP-1001", result.EmployeeNumber);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEmployee_ReturnsNull()
    {
        // Arrange
        var employeeId = "non-existing-id";
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _employeeService.GetByIdAsync(employeeId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEmployees()
    {
        // Arrange
        var employees = new List<Employee>
        {
            new Employee { Id = "1", EmployeeNumber = "EMP-1001", FirstName = "John", LastName = "Doe", Email = "john@example.com", DepartmentId = "dept-1", Status = EmploymentStatus.Active },
            new Employee { Id = "2", EmployeeNumber = "EMP-1002", FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", DepartmentId = "dept-2", Status = EmploymentStatus.Active }
        };
        _mockStateStore.Setup(x => x.QueryStateAsync<Employee>("{}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        // Act
        var result = await _employeeService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesEmployee()
    {
        // Arrange
        var request = new CreateEmployeeRequest
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
            Status = EmploymentStatus.Pending
        };

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal(request.EmployeeNumber, result.EmployeeNumber);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.Equal(request.Email, result.Email);
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.Is<string>(k => k.StartsWith("employee:")),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "", // Invalid: empty
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email", // Invalid email format
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _employeeService.CreateAsync(request));
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.IsAny<string>(),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEmployee_UpdatesEmployee()
    {
        // Arrange
        var employeeId = "test-id";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Title = "Software Engineer",
            Level = 5,
            Salary = 100000.00m,
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        var updateRequest = new UpdateEmployeeRequest
        {
            FirstName = "Jane",
            Title = "Senior Software Engineer",
            Level = 6
        };

        // Act
        var result = await _employeeService.UpdateAsync(employeeId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane", result.FirstName);
        Assert.Equal("Senior Software Engineer", result.Title);
        Assert.Equal(6, result.Level);
        Assert.Equal("Doe", result.LastName); // Unchanged
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"employee:{employeeId}",
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEmployee_ThrowsKeyNotFoundException()
    {
        // Arrange
        var employeeId = "non-existing-id";
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var updateRequest = new UpdateEmployeeRequest
        {
            FirstName = "Jane"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _employeeService.UpdateAsync(employeeId, updateRequest));
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.IsAny<string>(),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEmployee_SoftDeletesEmployee()
    {
        // Arrange
        var employeeId = "test-id";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active,
            TerminationDate = null
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // Act
        await _employeeService.DeleteAsync(employeeId);

        // Assert
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"employee:{employeeId}",
            It.Is<Employee>(e => 
                e.Status == EmploymentStatus.Terminated && 
                e.TerminationDate != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEmployee_ThrowsKeyNotFoundException()
    {
        // Arrange
        var employeeId = "non-existing-id";
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _employeeService.DeleteAsync(employeeId));
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.IsAny<string>(),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueryAsync_WithFilter_ReturnsFilteredEmployees()
    {
        // Arrange
        var filter = "{\"status\":\"Active\"}";
        var employees = new List<Employee>
        {
            new Employee { Id = "1", EmployeeNumber = "EMP-1001", FirstName = "John", LastName = "Doe", Email = "john@example.com", DepartmentId = "dept-1", Status = EmploymentStatus.Active }
        };
        _mockStateStore.Setup(x => x.QueryStateAsync<Employee>(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

        // Act
        var result = await _employeeService.QueryAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, e => Assert.Equal(EmploymentStatus.Active, e.Status));
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        var afterCreate = DateTime.UtcNow;
        Assert.InRange(result.CreatedAt, beforeCreate.AddSeconds(-1), afterCreate.AddSeconds(1));
        Assert.InRange(result.UpdatedAt, beforeCreate.AddSeconds(-1), afterCreate.AddSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var employeeId = "test-id";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        var updateRequest = new UpdateEmployeeRequest
        {
            FirstName = "Jane"
        };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _employeeService.UpdateAsync(employeeId, updateRequest);

        // Assert
        var afterUpdate = DateTime.UtcNow;
        Assert.InRange(result.UpdatedAt, beforeUpdate.AddSeconds(-1), afterUpdate.AddSeconds(1));
        Assert.Equal(existingEmployee.CreatedAt, result.CreatedAt); // CreatedAt should not change
    }

    [Fact]
    public async Task CreateAsync_UsesCorrectKeyPattern()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.Is<string>(k => k.StartsWith("employee:") && k.Contains(result.Id)),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var existingEmployeeId = "existing-id";
        var email = "duplicate@example.com";
        
        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{email.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployeeId);

        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "EMP-1002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = email,
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _employeeService.CreateAsync(request));
        Assert.Equal(email, exception.Email);
        Assert.Contains(email, exception.Message);
        
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.Is<string>(k => k.StartsWith("employee:")),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UniqueEmail_CreatesEmailIndex()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{request.Email.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"email-index:{request.Email.ToLowerInvariant()}",
            result.Id,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailCaseInsensitive_DetectsDuplicate()
    {
        // Arrange
        var existingEmployeeId = "existing-id";
        var email = "Test@Example.COM";
        
        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{email.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployeeId);

        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = "EMP-1002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = email,
            DepartmentId = "dept-123",
            Level = 5,
            Salary = 100000.00m,
            HireDate = DateTime.UtcNow,
            Status = EmploymentStatus.Pending
        };

        // Act & Assert
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _employeeService.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_ChangingToExistingEmail_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var employeeId = "test-id";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        var newEmail = "existing@example.com";
        var otherEmployeeId = "other-id";
        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{newEmail.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherEmployeeId);

        var updateRequest = new UpdateEmployeeRequest
        {
            Email = newEmail
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _employeeService.UpdateAsync(employeeId, updateRequest));
        Assert.Equal(newEmail, exception.Email);
        
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.IsAny<string>(),
            It.IsAny<Employee>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangingEmail_UpdatesEmailIndex()
    {
        // Arrange
        var employeeId = "test-id";
        var oldEmail = "old@example.com";
        var newEmail = "new@example.com";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = oldEmail,
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{newEmail.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var updateRequest = new UpdateEmployeeRequest
        {
            Email = newEmail
        };

        // Act
        var result = await _employeeService.UpdateAsync(employeeId, updateRequest);

        // Assert
        Assert.Equal(newEmail, result.Email);
        
        // Verify old index is deleted
        _mockStateStore.Verify(x => x.DeleteStateAsync(
            $"email-index:{oldEmail.ToLowerInvariant()}",
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify new index is created
        _mockStateStore.Verify(x => x.SaveStateAsync(
            $"email-index:{newEmail.ToLowerInvariant()}",
            employeeId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SameEmail_DoesNotUpdateIndex()
    {
        // Arrange
        var employeeId = "test-id";
        var email = "john.doe@example.com";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        var updateRequest = new UpdateEmployeeRequest
        {
            Email = email // Same email
        };

        // Act
        await _employeeService.UpdateAsync(employeeId, updateRequest);

        // Assert
        _mockStateStore.Verify(x => x.DeleteStateAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        
        _mockStateStore.Verify(x => x.SaveStateAsync(
            It.Is<string>(k => k.StartsWith("email-index:")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmailCaseChange_UpdatesIndex()
    {
        // Arrange
        var employeeId = "test-id";
        var oldEmail = "john.doe@example.com";
        var newEmail = "John.Doe@Example.COM"; // Same email, different case
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = oldEmail,
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // The index check should find the same employee (case-insensitive)
        _mockStateStore.Setup(x => x.GetStateAsync<string>($"email-index:{newEmail.ToLowerInvariant()}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeId); // Same employee

        var updateRequest = new UpdateEmployeeRequest
        {
            Email = newEmail
        };

        // Act
        var result = await _employeeService.UpdateAsync(employeeId, updateRequest);

        // Assert
        Assert.Equal(newEmail, result.Email);
        
        // Should not delete or recreate index since it's the same email (case-insensitive)
        _mockStateStore.Verify(x => x.DeleteStateAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_DoesNotDeleteEmailIndex()
    {
        // Arrange
        var employeeId = "test-id";
        var email = "john.doe@example.com";
        var existingEmployee = new Employee
        {
            Id = employeeId,
            EmployeeNumber = "EMP-1001",
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            DepartmentId = "dept-123",
            Status = EmploymentStatus.Active,
            TerminationDate = null
        };
        _mockStateStore.Setup(x => x.GetStateAsync<Employee>($"employee:{employeeId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // Act
        await _employeeService.DeleteAsync(employeeId);

        // Assert
        // Email index should NOT be deleted on soft delete
        _mockStateStore.Verify(x => x.DeleteStateAsync(
            It.Is<string>(k => k.StartsWith("email-index:")),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
