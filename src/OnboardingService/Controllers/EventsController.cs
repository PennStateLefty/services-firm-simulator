using Microsoft.AspNetCore.Mvc;
using Dapr;
using Dapr.Client;
using OnboardingService.Services;
using OnboardingService.Models;
using Shared.Models;

namespace OnboardingService.Controllers;

[ApiController]
[Route("v1/events")]
public class EventsController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<EventsController> _logger;
    private const string PubSubName = "pubsub";
    private const string EmployeeEventsTopic = "employee-events";

    public EventsController(
        IOnboardingService onboardingService,
        ILogger<EventsController> logger)
    {
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles EmployeeCreated events from the employee-events topic
    /// </summary>
    [Topic(PubSubName, EmployeeEventsTopic)]
    [HttpPost("employee-created")]
    public async Task<ActionResult> HandleEmployeeCreatedAsync(
        [FromBody] EmployeeCreatedEvent employeeCreatedEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            if (employeeCreatedEvent == null)
            {
                _logger.LogWarning("Received null EmployeeCreatedEvent");
                return BadRequest("Event payload is null");
            }

            _logger.LogInformation(
                "Received EmployeeCreated event for employee {EmployeeId} with email {Email}",
                employeeCreatedEvent.EmployeeId,
                employeeCreatedEvent.Email);

            // Check if an onboarding case already exists for this employee (idempotency)
            var existingCases = await _onboardingService.QueryStateAsync(
                $"{{\"filter\":{{\"EQ\":{{\"employeeId\":\"{employeeCreatedEvent.EmployeeId}\"}}}}}}",
                cancellationToken);

            if (existingCases.Any())
            {
                _logger.LogInformation(
                    "Onboarding case already exists for employee {EmployeeId}. Skipping creation (idempotent processing).",
                    employeeCreatedEvent.EmployeeId);
                return Ok(new { message = "Onboarding case already exists", processed = false });
            }

            // Create new onboarding case
            var onboardingCase = new OnboardingCase
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employeeCreatedEvent.EmployeeId,
                StartDate = DateTime.UtcNow,
                TargetCompletionDate = DateTime.UtcNow.AddDays(30),
                Status = OnboardingTaskStatus.NotStarted,
                Tasks = new List<OnboardingTask>(),
                Notes = $"Automatically created from EmployeeCreated event for {employeeCreatedEvent.Email}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _onboardingService.SaveStateAsync(onboardingCase, cancellationToken);

            _logger.LogInformation(
                "Successfully created onboarding case {OnboardingCaseId} for employee {EmployeeId}",
                onboardingCase.Id,
                employeeCreatedEvent.EmployeeId);

            return Ok(new 
            { 
                message = "Onboarding case created successfully", 
                onboardingCaseId = onboardingCase.Id,
                processed = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing EmployeeCreated event for employee {EmployeeId}",
                employeeCreatedEvent?.EmployeeId ?? "unknown");
            
            // Return 500 to trigger Dapr retry
            return StatusCode(500, new { error = "Failed to process event", message = ex.Message });
        }
    }
}
