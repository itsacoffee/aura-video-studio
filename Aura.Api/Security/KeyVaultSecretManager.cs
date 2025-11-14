using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Security;

/// <summary>
/// Manages secrets from Azure Key Vault with caching and automatic refresh
/// </summary>
public interface IKeyVaultSecretManager
{
    Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default);
    Task RefreshSecretsAsync(CancellationToken cancellationToken = default);
}

public class KeyVaultSecretManager : IKeyVaultSecretManager
{
    private readonly SecretClient? _secretClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyVaultSecretManager> _logger;
    private readonly KeyVaultOptions _options;
    private readonly bool _isEnabled;

    public KeyVaultSecretManager(
        IOptions<KeyVaultOptions> options,
        IMemoryCache cache,
        ILogger<KeyVaultSecretManager> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _isEnabled = _options.Enabled && !string.IsNullOrWhiteSpace(_options.VaultUri);

        if (_isEnabled)
        {
            try
            {
                var vaultUri = new Uri(_options.VaultUri!);
                
                // Use Managed Identity or Service Principal
                if (_options.UseManagedIdentity)
                {
                    _logger.LogInformation("Initializing Key Vault client with Managed Identity for {VaultUri}", vaultUri);
                    _secretClient = new SecretClient(vaultUri, new DefaultAzureCredential());
                }
                else if (!string.IsNullOrWhiteSpace(_options.TenantId) &&
                         !string.IsNullOrWhiteSpace(_options.ClientId) &&
                         !string.IsNullOrWhiteSpace(_options.ClientSecret))
                {
                    _logger.LogInformation("Initializing Key Vault client with Service Principal for {VaultUri}", vaultUri);
                    var credential = new ClientSecretCredential(
                        _options.TenantId,
                        _options.ClientId,
                        _options.ClientSecret);
                    _secretClient = new SecretClient(vaultUri, credential);
                }
                else
                {
                    _logger.LogWarning("Key Vault is enabled but authentication credentials are not properly configured");
                    _isEnabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Key Vault client. Key Vault integration will be disabled.");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogInformation("Key Vault integration is disabled");
        }
    }

    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _secretClient == null)
        {
            return null;
        }

        var cacheKey = $"keyvault_secret_{secretName}";

        // Try to get from cache first
        if (_cache.TryGetValue<string>(cacheKey, out var cachedSecret))
        {
            return cachedSecret;
        }

        try
        {
            _logger.LogDebug("Fetching secret {SecretName} from Key Vault", secretName);
            
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
            var secretValue = response.Value.Value;

            // Cache the secret
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, secretValue, cacheOptions);

            _logger.LogInformation("Successfully retrieved and cached secret {SecretName}", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Key Vault", secretName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {SecretName} from Key Vault", secretName);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
    {
        var secrets = new Dictionary<string, string>();

        if (!_isEnabled || _secretClient == null)
        {
            return secrets;
        }

        foreach (var mapping in _options.SecretMappings)
        {
            var secretValue = await GetSecretAsync(mapping.Value, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(secretValue))
            {
                secrets[mapping.Key] = secretValue;
            }
        }

        return secrets;
    }

    public async Task RefreshSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _secretClient == null)
        {
            return;
        }

        _logger.LogInformation("Refreshing all secrets from Key Vault");

        foreach (var mapping in _options.SecretMappings)
        {
            var cacheKey = $"keyvault_secret_{mapping.Value}";
            _cache.Remove(cacheKey);
            
            // Fetch fresh value
            await GetSecretAsync(mapping.Value, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Secret refresh completed");
    }
}

/// <summary>
/// Background service that periodically refreshes secrets from Key Vault
/// </summary>
public class KeyVaultRefreshBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IKeyVaultSecretManager _secretManager;
    private readonly ILogger<KeyVaultRefreshBackgroundService> _logger;
    private readonly KeyVaultOptions _options;
    private readonly TimeSpan _refreshInterval;

    public KeyVaultRefreshBackgroundService(
        IKeyVaultSecretManager secretManager,
        IOptions<KeyVaultOptions> options,
        ILogger<KeyVaultRefreshBackgroundService> logger)
    {
        _secretManager = secretManager;
        _logger = logger;
        _options = options.Value;
        _refreshInterval = TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2); // Refresh at half the cache expiration
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.AutoReload)
        {
            _logger.LogInformation("Key Vault auto-refresh is disabled");
            return;
        }

        _logger.LogInformation("Key Vault auto-refresh service started. Refresh interval: {Interval}", _refreshInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_refreshInterval, stoppingToken).ConfigureAwait(false);
                await _secretManager.RefreshSecretsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during secret refresh");
            }
        }

        _logger.LogInformation("Key Vault auto-refresh service stopped");
    }
}
