using Aura.Api.Services;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

/// <summary>
/// Unit tests for LocalizationService, particularly focusing on language validation
/// which now accepts any non-empty string to allow LLM-interpreted language descriptions.
/// </summary>
public class LocalizationServiceTests
{
    private readonly Mock<ILogger<LocalizationService>> _loggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly LocalizationService _service;

    public LocalizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalizationService>>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        
        // Setup logger factory to return mocked loggers
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        _service = new LocalizationService(
            _loggerMock.Object,
            _llmProviderMock.Object,
            _loggerFactoryMock.Object);
    }

    #region ValidateLanguageCode Tests

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("zh")]
    public void ValidateLanguageCode_WithStandardISOCode_ReturnsValid(string languageCode)
    {
        // Act
        var result = _service.ValidateLanguageCode(languageCode);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Spanish")]
    [InlineData("French")]
    [InlineData("German")]
    [InlineData("Chinese")]
    public void ValidateLanguageCode_WithFullLanguageName_ReturnsValid(string languageName)
    {
        // Act
        var result = _service.ValidateLanguageCode(languageName);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("English (US)")]
    [InlineData("English (UK)")]
    [InlineData("Spanish (Mexico)")]
    [InlineData("Portuguese (Brazil)")]
    [InlineData("French (Canadian)")]
    public void ValidateLanguageCode_WithRegionalVariant_ReturnsValid(string regionalVariant)
    {
        // Act
        var result = _service.ValidateLanguageCode(regionalVariant);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("Medieval English")]
    [InlineData("Texan English in 1891")]
    [InlineData("Shakespearean English")]
    [InlineData("Formal Japanese")]
    [InlineData("Casual Spanish")]
    [InlineData("Victorian Era British")]
    public void ValidateLanguageCode_WithCreativeLanguageDescription_ReturnsValid(string description)
    {
        // Act
        var result = _service.ValidateLanguageCode(description);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("Pirate Speak")]
    [InlineData("Old West Cowboy")]
    [InlineData("1950s American Commercial")]
    [InlineData("Formal Corporate Tone")]
    public void ValidateLanguageCode_WithStyleDescription_ReturnsValid(string style)
    {
        // Act
        var result = _service.ValidateLanguageCode(style);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ValidateLanguageCode_WithEmptyOrWhitespace_ReturnsInvalid(string input)
    {
        // Act
        var result = _service.ValidateLanguageCode(input);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_LANGUAGE_EMPTY", result.ErrorCode);
    }

    [Fact]
    public void ValidateLanguageCode_WithNull_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateLanguageCode(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("INVALID_LANGUAGE_EMPTY", result.ErrorCode);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    [InlineData("zh-CN")]
    [InlineData("es-MX")]
    public void ValidateLanguageCode_WithLanguageRegionCode_ReturnsValid(string code)
    {
        // Act
        var result = _service.ValidateLanguageCode(code);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsWarning);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("1")]
    [InlineData("x")]
    public void ValidateLanguageCode_WithSingleCharacter_StillReturnsValid(string singleChar)
    {
        // The new validation accepts any non-empty string
        // Even single characters are valid - the LLM will interpret them
        
        // Act
        var result = _service.ValidateLanguageCode(singleChar);

        // Assert - Single characters are now valid since we accept any non-empty string
        Assert.True(result.IsValid);
    }

    #endregion
}
