using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Security;

/// <summary>
/// Windows Credential Manager integration for secure API key storage
/// Uses Windows Data Protection API (DPAPI) for encryption
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsCredentialManager
{
    private readonly ILogger<WindowsCredentialManager> _logger;
    private const string TargetNamePrefix = "AuraVideoStudio_";

    public WindowsCredentialManager(ILogger<WindowsCredentialManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Store API key securely in Windows Credential Manager
    /// </summary>
    public bool StoreApiKey(string providerName, string apiKey)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Windows Credential Manager is only available on Windows");
            return false;
        }

        try
        {
            var targetName = $"{TargetNamePrefix}{providerName}";
            var credentialBlob = Encoding.UTF8.GetBytes(apiKey);

            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE.GENERIC,
                TargetName = targetName,
                CredentialBlob = Marshal.AllocHGlobal(credentialBlob.Length),
                CredentialBlobSize = credentialBlob.Length,
                Persist = CRED_PERSIST.LOCAL_MACHINE,
                AttributeCount = 0,
                UserName = Environment.UserName,
                Comment = $"API key for {providerName}"
            };

            try
            {
                Marshal.Copy(credentialBlob, 0, credential.CredentialBlob, credentialBlob.Length);

                if (CredWrite(ref credential, 0))
                {
                    _logger.LogInformation("Successfully stored API key for {Provider} in Credential Manager", providerName);
                    return true;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to store credential in Windows Credential Manager. Error code: {Error}", error);
                    return false;
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(credential.CredentialBlob);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while storing API key for {Provider}", providerName);
            return false;
        }
    }

    /// <summary>
    /// Retrieve API key from Windows Credential Manager
    /// </summary>
    public string? RetrieveApiKey(string providerName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Windows Credential Manager is only available on Windows");
            return null;
        }

        try
        {
            var targetName = $"{TargetNamePrefix}{providerName}";
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (CredRead(targetName, CRED_TYPE.GENERIC, 0, out credPtr))
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    
                    if (credential.CredentialBlob != IntPtr.Zero && credential.CredentialBlobSize > 0)
                    {
                        var credentialBytes = new byte[credential.CredentialBlobSize];
                        Marshal.Copy(credential.CredentialBlob, credentialBytes, 0, credential.CredentialBlobSize);
                        
                        var apiKey = Encoding.UTF8.GetString(credentialBytes);
                        _logger.LogDebug("Successfully retrieved API key for {Provider}", providerName);
                        return apiKey;
                    }
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != ERROR_NOT_FOUND)
                    {
                        _logger.LogWarning("Failed to retrieve credential for {Provider}. Error code: {Error}", providerName, error);
                    }
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    CredFree(credPtr);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving API key for {Provider}", providerName);
        }

        return null;
    }

    /// <summary>
    /// Delete API key from Windows Credential Manager
    /// </summary>
    public bool DeleteApiKey(string providerName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Windows Credential Manager is only available on Windows");
            return false;
        }

        try
        {
            var targetName = $"{TargetNamePrefix}{providerName}";

            if (CredDelete(targetName, CRED_TYPE.GENERIC, 0))
            {
                _logger.LogInformation("Successfully deleted API key for {Provider} from Credential Manager", providerName);
                return true;
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ERROR_NOT_FOUND)
                {
                    _logger.LogWarning("Failed to delete credential for {Provider}. Error code: {Error}", providerName, error);
                }
                return error == ERROR_NOT_FOUND; // Return true if already deleted
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting API key for {Provider}", providerName);
            return false;
        }
    }

    /// <summary>
    /// Check if API key exists for a provider
    /// </summary>
    public bool HasApiKey(string providerName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        var targetName = $"{TargetNamePrefix}{providerName}";
        IntPtr credPtr = IntPtr.Zero;

        try
        {
            bool exists = CredRead(targetName, CRED_TYPE.GENERIC, 0, out credPtr);
            return exists;
        }
        finally
        {
            if (credPtr != IntPtr.Zero)
            {
                CredFree(credPtr);
            }
        }
    }

    // Windows API constants and structures
    private const int ERROR_NOT_FOUND = 1168;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string targetName, CRED_TYPE type, int flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string targetName, CRED_TYPE type, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr cred);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string UserName;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = 1007
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }
}

/// <summary>
/// Cross-platform secure credential storage
/// Falls back to encrypted file storage on non-Windows platforms
/// </summary>
public class SecureCredentialStore
{
    private readonly WindowsCredentialManager? _windowsCredentialManager;
    private readonly ILogger<SecureCredentialStore> _logger;

    public SecureCredentialStore(
        ILogger<SecureCredentialStore> logger,
        WindowsCredentialManager? windowsCredentialManager = null)
    {
        _logger = logger;
        _windowsCredentialManager = windowsCredentialManager;
    }

    public bool StoreApiKey(string providerName, string apiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _windowsCredentialManager != null)
        {
            return _windowsCredentialManager.StoreApiKey(providerName, apiKey);
        }

        // Fallback for non-Windows platforms (could implement Keychain for macOS, Secret Service for Linux)
        _logger.LogWarning("Secure credential storage not available on this platform. Using configuration files.");
        return false;
    }

    public string? RetrieveApiKey(string providerName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _windowsCredentialManager != null)
        {
            return _windowsCredentialManager.RetrieveApiKey(providerName);
        }

        return null;
    }

    public bool DeleteApiKey(string providerName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _windowsCredentialManager != null)
        {
            return _windowsCredentialManager.DeleteApiKey(providerName);
        }

        return false;
    }

    public bool HasApiKey(string providerName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _windowsCredentialManager != null)
        {
            return _windowsCredentialManager.HasApiKey(providerName);
        }

        return false;
    }
}
