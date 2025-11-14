using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Services;

namespace Aura.Core.Configuration;

/// <summary>
/// Manages secure storage of API keys using SecureStorageService (DPAPI on Windows, AES-256 on Linux/macOS)
/// Delegates all operations to SecureStorageService for unified, encrypted storage across all platforms
/// </summary>
public class KeyStore : IKeyStore
{
    private readonly ILogger<KeyStore> _logger;
    private readonly string _legacyPlaintextPath;
    private readonly string _legacyDevKeysPath;
    private readonly bool _isWindows;
    private Dictionary<string, string>? _cachedKeys;
    private readonly object _lock = new();
    private readonly SecureStorageService _secureStorage;
    private bool _migrationCompleted;

    public KeyStore(ILogger<KeyStore> logger)
    {
        _logger = logger;
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        // Initialize SecureStorageService for all platforms
        // Note: Using NullLogger for SecureStorageService as KeyStore handles all user-facing logging
        // This avoids logger type mismatch while ensuring important security events are still logged by KeyStore
        var secureLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SecureStorageService>.Instance;
        _secureStorage = new SecureStorageService(secureLogger);
        _migrationCompleted = false;
        
        // Define legacy plaintext paths for migration
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        if (_isWindows)
        {
            // Windows legacy plaintext path: %LOCALAPPDATA%\Aura\apikeys.json
            _legacyPlaintextPath = Path.Combine(localAppData, "Aura", "apikeys.json");
            _legacyDevKeysPath = string.Empty;
        }
        else
        {
            // Linux/macOS legacy paths
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _legacyPlaintextPath = string.Empty;
            _legacyDevKeysPath = Path.Combine(homeDir, ".aura-dev", "apikeys.json");
        }
    }

    public string? GetKey(string providerName)
    {
        var keys = GetAllKeys();
        return keys.TryGetValue(providerName, out var key) ? key : null;
    }

    public Dictionary<string, string> GetAllKeys()
    {
        lock (_lock)
        {
            if (_cachedKeys != null)
            {
                return new Dictionary<string, string>(_cachedKeys);
            }

            try
            {
                // Perform one-time migration from legacy plaintext if needed
                if (!_migrationCompleted)
                {
                    MigrateLegacyPlaintextKeys();
                    _migrationCompleted = true;
                }

                _cachedKeys = LoadAndDecryptKeys();
                _logger.LogInformation("Loaded {Count} API keys from secure storage", _cachedKeys.Count);
                return new Dictionary<string, string>(_cachedKeys);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load API keys, returning empty dictionary");
                _cachedKeys = new Dictionary<string, string>();
                return _cachedKeys;
            }
        }
    }

    public bool IsOfflineOnly()
    {
        try
        {
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aura",
                "settings.json");

            if (!File.Exists(settingsPath))
            {
                return false;
            }

            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            if (settings != null && 
                settings.TryGetValue("offlineOnly", out var offlineValue) &&
                offlineValue.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check offline mode setting");
            return false;
        }
    }

