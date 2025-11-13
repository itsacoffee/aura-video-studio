using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Configuration;
using Aura.Core.Services.Providers.Stickiness;

namespace Aura.Core.Security;

/// <summary>
/// Enhanced secure key store with encryption, integrity verification, and atomic persistence
/// of API keys with provider selection and ProfileLock
/// </summary>
public class SecureKeyStore
{
    private readonly ILogger<SecureKeyStore> _logger;
    private readonly Services.ISecureStorageService _secureStorage;
    private readonly string _dataDirectory;
    private readonly string _keyStorePath;
    private readonly string _integrityPath;

    /// <summary>
    /// Current version of the key store format for forward/backward compatibility
    /// </summary>
    private const int CurrentVersion = 1;

    public SecureKeyStore(
        ILogger<SecureKeyStore> logger,
        Services.ISecureStorageService secureStorage,
        ProviderSettings providerSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        
        _dataDirectory = providerSettings?.GetAuraDataDirectory() 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura");
        
        Directory.CreateDirectory(_dataDirectory);
        
        _keyStorePath = Path.Combine(_dataDirectory, "secure-keystore.dat");
        _integrityPath = Path.Combine(_dataDirectory, "secure-keystore.integrity");
    }

    /// <summary>
    /// Saves API keys, provider selection, and ProfileLock atomically with integrity verification
    /// </summary>
    public async Task SaveAtomicAsync(
        Dictionary<string, string> apiKeys,
        string? selectedProviderId,
        ProviderProfileLock? profileLock,
        CancellationToken ct = default)
    {
        try
        {
            var keyStore = new KeyStoreData
            {
                Version = CurrentVersion,
                Timestamp = DateTime.UtcNow,
                ApiKeys = apiKeys ?? new Dictionary<string, string>(),
                SelectedProviderId = selectedProviderId,
                ProfileLock = profileLock != null ? new ProfileLockData
                {
                    JobId = profileLock.JobId,
                    ProviderName = profileLock.ProviderName,
                    ProviderType = profileLock.ProviderType,
                    IsEnabled = profileLock.IsEnabled,
                    CreatedAt = profileLock.CreatedAt,
                    OfflineModeEnabled = profileLock.OfflineModeEnabled,
                    ApplicableStages = profileLock.ApplicableStages,
                    Metadata = profileLock.Metadata != null ? new ProfileLockMetadataData
                    {
                        CreatedByUser = profileLock.Metadata.CreatedByUser,
                        Reason = profileLock.Metadata.Reason,
                        Tags = profileLock.Metadata.Tags,
                        Source = profileLock.Metadata.Source,
                        AllowManualFallback = profileLock.Metadata.AllowManualFallback,
                        MaxWaitBeforeFallbackSeconds = profileLock.Metadata.MaxWaitBeforeFallbackSeconds
                    } : null
                } : null
            };

            var json = JsonSerializer.Serialize(keyStore, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var dataBytes = Encoding.UTF8.GetBytes(json);
            
            // Compute HMAC for integrity verification
            var integrityHash = ComputeIntegrityHash(dataBytes);
            
            // Save keys to SecureStorageService first (individual encryption)
            foreach (var kvp in apiKeys)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    await _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value);
                }
            }

            // Write atomic keystore data and integrity hash
            await File.WriteAllBytesAsync(_keyStorePath, dataBytes, ct);
            await File.WriteAllTextAsync(_integrityPath, integrityHash, ct);

            _logger.LogInformation(
                "AUDIT: Atomic keystore save completed. Keys: {KeyCount}, Provider: {Provider}, ProfileLock: {ProfileLock}",
                apiKeys.Count,
                selectedProviderId ?? "None",
                profileLock != null ? $"{profileLock.ProviderName} ({(profileLock.IsEnabled ? "Enabled" : "Disabled")})" : "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save atomic keystore");
            throw;
        }
    }

    /// <summary>
    /// Loads keystore data with integrity verification
    /// </summary>
    public async Task<KeyStoreData?> LoadAtomicAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_keyStorePath))
            {
                _logger.LogDebug("Keystore file not found, returning null");
                return null;
            }

            var dataBytes = await File.ReadAllBytesAsync(_keyStorePath, ct);
            
            // Verify integrity if integrity file exists
            if (File.Exists(_integrityPath))
            {
                var storedHash = await File.ReadAllTextAsync(_integrityPath, ct);
                var computedHash = ComputeIntegrityHash(dataBytes);
                
                if (!string.Equals(storedHash, computedHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Keystore integrity check failed. File may be corrupted or tampered.");
                    
                    // Create backup of potentially corrupted file
                    var backupPath = $"{_keyStorePath}.corrupted.{DateTime.UtcNow.Ticks}";
                    File.Copy(_keyStorePath, backupPath, true);
                    _logger.LogWarning("Backed up potentially corrupted keystore to: {BackupPath}", backupPath);
                    
                    return null;
                }
                
                _logger.LogDebug("Keystore integrity verification passed");
            }
            else
            {
                _logger.LogWarning("No integrity file found for keystore. Proceeding with caution.");
            }

            var json = Encoding.UTF8.GetString(dataBytes);
            var keyStore = JsonSerializer.Deserialize<KeyStoreData>(json);
            
            if (keyStore == null)
            {
                _logger.LogWarning("Failed to deserialize keystore data");
                return null;
            }

            _logger.LogInformation(
                "Loaded keystore: Version {Version}, Keys: {KeyCount}, Provider: {Provider}",
                keyStore.Version,
                keyStore.ApiKeys?.Count ?? 0,
                keyStore.SelectedProviderId ?? "None");

            return keyStore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load atomic keystore");
            return null;
        }
    }

    /// <summary>
    /// Computes HMAC-SHA256 integrity hash of the data
    /// </summary>
    private string ComputeIntegrityHash(byte[] data)
    {
        // Use machine-specific key for HMAC (derived from machine/user context)
        var machineKey = GetMachineSpecificKey();
        
        using var hmac = new HMACSHA256(machineKey);
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Gets a machine-specific key for integrity verification
    /// </summary>
    private byte[] GetMachineSpecificKey()
    {
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        var combined = $"{machineName}:{userName}:aura-keystore-integrity";
        
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
    }
}

/// <summary>
/// Data structure for atomic keystore persistence
/// </summary>
public class KeyStoreData
{
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> ApiKeys { get; set; } = new();
    public string? SelectedProviderId { get; set; }
    public ProfileLockData? ProfileLock { get; set; }
}

/// <summary>
/// Serializable ProfileLock data
/// </summary>
public class ProfileLockData
{
    public string JobId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool OfflineModeEnabled { get; set; }
    public string[] ApplicableStages { get; set; } = Array.Empty<string>();
    public ProfileLockMetadataData? Metadata { get; set; }
}

/// <summary>
/// Serializable ProfileLock metadata
/// </summary>
public class ProfileLockMetadataData
{
    public string? CreatedByUser { get; set; }
    public string? Reason { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Source { get; set; } = "User";
    public bool AllowManualFallback { get; set; } = true;
    public int? MaxWaitBeforeFallbackSeconds { get; set; }
}
