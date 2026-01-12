using Shared.Models;

namespace OnboardingService.Models;

public class Task
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OnboardingCaseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Assignee { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public OnboardingTaskStatus Status { get; set; } = OnboardingTaskStatus.NotStarted;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
