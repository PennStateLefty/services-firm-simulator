using Dapr.Client;
using System.Text.Json;

namespace EmployeeService.Infrastructure;

public interface IDaprStateStore
{
    Task<T?> GetStateAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SaveStateAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;
    Task DeleteStateAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> QueryStateAsync<T>(string queryString, CancellationToken cancellationToken = default) where T : class;
    Task<int> IncrementCounterAsync(string key, CancellationToken cancellationToken = default);
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

    public async Task<int> IncrementCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 10;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogDebug("Incrementing counter for key: {Key} (attempt {Attempt})", key, retryCount + 1);
                
                // Get current value with ETag
                var (counter, etag) = await _daprClient.GetStateAndETagAsync<int?>(StateStoreName, key, cancellationToken: cancellationToken);
                
                var newValue = (counter ?? 0) + 1;
                
                // Try to save with ETag to ensure atomicity
                var success = await _daprClient.TrySaveStateAsync(
                    StateStoreName,
                    key,
                    newValue,
                    etag,
                    cancellationToken: cancellationToken
                );

                if (success)
                {
                    _logger.LogDebug("Successfully incremented counter for key: {Key} to value: {Value}", key, newValue);
                    return newValue;
                }

                // If save failed due to ETag mismatch, retry
                retryCount++;
                _logger.LogDebug("Counter increment failed due to concurrency, retrying... (attempt {Attempt})", retryCount);
                
                // Small delay before retry to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing counter for key: {Key}", key);
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to increment counter for key '{key}' after {maxRetries} attempts due to high contention.");
    }
}
