namespace OnboardingService.Services;

/// <summary>
/// Service for validating employees exist in the EmployeeService
/// </summary>
public interface IEmployeeValidationService
{
    Task<bool> ValidateEmployeeExistsAsync(string employeeId, CancellationToken cancellationToken = default);
}
