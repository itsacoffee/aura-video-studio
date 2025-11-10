using Aura.Core.Logging;
using Xunit;

namespace Aura.Tests.Logging;

public class LogSanitizerTests
{
    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("apikey")]
    [InlineData("api_key")]
    [InlineData("secret")]
    [InlineData("token")]
    [InlineData("credential")]
    public void SanitizeDictionary_Should_Redact_Sensitive_Keys(string sensitiveKey)
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            [sensitiveKey] = "sensitive-value",
            ["normalKey"] = "normal-value"
        };

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert
        Assert.Equal("***REDACTED***", result[sensitiveKey]);
        Assert.Equal("normal-value", result["normalKey"]);
    }

    [Fact]
    public void SanitizeDictionary_Should_Preserve_Non_Sensitive_Data()
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            ["userId"] = "user123",
            ["projectId"] = "proj456",
            ["count"] = 42,
            ["timestamp"] = "2024-01-15T10:30:00Z"
        };

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert
        Assert.Equal("user123", result["userId"]);
        Assert.Equal("proj456", result["projectId"]);
        Assert.Equal(42, result["count"]);
        Assert.Equal("2024-01-15T10:30:00Z", result["timestamp"]);
    }

    [Theory]
    [InlineData("mypassword", "my***rd")]
    [InlineData("secret123", "se***23")]
    [InlineData("ab", "***")]
    [InlineData("abc", "***")]
    [InlineData("abcd", "ab***cd")]
    [InlineData("verylongstring", "ve***ng")]
    public void MaskString_Should_Mask_String_Correctly(string input, string expected)
    {
        // Act
        var result = LogSanitizer.MaskString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, "m***d")]
    [InlineData(3, "myp***ord")]
    [InlineData(4, "mypa***word")]
    public void MaskString_Should_Respect_VisibleChars_Parameter(int visibleChars, string expected)
    {
        // Arrange
        var input = "mypassword";

        // Act
        var result = LogSanitizer.MaskString(input, visibleChars);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MaskString_Should_Handle_Empty_String()
    {
        // Act
        var result = LogSanitizer.MaskString(string.Empty);

        // Assert
        Assert.Equal("***", result);
    }

    [Fact]
    public void MaskString_Should_Handle_Short_String()
    {
        // Act
        var result = LogSanitizer.MaskString("ab");

        // Assert
        Assert.Equal("***", result);
    }

    [Fact]
    public void SanitizeDictionary_Should_Handle_Partial_Matches()
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            ["userPassword"] = "secret1",
            ["apiKeyToken"] = "secret2",
            ["mySecretValue"] = "secret3",
            ["regularField"] = "value"
        };

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert
        Assert.Equal("***REDACTED***", result["userPassword"]);
        Assert.Equal("***REDACTED***", result["apiKeyToken"]);
        Assert.Equal("***REDACTED***", result["mySecretValue"]);
        Assert.Equal("value", result["regularField"]);
    }

    [Fact]
    public void SanitizeDictionary_Should_Be_Case_Insensitive()
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            ["PASSWORD"] = "secret1",
            ["Password"] = "secret2",
            ["password"] = "secret3",
            ["PaSsWoRd"] = "secret4"
        };

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert
        Assert.All(result.Values, value => Assert.Equal("***REDACTED***", value));
    }

    [Fact]
    public void SanitizeDictionary_Should_Handle_Empty_Dictionary()
    {
        // Arrange
        var input = new Dictionary<string, object>();

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SanitizeDictionary_Should_Create_New_Dictionary()
    {
        // Arrange
        var input = new Dictionary<string, object>
        {
            ["password"] = "secret",
            ["userId"] = "user123"
        };

        // Act
        var result = LogSanitizer.SanitizeDictionary(input);

        // Assert - Original should be unchanged
        Assert.Equal("secret", input["password"]);
        Assert.Equal("***REDACTED***", result["password"]);
        Assert.NotSame(input, result);
    }
}
