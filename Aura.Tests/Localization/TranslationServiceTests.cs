using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

/// <summary>
/// Unit tests for TranslationService, particularly focusing on the ExtractTranslation
/// method that handles malformed LLM responses from Ollama and other local models.
/// </summary>
public class TranslationServiceTests
{
    private readonly Mock<ILogger<TranslationService>> _loggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly TranslationService _service;

    public TranslationServiceTests()
    {
        _loggerMock = new Mock<ILogger<TranslationService>>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _service = new TranslationService(_loggerMock.Object, _llmProviderMock.Object);
    }

    #region ExtractTranslation Tests

    [Fact]
    public async Task TranslateAsync_WithStructuredJSON_ExtractsTranslationField()
    {
        // Arrange - LLM returns malformed JSON with a translation field
        var jsonResponse = @"{""translation"": ""Hola mundo"", ""title"": ""Tutorial de traducción""}";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithPlainText_ReturnsAsIs()
    {
        // Arrange - LLM returns clean plain text
        var plainTextResponse = "Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plainTextResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithTranslationPrefix_RemovesPrefix()
    {
        // Arrange - LLM adds "Translation:" prefix
        var prefixedResponse = "Translation: Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefixedResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithTranslatedTextPrefix_RemovesPrefix()
    {
        // Arrange - LLM adds "Translated text:" prefix
        var prefixedResponse = "Translated text: Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefixedResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithHereIsTheTranslationPrefix_RemovesPrefix()
    {
        // Arrange - LLM adds "Here is the translation:" prefix
        var prefixedResponse = "Here is the translation: Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefixedResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithTranslatedTextField_ExtractsField()
    {
        // Arrange - LLM returns JSON with translatedText field
        var jsonResponse = @"{""translatedText"": ""Hola mundo"", ""notes"": ""Some notes""}";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithTextField_ExtractsField()
    {
        // Arrange - LLM returns JSON with text field
        var jsonResponse = @"{""text"": ""Hola mundo"", ""confidence"": 0.95}";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithContentField_ExtractsField()
    {
        // Arrange - LLM returns JSON with content field
        var jsonResponse = @"{""content"": ""Hola mundo"", ""metadata"": {}}";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithMultilineText_PreservesNewlines()
    {
        // Arrange - LLM returns multiline translation
        var multilineResponse = "Primera línea\n\nSegunda línea\n\nTercera línea";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(multilineResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "First line\n\nSecond line\n\nThird line",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - newlines should be preserved
        Assert.Contains("Primera línea", result.TranslatedText);
        Assert.Contains("Segunda línea", result.TranslatedText);
        Assert.Contains("Tercera línea", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithEmptyResponse_ReturnsEmpty()
    {
        // Arrange - LLM returns empty response
        var emptyResponse = "";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithComplexTutorialJSON_StripsArtifacts()
    {
        // Arrange - LLM returns complex tutorial JSON (worst case from Ollama)
        var complexJsonResponse = @"{""title"": ""Translation Tutorial"", ""description"": ""How to translate text"", ""tutorial"": {""steps"": [""Step 1"", ""Step 2""]}, ""translation"": ""Hola mundo""}";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(complexJsonResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - should extract the translation field
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithOutputPrefix_RemovesPrefix()
    {
        // Arrange - LLM adds "Output:" prefix
        var prefixedResponse = "Output: Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefixedResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WithResultPrefix_RemovesPrefix()
    {
        // Arrange - LLM adds "Result:" prefix
        var prefixedResponse = "Result: Hola mundo";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefixedResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Hola mundo", result.TranslatedText);
    }

    #endregion

    #region Prompt Constraint Tests

    [Fact]
    public async Task TranslateAsync_PromptIncludesCriticalOutputRequirements()
    {
        // Arrange - capture the system prompt
        string? capturedSystemPrompt = null;
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, LlmParameters?, CancellationToken>((system, user, param, ct) => 
                capturedSystemPrompt = system)
            .ReturnsAsync("Hola mundo");

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - verify the system prompt contains critical output requirements
        Assert.NotNull(capturedSystemPrompt);
        Assert.Contains("CRITICAL OUTPUT REQUIREMENTS", capturedSystemPrompt);
        Assert.Contains("DO NOT wrap the output in JSON", capturedSystemPrompt);
        Assert.Contains("DO NOT include metadata fields", capturedSystemPrompt);
        Assert.Contains("WRONG OUTPUT EXAMPLE", capturedSystemPrompt);
        Assert.Contains("CORRECT OUTPUT EXAMPLE", capturedSystemPrompt);
    }

    [Fact]
    public async Task TranslateAsync_PromptIncludesExampleOutput()
    {
        // Arrange - capture the system prompt
        string? capturedSystemPrompt = null;
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, LlmParameters?, CancellationToken>((system, user, param, ct) => 
                capturedSystemPrompt = system)
            .ReturnsAsync("Hola mundo");

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - verify the system prompt contains example outputs to guide the model
        Assert.NotNull(capturedSystemPrompt);
        // The prompt should show a wrong JSON example and a correct plain text example
        Assert.Contains("Prueba nuestro nuevo sabor", capturedSystemPrompt);
    }

    #endregion

    #region Output Validation Tests

    [Fact]
    public async Task TranslateAsync_WithStructuredArtifacts_LogsError()
    {
        // Arrange - LLM returns response with structured artifacts that can't be parsed as JSON
        var artifactResponse = @"Some text with ""title"": ""bad"" embedded";
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifactResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        var result = await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - verify logging was called for structured metadata detection
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("structured metadata")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TranslateAsync_WithVeryLongResponse_LogsWarning()
    {
        // Arrange - LLM returns an unusually long response (5x+ the source length)
        var longResponse = new string('a', 1000); // Much longer than "Hello world"
        
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(longResponse);

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            SourceText = "Hello world",
            Options = new TranslationOptions { Mode = TranslationMode.Literal, EnableQualityScoring = false }
        };

        // Act
        await _service.TranslateAsync(request, CancellationToken.None);

        // Assert - verify warning was logged about long response
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("longer than source")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}

