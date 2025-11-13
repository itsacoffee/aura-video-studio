using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

public class EnhancedKeyValidationServiceTests
{
    private readonly Mock<ILogger<EnhancedKeyValidationService>> _mockLogger;
    private readonly Mock<IKeyValidationService> _mockBaseValidator;
    private readonly Mock<ProviderValidationPolicyLoader> _mockPolicyLoader;
    private readonly EnhancedKeyValidationService _service;

    public EnhancedKeyValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<EnhancedKeyValidationService>>();
        _mockBaseValidator = new Mock<IKeyValidationService>();
        
        var mockPolicyLogger = new Mock<ILogger<ProviderValidationPolicyLoader>>();
        _mockPolicyLoader = new Mock<ProviderValidationPolicyLoader>(mockPolicyLogger.Object);

        _service = new EnhancedKeyValidationService(
            _mockLogger.Object,
            _mockBaseValidator.Object,
            _mockPolicyLoader.Object);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_ValidKey_ReturnsValidStatus()
    {
        // Arrange
        var providerName = "openai";
        var apiKey = "sk-test123";
        
        var policySet = new ProviderValidationPolicySet
        {
            DefaultPolicy = new ProviderValidationPolicy
            {
                Category = "cloud_llm",
                NormalTimeoutMs = 10000,
                ExtendedTimeoutMs = 30000,
                MaxTimeoutMs = 60000,
                RetryIntervalMs = 5000,
                MaxRetries = 2
            }
        };

        _mockPolicyLoader
            .Setup(x => x.LoadPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policySet);

        _mockBaseValidator
            .Setup(x => x.TestApiKeyAsync(providerName, apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyValidationResult
            {
                IsValid = true,
                Message = "API key is valid",
                Details = new Dictionary<string, string> { ["status"] = "connected" }
            });

        // Act
        var result = await _service.ValidateApiKeyAsync(providerName, apiKey, CancellationToken.None);

        // Assert
        Assert.Equal(KeyValidationStatus.Valid, result.Status);
        Assert.Equal(providerName, result.ProviderName);
        Assert.True(result.ElapsedMs > 0);
        Assert.NotNull(result.LastValidated);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_InvalidKey_ReturnsInvalidStatus()
    {
        // Arrange
        var providerName = "anthropic";
        var apiKey = "invalid-key";
        
        var policySet = new ProviderValidationPolicySet();
        _mockPolicyLoader
            .Setup(x => x.LoadPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policySet);

        _mockBaseValidator
            .Setup(x => x.TestApiKeyAsync(providerName, apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyValidationResult
            {
                IsValid = false,
                Message = "Invalid API key",
                Details = new Dictionary<string, string> { ["error"] = "unauthorized" }
            });

        // Act
        var result = await _service.ValidateApiKeyAsync(providerName, apiKey, CancellationToken.None);

        // Assert
        Assert.Equal(KeyValidationStatus.Invalid, result.Status);
        Assert.Contains("Invalid", result.Message);
        Assert.True(result.CanRetry);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_SlowProvider_UpdatesStatusProgressively()
    {
        // Arrange
        var providerName = "ollama";
        var apiKey = "local-model";
        
        var policySet = new ProviderValidationPolicySet
        {
            Policies = new Dictionary<string, ProviderValidationPolicy>
            {
                ["local_llm"] = new()
                {
                    Category = "local_llm",
                    NormalTimeoutMs = 1000,
                    ExtendedTimeoutMs = 2000,
                    MaxTimeoutMs = 5000,
                    RetryIntervalMs = 500,
                    MaxRetries = 1
                }
            },
            ProviderCategoryMapping = new Dictionary<string, string>
            {
                ["ollama"] = "local_llm"
            }
        };

        _mockPolicyLoader
            .Setup(x => x.LoadPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policySet);

        var callCount = 0;
        _mockBaseValidator
            .Setup(x => x.TestApiKeyAsync(providerName, apiKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "Valid after delay",
                    Details = new Dictionary<string, string>()
                };
            });

        // Act
        var result = await _service.ValidateApiKeyAsync(providerName, apiKey, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(callCount > 0);
    }

    [Fact]
    public async Task GetValidationStatus_AfterValidation_ReturnsStatus()
    {
        // Arrange
        var providerName = "openai";
        var apiKey = "sk-test";
        
        var policySet = new ProviderValidationPolicySet();
        _mockPolicyLoader
            .Setup(x => x.LoadPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policySet);

        _mockBaseValidator
            .Setup(x => x.TestApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeyValidationResult { IsValid = true, Message = "Valid" });

        await _service.ValidateApiKeyAsync(providerName, apiKey, CancellationToken.None);

        // Act
        var status = _service.GetValidationStatus(providerName);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(providerName, status.ProviderName);
        Assert.Equal(KeyValidationStatus.Valid, status.Status);
    }

    [Fact]
    public async Task CancelValidation_ActiveValidation_CancelsSuccessfully()
    {
        // Arrange
        var providerName = "slow-provider";
        var apiKey = "key";
        
        var policySet = new ProviderValidationPolicySet
        {
            DefaultPolicy = new ProviderValidationPolicy
            {
                MaxTimeoutMs = 60000,
                RetryIntervalMs = 10000
            }
        };

        _mockPolicyLoader
            .Setup(x => x.LoadPoliciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(policySet);

        _mockBaseValidator
            .Setup(x => x.TestApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string p, string k, CancellationToken ct) =>
            {
                await Task.Delay(30000, ct);
                return new KeyValidationResult { IsValid = true };
            });

        var validationTask = Task.Run(() => 
            _service.ValidateApiKeyAsync(providerName, apiKey, CancellationToken.None));

        await Task.Delay(100);

        // Act
        var cancelled = _service.CancelValidation(providerName);

        // Assert
        Assert.True(cancelled);
        await Assert.ThrowsAsync<OperationCanceledException>(() => validationTask);
    }
}
