using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

public class ProviderValidationPolicyLoaderTests : IDisposable
{
    private readonly Mock<ILogger<ProviderValidationPolicyLoader>> _mockLogger;
    private readonly string _testConfigPath;
    private readonly ProviderValidationPolicyLoader _service;

    public ProviderValidationPolicyLoaderTests()
    {
        _mockLogger = new Mock<ILogger<ProviderValidationPolicyLoader>>();
        _testConfigPath = Path.Combine(Path.GetTempPath(), "providerTimeoutProfiles_test.json");
        _service = new ProviderValidationPolicyLoader(_mockLogger.Object);
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoadPoliciesAsync_MissingFile_ReturnsDefaultPolicies()
    {
        // Act
        var policySet = await _service.LoadPoliciesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(policySet);
        Assert.NotNull(policySet.DefaultPolicy);
        Assert.True(policySet.Policies.Count > 0);
    }

    [Fact]
    public async Task LoadPoliciesAsync_ValidConfig_LoadsSuccessfully()
    {
        // Arrange
        var config = new
        {
            profiles = new
            {
                test_provider = new
                {
                    normalThresholdMs = 5000,
                    extendedThresholdMs = 15000,
                    deepWaitThresholdMs = 30000,
                    heartbeatIntervalMs = 2000,
                    description = "Test provider profile"
                }
            },
            providerMapping = new
            {
                TestProvider = "test_provider"
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(_testConfigPath, json);

        // Copy to expected location for loader
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var targetPath = Path.Combine(appDir, "providerTimeoutProfiles.json");
        File.Copy(_testConfigPath, targetPath, true);

        try
        {
            var loader = new ProviderValidationPolicyLoader(_mockLogger.Object);

            // Act
            var policySet = await loader.LoadPoliciesAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(policySet);
            Assert.True(policySet.Policies.ContainsKey("test_provider"));
            
            var policy = policySet.Policies["test_provider"];
            Assert.Equal(5000, policy.NormalTimeoutMs);
            Assert.Equal(15000, policy.ExtendedTimeoutMs);
            Assert.Equal(30000, policy.MaxTimeoutMs);
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }

    [Fact]
    public async Task LoadPoliciesAsync_InvalidJson_ReturnsDefaultPolicies()
    {
        // Arrange
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var targetPath = Path.Combine(appDir, "providerTimeoutProfiles.json");
        await File.WriteAllTextAsync(targetPath, "{ invalid json");

        try
        {
            var loader = new ProviderValidationPolicyLoader(_mockLogger.Object);

            // Act
            var policySet = await loader.LoadPoliciesAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(policySet);
            Assert.NotNull(policySet.DefaultPolicy);
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }

    [Fact]
    public async Task GetPolicyForProvider_MappedProvider_ReturnsCorrectPolicy()
    {
        // Arrange
        var policySet = await _service.LoadPoliciesAsync(CancellationToken.None);

        // Act
        var policy = policySet.GetPolicyForProvider("Ollama");

        // Assert
        Assert.NotNull(policy);
        Assert.Contains("local_llm", policy.Category.ToLower());
    }

    [Fact]
    public async Task GetPolicyForProvider_UnmappedProvider_ReturnsDefaultPolicy()
    {
        // Arrange
        var policySet = await _service.LoadPoliciesAsync(CancellationToken.None);

        // Act
        var policy = policySet.GetPolicyForProvider("UnknownProvider");

        // Assert
        Assert.NotNull(policy);
        Assert.Equal("default", policy.Category);
    }
}
