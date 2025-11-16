using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Interface for secure API key storage
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Saves an API key securely with encryption
    /// </summary>
    Task SaveApiKeyAsync(string providerName, string apiKey);

    /// <summary>
    /// Retrieves a decrypted API key
    /// </summary>
    Task<string?> GetApiKeyAsync(string providerName);

    /// <summary>
    /// Checks if an API key exists for a provider
    /// </summary>
    Task<bool> HasApiKeyAsync(string providerName);

    /// <summary>
    /// Deletes an API key for a specific provider
    /// </summary>
    Task DeleteApiKeyAsync(string providerName);

    /// <summary>
    /// Gets all configured provider names (without revealing keys)
    /// </summary>
    Task<List<string>> GetConfiguredProvidersAsync();
}

/// <summary>
/// Secure storage service with platform-specific encryption (DPAPI on Windows, AES-256 on Linux/macOS)
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private readonly ILogger<SecureStorageService> _logger;
    private readonly string _storagePath;
    private readonly bool _isWindows;
    private readonly byte[] _machineKey;

    public SecureStorageService(ILogger<SecureStorageService> logger)
    {
        _logger = logger;
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Set up storage path - unified for all platforms
        var dataRoot = AuraEnvironmentPaths.ResolveDataRoot(null);
        var secureDir = Path.Combine(dataRoot, "secure");
        Directory.CreateDirectory(secureDir);
        _storagePath = Path.Combine(secureDir, "apikeys.dat");

        // Generate or load machine-specific key for non-Windows platforms
        _machineKey = GetOrCreateMachineKey(secureDir);

        // Set proper file permissions on non-Windows systems
        if (!_isWindows)
        {
            SetUnixFilePermissions(secureDir);
            if (File.Exists(_storagePath))
            {
                SetUnixFilePermissions(_storagePath);
            }
        }
    }

    /// <summary>
    /// Sets file permissions to 600 (owner read/write only) on Unix-like systems
    /// </summary>
    private void SetUnixFilePermissions(string path)
    {
        try
        {
            // Use .NET 7+ UnixFileMode if available (better approach)
            // Fallback to chmod command for compatibility
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                
                // Use ArgumentList for proper escaping (no shell interpretation)
                startInfo.ArgumentList.Add("600");
                startInfo.ArgumentList.Add(path);
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        _logger.LogWarning("chmod command failed with exit code {ExitCode}: {Error}", 
                            process.ExitCode, error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set file permissions on {Path}", path);
        }
    }

    public async Task SaveApiKeyAsync(string providerName, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        try
        {
            var sanitizedProvider = SanitizeForLogging(providerName);
            _logger.LogInformation("Saving API key for provider: {Provider}", sanitizedProvider);

            // Load existing keys
            var keys = await LoadKeysAsync().ConfigureAwait(false);

            // Add or update the key
            keys[providerName] = apiKey;

            // Save encrypted
            await SaveKeysAsync(keys).ConfigureAwait(false);

            _logger.LogInformation("API key saved successfully for provider: {Provider}", sanitizedProvider);
        }
        catch (Exception ex)
        {
            var sanitizedProvider = SanitizeForLogging(providerName);
            _logger.LogError(ex, "Failed to save API key for provider: {Provider}", sanitizedProvider);
            throw;
        }
    }

    public async Task<string?> GetApiKeyAsync(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        try
        {
            var keys = await LoadKeysAsync().ConfigureAwait(false);
            return keys.TryGetValue(providerName, out var key) ? key : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve API key for provider: {Provider}", providerName);
            return null;
        }
    }

    public async Task<bool> HasApiKeyAsync(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        try
        {
            var keys = await LoadKeysAsync().ConfigureAwait(false);
            return keys.ContainsKey(providerName) && !string.IsNullOrWhiteSpace(keys[providerName]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check API key existence for provider: {Provider}", providerName);
            return false;
        }
    }

    public async Task DeleteApiKeyAsync(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
        }

        try
        {
            var sanitizedProvider = SanitizeForLogging(providerName);
            _logger.LogInformation("Deleting API key for provider: {Provider}", sanitizedProvider);

            var keys = await LoadKeysAsync().ConfigureAwait(false);

            if (keys.Remove(providerName))
            {
                await SaveKeysAsync(keys).ConfigureAwait(false);
                _logger.LogInformation("API key deleted successfully for provider: {Provider}", sanitizedProvider);
            }
            else
            {
                _logger.LogWarning("No API key found to delete for provider: {Provider}", sanitizedProvider);
            }
        }
        catch (Exception ex)
        {
            var sanitizedProvider = SanitizeForLogging(providerName);
            _logger.LogError(ex, "Failed to delete API key for provider: {Provider}", sanitizedProvider);
            throw;
        }
    }

    public async Task<List<string>> GetConfiguredProvidersAsync()
    {
        try
        {
            var keys = await LoadKeysAsync().ConfigureAwait(false);
            return keys.Keys.Where(k => !string.IsNullOrWhiteSpace(keys[k])).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configured providers");
            return new List<string>();
        }
    }

    /// <summary>
    /// Loads and decrypts all API keys from storage
    /// </summary>
    private async Task<Dictionary<string, string>> LoadKeysAsync()
    {
        if (!File.Exists(_storagePath))
        {
            _logger.LogDebug("API keys storage file not found, returning empty dictionary");
            return new Dictionary<string, string>();
        }

        try
        {
            var encryptedData = await File.ReadAllBytesAsync(_storagePath).ConfigureAwait(false);

            // Decrypt the data
            byte[] decryptedData;
            if (_isWindows)
            {
                // On Windows, use DPAPI via reflection to avoid compile-time dependency
                decryptedData = UnprotectDataWindows(encryptedData);
            }
            else
            {
                decryptedData = DecryptWithAes(encryptedData);
            }

            var json = Encoding.UTF8.GetString(decryptedData);
            var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            return keys ?? new Dictionary<string, string>();
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt API keys storage - file may be corrupted. Creating new storage.");
            
            // Handle corrupted file gracefully by creating a backup and starting fresh
            try
            {
                var backupPath = _storagePath + ".corrupted." + DateTime.UtcNow.Ticks;
                File.Move(_storagePath, backupPath);
                _logger.LogWarning("Moved corrupted storage file to: {BackupPath}", backupPath);
            }
            catch
            {
                // If we can't move the file, just delete it
                File.Delete(_storagePath);
            }

            return new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load API keys");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Encrypts and saves API keys to storage
    /// </summary>
    private async Task SaveKeysAsync(Dictionary<string, string> keys)
    {
        try
        {
            var json = JsonSerializer.Serialize(keys);
            var plainData = Encoding.UTF8.GetBytes(json);

            // Encrypt the data
            byte[] encryptedData;
            if (_isWindows)
            {
                // On Windows, use DPAPI via reflection to avoid compile-time dependency
                encryptedData = ProtectDataWindows(plainData);
            }
            else
            {
                encryptedData = EncryptWithAes(plainData);
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(_storagePath, encryptedData).ConfigureAwait(false);

            // Set proper file permissions on non-Windows systems
            if (!_isWindows)
            {
                SetUnixFilePermissions(_storagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save encrypted API keys");
            throw;
        }
    }

    /// <summary>
    /// Encrypts data using AES-256 (for Linux/macOS)
    /// </summary>
    private byte[] EncryptWithAes(byte[] plainData)
    {
        using var aes = Aes.Create();
        aes.Key = _machineKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainData, 0, plainData.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return result;
    }

    /// <summary>
    /// Decrypts data using AES-256 (for Linux/macOS)
    /// </summary>
    private byte[] DecryptWithAes(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = _machineKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract IV from the beginning of the encrypted data
        var iv = new byte[16];
        Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(encryptedData, 16, encryptedData.Length - 16);

        return decrypted;
    }

    /// <summary>
    /// Gets or creates a machine-specific key for AES encryption on non-Windows platforms
    /// </summary>
    private byte[] GetOrCreateMachineKey(string secureDir)
    {
        var keyPath = Path.Combine(secureDir, ".machinekey");

        try
        {
            if (File.Exists(keyPath))
            {
                var keyData = File.ReadAllBytes(keyPath);
                if (keyData.Length == 32) // AES-256 key size
                {
                    return keyData;
                }

                _logger.LogWarning("Machine key file has invalid size, generating new key");
            }

            // Generate new key
            using var rng = RandomNumberGenerator.Create();
            var key = new byte[32]; // 256 bits
            rng.GetBytes(key);

            // Save key with restricted permissions
            File.WriteAllBytes(keyPath, key);

            // On Unix-like systems, restrict file permissions
            if (!_isWindows)
            {
                SetUnixFilePermissions(keyPath);
            }

            _logger.LogInformation("Generated new machine-specific encryption key at {Path}", keyPath);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create machine key, using fallback");
            
            // Fallback: Generate key from machine-specific data
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var combined = $"{machineName}:{userName}:aura-video-studio";
            
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        }
    }

    /// <summary>
    /// Protects data using Windows DPAPI via reflection
    /// </summary>
    private byte[] ProtectDataWindows(byte[] plainData)
    {
        try
        {
            // Use reflection to call ProtectedData.Protect to avoid compile-time dependency
            var protectedDataType = Type.GetType("System.Security.Cryptography.ProtectedData, System.Security.Cryptography.ProtectedData");
            if (protectedDataType == null)
            {
                _logger.LogWarning("ProtectedData not available, falling back to AES encryption");
                return EncryptWithAes(plainData);
            }

            var protectMethod = protectedDataType.GetMethod("Protect", new[] { typeof(byte[]), typeof(byte[]), typeof(int) });
            if (protectMethod == null)
            {
                _logger.LogWarning("ProtectedData.Protect method not found, falling back to AES encryption");
                return EncryptWithAes(plainData);
            }

            // DataProtectionScope.CurrentUser = 0
            var result = protectMethod.Invoke(null, new object[] { plainData, null!, 0 });
            return (byte[])result!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to use Windows DPAPI, falling back to AES encryption");
            return EncryptWithAes(plainData);
        }
    }

    /// <summary>
    /// Unprotects data using Windows DPAPI via reflection
    /// </summary>
    private byte[] UnprotectDataWindows(byte[] encryptedData)
    {
        try
        {
            // Use reflection to call ProtectedData.Unprotect to avoid compile-time dependency
            var protectedDataType = Type.GetType("System.Security.Cryptography.ProtectedData, System.Security.Cryptography.ProtectedData");
            if (protectedDataType == null)
            {
                _logger.LogWarning("ProtectedData not available, falling back to AES decryption");
                return DecryptWithAes(encryptedData);
            }

            var unprotectMethod = protectedDataType.GetMethod("Unprotect", new[] { typeof(byte[]), typeof(byte[]), typeof(int) });
            if (unprotectMethod == null)
            {
                _logger.LogWarning("ProtectedData.Unprotect method not found, falling back to AES decryption");
                return DecryptWithAes(encryptedData);
            }

            // DataProtectionScope.CurrentUser = 0
            var result = unprotectMethod.Invoke(null, new object[] { encryptedData, null!, 0 });
            return (byte[])result!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to use Windows DPAPI, falling back to AES decryption");
            return DecryptWithAes(encryptedData);
        }
    }

    /// <summary>
    /// Sanitizes user input for safe logging to prevent log injection
    /// </summary>
    private static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "[empty]";
        }

        // Remove newlines and control characters to prevent log forging
        return System.Text.RegularExpressions.Regex.Replace(input, @"[\r\n\t\x00-\x1F\x7F]", "");
    }
}
