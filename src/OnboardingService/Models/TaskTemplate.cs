namespace OnboardingService.Models;

public class TaskTemplate
{
    public string Description { get; set; } = string.Empty;
    public OnboardingTaskType TaskType { get; set; }
    public int Order { get; set; }
    public int DueDateOffsetDays { get; set; }
}
