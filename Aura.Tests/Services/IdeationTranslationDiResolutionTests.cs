using System;
using Aura.Core.Services.Ideation;
using Aura.Core.Services.Localization;
using Aura.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Tests to verify that IdeationService and TranslationService
/// can be constructed with their IOllamaDirectClient dependencies.
/// </summary>
public class IdeationTranslationDiResolutionTests
{
    [Fact]
    public void IdeationService_CanBeConstructedWithOllamaDirectClient()
    {
        // Arrange - Create mocks for dependencies
        var mockLogger = new Mock<ILogger<IdeationService>>();
        var mockLlmProvider = new Mock<ILlmProvider>();
        var mockProjectManager = new Mock<Aura.Core.Services.Conversation.ProjectContextManager>();
        var mockConversationManager = new Mock<Aura.Core.Services.Conversation.ConversationContextManager>();
        var mockTrendingTopics = new Mock<Aura.Core.Services.Ideation.TrendingTopicsService>();
        var mockStageAdapter = new Mock<Aura.Core.Orchestration.LlmStageAdapter>();
        var mockOllamaClient = new Mock<IOllamaDirectClient>();

        // Act - Create IdeationService with all dependencies including IOllamaDirectClient
        var service = new IdeationService(
            mockLogger.Object,
            mockLlmProvider.Object,
            mockProjectManager.Object,
            mockConversationManager.Object,
            mockTrendingTopics.Object,
            mockStageAdapter.Object,
            ragContextBuilder: null,
            webSearchService: null,
            ollamaDirectClient: mockOllamaClient.Object
        );

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void TranslationService_CanBeConstructedWithOllamaDirectClient()
    {
        // Arrange - Create mocks for dependencies
        var mockLogger = new Mock<ILogger<TranslationService>>();
        var mockLlmProvider = new Mock<ILlmProvider>();
        var mockStageAdapter = new Mock<Aura.Core.Orchestration.LlmStageAdapter>();
        var mockOllamaClient = new Mock<IOllamaDirectClient>();

        // Act - Create TranslationService with all dependencies including IOllamaDirectClient
        var service = new TranslationService(
            mockLogger.Object,
            mockLlmProvider.Object,
            mockStageAdapter.Object,
            ollamaDirectClient: mockOllamaClient.Object
        );

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void IdeationService_DI_Registration_IncludesOllamaDirectClient()
    {
        // Arrange - Build a minimal service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add HTTP client factory
        services.AddHttpClient();
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Register IOllamaDirectClient (copy from Program.cs)
        services.AddHttpClient<IOllamaDirectClient, Aura.Core.Providers.OllamaDirectClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Register minimal required dependencies
        services.AddSingleton<ILlmProvider, Aura.Providers.Llm.RuleBasedLlmProvider>();
        
        // Register context managers with temp directory
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        services.AddSingleton<Aura.Core.Services.Conversation.ContextPersistence>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Conversation.ContextPersistence>>();
            return new Aura.Core.Services.Conversation.ContextPersistence(logger, tempPath);
        });
        services.AddSingleton<Aura.Core.Services.Conversation.ProjectContextManager>();
        services.AddSingleton<Aura.Core.Services.Conversation.ConversationContextManager>();
        
        // Register TrendingTopicsService
        services.AddSingleton<Aura.Core.Services.Ideation.TrendingTopicsService>();
        
        // Register LlmStageAdapter
        services.AddSingleton<Aura.Core.Orchestration.LlmStageAdapter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Orchestration.LlmStageAdapter>>();
            var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
            {
                { "RuleBased", sp.GetRequiredService<ILlmProvider>() }
            };
            var mockMixer = new Mock<Aura.Core.Orchestrator.ProviderMixer>();
            return new Aura.Core.Orchestration.LlmStageAdapter(logger, providers, mockMixer.Object, null);
        });
        
        // Register IdeationService with ollamaDirectClient (THIS IS WHAT WE'RE TESTING)
        services.AddSingleton<IdeationService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IdeationService>>();
            var llmProvider = sp.GetRequiredService<ILlmProvider>();
            var projectManager = sp.GetRequiredService<Aura.Core.Services.Conversation.ProjectContextManager>();
            var conversationManager = sp.GetRequiredService<Aura.Core.Services.Conversation.ConversationContextManager>();
            var trendingTopicsService = sp.GetRequiredService<Aura.Core.Services.Ideation.TrendingTopicsService>();
            var stageAdapter = sp.GetRequiredService<Aura.Core.Orchestration.LlmStageAdapter>();
            var ragContextBuilder = sp.GetService<Aura.Core.Services.RAG.RagContextBuilder>();
            var webSearchService = sp.GetService<Aura.Core.Services.Ideation.WebSearchService>();
            var ollamaDirectClient = sp.GetService<IOllamaDirectClient>();
            return new IdeationService(logger, llmProvider, projectManager, conversationManager, 
                trendingTopicsService, stageAdapter, ragContextBuilder, webSearchService, ollamaDirectClient);
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act - Try to resolve IdeationService
        var service = serviceProvider.GetService<IdeationService>();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void TranslationService_DI_Registration_IncludesOllamaDirectClient()
    {
        // Arrange - Build a minimal service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add HTTP client factory
        services.AddHttpClient();
        
        // Register IOllamaDirectClient (copy from Program.cs)
        services.AddHttpClient<IOllamaDirectClient, Aura.Core.Providers.OllamaDirectClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Register minimal required dependencies
        services.AddSingleton<ILlmProvider, Aura.Providers.Llm.RuleBasedLlmProvider>();
        
        // Register LlmStageAdapter
        services.AddSingleton<Aura.Core.Orchestration.LlmStageAdapter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Orchestration.LlmStageAdapter>>();
            var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
            {
                { "RuleBased", sp.GetRequiredService<ILlmProvider>() }
            };
            var mockMixer = new Mock<Aura.Core.Orchestrator.ProviderMixer>();
            return new Aura.Core.Orchestration.LlmStageAdapter(logger, providers, mockMixer.Object, null);
        });
        
        // Register TranslationService with ollamaDirectClient (THIS IS WHAT WE'RE TESTING)
        services.AddSingleton<TranslationService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TranslationService>>();
            var llmProvider = sp.GetRequiredService<ILlmProvider>();
            var stageAdapter = sp.GetRequiredService<Aura.Core.Orchestration.LlmStageAdapter>();
            var ollamaDirectClient = sp.GetService<IOllamaDirectClient>();
            return new TranslationService(logger, llmProvider, stageAdapter, ollamaDirectClient);
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act - Try to resolve TranslationService
        var service = serviceProvider.GetService<TranslationService>();

        // Assert
        Assert.NotNull(service);
    }
}
