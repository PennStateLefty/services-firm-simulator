using EmployeeService.Infrastructure;
using EmployeeService.Models;
using EmployeeService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Dapr.Client;
using Shared.Models;

namespace EmployeeService.Tests;

public class EmployeeCreatedEventTests
{
    private readonly Mock<IDaprStateStore> _mockStateStore;
    private readonly Mock<IDepartmentService> _mockDepartmentService;
    private readonly Mock<ILogger<EmployeeServiceImpl>> _mockLogger;
    private readonly Mock<DaprClient> _mockDaprClient;
    private readonly IEmployeeService _employeeService;

    public EmployeeCreatedEventTests()
    {
        _mockStateStore = new Mock<IDaprStateStore>();
        _mockDepartmentService = new Mock<IDepartmentService>();
        _mockLogger = new Mock<ILogger<EmployeeServiceImpl>>();
        _mockDaprClient = new Mock<DaprClient>();
        _employeeService = new EmployeeServiceImpl(
            _mockStateStore.Object,
            _mockDepartmentService.Object,
            _mockLogger.Object,
            _mockDaprClient.Object);
    }

    [Fact]
    public async Task CreateAsync_SuccessfulCreation_PublishesEmployeeCreatedEvent()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-123",
            JobTitle = "Software Engineer",
            CurrentSalary = 100000.00m,
            HireDate = DateTime.UtcNow
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        _mockDaprClient.Verify(x => x.PublishEventAsync(
            "pubsub",
            "employee-events",
            It.Is<EmployeeCreatedEvent>(e =>
                e.EmployeeId == result.Id &&
                e.Email == request.Email &&
                e.DepartmentId == request.Department &&
                e.HireDate == request.HireDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EventPublishingFails_DoesNotThrowException()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-456",
            JobTitle = "Product Manager",
            CurrentSalary = 120000.00m,
            HireDate = DateTime.UtcNow
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient.Setup(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Dapr publish failed"));

        // Act & Assert - should not throw
        var result = await _employeeService.CreateAsync(request);

        // Verify employee was still created
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal(request.Email, result.PersonalInfo.Email);

        // Verify publish was attempted
        _mockDaprClient.Verify(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.AtLeast(3)); // Should retry 3 times
    }

    [Fact]
    public async Task CreateAsync_EventPublishingFailsOnce_RetriesAndSucceeds()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "Bob",
            LastName = "Johnson",
            Email = "bob.j@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-789",
            JobTitle = "Designer",
            CurrentSalary = 90000.00m,
            HireDate = DateTime.UtcNow
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var callCount = 0;
        _mockDaprClient.Setup(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Task.FromException(new Exception("Temporary failure"));
                }
                return Task.CompletedTask;
            });

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        _mockDaprClient.Verify(x => x.PublishEventAsync(
            "pubsub",
            "employee-events",
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2)); // First attempt + one retry
    }

    [Fact]
    public async Task CreateAsync_PublishesEventWithCorrectPayload()
    {
        // Arrange
        var hireDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = new CreateEmployeeRequest
        {
            FirstName = "Alice",
            LastName = "Williams",
            Email = "alice.w@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-engineering",
            JobTitle = "Senior Engineer",
            CurrentSalary = 150000.00m,
            HireDate = hireDate
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EmployeeCreatedEvent? capturedEvent = null;
        _mockDaprClient.Setup(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, string, EmployeeCreatedEvent, CancellationToken>((pubsub, topic, ev, ct) =>
            {
                capturedEvent = ev;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _employeeService.CreateAsync(request);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(result.Id, capturedEvent.EmployeeId);
        Assert.Equal(request.Email, capturedEvent.Email);
        Assert.Equal(request.Department, capturedEvent.DepartmentId);
        Assert.Equal(hireDate, capturedEvent.HireDate);
    }

    [Fact]
    public async Task CreateAsync_EventPublishingLogsCorrectly()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test.user@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-test",
            JobTitle = "Tester",
            CurrentSalary = 80000.00m,
            HireDate = DateTime.UtcNow
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient.Setup(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _employeeService.CreateAsync(request);

        // Assert - verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing EmployeeCreated event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published EmployeeCreated event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EventPublishingFailure_LogsError()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            FirstName = "Error",
            LastName = "Test",
            Email = "error.test@example.com",
            SalaryType = SalaryType.Annual,
            Department = "dept-test",
            JobTitle = "Tester",
            CurrentSalary = 80000.00m,
            HireDate = DateTime.UtcNow
        };

        _mockStateStore.Setup(x => x.GetStateAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockStateStore.Setup(x => x.IncrementCounterAsync(
            It.Is<string>(k => k.StartsWith("employee-counter:")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStateStore.Setup(x => x.ExecuteStateTransactionAsync(
            It.IsAny<IEnumerable<(string key, object value)>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockDaprClient.Setup(x => x.PublishEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<EmployeeCreatedEvent>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Persistent failure"));

        // Act
        await _employeeService.CreateAsync(request);

        // Assert - verify error logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish EmployeeCreated event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
