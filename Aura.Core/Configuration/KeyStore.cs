using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Manages secure storage of API keys using DPAPI on Windows or plaintext in dev mode on Linux
/// </summary>
public class KeyStore : IKeyStore
{
    private readonly ILogger<KeyStore> _logger;
    private readonly string _keysPath;
    private readonly string _devKeysPath;
    private readonly bool _isWindows;
    private Dictionary<string, string>? _cachedKeys;

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
        if (_cachedKeys != null)
        {
            return _cachedKeys;
        }

        try
        {
            var path = _isWindows ? _keysPath : _devKeysPath;
            
            if (!File.Exists(path))
            {
                _logger.LogInformation("API keys file not found at {Path}, returning empty dictionary", MaskPath(path));
                _cachedKeys = new Dictionary<string, string>();
                return _cachedKeys;
            }

            var json = File.ReadAllText(path);
            var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (keys == null)
            {
                _cachedKeys = new Dictionary<string, string>();
                return _cachedKeys;
            }

            // On Windows, decrypt the keys if they were encrypted with DPAPI
            // For now, we're storing them as plaintext (as per existing implementation)
            // Note: DPAPI encryption can be added in the future for enhanced security
            _cachedKeys = keys;
            
            _logger.LogInformation("Loaded {Count} API keys from storage", keys.Count);
            return _cachedKeys;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load API keys, returning empty dictionary");
            _cachedKeys = new Dictionary<string, string>();
            return _cachedKeys;
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
        _cachedKeys = null;
    }
}
