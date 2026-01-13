using System.ComponentModel.DataAnnotations;

namespace OnboardingService.Models;

public class CreateOnboardingRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public string? Notes { get; set; }
}
