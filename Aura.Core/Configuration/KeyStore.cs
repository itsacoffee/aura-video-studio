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
/// Manages secure storage of API keys using DPAPI on Windows or AES-256 encryption on Linux/macOS
/// </summary>
public class KeyStore : IKeyStore
{
    private readonly ILogger<KeyStore> _logger;
    private readonly string _keysPath;
    private readonly string _legacyDevKeysPath;
    private readonly bool _isWindows;
    private Dictionary<string, string>? _cachedKeys;
    private readonly object _lock = new();
    private readonly SecureStorageService? _secureStorage;
    private bool _migrationCompleted;

    public KeyStore(ILogger<KeyStore> logger)
    {
        _logger = logger;
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        if (_isWindows)
        {
            // Windows: Use DPAPI-encrypted storage in LocalApplicationData
            _keysPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aura",
                "apikeys.json");
            _legacyDevKeysPath = string.Empty;
            _secureStorage = null;
        }
        else
        {
            // Linux/macOS: Use AES-256 encrypted storage via SecureStorageService
            _keysPath = string.Empty;
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Legacy plaintext locations to check for migration
            _legacyDevKeysPath = Path.Combine(homeDir, ".aura-dev", "apikeys.json");
            
            // Initialize SecureStorageService for encrypted storage
            var secureLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SecureStorageService>.Instance;
            _secureStorage = new SecureStorageService(secureLogger);
            _migrationCompleted = false;
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
                _cachedKeys = LoadAndDecryptKeys();
                _logger.LogInformation("Loaded {Count} API keys from storage", _cachedKeys.Count);
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
            return "..." + path.Substring(path.Length - 30);
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
    /// Save API keys securely to storage
    /// </summary>
    public async Task SaveKeysAsync(Dictionary<string, string> keys, CancellationToken ct = default)
    {
        try
        {
            if (_isWindows)
            {
                await SaveKeysWindowsAsync(keys, ct);
            }
            else
            {
                await SaveKeysNonWindowsAsync(keys, ct);
            }

            lock (_lock)
            {
                _cachedKeys = keys;
            }

            _logger.LogInformation("API keys saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API keys");
            throw;
        }
    }

    private async Task SaveKeysWindowsAsync(Dictionary<string, string> keys, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_keysPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var keysToStore = new Dictionary<string, string>();
        foreach (var kvp in keys)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                var encrypted = ProtectString(kvp.Value);
                keysToStore[kvp.Key] = encrypted;
            }
        }
        
        _logger.LogInformation("Encrypting {Count} API keys using DPAPI", keysToStore.Count);

        var json = JsonSerializer.Serialize(keysToStore, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_keysPath, json, ct);
    }

    private async Task SaveKeysNonWindowsAsync(Dictionary<string, string> keys, CancellationToken ct)
    {
        if (_secureStorage == null)
        {
            throw new InvalidOperationException("SecureStorageService not initialized for non-Windows platform");
        }

        var saveCount = 0;
        foreach (var kvp in keys)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
            {
                await _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value);
                saveCount++;
            }
        }

        _logger.LogInformation("Saved {Count} API keys using AES-256 encryption", saveCount);
    }

    /// <summary>
    /// Set a single API key
    /// </summary>
    public async Task SetKeyAsync(string providerName, string apiKey, CancellationToken ct = default)
    {
        var allKeys = GetAllKeys();
        allKeys[providerName] = apiKey;
        await SaveKeysAsync(allKeys, ct);
    }

    /// <summary>
    /// Delete an API key
    /// </summary>
    public async Task DeleteKeyAsync(string providerName, CancellationToken ct = default)
    {
        var allKeys = GetAllKeys();
        if (allKeys.Remove(providerName))
        {
            await SaveKeysAsync(allKeys, ct);
            _logger.LogInformation("Deleted API key for provider {Provider}", providerName);
        }
    }

    /// <summary>
    /// Encrypt a string using DPAPI (Windows only)
    /// </summary>
    private string ProtectString(string plainText)
    {
        if (!_isWindows)
        {
            return plainText;
        }

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to encrypt data, storing as plaintext");
            return plainText;
        }
    }

    /// <summary>
    /// Decrypt a string using DPAPI (Windows only)
    /// </summary>
    private string UnprotectString(string encryptedText)
    {
        if (!_isWindows)
        {
            return encryptedText;
        }

        try
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return encryptedText;
        }
    }

    /// <summary>
    /// Load and decrypt keys from storage
    /// </summary>
    private Dictionary<string, string> LoadAndDecryptKeys()
    {
        if (_isWindows)
        {
            return LoadAndDecryptKeysWindows();
        }
        else
        {
            return LoadAndDecryptKeysNonWindows();
        }
    }

    private Dictionary<string, string> LoadAndDecryptKeysWindows()
    {
        if (!File.Exists(_keysPath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = File.ReadAllText(_keysPath);
            var encryptedKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (encryptedKeys == null)
            {
                return new Dictionary<string, string>();
            }

            var decryptedKeys = new Dictionary<string, string>();
            foreach (var kvp in encryptedKeys)
            {
                try
                {
                    decryptedKeys[kvp.Key] = UnprotectString(kvp.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt key for provider {Provider}, skipping", kvp.Key);
                }
            }
            return decryptedKeys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load and decrypt API keys from Windows storage");
            return new Dictionary<string, string>();
        }
    }

    private Dictionary<string, string> LoadAndDecryptKeysNonWindows()
    {
        if (_secureStorage == null)
        {
            _logger.LogError("SecureStorageService not initialized for non-Windows platform");
            return new Dictionary<string, string>();
        }

        // Perform one-time migration from legacy plaintext if needed
        if (!_migrationCompleted)
        {
            MigrateLegacyPlaintextKeys();
            _migrationCompleted = true;
        }

        try
        {
            // Load keys from encrypted storage using SecureStorageService
            var providers = _secureStorage.GetConfiguredProvidersAsync().GetAwaiter().GetResult();
            var keys = new Dictionary<string, string>();
            
            foreach (var provider in providers)
            {
                var key = _secureStorage.GetApiKeyAsync(provider).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(key))
                {
                    keys[provider] = key;
                }
            }
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load API keys from encrypted storage");
            return new Dictionary<string, string>();
        }
    }

    private void MigrateLegacyPlaintextKeys()
    {
        if (_secureStorage == null || string.IsNullOrEmpty(_legacyDevKeysPath))
        {
            return;
        }

        // Check for legacy plaintext file
        if (!File.Exists(_legacyDevKeysPath))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Detected legacy plaintext API keys file, beginning migration");
            
            // Read legacy plaintext keys
            var json = File.ReadAllText(_legacyDevKeysPath);
            var plaintextKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (plaintextKeys == null || plaintextKeys.Count == 0)
            {
                _logger.LogInformation("Legacy file empty, skipping migration");
                return;
            }

            // Migrate each key to encrypted storage
            int migratedCount = 0;
            foreach (var kvp in plaintextKeys)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    try
                    {
                        _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value).GetAwaiter().GetResult();
                        migratedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to migrate key for provider {Provider}", kvp.Key);
                    }
                }
            }

            _logger.LogInformation("Successfully migrated {Count} API keys from plaintext to encrypted storage", migratedCount);

            // Securely delete the old plaintext file
            SecureDeleteFile(_legacyDevKeysPath);
            
            _logger.LogInformation("Legacy plaintext file securely deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during legacy key migration");
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
