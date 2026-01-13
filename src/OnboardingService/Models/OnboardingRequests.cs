using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace OnboardingService.Models;

public class CreateOnboardingRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public string? Notes { get; set; }
}

public class TaskUpdateRequest
{
    [Required]
    public OnboardingTaskStatus Status { get; set; }
}
