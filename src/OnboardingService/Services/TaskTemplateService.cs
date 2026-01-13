using Microsoft.Extensions.Options;
using OnboardingService.Models;
using Shared.Models;

namespace OnboardingService.Services;

/// <summary>
/// Service for generating onboarding tasks from configurable templates.
/// </summary>
/// <remarks>
/// Current implementation reads templates from appsettings.json via IOptions.
/// Future enhancements could include:
/// - Loading templates from a database or state store for runtime editability
/// - Supporting multiple template sets based on employee type or department
/// - Allowing custom templates per organization
/// </remarks>
public interface ITaskTemplateService
{
    /// <summary>
    /// Generates a list of onboarding tasks based on configured templates.
    /// </summary>
    /// <param name="startDate">The start date for the onboarding case, used to calculate due dates</param>
    /// <returns>A list of OnboardingTask objects generated from templates</returns>
    List<OnboardingTask> GenerateTasksFromTemplates(DateTime startDate);
}

/// <summary>
/// Default implementation that reads task templates from configuration.
/// </summary>
public class TaskTemplateService : ITaskTemplateService
{
    private readonly IOptions<List<TaskTemplate>> _taskTemplates;
    private readonly ILogger<TaskTemplateService> _logger;

    public TaskTemplateService(
        IOptions<List<TaskTemplate>> taskTemplates,
        ILogger<TaskTemplateService> logger)
    {
        _taskTemplates = taskTemplates ?? throw new ArgumentNullException(nameof(taskTemplates));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<OnboardingTask> GenerateTasksFromTemplates(DateTime startDate)
    {
        _logger.LogInformation("Generating onboarding tasks from {Count} templates", _taskTemplates.Value?.Count ?? 0);

        var tasks = new List<OnboardingTask>();

        if (_taskTemplates.Value == null || _taskTemplates.Value.Count == 0)
        {
            _logger.LogWarning("No task templates configured. Creating empty task list.");
            return tasks;
        }

        foreach (var template in _taskTemplates.Value.OrderBy(t => t.Order))
        {
            var task = new OnboardingTask
            {
                Id = Guid.NewGuid().ToString(),
                Description = template.Description,
                TaskType = template.TaskType,
                DueDate = startDate.AddDays(template.DueDateOffsetDays),
                Status = OnboardingTaskStatus.NotStarted,
                AssignedTo = string.Empty
            };

            tasks.Add(task);
            
            _logger.LogDebug(
                "Created task '{Description}' (Order: {Order}, DueDate: {DueDate})",
                task.Description,
                template.Order,
                task.DueDate);
        }

        _logger.LogInformation("Successfully generated {Count} onboarding tasks", tasks.Count);
        return tasks;
    }
}
