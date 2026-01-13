using Microsoft.AspNetCore.Mvc;
using OnboardingService.Models;
using OnboardingService.Services;
using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace OnboardingService.Controllers;

[ApiController]
[Route("v1/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;
    private readonly ITaskTemplateService _taskTemplateService;
    private readonly IEmployeeValidationService _employeeValidationService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IOnboardingService onboardingService,
        ITaskTemplateService taskTemplateService,
        IEmployeeValidationService employeeValidationService,
        ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
        _taskTemplateService = taskTemplateService ?? throw new ArgumentNullException(nameof(taskTemplateService));
        _employeeValidationService = employeeValidationService ?? throw new ArgumentNullException(nameof(employeeValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new onboarding case for an employee
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OnboardingCase>> CreateOnboarding(
        [FromBody] CreateOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating onboarding case for employee: {EmployeeId}", request.EmployeeId);

            // Validate required fields
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                return BadRequest(ErrorResponse.ValidationError(errors, HttpContext?.TraceIdentifier));
            }

            // Validate that the employee exists by calling EmployeeService via Dapr
            var employeeExists = await _employeeValidationService.ValidateEmployeeExistsAsync(
                request.EmployeeId,
                cancellationToken);

            if (!employeeExists)
            {
                _logger.LogWarning("Employee not found: {EmployeeId}", request.EmployeeId);
                return BadRequest(ErrorResponse.ValidationError(
                    new Dictionary<string, string[]>
                    {
                        ["EmployeeId"] = new[] { $"Employee with ID '{request.EmployeeId}' does not exist" }
                    },
                    HttpContext?.TraceIdentifier
                ));
            }

            // Check if an onboarding case already exists for this employee
            // Escape the employee ID to prevent query injection
            var escapedEmployeeId = System.Text.Json.JsonSerializer.Serialize(request.EmployeeId).Trim('"');
            var existingCases = await _onboardingService.QueryStateAsync(
                $"{{\"filter\":{{\"EQ\":{{\"employeeId\":\"{escapedEmployeeId}\"}}}}}}",
                cancellationToken);

            if (existingCases.Any())
            {
                var existingCase = existingCases.First();
                _logger.LogWarning(
                    "Onboarding case already exists for employee {EmployeeId}: {OnboardingCaseId}",
                    request.EmployeeId,
                    existingCase.Id);
                return Conflict(ErrorResponse.Conflict(
                    $"An onboarding case already exists for employee '{request.EmployeeId}'",
                    HttpContext?.TraceIdentifier
                ));
            }

            // Create new onboarding case
            var onboardingCase = new OnboardingCase
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = request.EmployeeId,
                StartDate = request.StartDate,
                TargetCompletionDate = request.StartDate.AddDays(30),
                Status = OnboardingTaskStatus.NotStarted,
                Tasks = _taskTemplateService.GenerateTasksFromTemplates(request.StartDate),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save the onboarding case
            await _onboardingService.SaveStateAsync(onboardingCase, cancellationToken);

            _logger.LogInformation(
                "Successfully created onboarding case {OnboardingCaseId} for employee {EmployeeId}",
                onboardingCase.Id,
                request.EmployeeId);

            // Return 201 Created with Location header
            return CreatedAtAction(
                nameof(GetOnboarding),
                new { onboardingCaseId = onboardingCase.Id },
                onboardingCase
            );
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during onboarding case creation");
            return BadRequest(ErrorResponse.Create(400, "Validation failed", ex.Message, HttpContext?.TraceIdentifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding case for employee: {EmployeeId}", request.EmployeeId);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext?.TraceIdentifier));
        }
    }

    /// <summary>
    /// Gets an onboarding case by ID
    /// </summary>
    [HttpGet("{onboardingCaseId}")]
    public async Task<ActionResult<OnboardingCase>> GetOnboarding(
        string onboardingCaseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting onboarding case: {OnboardingCaseId}", onboardingCaseId);

            var onboardingCase = await _onboardingService.GetStateAsync(onboardingCaseId, cancellationToken);

            if (onboardingCase == null)
            {
                return NotFound(ErrorResponse.NotFound("Onboarding case", HttpContext?.TraceIdentifier));
            }

            return Ok(onboardingCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding case: {OnboardingCaseId}", onboardingCaseId);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext?.TraceIdentifier));
        }
    }

    /// <summary>
    /// Updates an onboarding task status
    /// </summary>
    [HttpPut("tasks/{taskId}")]
    public async Task<ActionResult<OnboardingTask>> UpdateTask(
        string taskId,
        [FromBody] TaskUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating task: {TaskId} to status: {Status}", taskId, request.Status);

            // Validate ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                return BadRequest(ErrorResponse.ValidationError(errors, HttpContext?.TraceIdentifier));
            }

            // Find the onboarding case that contains this task
            // We need to query all cases since we don't have the case ID
            var allCases = await _onboardingService.QueryStateAsync("{}", cancellationToken);
            
            OnboardingCase? targetCase = null;
            OnboardingTask? targetTask = null;

            foreach (var onboardingCase in allCases)
            {
                var task = onboardingCase.Tasks?.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    targetCase = onboardingCase;
                    targetTask = task;
                    break;
                }
            }

            if (targetTask == null || targetCase == null)
            {
                _logger.LogWarning("Task not found: {TaskId}", taskId);
                return NotFound(ErrorResponse.NotFound("Task", HttpContext?.TraceIdentifier));
            }

            // Validate status transitions
            var currentStatus = targetTask.Status;
            var newStatus = request.Status;

            // Status transition validation
            if (!IsValidStatusTransition(currentStatus, newStatus))
            {
                _logger.LogWarning(
                    "Invalid status transition for task {TaskId}: {CurrentStatus} -> {NewStatus}",
                    taskId,
                    currentStatus,
                    newStatus);
                return BadRequest(ErrorResponse.ValidationError(
                    new Dictionary<string, string[]>
                    {
                        ["Status"] = new[] { $"Invalid status transition from {currentStatus} to {newStatus}" }
                    },
                    HttpContext?.TraceIdentifier
                ));
            }

            // Update task status
            targetTask.Status = newStatus;

            // Set completed date if status is Completed
            if (newStatus == OnboardingTaskStatus.Completed && targetTask.CompletedDate == null)
            {
                targetTask.CompletedDate = DateTime.UtcNow;
            }

            // Clear completed date if status is changed from Completed
            if (newStatus != OnboardingTaskStatus.Completed && targetTask.CompletedDate != null)
            {
                targetTask.CompletedDate = null;
            }

            // Save the updated onboarding case
            await _onboardingService.SaveStateAsync(targetCase, cancellationToken);

            _logger.LogInformation(
                "Successfully updated task {TaskId} to status {Status}",
                taskId,
                newStatus);

            return Ok(targetTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task: {TaskId}", taskId);
            return StatusCode(500, ErrorResponse.InternalServerError(ex.Message, HttpContext?.TraceIdentifier));
        }
    }

    private static bool IsValidStatusTransition(OnboardingTaskStatus current, OnboardingTaskStatus next)
    {
        // Allow staying in the same status
        if (current == next)
        {
            return true;
        }

        // Valid transitions:
        // NotStarted -> InProgress, Completed, Blocked
        // InProgress -> Completed, Blocked, NotStarted (rollback)
        // Completed -> InProgress (reopen)
        // Blocked -> NotStarted, InProgress
        return (current, next) switch
        {
            (OnboardingTaskStatus.NotStarted, OnboardingTaskStatus.InProgress) => true,
            (OnboardingTaskStatus.NotStarted, OnboardingTaskStatus.Completed) => true,
            (OnboardingTaskStatus.NotStarted, OnboardingTaskStatus.Blocked) => true,
            (OnboardingTaskStatus.InProgress, OnboardingTaskStatus.Completed) => true,
            (OnboardingTaskStatus.InProgress, OnboardingTaskStatus.Blocked) => true,
            (OnboardingTaskStatus.InProgress, OnboardingTaskStatus.NotStarted) => true,
            (OnboardingTaskStatus.Completed, OnboardingTaskStatus.InProgress) => true,
            (OnboardingTaskStatus.Blocked, OnboardingTaskStatus.NotStarted) => true,
            (OnboardingTaskStatus.Blocked, OnboardingTaskStatus.InProgress) => true,
            _ => false
        };
    }
}
