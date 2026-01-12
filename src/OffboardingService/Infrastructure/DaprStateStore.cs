using Dapr.Client;
using System.Text.Json;

namespace OffboardingService.Infrastructure;

public interface IDaprStateStore
{
    Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;
    Task DeleteStateAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryStateAsync<T>(string queryString, CancellationToken cancellationToken = default) where T : class;
}

public class DaprStateStore : IDaprStateStore
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprStateStore> _logger;
    private const string StateStoreName = "statestore";

    public DaprStateStore(DaprClient daprClient, ILogger<DaprStateStore> logger)
    {
        _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Getting state for key: {Key}", key);
            var state = await _daprClient.GetStateAsync<T>(StateStoreName, key, cancellationToken: cancellationToken);
            
            if (state == null)
            {
                _logger.LogDebug("No state found for key: {Key}", key);
            }
            
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting state for key: {Key}", key);
            throw;
        }
    }

    public async Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Saving state for key: {Key}", key);
            await _daprClient.SaveStateAsync(StateStoreName, key, value, cancellationToken: cancellationToken);
            _logger.LogDebug("Successfully saved state for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving state for key: {Key}", key);
            throw;
        }
    }

    public async Task DeleteStateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting state for key: {Key}", key);
            await _daprClient.DeleteStateAsync(StateStoreName, key, cancellationToken: cancellationToken);
            _logger.LogDebug("Successfully deleted state for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting state for key: {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryStateAsync<T>(string queryString, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("Querying state with query: {Query}", queryString);
            
            var query = new
            {
                filter = JsonDocument.Parse(queryString)
            };

            var queryResponse = await _daprClient.QueryStateAsync<T>(
                StateStoreName,
                JsonSerializer.Serialize(query),
                cancellationToken: cancellationToken
            );

            var results = queryResponse.Results.Select(r => r.Data).ToList();
            _logger.LogDebug("Query returned {Count} results", results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying state with query: {Query}", queryString);
            throw;
        }
    }
}
