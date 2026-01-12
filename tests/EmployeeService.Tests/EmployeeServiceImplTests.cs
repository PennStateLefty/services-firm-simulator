using EmployeeService.Infrastructure;
using EmployeeService.Models;
using EmployeeService.Services;
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
}
