using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OnboardingService.Controllers;
using OnboardingService.Models;
using OnboardingService.Services;
using Shared.Models;
using Task = System.Threading.Tasks.Task;

namespace OnboardingService.Tests;

public class OnboardingControllerTests
{
    private readonly Mock<IOnboardingService> _mockOnboardingService;
    private readonly Mock<ITaskTemplateService> _mockTaskTemplateService;
    private readonly Mock<IEmployeeValidationService> _mockEmployeeValidationService;
    private readonly Mock<ILogger<OnboardingController>> _mockLogger;
    private readonly OnboardingController _controller;

    public OnboardingControllerTests()
    {
        _mockOnboardingService = new Mock<IOnboardingService>();
        _mockTaskTemplateService = new Mock<ITaskTemplateService>();
        _mockEmployeeValidationService = new Mock<IEmployeeValidationService>();
        _mockLogger = new Mock<ILogger<OnboardingController>>();
        
        // Setup default behavior for task template service
        _mockTaskTemplateService
            .Setup(x => x.GenerateTasksFromTemplates(It.IsAny<DateTime>()))
            .Returns(new List<OnboardingTask>());
        
        _controller = new OnboardingController(
            _mockOnboardingService.Object,
            _mockTaskTemplateService.Object,
            _mockEmployeeValidationService.Object,
            _mockLogger.Object);
    }

