using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OnboardingService.Controllers;
using OnboardingService.Models;
using OnboardingService.Services;
using Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace OnboardingService.Tests;

public class EventsControllerTests
{
    private readonly Mock<IOnboardingService> _mockOnboardingService;
    private readonly Mock<ILogger<EventsController>> _mockLogger;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockOnboardingService = new Mock<IOnboardingService>();
        _mockLogger = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(
            _mockOnboardingService.Object,
            _mockLogger.Object);
    }

    private static EmployeeCreatedEvent CreateTestEmployeeCreatedEvent(
        string employeeId = "emp-123",
        string email = "test@example.com",
        string departmentId = "dept-456")
    {
        return new EmployeeCreatedEvent
        {
            EmployeeId = employeeId,
            Email = email,
            DepartmentId = departmentId,
            HireDate = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_CreatesOnboardingCase()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        _mockOnboardingService.Verify(
            x => x.SaveStateAsync(
                It.Is<OnboardingCase>(oc => 
                    oc.EmployeeId == employeeEvent.EmployeeId &&
                    oc.Status == OnboardingTaskStatus.NotStarted &&
                    oc.TargetCompletionDate.HasValue),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_SetsTargetCompletionDateTo30Days()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var startTime = DateTime.UtcNow;
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        OnboardingCase? savedCase = null;
        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .Callback<OnboardingCase, CancellationToken>((oc, ct) => savedCase = oc)
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedCase);
        Assert.NotNull(savedCase.TargetCompletionDate);
        
        var expectedDate = startTime.AddDays(30);
        var actualDate = savedCase.TargetCompletionDate.Value;
        
        // Allow for small time differences (within 1 minute)
        Assert.True(
            Math.Abs((actualDate - expectedDate).TotalMinutes) < 1,
            $"Expected target completion date around {expectedDate}, but got {actualDate}");
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_SetsStatusToNotStarted()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        OnboardingCase? savedCase = null;
        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .Callback<OnboardingCase, CancellationToken>((oc, ct) => savedCase = oc)
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedCase);
        Assert.Equal(OnboardingTaskStatus.NotStarted, savedCase.Status);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_ExtractsEmployeeIdCorrectly()
    {
        // Arrange
        var employeeId = "emp-999";
        var employeeEvent = CreateTestEmployeeCreatedEvent(employeeId: employeeId);
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        OnboardingCase? savedCase = null;
        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .Callback<OnboardingCase, CancellationToken>((oc, ct) => savedCase = oc)
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedCase);
        Assert.Equal(employeeId, savedCase.EmployeeId);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ExistingCase_SkipsCreation()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var existingCase = new OnboardingCase
        {
            Id = "existing-case-id",
            EmployeeId = employeeEvent.EmployeeId,
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.InProgress
        };

        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase> { existingCase });

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify SaveStateAsync was NOT called (idempotency)
        _mockOnboardingService.Verify(
            x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ExistingCase_ReturnsOkWithProcessedFalse()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var existingCase = new OnboardingCase
        {
            Id = "existing-case-id",
            EmployeeId = employeeEvent.EmployeeId,
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.InProgress
        };

        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase> { existingCase });

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value as dynamic;
        Assert.NotNull(value);
        Assert.False((bool)value.GetType().GetProperty("processed")?.GetValue(value)!);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_NullEvent_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_SaveStateThrowsException_Returns500()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_QueryStateThrowsException_Returns500()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Query error"));

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_InitializesEmptyTasksList()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        OnboardingCase? savedCase = null;
        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .Callback<OnboardingCase, CancellationToken>((oc, ct) => savedCase = oc)
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedCase);
        Assert.NotNull(savedCase.Tasks);
        Assert.Empty(savedCase.Tasks);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_SetsStartDateToCurrentTime()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var startTime = DateTime.UtcNow;
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        OnboardingCase? savedCase = null;
        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .Callback<OnboardingCase, CancellationToken>((oc, ct) => savedCase = oc)
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(savedCase);
        
        // Allow for small time differences (within 1 minute)
        Assert.True(
            Math.Abs((savedCase.StartDate - startTime).TotalMinutes) < 1,
            $"Expected start date around {startTime}, but got {savedCase.StartDate}");
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_ReturnsSuccessMessage()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase>());

        _mockOnboardingService
            .Setup(x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase oc, CancellationToken ct) => oc);

        // Act
        var result = await _controller.HandleEmployeeCreatedAsync(employeeEvent, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var value = okResult.Value as dynamic;
        Assert.NotNull(value);
        Assert.True((bool)value.GetType().GetProperty("processed")?.GetValue(value)!);
    }
}
