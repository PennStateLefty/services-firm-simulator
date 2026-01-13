using OnboardingService.Infrastructure;
using OnboardingService.Models;

namespace OnboardingService.Services;

public interface IOnboardingService
{
    System.Threading.Tasks.Task<OnboardingCase?> GetStateAsync(string id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<OnboardingCase> SaveStateAsync(OnboardingCase onboardingCase, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task DeleteStateAsync(string id, CancellationToken cancellationToken = default);
    System.Threading.Tasks.Task<IEnumerable<OnboardingCase>> QueryStateAsync(string filter, CancellationToken cancellationToken = default);
}

public class OnboardingServiceImpl : IOnboardingService
{
    private readonly IDaprStateStore _stateStore;
    private readonly ILogger<OnboardingServiceImpl> _logger;
    private const string OnboardingCasePrefix = "onboarding-case:";

    public OnboardingServiceImpl(
        IDaprStateStore stateStore,
        ILogger<OnboardingServiceImpl> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async System.Threading.Tasks.Task<OnboardingCase?> GetStateAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting onboarding case by ID: {Id}", id);
        
        try
        {
            var onboardingCase = await _stateStore.GetStateAsync<OnboardingCase>(
                $"{OnboardingCasePrefix}{id}", 
                cancellationToken);
            
            if (onboardingCase == null)
            {
                _logger.LogWarning("Onboarding case not found: {Id}", id);
            }
            
            return onboardingCase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding case: {Id}", id);
            throw;
        }
    }

    public async System.Threading.Tasks.Task<OnboardingCase> SaveStateAsync(OnboardingCase onboardingCase, CancellationToken cancellationToken = default)
    {
        if (onboardingCase == null)
        {
            throw new ArgumentNullException(nameof(onboardingCase));
        }

        _logger.LogInformation("Saving onboarding case: {Id}", onboardingCase.Id);
        
        try
        {
            // Update timestamp
            onboardingCase.UpdatedAt = DateTime.UtcNow;
            
            // If this is a new case, set CreatedAt
            var existingCase = await _stateStore.GetStateAsync<OnboardingCase>(
                $"{OnboardingCasePrefix}{onboardingCase.Id}", 
                cancellationToken);
            
            if (existingCase == null)
            {
                onboardingCase.CreatedAt = DateTime.UtcNow;
                _logger.LogInformation("Creating new onboarding case: {Id}", onboardingCase.Id);
            }
            else
            {
                // Preserve original CreatedAt
                onboardingCase.CreatedAt = existingCase.CreatedAt;
                _logger.LogInformation("Updating existing onboarding case: {Id}", onboardingCase.Id);
            }

            await _stateStore.SaveStateAsync(
                $"{OnboardingCasePrefix}{onboardingCase.Id}", 
                onboardingCase, 
                cancellationToken);
            
            _logger.LogInformation("Successfully saved onboarding case: {Id}", onboardingCase.Id);
            return onboardingCase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving onboarding case: {Id}", onboardingCase.Id);
            throw;
        }
    }

    public async System.Threading.Tasks.Task DeleteStateAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting onboarding case: {Id}", id);
        
        try
        {
            // Check if the case exists before deleting
            var existingCase = await _stateStore.GetStateAsync<OnboardingCase>(
                $"{OnboardingCasePrefix}{id}", 
                cancellationToken);
            
            if (existingCase == null)
            {
                _logger.LogWarning("Onboarding case not found for deletion: {Id}", id);
                throw new KeyNotFoundException($"Onboarding case with ID {id} not found");
            }

            await _stateStore.DeleteStateAsync(
                $"{OnboardingCasePrefix}{id}", 
                cancellationToken);
            
            _logger.LogInformation("Successfully deleted onboarding case: {Id}", id);
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting onboarding case: {Id}", id);
            throw;
        }
    }

    public async System.Threading.Tasks.Task<IEnumerable<OnboardingCase>> QueryStateAsync(string filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying onboarding cases with filter: {Filter}", filter);
        
        try
        {
            var onboardingCases = await _stateStore.QueryStateAsync<OnboardingCase>(
                filter, 
                cancellationToken);
            
            _logger.LogInformation("Query returned {Count} onboarding cases", onboardingCases.Count());
            return onboardingCases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying onboarding cases with filter: {Filter}", filter);
            throw;
        }
    }
}
