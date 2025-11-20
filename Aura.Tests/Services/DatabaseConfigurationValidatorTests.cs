using System;
using Aura.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Services;

public class DatabaseConfigurationValidatorTests
{
    private readonly DatabaseConfigurationValidator _validator;

    public DatabaseConfigurationValidatorTests()
    {
        _validator = new DatabaseConfigurationValidator(NullLogger<DatabaseConfigurationValidator>.Instance);
    }

    [Fact]
    public void ValidateConnectionString_ValidConnectionString_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Data Source=test.db;Mode=ReadWriteCreate;Foreign Keys=True";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithJournalModeKeyword_ReturnsFalse()
    {
        // Arrange
        var connectionString = "Data Source=test.db;Journal Mode=WAL;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("unsupported keyword 'journal mode'", errorMessage);
        Assert.Contains("Use PRAGMA statements after connection instead", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithJournalModeUnderscoreKeyword_ReturnsFalse()
    {
        // Arrange
        var connectionString = "Data Source=test.db;journal_mode=WAL;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("unsupported keyword 'journal_mode'", errorMessage);
        Assert.Contains("PRAGMA", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_CaseInsensitiveKeywordDetection_ReturnsFalse()
    {
        // Arrange
        var connectionString = "Data Source=test.db;JOURNAL MODE=WAL;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("unsupported keyword", errorMessage.ToLowerInvariant());
    }

    [Fact]
    public void ValidateConnectionString_NullConnectionString_ReturnsFalse()
    {
        // Arrange
        string? connectionString = null;

        // Act
        var result = _validator.ValidateConnectionString(connectionString!, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("cannot be null or empty", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_EmptyConnectionString_ReturnsFalse()
    {
        // Arrange
        var connectionString = string.Empty;

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("cannot be null or empty", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WhitespaceConnectionString_ReturnsFalse()
    {
        // Arrange
        var connectionString = "   ";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("cannot be null or empty", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var connectionString = "InvalidFormat;;;===";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("must specify a Data Source", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_MissingDataSource_ReturnsFalse()
    {
        // Arrange
        var connectionString = "Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Contains("must specify a Data Source", errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_MemoryDatabase_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Data Source=:memory:;Mode=Memory";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithFilename_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Filename=test.db;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithAllSupportedParameters_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Data Source=test.db;" +
                              "Mode=ReadWriteCreate;" +
                              "Cache=Shared;" +
                              "Foreign Keys=True;" +
                              "Recursive Triggers=True";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithWindowsPath_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Data Source=C:\\Users\\Test\\AppData\\Local\\Aura\\aura.db;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }

    [Fact]
    public void ValidateConnectionString_WithUnixPath_ReturnsTrue()
    {
        // Arrange
        var connectionString = "Data Source=/home/user/.local/share/aura/aura.db;Mode=ReadWriteCreate";

        // Act
        var result = _validator.ValidateConnectionString(connectionString, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessage);
    }
}
