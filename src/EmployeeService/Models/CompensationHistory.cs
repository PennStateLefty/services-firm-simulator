using Shared.Models;

namespace EmployeeService.Models;

public class CompensationHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public decimal? PreviousSalary { get; set; }
    public decimal NewSalary { get; set; }
    public CompensationChangeType ChangeType { get; set; }
    public string? ChangeReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
