using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ollama;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Tools;

/// <summary>
/// Tool for retrieving research data to enhance script generation
/// </summary>
public class ScriptResearchTool : IToolExecutor
{
    private readonly ILogger<ScriptResearchTool> _logger;

    public string Name => "get_research_data";

    public ScriptResearchTool(ILogger<ScriptResearchTool> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the tool definition for Ollama
    /// </summary>
    public OllamaToolDefinition GetToolDefinition()
    {
        return new OllamaToolDefinition
        {
            Type = "function",
            Function = new OllamaFunctionDefinition
            {
                Name = Name,
                Description = "Get research data about a topic to enhance script content with factual information and key points",
                Parameters = new OllamaFunctionParameters
                {
                    Type = "object",
                    Properties = new Dictionary<string, OllamaPropertyDefinition>
                    {
                        ["topic"] = new OllamaPropertyDefinition
                        {
                            Type = "string",
                            Description = "The topic to research (e.g., 'quantum computing', 'climate change', 'machine learning')"
                        },
                        ["depth"] = new OllamaPropertyDefinition
                        {
                            Type = "string",
                            Description = "How detailed the research should be: 'basic' for overview, 'detailed' for in-depth information",
                            Enum = new List<string> { "basic", "detailed" }
                        }
                    },
                    Required = new List<string> { "topic" }
                }
            }
        };
    }

    /// <summary>
    /// Execute the research tool
    /// </summary>
    public async Task<string> ExecuteAsync(string arguments, CancellationToken ct)
    {
        _logger.LogInformation("Executing research tool with arguments: {Arguments}", arguments);

        try
        {
            var args = JsonSerializer.Deserialize<ResearchArguments>(arguments, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (args == null || string.IsNullOrWhiteSpace(args.Topic))
            {
                return JsonSerializer.Serialize(new ResearchResult
                {
                    Success = false,
                    Error = "Topic parameter is required"
                });
            }

            var depth = args.Depth?.ToLowerInvariant() ?? "basic";
            
            _logger.LogInformation("Researching topic '{Topic}' with depth '{Depth}'", args.Topic, depth);

            var result = await PerformResearchAsync(args.Topic, depth, ct).ConfigureAwait(false);

            _logger.LogInformation("Research completed for topic '{Topic}'. Found {FactCount} key facts", 
                args.Topic, result.KeyFacts.Count);

            return JsonSerializer.Serialize(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse research tool arguments");
            return JsonSerializer.Serialize(new ResearchResult
            {
                Success = false,
                Error = "Invalid arguments format"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing research tool");
            return JsonSerializer.Serialize(new ResearchResult
            {
                Success = false,
                Error = "Internal error during research"
            });
        }
    }

    /// <summary>
    /// Perform the actual research (internal implementation)
    /// </summary>
    private async Task<ResearchResult> PerformResearchAsync(string topic, string depth, CancellationToken ct)
    {
        await Task.Delay(50, ct).ConfigureAwait(false);

        var isDetailed = depth == "detailed";
        var factCount = isDetailed ? 8 : 4;

        var result = new ResearchResult
        {
            Success = true,
            Topic = topic,
            Depth = depth,
            KeyFacts = GenerateKeyFacts(topic, factCount),
            Summary = GenerateSummary(topic, isDetailed),
            RelevantStatistics = isDetailed ? GenerateStatistics(topic) : new List<string>(),
            Sources = GenerateSources(topic, isDetailed ? 3 : 2)
        };

        return result;
    }

    private List<string> GenerateKeyFacts(string topic, int count)
    {
        var facts = new List<string>();
        var topicLower = topic.ToLowerInvariant();

        if (topicLower.Contains("quantum"))
        {
            facts.Add("Quantum computing uses quantum bits (qubits) that can exist in multiple states simultaneously");
            facts.Add("Quantum computers can solve certain problems exponentially faster than classical computers");
            facts.Add("Major tech companies like IBM, Google, and Microsoft are investing heavily in quantum research");
            facts.Add("Quantum supremacy was first claimed by Google in 2019 with their Sycamore processor");
            if (count > 4)
            {
                facts.Add("Quantum computers require extremely cold temperatures near absolute zero to operate");
                facts.Add("Error correction is one of the biggest challenges in quantum computing");
                facts.Add("Applications include cryptography, drug discovery, and optimization problems");
                facts.Add("Quantum entanglement allows qubits to be correlated in ways impossible for classical bits");
            }
        }
        else if (topicLower.Contains("ai") || topicLower.Contains("artificial intelligence") || topicLower.Contains("machine learning"))
        {
            facts.Add("AI can process and analyze vast amounts of data faster than humans");
            facts.Add("Machine learning algorithms improve their performance through experience");
            facts.Add("AI is being used in healthcare, finance, transportation, and many other industries");
            facts.Add("Deep learning models use neural networks inspired by the human brain");
            if (count > 4)
            {
                facts.Add("Natural language processing enables AI to understand and generate human language");
                facts.Add("Computer vision allows AI systems to interpret and understand visual information");
                facts.Add("Reinforcement learning trains AI through trial and error with rewards");
                facts.Add("Ethical concerns include bias, privacy, and the potential impact on employment");
            }
        }
        else
        {
            for (int i = 1; i <= count; i++)
            {
                facts.Add($"Key fact {i} about {topic}: Important information that provides context and depth");
            }
        }

        return facts.GetRange(0, Math.Min(count, facts.Count));
    }

    private string GenerateSummary(string topic, bool isDetailed)
    {
        if (isDetailed)
        {
            return $"{topic} is a complex and evolving field with significant implications across multiple domains. " +
                   $"Current research and development show promising advances, though challenges remain. " +
                   $"Understanding the fundamental concepts is essential for grasping both the opportunities and limitations.";
        }
        else
        {
            return $"{topic} represents an important area of study and application with growing relevance in today's world.";
        }
    }

    private List<string> GenerateStatistics(string topic)
    {
        return new List<string>
        {
            $"Global market for {topic} expected to reach significant growth by 2030",
            $"Over 70% of organizations are exploring or implementing {topic} solutions",
            $"Research investment in {topic} has increased by 40% in the past 5 years"
        };
    }

    private List<string> GenerateSources(string topic, int count)
    {
        var sources = new List<string>
        {
            "Leading academic research institutions",
            "Industry white papers and technical documentation",
            "Expert consensus reports"
        };
        return sources.GetRange(0, Math.Min(count, sources.Count));
    }

    private class ResearchArguments
    {
        public string Topic { get; set; } = string.Empty;
        public string? Depth { get; set; }
    }

    private class ResearchResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Depth { get; set; } = string.Empty;
        public List<string> KeyFacts { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public List<string> RelevantStatistics { get; set; } = new();
        public List<string> Sources { get; set; } = new();
    }
}
