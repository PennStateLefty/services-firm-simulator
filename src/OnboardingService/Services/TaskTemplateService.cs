using Microsoft.Extensions.Options;
using OnboardingService.Models;
using Shared.Models;

namespace OnboardingService.Services;

public interface ITaskTemplateService
{
    List<OnboardingTask> GenerateTasksFromTemplates(DateTime startDate);
}

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