    /// <summary>
    /// Mask sensitive path information in logs
    /// </summary>
    private string MaskPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "[empty]";
        }

        // Only show last 30 characters
        if (path.Length > 30)
        {
            return string.Concat("...", path.AsSpan(path.Length - 30));
        }

        return path;
    }

    /// <summary>
    /// Reload keys from disk (useful after changes)
    /// </summary>
    public void Reload()
    {
        lock (_lock)
        {
            _cachedKeys = null;
        }
    }

    /// <summary>
    /// Save API keys securely to storage via SecureStorageService
    /// </summary>
    public async Task SaveKeysAsync(Dictionary<string, string> keys, CancellationToken ct = default)
    {
        try
        {
            var saveCount = 0;
            foreach (var kvp in keys)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    await _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value).ConfigureAwait(false);
                    saveCount++;
                }
            }

            lock (_lock)
            {
                _cachedKeys = new Dictionary<string, string>(keys);
            }

            _logger.LogInformation("Saved {Count} API keys to secure encrypted storage", saveCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API keys to secure storage");
            throw;
        }
    }

    /// <summary>
    /// Set a single API key
    /// </summary>
    public async Task SetKeyAsync(string providerName, string apiKey, CancellationToken ct = default)
    {
        var allKeys = GetAllKeys();
        allKeys[providerName] = apiKey;
        await SaveKeysAsync(allKeys, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete an API key
    /// </summary>
    public async Task DeleteKeyAsync(string providerName, CancellationToken ct = default)
    {
        var allKeys = GetAllKeys();
        if (allKeys.Remove(providerName))
        {
            await SaveKeysAsync(allKeys, ct).ConfigureAwait(false);
            _logger.LogInformation("Deleted API key for provider {Provider}", providerName);
        }
    }



    /// <summary>
    /// Load and decrypt keys from secure storage via SecureStorageService
    /// </summary>
    private Dictionary<string, string> LoadAndDecryptKeys()
    {
        try
        {
            // Load keys from encrypted storage using SecureStorageService
            // Use ConfigureAwait(false) to avoid potential deadlocks in library code
            var providers = _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var keys = new Dictionary<string, string>();
            
            foreach (var provider in providers)
            {
                var key = _secureStorage.GetApiKeyAsync(provider).ConfigureAwait(false).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(key))
                {
                    keys[provider] = key;
                }
            }
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load API keys from secure encrypted storage");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Migrate legacy plaintext API keys to encrypted storage
    /// Supports both Windows (%LOCALAPPDATA%\Aura\apikeys.json) and Linux/macOS ($HOME/.aura-dev/apikeys.json)
    /// </summary>
    private void MigrateLegacyPlaintextKeys()
    {
        var legacyFilesToCheck = new List<string>();
        
        // Add all applicable legacy paths (check both if present, not exclusive)
        if (_isWindows && !string.IsNullOrEmpty(_legacyPlaintextPath))
        {
            legacyFilesToCheck.Add(_legacyPlaintextPath);
        }
        
        if (!string.IsNullOrEmpty(_legacyDevKeysPath))
        {
            legacyFilesToCheck.Add(_legacyDevKeysPath);
        }

        foreach (var legacyPath in legacyFilesToCheck)
        {
            if (!File.Exists(legacyPath))
            {
                continue;
            }

            try
            {
                _logger.LogInformation("Detected legacy plaintext API keys file at {Path}, beginning migration", MaskPath(legacyPath));
                
                // Read legacy plaintext keys
                var json = File.ReadAllText(legacyPath);
                var plaintextKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (plaintextKeys == null || plaintextKeys.Count == 0)
                {
                    _logger.LogInformation("Legacy file empty, skipping migration");
                    
                    // Still delete the empty legacy file
                    SecureDeleteFile(legacyPath);
                    continue;
                }

                // Migrate each key to encrypted storage
                int migratedCount = 0;
                foreach (var kvp in plaintextKeys)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        try
                        {
                            // Use ConfigureAwait(false) to avoid potential deadlocks in library code
                            _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value).ConfigureAwait(false).GetAwaiter().GetResult();
                            migratedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to migrate key for provider {Provider}", kvp.Key);
                        }
                    }
                }

                // Log audit trail for security compliance
                _logger.LogInformation(
                    "AUDIT: Successfully migrated {Count} API keys from legacy plaintext storage to encrypted storage. " +
                    "Platform: {Platform}, LegacyPath: {Path}",
                    migratedCount,
                    _isWindows ? "Windows" : "Linux/macOS",
                    MaskPath(legacyPath));

                // Securely delete the old plaintext file
                SecureDeleteFile(legacyPath);
                
                _logger.LogInformation("Legacy plaintext file securely deleted: {Path}", MaskPath(legacyPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during legacy key migration from {Path}", MaskPath(legacyPath));
            }
        }
    }

    private void SecureDeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            // Overwrite file contents before deletion for better security
            // Use chunked overwriting to handle large files safely without loading entire file into memory
            var fileInfo = new FileInfo(filePath);
            var fileLength = fileInfo.Length;
            
            if (fileLength > 0)
            {
                // 64KB chunk size balances memory usage and I/O performance
                // Large enough to minimize system calls, small enough to avoid memory pressure
                const int chunkSize = 64 * 1024;
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write);
                using var rng = RandomNumberGenerator.Create();
                
                long bytesRemaining = fileLength;
                while (bytesRemaining > 0)
                {
                    var currentChunkSize = (int)Math.Min(chunkSize, bytesRemaining);
                    var randomData = new byte[currentChunkSize];
                    rng.GetBytes(randomData);
                    fs.Write(randomData, 0, currentChunkSize);
                    bytesRemaining -= currentChunkSize;
                }
                
                fs.Flush();
            }

            // Delete the file
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to securely delete file {Path}, attempting normal delete", MaskPath(filePath));
            
            try
            {
                File.Delete(filePath);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx, "Failed to delete file {Path}", MaskPath(filePath));
            }
        }
    }
}
