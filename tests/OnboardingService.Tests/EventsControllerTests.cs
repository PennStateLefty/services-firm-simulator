using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OnboardingService.Controllers;
using OnboardingService.Models;
using OnboardingService.Services;
using Shared.Models;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace OnboardingService.Tests;

public class EventsControllerTests
{
    private readonly Mock<IOnboardingService> _mockOnboardingService;
    private readonly Mock<ITaskTemplateService> _mockTaskTemplateService;
    private readonly Mock<ILogger<EventsController>> _mockLogger;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockOnboardingService = new Mock<IOnboardingService>();
        _mockTaskTemplateService = new Mock<ITaskTemplateService>();
        _mockLogger = new Mock<ILogger<EventsController>>();
        
        // Setup default behavior for task template service
        _mockTaskTemplateService
            .Setup(x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()))
            .Returns(new List<OnboardingTask>());
        
        _controller = new EventsController(
            _mockOnboardingService.Object,
            _mockTaskTemplateService.Object,
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
        
        // Serialize and deserialize to access anonymous object properties
        var json = JsonSerializer.Serialize(okResult.Value);
        var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        Assert.NotNull(responseData);
        Assert.False(responseData["processed"].GetBoolean());
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
    public async Task HandleEmployeeCreatedAsync_ValidEvent_InitializesTasksListFromTemplates()
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
        
        // Verify that GenerateTasksFromTemplates was called
        _mockTaskTemplateService.Verify(
            x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()),
            Times.Once);
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
        
        // Serialize and deserialize to access anonymous object properties
        var json = JsonSerializer.Serialize(okResult.Value);
        var responseData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        Assert.NotNull(responseData);
        Assert.True(responseData["processed"].GetBoolean());
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_GeneratesTasksUsingTemplateService()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var startDate = DateTime.UtcNow;
        
        var generatedTasks = new List<OnboardingTask>
        {
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Setup workstation",
                TaskType = OnboardingTaskType.Equipment,
                DueDate = startDate.AddDays(7),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "HR paperwork",
                TaskType = OnboardingTaskType.Paperwork,
                DueDate = startDate.AddDays(14),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            }
        };
        
        _mockTaskTemplateService
            .Setup(x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()))
            .Returns(generatedTasks);
        
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
        Assert.Equal(2, savedCase.Tasks.Count);
        Assert.Equal("Setup workstation", savedCase.Tasks[0].Description);
        Assert.Equal("HR paperwork", savedCase.Tasks[1].Description);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_GeneratesTasksWithCorrectDueDates()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var testStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var generatedTasks = new List<OnboardingTask>
        {
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Setup workstation",
                TaskType = OnboardingTaskType.Equipment,
                DueDate = testStartDate.AddDays(7),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "HR paperwork",
                TaskType = OnboardingTaskType.Paperwork,
                DueDate = testStartDate.AddDays(14),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "IT account",
                TaskType = OnboardingTaskType.Access,
                DueDate = testStartDate.AddDays(21),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Security badge",
                TaskType = OnboardingTaskType.Access,
                DueDate = testStartDate.AddDays(28),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Benefits enrollment",
                TaskType = OnboardingTaskType.Paperwork,
                DueDate = testStartDate.AddDays(30),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            }
        };
        
        _mockTaskTemplateService
            .Setup(x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()))
            .Returns(generatedTasks);
        
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
        Assert.Equal(5, savedCase.Tasks.Count);
        
        // Verify due dates match template offsets
        Assert.Equal(testStartDate.AddDays(7), savedCase.Tasks[0].DueDate);
        Assert.Equal(testStartDate.AddDays(14), savedCase.Tasks[1].DueDate);
        Assert.Equal(testStartDate.AddDays(21), savedCase.Tasks[2].DueDate);
        Assert.Equal(testStartDate.AddDays(28), savedCase.Tasks[3].DueDate);
        Assert.Equal(testStartDate.AddDays(30), savedCase.Tasks[4].DueDate);
    }

    [Fact]
    public async Task HandleEmployeeCreatedAsync_ValidEvent_GeneratesTasksWithNotStartedStatus()
    {
        // Arrange
        var employeeEvent = CreateTestEmployeeCreatedEvent();
        var startDate = DateTime.UtcNow;
        
        var generatedTasks = new List<OnboardingTask>
        {
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Setup workstation",
                TaskType = OnboardingTaskType.Equipment,
                DueDate = startDate.AddDays(7),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            },
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "HR paperwork",
                TaskType = OnboardingTaskType.Paperwork,
                DueDate = startDate.AddDays(14),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            }
        };
        
        _mockTaskTemplateService
            .Setup(x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()))
            .Returns(generatedTasks);
        
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
        Assert.All(savedCase.Tasks, task => 
            Assert.Equal(OnboardingTaskStatus.NotStarted, task.Status));
    }
}
