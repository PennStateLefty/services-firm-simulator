using Shared.Models;
using System.Text.Json.Serialization;

namespace OnboardingService.Models;

public class OnboardingCase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public OnboardingTaskStatus Status { get; set; }
    public List<OnboardingTask> Tasks { get; set; } = new();
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonInclude]
    public double CompletionPercentage
    {
        get
        {
            if (Tasks == null || Tasks.Count == 0)
            {
                return 0.0;
            }
            
            var completedTasks = Tasks.Count(t => t.Status == OnboardingTaskStatus.Completed);
            return Math.Round((double)completedTasks / Tasks.Count * 100, 2);
        }
    }
}

public class OnboardingTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public OnboardingTaskType TaskType { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CompletedBy { get; set; }
    public OnboardingTaskStatus Status { get; set; }
}

public enum OnboardingTaskType
{
    Paperwork,
    Training,
    Equipment,
    Access,
    Other
}
