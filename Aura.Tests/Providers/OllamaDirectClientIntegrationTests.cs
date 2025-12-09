using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Aura.Core.Providers;
using Aura.Core.Services.Ideation;
using Aura.Core.Services.Localization;
using Aura.Core.Orchestration;
using Aura.Providers.Llm;
using Aura.Core.Services.Conversation;

namespace Aura.Tests.Providers
{
    public class OllamaDirectClientIntegrationTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath?.TrimEnd('/') ?? string.Empty;

                if (request.Method == HttpMethod.Get && path.EndsWith("/api/version"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { version = "1.0.0" })
                    };
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return Task.FromResult(resp);
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/api/tags"))
                {
                    var payload = new
                    {
                        models = new[] { new { name = "llama3.1" } }
                    };
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(payload)
                    };
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return Task.FromResult(resp);
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/api/generate"))
                {
                    var payload = new
                    {
                        response = "Test response from Ollama",
                        model = "llama3.1",
                        done = true
                    };
                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(payload)
                    };
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return Task.FromResult(resp);
                }

                // Default: NotFound
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        private ServiceProvider BuildServiceProviderWithMockHandler(out MockHttpMessageHandler handler)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddMemoryCache();

            // Configure OllamaSettings explicitly for tests
            services.Configure<OllamaSettings>(options =>
            {
                options.BaseUrl = "http://localhost:11434";
                options.Timeout = TimeSpan.FromMinutes(3);
                options.MaxRetries = 1;
            });

            // Create mock handler and register typed client to use it
            handler = new MockHttpMessageHandler();
            var mockHandler = handler; // Capture for lambda
            services.AddHttpClient<IOllamaDirectClient, OllamaDirectClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434/");
                client.Timeout = TimeSpan.FromMinutes(5);
            })
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

            // Minimal required dependencies used by other tests
            services.AddSingleton<ILlmProvider, RuleBasedLlmProvider>();

            // LlmStageAdapter minimal registration (used by Ideation/Translation factories below)
            services.AddSingleton<Aura.Core.Orchestration.LlmStageAdapter>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Aura.Core.Orchestration.LlmStageAdapter>>();
                var providers = new Dictionary<string, ILlmProvider>
                {
                    { "RuleBased", sp.GetRequiredService<ILlmProvider>() }
                };
                var mockMixer = new Mock<Aura.Core.Orchestrator.ProviderMixer>();
                return new Aura.Core.Orchestration.LlmStageAdapter(logger, providers, mockMixer.Object, null);
            });

            // Minimal Conversation services (for IdeationService construction)
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            services.AddSingleton<Aura.Core.Services.Conversation.ContextPersistence>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Conversation.ContextPersistence>>();
                System.IO.Directory.CreateDirectory(tempPath);
                return new Aura.Core.Services.Conversation.ContextPersistence(logger, tempPath);
            });
            services.AddSingleton<Aura.Core.Services.Conversation.ProjectContextManager>();
            services.AddSingleton<Aura.Core.Services.Conversation.ConversationContextManager>();

            // Register IdeationService factory (mirrors Program.cs pattern)
            services.AddSingleton<Aura.Core.Services.Ideation.IdeationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Ideation.IdeationService>>();
                var llmProvider = sp.GetRequiredService<ILlmProvider>();
                var projectManager = sp.GetRequiredService<Aura.Core.Services.Conversation.ProjectContextManager>();
                var conversationManager = sp.GetRequiredService<Aura.Core.Services.Conversation.ConversationContextManager>();
                var trendingTopicsService = sp.GetService<Aura.Core.Services.Ideation.TrendingTopicsService>();
                var stageAdapter = sp.GetRequiredService<Aura.Core.Orchestration.LlmStageAdapter>();
                var ragContextBuilder = sp.GetService<Aura.Core.Services.RAG.RagContextBuilder>();
                var webSearchService = sp.GetService<Aura.Core.Services.Ideation.WebSearchService>();
                var ollamaDirectClient = sp.GetService<IOllamaDirectClient>();
                return new Aura.Core.Services.Ideation.IdeationService(logger, llmProvider, projectManager, conversationManager, trendingTopicsService, stageAdapter, ragContextBuilder, webSearchService, ollamaDirectClient);
            });

            // Register TranslationService factory
            services.AddSingleton<Aura.Core.Services.Localization.TranslationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Localization.TranslationService>>();
                var llmProvider = sp.GetRequiredService<ILlmProvider>();
                var stageAdapter = sp.GetRequiredService<Aura.Core.Orchestration.LlmStageAdapter>();
                var ollamaDirectClient = sp.GetService<IOllamaDirectClient>();
                return new Aura.Core.Services.Localization.TranslationService(logger, llmProvider, stageAdapter, ollamaDirectClient);
            });

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task OllamaDirectClient_GenerateAsync_Returns_CannedResponse()
        {
            var sp = BuildServiceProviderWithMockHandler(out var handler);

            var client = sp.GetRequiredService<IOllamaDirectClient>();

            var result = await client.GenerateAsync("llama3.1", "hello", cancellationToken: CancellationToken.None);

            Assert.Equal("Test response from Ollama", result);
        }

        [Fact]
        public async Task OllamaDirectClient_IsAvailableAndListModels_ReturnTrueAndModelPresent()
        {
            var sp = BuildServiceProviderWithMockHandler(out var handler);

            var client = sp.GetRequiredService<IOllamaDirectClient>();

            var isAvailable = await client.IsAvailableAsync();
            Assert.True(isAvailable);

            var models = await client.ListModelsAsync();
            Assert.Contains("llama3.1", models);
        }

        [Fact]
        public async Task DI_Registration_Allows_Ideation_and_Translation_to_Resolve_With_OllamaClient()
        {
            using var sp = BuildServiceProviderWithMockHandler(out var handler);

            // Ensure the typed Ollama client resolves and is the expected type
            var resolvedClient = sp.GetService<IOllamaDirectClient>();
            Assert.NotNull(resolvedClient);
            Assert.IsType<OllamaDirectClient>(resolvedClient);

            // Ensure IdeationService and TranslationService resolve
            var ideation = sp.GetService<Aura.Core.Services.Ideation.IdeationService>();
            var translation = sp.GetService<Aura.Core.Services.Localization.TranslationService>();

            Assert.NotNull(ideation);
            Assert.NotNull(translation);

            // Finally: verify that the resolved Ollama client uses our handler by calling IsAvailableAsync
            var isAvailable = await resolvedClient.IsAvailableAsync();
            Assert.True(isAvailable);
        }
    }
}
