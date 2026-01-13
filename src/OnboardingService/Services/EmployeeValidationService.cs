using Dapr.Client;

namespace OnboardingService.Services;

/// <summary>
/// Service for validating employees exist in the EmployeeService via Dapr
/// </summary>
public class EmployeeValidationService : IEmployeeValidationService
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<EmployeeValidationService> _logger;
    private const string EmployeeServiceAppId = "employeeservice";

    public EmployeeValidationService(
        DaprClient daprClient,
        ILogger<EmployeeValidationService> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidateEmployeeExistsAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating employee exists: {EmployeeId}", employeeId);
            
            var response = await _daprClient.InvokeMethodAsync<object>(
                HttpMethod.Get,
                EmployeeServiceAppId,
                $"v1/employees/{employeeId}",
                cancellationToken);

            return response != null;
        }
        catch (Dapr.DaprException ex) when (ex.Message.Contains("404") || ex.InnerException?.Message?.Contains("404") == true)
        {
            _logger.LogWarning("Employee not found: {EmployeeId}", employeeId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating employee: {EmployeeId}", employeeId);
            throw;
        }
    }
}
