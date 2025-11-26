using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Agents;
using Aura.Core.Configuration;
using Aura.Core.Extensions;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aura.Tests.AI.Agents;

public class AgentIntegrationTests
{
    [Fact]
    public void ServiceCollection_RegistersAllAgents()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AgenticMode:Enabled"] = "true",
                ["AgenticMode:MaxIterations"] = "3",
                ["AgenticMode:TimeoutPerIterationSeconds"] = "180",
                ["AgenticMode:EnableLogging"] = "true",
                ["AgenticMode:FallbackToSinglePass"] = "true"
            })
            .Build();

        // Register required dependencies
        services.AddSingleton<ILlmProvider>(Mock.Of<ILlmProvider>());
        services.AddSingleton(Mock.Of<ILogger<ScreenwriterAgent>>());
        services.AddSingleton(Mock.Of<ILogger<VisualDirectorAgent>>());
        services.AddSingleton(Mock.Of<ILogger<CriticAgent>>());
        services.AddSingleton(Mock.Of<ILogger<AgentOrchestrator>>());
        services.AddSingleton(Mock.Of<Aura.Core.AI.Validation.ScriptSchemaValidator>());

        // Act
        services.AddAgentServices(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ScreenwriterAgent>());
        Assert.NotNull(provider.GetService<VisualDirectorAgent>());
        Assert.NotNull(provider.GetService<CriticAgent>());
        Assert.NotNull(provider.GetService<AgentOrchestrator>());
        
        var options = provider.GetService<IOptions<AgenticModeOptions>>();
        Assert.NotNull(options);
        Assert.True(options.Value.Enabled);
        Assert.Equal(3, options.Value.MaxIterations);
        Assert.Equal(180, options.Value.TimeoutPerIterationSeconds);
        Assert.True(options.Value.EnableLogging);
        Assert.True(options.Value.FallbackToSinglePass);
    }

    [Fact]
    public async Task ScriptPipeline_UsesAgenticMode_WhenEnabled()
    {
        // Arrange
        var mockOrchestrator = new Mock<AgentOrchestrator>();
        var mockLogger = Mock.Of<ILogger<Aura.Core.AI.ScriptGenerationPipeline>>();
        var mockLlmProvider = Mock.Of<ILlmProvider>();
        var mockValidator = Mock.Of<Aura.Core.AI.Validation.ScriptSchemaValidator>();
        var mockFallbackGenerator = Mock.Of<Aura.Core.AI.Templates.FallbackScriptGenerator>();
        
        var options = Options.Create(new AgenticModeOptions { Enabled = true });
        var pipeline = new Aura.Core.AI.ScriptGenerationPipeline(
            mockLlmProvider,
            mockValidator,
            mockFallbackGenerator,
            mockLogger,
            mockOrchestrator.Object,
            options);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var scriptDocument = new ScriptDocument("Test script");
        var performanceReport = new Aura.Core.AI.Agents.Telemetry.AgentPerformanceReport
        {
            TotalIterations = 1,
            ApprovedOnIteration = 1,
            TotalProcessingTime = TimeSpan.FromSeconds(1),
            TimePerAgent = new Dictionary<string, TimeSpan>(),
            IterationHistory = new List<Aura.Core.AI.Agents.Telemetry.IterationRecord>(),
            InvocationCount = 0
        };
        var result = new AgentOrchestratorResult(
            scriptDocument,
            new List<VisualPrompt>(),
            new List<AgentIteration>(),
            true,
            ScriptId: Guid.NewGuid().ToString(),
            CorrelationId: Guid.NewGuid().ToString(),
            PerformanceReport: performanceReport
        );

        mockOrchestrator
            .Setup(x => x.GenerateAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var pipelineResult = await pipeline.GenerateAsync(brief, spec, useAgenticMode: true, CancellationToken.None);

        // Assert
        Assert.NotNull(pipelineResult);
        Assert.Equal("Test script", pipelineResult.Script);
        mockOrchestrator.Verify(
            x => x.GenerateAsync(brief, spec, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScriptPipeline_FallsBackToSinglePass_WhenAgenticModeDisabled()
    {
        // Arrange
        var mockLlm = new Mock<ILlmProvider>();
        var mockLogger = Mock.Of<ILogger<Aura.Core.AI.ScriptGenerationPipeline>>();
        var mockValidator = new Mock<Aura.Core.AI.Validation.ScriptSchemaValidator>();
        var mockFallbackGenerator = Mock.Of<Aura.Core.AI.Templates.FallbackScriptGenerator>();
        
        var options = Options.Create(new AgenticModeOptions { Enabled = false });
        var pipeline = new Aura.Core.AI.ScriptGenerationPipeline(
            mockLlm.Object,
            mockValidator.Object,
            mockFallbackGenerator,
            mockLogger,
            Mock.Of<AgentOrchestrator>(),
            options);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        mockLlm
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Single-pass script");

        var validationResult = new Aura.Core.AI.Validation.ScriptSchemaValidator.ValidationResult(
            IsValid: true,
            Errors: new List<string>(),
            QualityScore: 0.8,
            Metrics: new Aura.Core.AI.Validation.ScriptSchemaValidator.ScriptMetrics(
                SceneCount: 3,
                TotalCharacters: 100,
                AverageSceneLength: 33,
                HasIntroduction: true,
                HasConclusion: true,
                ReadabilityScore: 0.9
            )
        );
        mockValidator
            .Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<Brief>(), It.IsAny<PlanSpec>()))
            .Returns(validationResult);

        // Act
        var result = await pipeline.GenerateAsync(brief, spec, useAgenticMode: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        mockLlm.Verify(
            x => x.DraftScriptAsync(brief, spec, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScriptPipeline_FallsBackToSinglePass_WhenAgenticModeFails_AndFallbackEnabled()
    {
        // Arrange
        var mockLlm = new Mock<ILlmProvider>();
        var mockLogger = Mock.Of<ILogger<Aura.Core.AI.ScriptGenerationPipeline>>();
        var mockValidator = new Mock<Aura.Core.AI.Validation.ScriptSchemaValidator>();
        var mockFallbackGenerator = Mock.Of<Aura.Core.AI.Templates.FallbackScriptGenerator>();
        var mockOrchestrator = new Mock<AgentOrchestrator>();
        
        var options = Options.Create(new AgenticModeOptions 
        { 
            Enabled = true,
            FallbackToSinglePass = true 
        });
        var pipeline = new Aura.Core.AI.ScriptGenerationPipeline(
            mockLlm.Object,
            mockValidator.Object,
            mockFallbackGenerator,
            mockLogger,
            mockOrchestrator.Object,
            options);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        // Agentic mode fails
        mockOrchestrator
            .Setup(x => x.GenerateAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Agentic mode failed"));

        // Single-pass succeeds
        mockLlm
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Fallback script");

        var validationResult = new Aura.Core.AI.Validation.ScriptSchemaValidator.ValidationResult(
            IsValid: true,
            Errors: new List<string>(),
            QualityScore: 0.8,
            Metrics: new Aura.Core.AI.Validation.ScriptSchemaValidator.ScriptMetrics(
                SceneCount: 3,
                TotalCharacters: 100,
                AverageSceneLength: 33,
                HasIntroduction: true,
                HasConclusion: true,
                ReadabilityScore: 0.9
            )
        );
        mockValidator
            .Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<Brief>(), It.IsAny<PlanSpec>()))
            .Returns(validationResult);

        // Act
        var result = await pipeline.GenerateAsync(brief, spec, useAgenticMode: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        mockOrchestrator.Verify(
            x => x.GenerateAsync(brief, spec, It.IsAny<CancellationToken>()),
            Times.Once);
        mockLlm.Verify(
            x => x.DraftScriptAsync(brief, spec, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

