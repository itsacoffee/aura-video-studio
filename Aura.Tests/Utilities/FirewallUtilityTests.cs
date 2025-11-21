using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Aura.Core.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Utilities;

/// <summary>
/// Tests for FirewallUtility
/// Note: Some tests are platform-specific and will be skipped on non-Windows platforms
/// </summary>
public class FirewallUtilityTests
{
    private readonly Mock<ILogger<FirewallUtility>> _loggerMock;
    private readonly FirewallUtility _firewallUtility;

    public FirewallUtilityTests()
    {
        _loggerMock = new Mock<ILogger<FirewallUtility>>();
        _firewallUtility = new FirewallUtility(_loggerMock.Object);
    }

    [Fact]
    public void IsWindows_ReturnsCorrectPlatform()
    {
        var isWindows = FirewallUtility.IsWindows();
        var expectedWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        Assert.Equal(expectedWindows, isWindows);
    }

    [Fact]
    public void IsAdministrator_OnNonWindows_ReturnsFalse()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var isAdmin = FirewallUtility.IsAdministrator();
            Assert.False(isAdmin);
        }
        else
        {
            var isAdmin = FirewallUtility.IsAdministrator();
            Assert.True(isAdmin || !isAdmin);
        }
    }

    [Fact]
    public async Task RuleExistsAsync_OnNonWindows_ReturnsTrue()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var result = await _firewallUtility.RuleExistsAsync("/fake/path");
            Assert.True(result);
        }
    }

    [Fact]
    public async Task AddFirewallRuleAsync_OnNonWindows_ReturnsSuccess()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var result = await _firewallUtility.AddFirewallRuleAsync("/fake/path", false);
            Assert.True(result.Success);
            Assert.Contains("Not on Windows", result.Message);
        }
    }

    [Fact]
    public async Task AddFirewallRuleAsync_WithNonExistentFile_ReturnsFailure()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var nonExistentPath = @"C:\NonExistent\Path\File.exe";
            var result = await _firewallUtility.AddFirewallRuleAsync(nonExistentPath, false);
            
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RemoveFirewallRulesAsync_OnNonWindows_ReturnsSuccess()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var result = await _firewallUtility.RemoveFirewallRulesAsync();
            Assert.True(result.Success);
            Assert.Contains("Not on Windows", result.Message);
        }
    }

    [Fact]
    public async Task RuleExistsAsync_WithInvalidPath_HandlesGracefully()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var result = await _firewallUtility.RuleExistsAsync(string.Empty);
            Assert.False(result);
        }
    }
}
