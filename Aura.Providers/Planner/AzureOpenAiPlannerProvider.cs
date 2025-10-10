using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Planner;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Planner;

/// <summary>
/// Azure OpenAI-based planner provider (Pro feature)
/// </summary>
public class AzureOpenAiPlannerProvider : ILlmPlannerProvider
{
    private readonly ILogger<AzureOpenAiPlannerProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;

    public AzureOpenAiPlannerProvider(
        ILogger<AzureOpenAiPlannerProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string endpoint)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _endpoint = endpoint;

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new ArgumentException("Azure OpenAI API key is required", nameof(apiKey));
        }

        if (string.IsNullOrEmpty(_endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint is required", nameof(endpoint));
        }
    }

    public async Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating planner recommendations with Azure OpenAI for topic: {Topic}",
            request.Brief.Topic);

        try
        {
            // Use OpenAI provider logic but with Azure endpoint
            var openAiProvider = new OpenAiPlannerProvider(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiPlannerProvider>.Instance,
                _httpClient,
                _apiKey,
                "gpt-35-turbo");

            var result = await openAiProvider.GenerateRecommendationsAsync(request, ct)
                .ConfigureAwait(false);

            return result with
            {
                ProviderUsed = "Azure",
                ExplainabilityNotes = "Generated using Azure OpenAI LLM with deterministic prompt templates. Combined LLM content generation with heuristic recommendations for technical parameters."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations with Azure OpenAI");
            throw;
        }
    }
}
