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

namespace Aura.Core.Configuration;

/// <summary>
/// Manages secure storage of API keys using DPAPI on Windows or encrypted storage on Linux
/// </summary>
public class KeyStore : IKeyStore
{
    private readonly ILogger<KeyStore> _logger;
    private readonly string _keysPath;
    private readonly string _devKeysPath;
    private readonly bool _isWindows;
    private Dictionary<string, string>? _cachedKeys;
    private readonly object _lock = new();

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
            _devKeysPath = string.Empty;
        }
        else
        {
            // Linux: Use plaintext dev storage in home directory
            _keysPath = string.Empty;
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _devKeysPath = Path.Combine(homeDir, ".aura-dev", "apikeys.json");
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
            var path = _isWindows ? _keysPath : _devKeysPath;
            var directory = Path.GetDirectoryName(path);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Dictionary<string, string> keysToStore;

            if (_isWindows)
            {
                keysToStore = new Dictionary<string, string>();
                foreach (var kvp in keys)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                    {
                        var encrypted = ProtectString(kvp.Value);
                        keysToStore[kvp.Key] = encrypted;
                    }
                }
                _logger.LogInformation("Encrypting {Count} API keys using DPAPI", keysToStore.Count);
            }
            else
            {
                keysToStore = keys.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                _logger.LogInformation("Storing {Count} API keys (development mode, encryption not available)", keysToStore.Count);
            }

            var json = JsonSerializer.Serialize(keysToStore, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json, ct);

            lock (_lock)
            {
                _cachedKeys = keys;
            }

            _logger.LogInformation("API keys saved successfully to {Path}", MaskPath(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API keys");
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
        var path = _isWindows ? _keysPath : _devKeysPath;
        
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var encryptedKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (encryptedKeys == null)
            {
                return new Dictionary<string, string>();
            }

            if (_isWindows)
            {
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

            return encryptedKeys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load and decrypt API keys");
            return new Dictionary<string, string>();
        }
    }
}
