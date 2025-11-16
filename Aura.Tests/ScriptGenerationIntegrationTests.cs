using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.AI;
using Aura.Core.Services.Generation;
using Aura.Core.Services.Validation;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for script generation with schema validation
/// These tests demonstrate the end-to-end flow
/// </summary>
public class ScriptGenerationIntegrationTests
{
    [Fact(Skip = "Integration test - requires OpenAI API key")]
    public async Task GenerateScriptWithSchemaAsync_WithOpenAI_ReturnsValidScript()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "test-key";
        var httpClient = new HttpClient();
        var logger = NullLogger<OpenAiLlmProvider>.Instance;
        var promptLogger = NullLogger<PromptCustomizationService>.Instance;
        var promptService = new PromptCustomizationService(promptLogger);

        var provider = new OpenAiLlmProvider(
            logger,
            httpClient,
            apiKey,
            model: "gpt-4o-mini",
            maxRetries: 2,
            timeoutSeconds: 120,
            promptCustomizationService: promptService);

        var brief = new Brief(
            Topic: "Introduction to Machine Learning",
            Audience: "Tech enthusiasts",
            Goal: "Educate viewers about ML basics",
            Tone: "Professional yet accessible",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        // Act
        var scriptJson = await provider.GenerateScriptWithSchemaAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(scriptJson);
        Assert.NotEmpty(scriptJson);
        
        // Validate the structure
        var validationService = new SchemaValidationService(NullLogger<SchemaValidationService>.Instance);
        var validationResult = validationService.ValidateScriptJson(scriptJson);
        
        Assert.True(validationResult.IsValid, 
            $"Generated script failed validation: {string.Join(", ", validationResult.Errors)}");
    }

    [Fact]
    public async Task ScriptGenerationService_WithInvalidMockProvider_ThrowsException()
    {
        // Arrange
        var validationService = new SchemaValidationService(NullLogger<SchemaValidationService>.Instance);
        var scriptService = new ScriptGenerationService(
            NullLogger<ScriptGenerationService>.Instance,
            validationService);

        // MockLlmProvider returns markdown format which won't pass JSON schema validation
        var invalidProvider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "Test Audience",
            Goal: "Test Goal",
            Tone: "Test Tone",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act & Assert - Validation should catch invalid format
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await scriptService.GenerateScriptAsync(
                invalidProvider,
                brief,
                spec,
                CancellationToken.None);
        });
    }
}

