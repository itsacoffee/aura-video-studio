using Aura.Core.Errors;
using Xunit;

namespace Aura.Tests.Errors;

public class ErrorDocumentationTests
{
    [Theory]
    [InlineData("E100", true)]
    [InlineData("E100-401", true)]
    [InlineData("E200", true)]
    [InlineData("FFmpegNotFound", true)]
    [InlineData("MissingApiKey:OPENAI_KEY", true)]
    [InlineData("UNKNOWN_CODE", false)]
    public void HasDocumentation_ReturnsExpectedResult(string errorCode, bool expected)
    {
        // Act
        var hasDoc = ErrorDocumentation.HasDocumentation(errorCode);

        // Assert
        Assert.Equal(expected, hasDoc);
    }

    [Fact]
    public void GetDocumentation_WithValidCode_ReturnsDocumentation()
    {
        // Arrange
        var errorCode = "E100";

        // Act
        var doc = ErrorDocumentation.GetDocumentation(errorCode);

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("LLM Provider Error", doc.Title);
        Assert.NotNull(doc.Description);
        Assert.NotNull(doc.Url);
        Assert.StartsWith("https://", doc.Url);
    }

    [Fact]
    public void GetDocumentation_WithSpecificCode_ReturnsSpecificDocumentation()
    {
        // Arrange
        var errorCode = "E100-401";

        // Act
        var doc = ErrorDocumentation.GetDocumentation(errorCode);

        // Assert
        Assert.NotNull(doc);
        Assert.Equal("LLM Authentication Failed", doc.Title);
    }

    [Fact]
    public void GetDocumentation_WithCategoryCode_ReturnsCategoryDocumentation()
    {
        // Arrange
        var errorCode = "MissingApiKey:CUSTOM_KEY";

        // Act
        var doc = ErrorDocumentation.GetDocumentation(errorCode);

        // Assert
        Assert.NotNull(doc);
        Assert.Contains("Missing API Key", doc.Title);
    }

    [Fact]
    public void GetDocumentation_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var errorCode = "NONEXISTENT_CODE";

        // Act
        var doc = ErrorDocumentation.GetDocumentation(errorCode);

        // Assert
        Assert.Null(doc);
    }

    [Fact]
    public void GetFallbackUrl_ReturnsValidUrl()
    {
        // Act
        var url = ErrorDocumentation.GetFallbackUrl();

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("https://", url);
    }

    [Theory]
    [InlineData("FFmpegNotFound")]
    [InlineData("OutOfDiskSpace")]
    [InlineData("TransientNetworkFailure")]
    [InlineData("RequiresNvidiaGPU")]
    public void CommonErrorCodes_HaveDocumentation(string errorCode)
    {
        // Act
        var doc = ErrorDocumentation.GetDocumentation(errorCode);

        // Assert
        Assert.NotNull(doc);
        Assert.NotEmpty(doc.Title);
        Assert.NotEmpty(doc.Description);
        Assert.NotEmpty(doc.Url);
    }
}
