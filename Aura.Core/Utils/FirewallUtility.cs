using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Utils;

/// <summary>
/// Utility for configuring Windows Firewall rules for Aura Video Studio backend
/// </summary>
public class FirewallUtility
{
    private readonly ILogger<FirewallUtility> _logger;
    private const string RuleName = "Aura Video Studio Backend";
    private const string RuleNamePublic = "Aura Video Studio Backend (Public)";

    public FirewallUtility(ILogger<FirewallUtility> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if running on Windows
    /// </summary>
    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <summary>
    /// Check if firewall rule exists for the given executable
    /// </summary>
    public async Task<bool> RuleExistsAsync(string executablePath)
    {
        if (!IsWindows())
        {
            _logger.LogInformation("Not on Windows, skipping firewall check");
            return true;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{RuleName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            bool exists = process.ExitCode == 0 && output.Contains(executablePath, StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("Firewall rule exists: {Exists}", exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check firewall rule");
            return false;
        }
    }

    /// <summary>
    /// Add firewall rule for the backend executable
    /// </summary>
    public async Task<(bool Success, string Message)> AddFirewallRuleAsync(string executablePath, bool includePublicProfile = false)
    {
        if (!IsWindows())
        {
            return (true, "Not on Windows, no firewall configuration needed");
        }

        if (!File.Exists(executablePath))
        {
            _logger.LogError("Executable not found: {Path}", executablePath);
            return (false, $"Executable not found: {executablePath}");
        }

        try
        {
            var privateResult = await AddRuleAsync(
                RuleName,
                executablePath,
                "private,domain"
            );

            if (!privateResult.Success)
            {
                return privateResult;
            }

            if (includePublicProfile)
            {
                var publicResult = await AddRuleAsync(
                    RuleNamePublic,
                    executablePath,
                    "public"
                );

                if (!publicResult.Success)
                {
                    _logger.LogWarning("Failed to add public profile rule, but private/domain rule succeeded");
                }
            }

            return (true, "Firewall rules added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add firewall rule");
            return (false, $"Failed to add firewall rule: {ex.Message}");
        }
    }

    private async Task<(bool Success, string Message)> AddRuleAsync(string ruleName, string executablePath, string profile)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{executablePath}\" enable=yes profile={profile}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Firewall rule added: {RuleName}", ruleName);
                return (true, $"Firewall rule '{ruleName}' added successfully");
            }
            else
            {
                _logger.LogError("Failed to add firewall rule. Exit code: {ExitCode}, Error: {Error}", process.ExitCode, error);
                return (false, $"Failed to add firewall rule: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while adding firewall rule");
            return (false, $"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove firewall rules for Aura Video Studio
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveFirewallRulesAsync()
    {
        if (!IsWindows())
        {
            return (true, "Not on Windows, no firewall rules to remove");
        }

        try
        {
            await RemoveRuleAsync(RuleName);
            await RemoveRuleAsync(RuleNamePublic);

            return (true, "Firewall rules removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove firewall rules");
            return (false, $"Failed to remove firewall rules: {ex.Message}");
        }
    }

    private async Task RemoveRuleAsync(string ruleName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            _logger.LogInformation("Firewall rule removed: {RuleName}", ruleName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove firewall rule: {RuleName}", ruleName);
        }
    }

    /// <summary>
    /// Check if current process has admin privileges
    /// </summary>
    public static bool IsAdministrator()
    {
        if (!IsWindows())
        {
            return false;
        }

        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
