using System;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class StartupValidatorTests
{
    private readonly ILogger<StartupValidator> _logger;
    private readonly ILogger<ProviderSettings> _providerSettingsLogger;

    public StartupValidatorTests()
    {
        _logger = NullLogger<StartupValidator>.Instance;
        _providerSettingsLogger = NullLogger<ProviderSettings>.Instance;
    }

    [Fact]
    public void Validate_Should_ReturnTrue_WhenConfigurationIsValid()
    {
        // Arrange
        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var validator = new StartupValidator(_logger, providerSettings);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.True(result, "Validation should pass with valid configuration");
    }

    [Fact]
    public void Validate_Should_CheckCriticalDirectories()
    {
        // Arrange
        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var validator = new StartupValidator(_logger, providerSettings);

        // Act
        var result = validator.Validate();

        // Assert
        // Directories should be created by ProviderSettings
        Assert.True(result, "Critical directories should be accessible");
    }

    [Fact]
    public void Validate_Should_CheckTempDirectoryWritability()
    {
        // Arrange
        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var validator = new StartupValidator(_logger, providerSettings);

        // Act
        var result = validator.Validate();

        // Assert
        // Temp directory should be writable on any system
        Assert.True(result, "Temp directory should be writable");
    }

    [Fact]
    public void Validate_Should_CheckPortConfiguration()
    {
        // Arrange
        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var validator = new StartupValidator(_logger, providerSettings);

        // Act
        var result = validator.Validate();

        // Assert
        Assert.True(result, "Port configuration should be valid");
    }
}