    private static CreateOnboardingRequest CreateTestRequest(
        string employeeId = "emp-123",
        DateTime? startDate = null,
        string? notes = null)
    {
        return new CreateOnboardingRequest
        {
            EmployeeId = employeeId,
            StartDate = startDate ?? DateTime.UtcNow,
            Notes = notes
        };
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_CreatesOnboardingCase()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var onboardingCase = Assert.IsType<OnboardingCase>(createdResult.Value);
        Assert.Equal(request.EmployeeId, onboardingCase.EmployeeId);
        Assert.Equal(OnboardingTaskStatus.NotStarted, onboardingCase.Status);
        
        _mockOnboardingService.Verify(
            x => x.SaveStateAsync(
                It.Is<OnboardingCase>(oc => oc.EmployeeId == request.EmployeeId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_Returns201WithLocationHeader()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(OnboardingController.GetOnboarding), createdResult.ActionName);
        Assert.NotNull(savedCase);
        var routeValues = createdResult.RouteValues;
        Assert.NotNull(routeValues);
        Assert.Equal(savedCase.Id, routeValues["onboardingCaseId"]);
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_SetsTargetCompletionDateTo30Days()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = CreateTestRequest(startDate: startDate);
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(savedCase);
        Assert.NotNull(savedCase.TargetCompletionDate);
        Assert.Equal(startDate.AddDays(30), savedCase.TargetCompletionDate.Value);
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_GeneratesTasksFromTemplates()
    {
        // Arrange
        var request = CreateTestRequest();
        var generatedTasks = new List<OnboardingTask>
        {
            new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Setup workstation",
                TaskType = OnboardingTaskType.Equipment,
                DueDate = request.StartDate.AddDays(7),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            }
        };
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(savedCase);
        Assert.NotNull(savedCase.Tasks);
        Assert.Single(savedCase.Tasks);
        Assert.Equal("Setup workstation", savedCase.Tasks[0].Description);
        
        _mockTaskTemplateService.Verify(
            x => x.GenerateTasksFromTemplates(request.StartDate),
            Times.Once);
    }

    [Fact]
    public async Task CreateOnboarding_EmployeeNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal(400, errorResponse.StatusCode);
        Assert.NotNull(errorResponse.Errors);
        Assert.Contains("EmployeeId", errorResponse.Errors.Keys);
        
        // Verify SaveStateAsync was not called
        _mockOnboardingService.Verify(
            x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOnboarding_ExistingCase_ReturnsConflict()
    {
        // Arrange
        var request = CreateTestRequest();
        var existingCase = new OnboardingCase
        {
            Id = "existing-case-id",
            EmployeeId = request.EmployeeId,
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.InProgress
        };
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OnboardingCase> { existingCase });

        // Act
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, errorResponse.StatusCode);
        
        // Verify SaveStateAsync was not called
        _mockOnboardingService.Verify(
            x => x.SaveStateAsync(
                It.IsAny<OnboardingCase>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_IncludesNotes()
    {
        // Arrange
        var notes = "Special onboarding requirements";
        var request = CreateTestRequest(notes: notes);
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(savedCase);
        Assert.Equal(notes, savedCase.Notes);
    }

    [Fact]
    public async Task CreateOnboarding_SaveStateThrowsException_Returns500()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateOnboarding_QueryStateThrowsException_Returns500()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        _mockOnboardingService
            .Setup(x => x.QueryStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Query error"));

        // Act
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateOnboarding_ValidRequest_SetsStatusToNotStarted()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(savedCase);
        Assert.Equal(OnboardingTaskStatus.NotStarted, savedCase.Status);
    }

    [Fact]
    public async Task CreateOnboarding_ValidatesEmployeeId_CallsEmployeeService()
    {
        // Arrange
        var request = CreateTestRequest();
        
        _mockEmployeeValidationService
            .Setup(x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
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
        var result = await _controller.CreateOnboarding(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        
        _mockEmployeeValidationService.Verify(
            x => x.ValidateEmployeeExistsAsync(request.EmployeeId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOnboarding_ExistingCase_ReturnsOk()
    {
        // Arrange
        var caseId = "case-123";
        var onboardingCase = new OnboardingCase
        {
            Id = caseId,
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.NotStarted
        };
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboardingCase);

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCase = Assert.IsType<OnboardingCase>(okResult.Value);
        Assert.Equal(caseId, returnedCase.Id);
    }

    [Fact]
    public async Task GetOnboarding_NonExistingCase_ReturnsNotFound()
    {
        // Arrange
        var caseId = "non-existing-case";
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingCase?)null);

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, errorResponse.StatusCode);
    }

    [Fact]
    public async Task GetOnboarding_ServiceThrowsException_Returns500()
    {
        // Arrange
        var caseId = "case-123";
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetOnboarding_WithTasks_IncludesCompletionPercentage()
    {
        // Arrange
        var caseId = "case-123";
        var onboardingCase = new OnboardingCase
        {
            Id = caseId,
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.InProgress,
            Tasks = new List<OnboardingTask>
            {
                new OnboardingTask { Id = "1", Status = OnboardingTaskStatus.Completed },
                new OnboardingTask { Id = "2", Status = OnboardingTaskStatus.Completed },
                new OnboardingTask { Id = "3", Status = OnboardingTaskStatus.InProgress },
                new OnboardingTask { Id = "4", Status = OnboardingTaskStatus.NotStarted }
            }
        };
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboardingCase);

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCase = Assert.IsType<OnboardingCase>(okResult.Value);
        Assert.Equal(50.0, returnedCase.CompletionPercentage); // 2 out of 4 tasks completed
    }

    [Fact]
    public async Task GetOnboarding_WithNoTasks_ReturnsZeroCompletionPercentage()
    {
        // Arrange
        var caseId = "case-123";
        var onboardingCase = new OnboardingCase
        {
            Id = caseId,
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.NotStarted,
            Tasks = new List<OnboardingTask>()
        };
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboardingCase);

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCase = Assert.IsType<OnboardingCase>(okResult.Value);
        Assert.Equal(0.0, returnedCase.CompletionPercentage);
    }

    [Fact]
    public async Task GetOnboarding_WithAllTasksCompleted_Returns100CompletionPercentage()
    {
        // Arrange
        var caseId = "case-123";
        var onboardingCase = new OnboardingCase
        {
            Id = caseId,
            EmployeeId = "emp-123",
            StartDate = DateTime.UtcNow,
            Status = OnboardingTaskStatus.Completed,
            Tasks = new List<OnboardingTask>
            {
                new OnboardingTask { Id = "1", Status = OnboardingTaskStatus.Completed },
                new OnboardingTask { Id = "2", Status = OnboardingTaskStatus.Completed },
                new OnboardingTask { Id = "3", Status = OnboardingTaskStatus.Completed }
            }
        };
        
        _mockOnboardingService
            .Setup(x => x.GetStateAsync(caseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(onboardingCase);

        // Act
        var result = await _controller.GetOnboarding(caseId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCase = Assert.IsType<OnboardingCase>(okResult.Value);
        Assert.Equal(100.0, returnedCase.CompletionPercentage);
    }
}
