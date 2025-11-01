using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Manages topic-based content filtering
/// </summary>
public class TopicFilterManager
{
    private readonly ILogger<TopicFilterManager> _logger;

    public TopicFilterManager(ILogger<TopicFilterManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect topics in content
    /// </summary>
    public Task<List<DetectedTopic>> DetectTopicsAsync(
        string content,
        CancellationToken ct = default)
    {
        var topics = new List<DetectedTopic>();

        try
        {
            var lowerContent = content.ToLowerInvariant();
            
            var topicKeywords = new Dictionary<string, string[]>
            {
                ["Politics"] = new[] { "politic", "election", "vote", "government", "president", "congress" },
                ["Religion"] = new[] { "religion", "church", "faith", "god", "prayer", "worship" },
                ["Violence"] = new[] { "violence", "fight", "attack", "war", "weapon" },
                ["Drugs"] = new[] { "drug", "marijuana", "cocaine", "narcotic", "substance" },
                ["Sexuality"] = new[] { "sexual", "sex", "intimate", "erotic" },
                ["Gambling"] = new[] { "gamble", "bet", "casino", "lottery" },
                ["Abortion"] = new[] { "abortion", "pro-life", "pro-choice" },
                ["Guns"] = new[] { "gun", "firearm", "rifle", "pistol", "weapon" },
                ["Climate"] = new[] { "climate", "global warming", "carbon", "emissions" }
            };

            foreach (var (topic, keywords) in topicKeywords)
            {
                var matchCount = keywords.Count(keyword => 
                    lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (matchCount > 0)
                {
                    var confidence = Math.Min(matchCount * 0.2, 1.0);
                    topics.Add(new DetectedTopic
                    {
                        Topic = topic,
                        Confidence = confidence,
                        MatchedKeywords = keywords.Where(k => 
                            lowerContent.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList()
                    });
                }
            }

            _logger.LogInformation("Detected {Count} topics in content", topics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting topics");
        }

        return Task.FromResult(topics);
    }

    /// <summary>
    /// Check if topic should be blocked
    /// </summary>
    public bool ShouldBlockTopic(
        DetectedTopic topic,
        List<TopicFilter> filters,
        string? context = null)
    {
        var filter = filters.FirstOrDefault(f => 
            f.Topic.Equals(topic.Topic, StringComparison.OrdinalIgnoreCase));

        if (filter == null)
            return false;

        if (!filter.IsBlocked)
            return false;

        if (topic.Confidence < filter.ConfidenceThreshold)
            return false;

        if (!string.IsNullOrEmpty(context) && filter.AllowedContexts.Any(c => 
            context.Contains(c, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("Topic {Topic} allowed in context: {Context}", topic.Topic, context);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get common topic categories
    /// </summary>
    public List<string> GetCommonTopics()
    {
        return new List<string>
        {
            "Politics",
            "Religion",
            "Violence",
            "Drugs",
            "Sexuality",
            "Gambling",
            "Abortion",
            "Guns",
            "Climate Change",
            "LGBTQ+",
            "Terrorism",
            "Suicide",
            "Mental Health",
            "Eating Disorders",
            "Self-Harm"
        };
    }
}

/// <summary>
/// Represents a detected topic in content
/// </summary>
public class DetectedTopic
{
    public string Topic { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> MatchedKeywords { get; set; } = new();
}
