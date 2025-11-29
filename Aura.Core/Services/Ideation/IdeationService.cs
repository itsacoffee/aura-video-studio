using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Ideation;
using Aura.Core.Models.RAG;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Aura.Core.Services.Conversation;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Ideation;

/// <summary>
/// Service for AI-powered ideation and brainstorming
/// Now uses LlmStageAdapter for unified orchestration
/// </summary>
public class IdeationService
{
    private readonly ILogger<IdeationService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly ProjectContextManager _projectManager;
    private readonly ConversationContextManager _conversationManager;
    private readonly TrendingTopicsService _trendingTopicsService;
    private readonly RagContextBuilder? _ragContextBuilder;
    private readonly WebSearchService? _webSearchService;

    private const int MinConceptCount = 3;
    private const int MaxConceptCount = 9;
    private const int MaxTopicLength = 500;
    private const int MaxPromptLength = 10000;

    public IdeationService(
        ILogger<IdeationService> logger,
        ILlmProvider llmProvider,
        ProjectContextManager projectManager,
        ConversationContextManager conversationManager,
        TrendingTopicsService trendingTopicsService,
        LlmStageAdapter? stageAdapter = null,
        RagContextBuilder? ragContextBuilder = null,
        WebSearchService? webSearchService = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _projectManager = projectManager;
        _conversationManager = conversationManager;
        _trendingTopicsService = trendingTopicsService;
        _stageAdapter = stageAdapter;
        _ragContextBuilder = ragContextBuilder;
        _webSearchService = webSearchService;
    }

    /// <summary>
    /// Generate creative concept variations from a topic
    /// </summary>
    public async Task<BrainstormResponse> BrainstormConceptsAsync(
        BrainstormRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic cannot be null or empty", nameof(request));
        }

        // Validate topic length
        if (request.Topic.Length > MaxTopicLength)
        {
            throw new ArgumentException($"Topic length exceeds maximum of {MaxTopicLength} characters", nameof(request));
        }

        _logger.LogInformation("Brainstorming concepts for topic: {Topic} (length: {Length})",
            request.Topic, request.Topic.Length);

        var desiredConceptCount = Math.Clamp(
            request.ConceptCount ?? MinConceptCount,
            MinConceptCount,
            MaxConceptCount);

        // Try to retrieve RAG context for the topic to provide more specific information
        RagContext? ragContext = null;
        if (_ragContextBuilder != null && request.RagConfiguration != null && request.RagConfiguration.Enabled)
        {
            try
            {
                var ragConfig = new RagConfig
                {
                    Enabled = true,
                    TopK = request.RagConfiguration.TopK,
                    MinimumScore = request.RagConfiguration.MinimumScore,
                    MaxContextTokens = request.RagConfiguration.MaxContextTokens,
                    IncludeCitations = request.RagConfiguration.IncludeCitations
                };
                ragContext = await _ragContextBuilder.BuildContextAsync(request.Topic, ragConfig, ct).ConfigureAwait(false);
                if (ragContext.Chunks.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} RAG chunks for topic: {Topic} (TopK={TopK}, MinScore={MinScore})",
                        ragContext.Chunks.Count, request.Topic, ragConfig.TopK, ragConfig.MinimumScore);
                }
                else
                {
                    _logger.LogDebug("No RAG chunks found for topic: {Topic} with current configuration", request.Topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve RAG context for ideation, continuing without RAG");
            }
        }
        else if (request.RagConfiguration != null && !request.RagConfiguration.Enabled)
        {
            _logger.LogDebug("RAG is explicitly disabled for ideation topic: {Topic}", request.Topic);
        }

        try
        {
            // Build expert-level system prompt for high-value concept generation
            // CRITICAL: This prompt MUST produce specific, actionable, unique concepts - NO GENERIC PLACEHOLDERS
            var systemPrompt = @"You are a world-class content strategist with 10+ years of experience helping creators achieve viral success on YouTube, TikTok, and Instagram. You have deep expertise in audience psychology, viral mechanics, and content differentiation strategies.

YOUR MISSION: Generate SPECIFIC, ACTIONABLE, and UNIQUE video concept variations that will genuinely help creators stand out.

ABSOLUTE REQUIREMENTS - NO EXCEPTIONS:
1. EVERY field must be SPECIFIC to the exact topic provided. Generic phrases like 'engaging content', 'valuable information', 'This approach provides unique value through its specific perspective', 'Introduction to the topic', 'Key aspects of [topic]', or 'Practical applications' are FORBIDDEN.
2. Each concept must have a genuinely different angle that could compete in a crowded market.
3. Talking points and insights must be things a creator can immediately use - NOT generic advice.
4. Hook must use a specific psychological trigger (curiosity gap, fear of missing out, controversy, pattern interrupt).
5. uniqueValue and contentGap must explain how this stands out from existing content with SPECIFIC examples.

You MUST respond with ONLY valid JSON in the following format (no markdown, no code blocks, no explanations):
{
  ""concepts"": [
    {
      ""title"": ""Specific, curiosity-driven title using viral formula - MUST be unique to this topic"",
      ""description"": ""3-4 sentences with SPECIFIC tactics and insights, not vague concepts. Include what makes this approach unique and why it works. MUST reference the actual topic specifically."",
      ""angle"": ""Unique perspective (Tutorial, Narrative, Case Study, Comparison, Documentary, Behind-the-Scenes, Expert Analysis, Deep Dive)"",
      ""targetAudience"": ""Detailed persona with specific pain points and desires - not just 'beginners' but 'entrepreneurs struggling to get their first 1000 followers' - MUST be specific to this topic"",
      ""hook"": ""Specific viral hook (15 seconds max) with psychological trigger - curiosity gap, pattern interrupt, or controversy - MUST mention the topic specifically"",
      ""uniqueValue"": ""What makes this concept stand out from the 1000 other videos on this topic - MUST be specific and different from generic statements"",
      ""contentGap"": ""What competitors are missing that this video addresses - MUST be specific to this topic"",
      ""keyInsights"": [""Specific actionable insight 1 related to the topic"", ""Specific insight 2 related to the topic"", ""Specific insight 3 related to the topic""],
      ""talkingPoints"": [""Specific point with example 1 related to the topic"", ""Point 2 related to the topic"", ""Point 3 related to the topic"", ""Point 4 related to the topic"", ""Point 5 related to the topic""],
      ""visualSuggestions"": [""Specific visual idea 1 related to the topic"", ""Visual idea 2 related to the topic""],
      ""monetizationPotential"": ""High/Medium/Low - with specific reasoning about sponsorship, affiliate, or product opportunities for this topic"",
      ""viralityScore"": 85,
      ""appealScore"": 90,
      ""pros"": [""Specific advantage with data or reasoning for this topic"", ""Advantage 2 for this topic"", ""Advantage 3 for this topic""],
      ""cons"": [""Specific challenge with mitigation strategy for this topic"", ""Challenge 2 for this topic""]
    }
  ]
}

FORBIDDEN PHRASES (DO NOT USE):
- 'This approach provides unique value through its specific perspective'
- 'Introduction to [topic]' or 'Introduction to how to [topic]'
- 'Key aspects of [topic]' or 'Key aspects of [word]'
- 'Practical applications' (without specific examples)
- 'Engaging and accessible format'
- Any generic placeholder text

You MUST analyze the topic deeply and provide concepts that are genuinely useful for brainstorming. If you cannot provide specific, unique concepts, you must still try your best - do NOT use placeholder text.";

            // Build user prompt with topic and context
            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine($"Generate {desiredConceptCount} HIGH-VALUE, DIFFERENTIATED video concepts for:");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine($"TOPIC: {request.Topic}");
            if (!string.IsNullOrEmpty(request.Audience))
            {
                userPromptBuilder.AppendLine($"TARGET AUDIENCE HINT: {request.Audience}");
            }
            if (!string.IsNullOrEmpty(request.Tone))
            {
                userPromptBuilder.AppendLine($"DESIRED TONE: {request.Tone}");
            }
            if (!string.IsNullOrEmpty(request.Platform))
            {
                userPromptBuilder.AppendLine($"PLATFORM: {request.Platform}");
            }
            userPromptBuilder.AppendLine();

            // Include RAG context if available
            if (ragContext != null && ragContext.Chunks.Count > 0)
            {
                userPromptBuilder.AppendLine("=== RELEVANT CONTEXT FROM KNOWLEDGE BASE ===");
                foreach (var chunk in ragContext.Chunks.Take(5))
                {
                    userPromptBuilder.AppendLine(chunk.Content);
                    if (chunk.CitationNumber > 0)
                    {
                        userPromptBuilder.AppendLine($"[Source: {chunk.Source}]");
                    }
                    userPromptBuilder.AppendLine();
                }
            }

            // Add trending topics if available
            if (_trendingTopicsService != null)
            {
                try
                {
                    var trendingTopics = await _trendingTopicsService.GetTrendingTopicsAsync(
                        request.Topic,
                        maxResults: 5,
                        forceRefresh: false,
                        ct).ConfigureAwait(false);

                    if (trendingTopics.Count > 0)
                    {
                        userPromptBuilder.AppendLine("=== TRENDING CONTEXT (use for relevance) ===");
                        foreach (var topic in trendingTopics.Take(3))
                        {
                            userPromptBuilder.AppendLine($"- {topic.Topic} (Trend Score: {topic.TrendScore:F1}/100)");
                        }
                        userPromptBuilder.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve trending topics for ideation, continuing without trend data");
                }
            }

            userPromptBuilder.AppendLine("=== CRITICAL REQUIREMENTS ===");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("For EACH concept, you MUST provide:");
            userPromptBuilder.AppendLine("1. TITLE: Use a viral formula (curiosity gap, 'X vs Y', 'The truth about...', numbered list)");
            userPromptBuilder.AppendLine("2. DESCRIPTION: 100+ characters with SPECIFIC tactics, not vague promises");
            userPromptBuilder.AppendLine("3. HOOK: Specific 15-second opening that creates immediate curiosity");
            userPromptBuilder.AppendLine("4. UNIQUE VALUE: What makes THIS concept different from the 1000 other videos on this topic");
            userPromptBuilder.AppendLine("5. CONTENT GAP: What are competitors MISSING that this addresses");
            userPromptBuilder.AppendLine("6. KEY INSIGHTS: 3 specific, actionable takeaways (not generic advice)");
            userPromptBuilder.AppendLine("7. VIRALITY SCORE: 60-95 based on shareability, controversy potential, and trending alignment");
            userPromptBuilder.AppendLine("8. MONETIZATION POTENTIAL: How can this be monetized (sponsorship, affiliate, products)");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("BAD EXAMPLES (DO NOT DO THIS):");
            userPromptBuilder.AppendLine("- 'A video about the topic' (too vague)");
            userPromptBuilder.AppendLine("- 'Beginners interested in learning' (not specific)");
            userPromptBuilder.AppendLine("- 'Key concepts and tips' (generic talking points)");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("GOOD EXAMPLES:");
            userPromptBuilder.AppendLine("- Title: 'I Tried X for 30 Days - Here's What Happened'");
            userPromptBuilder.AppendLine("- Audience: 'Side-hustlers aged 25-35 who tried and failed at their first online business'");
            userPromptBuilder.AppendLine("- Insight: 'Most creators fail because they optimize for views instead of watch time - here's the data'");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine($"Generate exactly {desiredConceptCount} concepts now. Return ONLY valid JSON.");

            var userPrompt = userPromptBuilder.ToString();

            // Validate prompt length
            var totalPromptLength = systemPrompt.Length + userPrompt.Length;
            if (totalPromptLength > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    totalPromptLength, MaxPromptLength);
                var maxUserPromptLength = MaxPromptLength - systemPrompt.Length - 100; // Leave buffer
                if (maxUserPromptLength > 0)
                {
                    userPrompt = userPrompt.Substring(0, Math.Min(maxUserPromptLength, userPrompt.Length));
                }
            }

            // Call with retry logic
            const int maxRetries = 3;
            string? jsonResponse = null;
            Exception? lastException = null;
            bool lastAttemptHadGenericContent = false;

            // Create LLM parameters with JSON format for ideation (requires structured output)
            // This ensures Ollama and other providers return valid JSON
            var ideationParams = request.LlmParameters != null
                ? request.LlmParameters with { ResponseFormat = "json" }
                : new LlmParameters(ResponseFormat: "json");

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger.LogInformation("Retrying ideation (attempt {Attempt}/{Max}) for topic: {Topic}",
                            attempt + 1, maxRetries + 1, request.Topic);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct); // Exponential backoff
                    }

                    // Strengthen prompt on retries if previous attempt had generic content
                    var currentSystemPrompt = systemPrompt;
                    var currentUserPrompt = userPrompt;
                    if (attempt > 0 && lastAttemptHadGenericContent)
                    {
                        _logger.LogInformation("Strengthening prompt for retry attempt {Attempt} due to generic content detection",
                            attempt + 1);

                        // Add more explicit instructions to system prompt
                        currentSystemPrompt = systemPrompt + @"

CRITICAL REMINDER FOR THIS RETRY: The previous attempt generated generic placeholder content.
You MUST generate SPECIFIC, DETAILED concepts that are directly related to the topic.
DO NOT use any generic phrases or placeholders. Every field must contain topic-specific information.";

                        // Add explicit examples and warnings to user prompt
                        var topicName = request.Topic;
                        currentUserPrompt = userPrompt + $@"

=== CRITICAL: PREVIOUS ATTEMPT FAILED ===
The previous attempt generated generic placeholder content. This retry MUST succeed.

REQUIREMENTS FOR THIS RETRY:
1. Every description must be AT LEAST 100 characters and SPECIFIC to '{topicName}'
2. Every talking point must mention '{topicName}' explicitly or reference specific aspects of it
3. Every pro and con must be SPECIFIC to this topic, not generic statements
4. The hook must be compelling and SPECIFIC to '{topicName}'
5. DO NOT use phrases like 'This approach', 'Introduction to', 'Key aspects' without specific details

EXAMPLE OF WHAT TO AVOID:
❌ Description: 'This approach provides unique value through its specific perspective'
❌ Talking Point: 'Introduction to the topic'
❌ Pro: 'Engaging and accessible format'

EXAMPLE OF WHAT TO DO:
✅ Description: 'A detailed tutorial on {topicName} that covers [specific aspect 1], [specific aspect 2], and [specific aspect 3], with step-by-step demonstrations'
✅ Talking Point: 'Step 1: [Specific action related to {topicName}] - [Why this matters]'
✅ Pro: 'Visual demonstrations make [specific technique for {topicName}] easy to understand for beginners'

Generate SPECIFIC content NOW. Do not use placeholders.";
                    }

                    // Verify provider type and log detailed information
                    var providerType = _llmProvider.GetType();
                    var providerTypeName = providerType.Name;
                    var isComposite = providerTypeName == "CompositeLlmProvider";

                    _logger.LogInformation(
                        "Calling LLM for ideation (Attempt {Attempt}/{Max}, Provider: {Provider}, Topic: {Topic})",
                        attempt + 1, maxRetries + 1, providerTypeName, request.Topic);

                    // CRITICAL: Verify we're not using RuleBased or mock providers
                    if (providerTypeName.Contains("RuleBased", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "CRITICAL: Ideation is using RuleBased provider instead of real LLM (Ollama). " +
                            "This will produce low-quality placeholder concepts. Check Ollama is running and configured.");
                    }
                    else if (providerTypeName.Contains("Mock", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "CRITICAL: Ideation is using Mock provider. This should never happen in production. " +
                            "Check LLM provider configuration.");
                    }
                    else if (isComposite)
                    {
                        // For CompositeLlmProvider, log that it will select the best available provider
                        _logger.LogInformation(
                            "Using CompositeLlmProvider - it will select the best available provider (Ollama if available)");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Using direct provider: {Provider} - this should be Ollama or another real LLM provider",
                            providerTypeName);
                    }

                    var callStartTime = DateTime.UtcNow;

                    // Try direct Ollama call first (similar to script generation) for better reliability
                    // This bypasses CompositeLlmProvider fallback logic and ensures we use Ollama when available
                    try
                    {
                        jsonResponse = await GenerateWithOllamaDirectAsync(
                            currentSystemPrompt,
                            currentUserPrompt,
                            ideationParams,
                            request,
                            ct).ConfigureAwait(false);
                        _logger.LogInformation("Successfully used direct Ollama API call for ideation");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Ollama") || ex.Message.Contains("not available") || ex.Message.Contains("cannot be used"))
                    {
                        // Ollama not available or direct call failed - fall back to CompositeLlmProvider
                        _logger.LogInformation("Direct Ollama call not available, falling back to CompositeLlmProvider: {Error}", ex.Message);
                        jsonResponse = await _llmProvider.GenerateChatCompletionAsync(
                            currentSystemPrompt,
                            currentUserPrompt,
                            ideationParams,
                            ct).ConfigureAwait(false);
                    }

                    var callDuration = DateTime.UtcNow - callStartTime;

                    // Log provider utilization verification
                    _logger.LogInformation(
                        "LLM call completed: Provider={Provider}, Duration={Duration}ms, ResponseLength={Length} chars. " +
                        "If Ollama is running, you should see CPU/GPU utilization in system monitor.",
                        providerTypeName, callDuration.TotalMilliseconds, jsonResponse?.Length ?? 0);

                    if (string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        _logger.LogError("LLM returned empty response (Attempt {Attempt}, Duration: {Duration}ms)",
                            attempt + 1, callDuration.TotalMilliseconds);
                        throw new InvalidOperationException("LLM returned empty response");
                    }

                    _logger.LogInformation(
                        "LLM returned response (Attempt {Attempt}, Duration: {Duration}ms, Length: {Length} chars, Preview: {Preview})",
                        attempt + 1, callDuration.TotalMilliseconds, jsonResponse.Length,
                        jsonResponse.Substring(0, Math.Min(200, jsonResponse.Length)));

                    // Clean response before parsing (remove markdown code blocks)
                    var cleanedResponse = CleanJsonResponse(jsonResponse);

                    // Validate JSON structure before breaking
                    var testDoc = JsonDocument.Parse(cleanedResponse);
                    if (testDoc.RootElement.TryGetProperty("concepts", out var conceptsArray) &&
                        conceptsArray.ValueKind == JsonValueKind.Array &&
                        conceptsArray.GetArrayLength() > 0)
                    {
                        // Quick quality check - reject if concepts have obvious placeholder descriptions
                        // Use the same comprehensive validation logic as final validation to catch all generic patterns
                        var containsGenericContent = false;
                        var genericContentDetails = new List<string>();
                        var topicName = request.Topic;

                        foreach (var concept in conceptsArray.EnumerateArray())
                        {
                            // Check description field
                            if (concept.TryGetProperty("description", out var desc))
                            {
                                var descText = desc.GetString() ?? "";

                                // Check all the same patterns as final validation
                                if (descText.Contains("This approach provides unique value through its specific perspective"))
                                {
                                    containsGenericContent = true;
                                    genericContentDetails.Add($"Description contains placeholder: '{descText.Substring(0, Math.Min(50, descText.Length))}...'");
                                }
                                else if (descText.Contains("Introduction to how to") && descText.Length < 80)
                                {
                                    containsGenericContent = true;
                                    genericContentDetails.Add($"Description too generic/short: '{descText}'");
                                }
                                else if (descText.Contains("Key aspects of") && !descText.Contains(topicName) && descText.Length < 60)
                                {
                                    containsGenericContent = true;
                                    genericContentDetails.Add($"Description contains 'Key aspects of' without topic: '{descText.Substring(0, Math.Min(50, descText.Length))}...'");
                                }
                                else if (descText.Length < 30 && (
                                    descText.Contains("This approach") ||
                                    descText.Contains("Introduction to") ||
                                    descText.Contains("Key aspects")))
                                {
                                    containsGenericContent = true;
                                    genericContentDetails.Add($"Description too short with placeholder phrase: '{descText}'");
                                }
                            }

                            // Check talkingPoints array if it exists
                            if (concept.TryGetProperty("talkingPoints", out var talkingPoints) &&
                                talkingPoints.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var tp in talkingPoints.EnumerateArray())
                                {
                                    var tpText = tp.GetString() ?? "";
                                    if (tpText.Contains("Introduction to") && !tpText.Contains(topicName))
                                    {
                                        containsGenericContent = true;
                                        genericContentDetails.Add($"Talking point contains generic 'Introduction to': '{tpText.Substring(0, Math.Min(50, tpText.Length))}...'");
                                        break; // Found one, no need to check more
                                    }
                                }
                            }

                            // Check pros array if it exists
                            if (concept.TryGetProperty("pros", out var pros) &&
                                pros.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var pro in pros.EnumerateArray())
                                {
                                    var proText = pro.GetString() ?? "";
                                    if (proText == "Engaging and accessible format")
                                    {
                                        containsGenericContent = true;
                                        genericContentDetails.Add($"Pro contains generic placeholder: '{proText}'");
                                        break; // Found one, no need to check more
                                    }
                                }
                            }

                            // If we found generic content in this concept, no need to check others
                            if (containsGenericContent)
                            {
                                break;
                            }
                        }

                        if (containsGenericContent)
                        {
                            lastAttemptHadGenericContent = true;
                            if (attempt < maxRetries)
                            {
                                _logger.LogWarning(
                                    "LLM returned generic/placeholder content (Attempt {Attempt}/{Max}). Details: {Details}. Retrying with stronger prompt",
                                    attempt + 1, maxRetries + 1, string.Join("; ", genericContentDetails));
                                throw new InvalidOperationException("Response contains generic placeholder content - retrying");
                            }
                            else
                            {
                                // Last attempt - log detailed information
                                _logger.LogError(
                                    "LLM returned generic/placeholder content on final attempt. Response length: {Length}, Details: {Details}",
                                    jsonResponse.Length, string.Join("; ", genericContentDetails));
                                // Will be caught by outer validation
                            }
                        }
                        else
                        {
                            lastAttemptHadGenericContent = false;
                            _logger.LogInformation("Successfully generated {Count} concepts for topic: {Topic} (Attempt {Attempt})",
                                conceptsArray.GetArrayLength(), request.Topic, attempt + 1);
                            jsonResponse = cleanedResponse; // Use cleaned response
                            break; // Valid response
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Response missing 'concepts' array or array is empty");
                    }
                }
                catch (JsonException jsonEx)
                {
                    lastException = jsonEx;
                    _logger.LogWarning(jsonEx, "JSON parsing failed (attempt {Attempt}/{Max})", attempt + 1, maxRetries + 1);

                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException(
                            $"LLM returned invalid JSON after {maxRetries + 1} attempts. " +
                            $"Response preview: {jsonResponse?.Substring(0, Math.Min(200, jsonResponse?.Length ?? 0))}",
                            jsonEx);
                    }
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Ideation attempt {Attempt} failed", attempt + 1);
                }
            }

            if (jsonResponse == null)
            {
                throw lastException ?? new InvalidOperationException(
                    $"Failed to generate concepts after {maxRetries + 1} attempts");
            }

            var response = jsonResponse;

            // Parse the response into structured concepts
            var concepts = ParseBrainstormResponse(response, request.Topic, desiredConceptCount);

            // Validate parsed concepts are meaningful (not just fallback placeholders)
            if (concepts.Count == 0)
            {
                _logger.LogError("Failed to parse any concepts from LLM response for topic: {Topic}. Response length: {Length}",
                    request.Topic, response.Length);
                throw new InvalidOperationException(
                    "Failed to generate concepts. The LLM response could not be parsed. Please try again.");
            }

            // Validate concepts are not generic placeholders
            // Focus on actual placeholder phrases rather than strict length requirements
            // Local LLMs like Ollama may produce shorter but valid content
            var hasGenericContent = concepts.Any(c =>
                c.Description.Contains("This approach provides unique value through its specific perspective") ||
                (c.Description.Contains("Introduction to how to") && c.Description.Length < 80) ||
                (c.Description.Contains("Key aspects of") && !c.Description.Contains(request.Topic) && c.Description.Length < 60) ||
                c.TalkingPoints?.Any(tp => tp.Contains("Introduction to") && !tp.Contains(request.Topic)) == true ||
                c.Pros.Any(p => p == "Engaging and accessible format") ||
                // Only flag as generic if description is extremely short AND contains placeholder phrases
                (c.Description.Length < 30 && (
                    c.Description.Contains("This approach") ||
                    c.Description.Contains("Introduction to") ||
                    c.Description.Contains("Key aspects"))));

            if (hasGenericContent)
            {
                // Log detailed diagnostic information
                var providerType = _llmProvider.GetType().Name;
                var genericConcepts = concepts.Where(c =>
                    c.Description.Contains("This approach provides unique value through its specific perspective") ||
                    (c.Description.Contains("Introduction to how to") && c.Description.Length < 80) ||
                    (c.Description.Contains("Key aspects of") && !c.Description.Contains(request.Topic) && c.Description.Length < 60) ||
                    c.TalkingPoints?.Any(tp => tp.Contains("Introduction to") && !tp.Contains(request.Topic)) == true ||
                    c.Pros.Any(p => p == "Engaging and accessible format") ||
                    (c.Description.Length < 30 && (
                        c.Description.Contains("This approach") ||
                        c.Description.Contains("Introduction to") ||
                        c.Description.Contains("Key aspects")))).ToList();

                _logger.LogError(
                    "Parsed concepts contain generic/placeholder content after all retries. " +
                    "Provider: {Provider}, Response length: {Length}, Generic concepts: {GenericCount}/{TotalCount}. " +
                    "Response preview: {Preview}. " +
                    "This indicates the LLM may not be properly configured, the model is too small/weak, or needs different prompting.",
                    providerType,
                    response.Length,
                    genericConcepts.Count,
                    concepts.Count,
                    response.Substring(0, Math.Min(500, response.Length)));

                // Log specific examples of generic content for debugging
                foreach (var concept in genericConcepts.Take(3))
                {
                    _logger.LogWarning(
                        "Generic concept detected - Title: '{Title}', Description: '{Description}' (Length: {Length})",
                        concept.Title,
                        concept.Description.Substring(0, Math.Min(100, concept.Description.Length)),
                        concept.Description.Length);
                }

                // Throw error with more helpful message
                throw new InvalidOperationException(
                    "The LLM generated generic placeholder content instead of specific concepts. " +
                    "This usually means: (1) The LLM provider is not properly configured, (2) The model is not responding correctly, " +
                    "or (3) The prompt needs adjustment. Please check your LLM provider settings and try again.");
            }

            _logger.LogInformation("Successfully generated {Count} concepts for topic: {Topic}",
                concepts.Count, request.Topic);

            return new BrainstormResponse(
                Concepts: concepts,
                OriginalTopic: request.Topic,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Brainstorming operation was cancelled for topic: {Topic}", request.Topic);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error brainstorming concepts for topic: {Topic}", request.Topic);
            throw;
        }
    }

    /// <summary>
    /// Expand brief with AI asking clarifying questions
    /// </summary>
    public async Task<ExpandBriefResponse> ExpandBriefAsync(
        ExpandBriefRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            throw new ArgumentException("ProjectId cannot be null or empty", nameof(request));
        }
        ArgumentNullException.ThrowIfNull(request.CurrentBrief, nameof(request.CurrentBrief));
        if (string.IsNullOrWhiteSpace(request.CurrentBrief.Topic))
        {
            throw new ArgumentException("CurrentBrief.Topic cannot be null or empty", nameof(request));
        }

        _logger.LogInformation("Expanding brief for project: {ProjectId}", request.ProjectId);

        try
        {
            var prompt = BuildExpandBriefPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for expand brief, project: {ProjectId}", request.ProjectId);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            // Store in conversation history
            if (!string.IsNullOrEmpty(request.UserMessage))
            {
                try
                {
                    await _conversationManager.AddMessageAsync(
                        request.ProjectId,
                        "user",
                        request.UserMessage,
                        ct: ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to store user message in conversation history, continuing");
                }
            }

            try
            {
                await _conversationManager.AddMessageAsync(
                    request.ProjectId,
                    "assistant",
                    response,
                    ct: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store assistant message in conversation history, continuing");
            }

            // Parse response for questions or updated brief
            var (questions, aiResponse) = ParseExpandBriefResponse(response);

            _logger.LogInformation("Successfully expanded brief for project: {ProjectId}, generated {QuestionCount} questions",
                request.ProjectId, questions?.Count ?? 0);

            return new ExpandBriefResponse(
                UpdatedBrief: null,  // Will be implemented with more sophisticated parsing
                Questions: questions,
                AiResponse: aiResponse
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Expand brief operation was cancelled for project: {ProjectId}", request.ProjectId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expanding brief for project: {ProjectId}", request.ProjectId);
            throw;
        }
    }

    /// <summary>
    /// Get trending topics for a niche with AI analysis
    /// </summary>
    public async Task<TrendingTopicsResponse> GetTrendingTopicsAsync(
        TrendingTopicsRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // Validate niche length if provided
        if (!string.IsNullOrWhiteSpace(request.Niche) && request.Niche.Length > MaxTopicLength)
        {
            throw new ArgumentException($"Niche length exceeds maximum of {MaxTopicLength} characters", nameof(request));
        }

        // Validate MaxResults
        var maxResults = Math.Clamp(request.MaxResults ?? 10, 1, 100);

        _logger.LogInformation("Analyzing trending topics for niche: {Niche} (max results: {MaxResults})",
            request.Niche ?? "general", maxResults);

        try
        {
            var topics = await _trendingTopicsService.GetTrendingTopicsAsync(
                request.Niche,
                maxResults,
                forceRefresh: false,
                ct).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {Count} trending topics for niche: {Niche}",
                topics.Count, request.Niche ?? "general");

            return new TrendingTopicsResponse(
                Topics: topics,
                AnalyzedAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get trending topics operation was cancelled for niche: {Niche}", request.Niche ?? "general");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending topics for niche: {Niche}", request.Niche ?? "general");
            throw;
        }
    }

    /// <summary>
    /// Analyze content gaps and opportunities with real-time web intelligence
    /// </summary>
    public async Task<GapAnalysisResponse> AnalyzeContentGapsAsync(
        GapAnalysisRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // Validate niche length if provided
        if (!string.IsNullOrWhiteSpace(request.Niche) && request.Niche.Length > MaxTopicLength)
        {
            throw new ArgumentException($"Niche length exceeds maximum of {MaxTopicLength} characters", nameof(request));
        }

        _logger.LogInformation("Analyzing content gaps for niche: {Niche}", request.Niche ?? "general");

        try
        {

        // Gather real-time competitive intelligence if web search is available
        ContentGapAnalysisResult? webGapAnalysis = null;
        if (_webSearchService != null && !string.IsNullOrWhiteSpace(request.Niche))
        {
            try
            {
                _logger.LogInformation("Gathering real-time web intelligence for content gap analysis");
                var relatedTopics = new List<string>();
                if (request.ExistingTopics != null)
                    relatedTopics.AddRange(request.ExistingTopics);
                if (request.CompetitorTopics != null)
                    relatedTopics.AddRange(request.CompetitorTopics);
                webGapAnalysis = await _webSearchService.AnalyzeContentGapsAsync(
                    request.Niche,
                    relatedTopics.Count > 0 ? relatedTopics : null,
                    maxResults: 20,
                    ct).ConfigureAwait(false);

                _logger.LogInformation("Found {GapCount} content gaps and {OversaturatedCount} oversaturated topics from web search",
                    webGapAnalysis.GapKeywords.Count, webGapAnalysis.OversaturatedTopics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to gather web intelligence for gap analysis, continuing with LLM-only analysis");
            }
        }

            // Build enhanced prompt with web intelligence if available
            var prompt = BuildGapAnalysisPrompt(request, webGapAnalysis);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for gap analysis, niche: {Niche}", request.Niche ?? "general");
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var (missingTopics, opportunities, oversaturated, uniqueAngles) =
                ParseGapAnalysisResponse(response);

            // Merge web intelligence results with LLM analysis
            if (webGapAnalysis != null)
            {
                missingTopics = missingTopics
                    .Concat(webGapAnalysis.GapKeywords)
                    .Distinct()
                    .ToList();

                oversaturated = oversaturated
                    .Concat(webGapAnalysis.OversaturatedTopics)
                    .Distinct()
                    .ToList();

                // Merge unique angles - webGapAnalysis.UniqueAngles is List<string>, uniqueAngles is Dictionary<string, List<string>>
                foreach (var angle in webGapAnalysis.UniqueAngles)
                {
                    if (!uniqueAngles.ContainsKey("Web Intelligence"))
                    {
                        uniqueAngles["Web Intelligence"] = new List<string>();
                    }
                    if (!uniqueAngles["Web Intelligence"].Contains(angle))
                    {
                        uniqueAngles["Web Intelligence"].Add(angle);
                    }
                }
            }

            _logger.LogInformation("Successfully analyzed content gaps for niche: {Niche}, found {MissingCount} missing topics, {OpportunityCount} opportunities",
                request.Niche ?? "general", missingTopics.Count, opportunities.Count);

            return new GapAnalysisResponse(
                MissingTopics: missingTopics,
                Opportunities: opportunities,
                OversaturatedTopics: oversaturated,
                UniqueAngles: uniqueAngles
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gap analysis operation was cancelled for niche: {Niche}", request.Niche ?? "general");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content gaps for niche: {Niche}", request.Niche ?? "general");
            throw;
        }
    }

    /// <summary>
    /// Gather research and facts for a topic
    /// </summary>
    public async Task<ResearchResponse> GatherResearchAsync(
        ResearchRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic cannot be null or empty", nameof(request));
        }

        if (request.Topic.Length > MaxTopicLength)
        {
            throw new ArgumentException($"Topic length exceeds maximum of {MaxTopicLength} characters", nameof(request));
        }

        // Validate MaxFindings
        var maxFindings = Math.Clamp(request.MaxFindings ?? 10, 1, 50);

        _logger.LogInformation("Gathering research for topic: {Topic} (max findings: {MaxFindings})",
            request.Topic, maxFindings);

        try
        {
            var prompt = BuildResearchPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for research topic: {Topic}", request.Topic);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var findings = ParseResearchResponse(response, request.Topic);

            _logger.LogInformation("Successfully gathered {Count} research findings for topic: {Topic}",
                findings.Count, request.Topic);

            return new ResearchResponse(
                Findings: findings,
                Topic: request.Topic,
                GatheredAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Research gathering operation was cancelled for topic: {Topic}", request.Topic);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error gathering research for topic: {Topic}", request.Topic);
            throw;
        }
    }

    /// <summary>
    /// Generate visual storyboard for a concept
    /// </summary>
    public async Task<StoryboardResponse> GenerateStoryboardAsync(
        StoryboardRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.Concept, nameof(request.Concept));

        if (string.IsNullOrWhiteSpace(request.Concept.Title))
        {
            throw new ArgumentException("Concept.Title cannot be null or empty", nameof(request));
        }

        if (request.TargetDurationSeconds <= 0)
        {
            throw new ArgumentException("TargetDurationSeconds must be greater than 0", nameof(request));
        }

        if (request.TargetDurationSeconds > 3600) // 1 hour max
        {
            throw new ArgumentException("TargetDurationSeconds cannot exceed 3600 seconds (1 hour)", nameof(request));
        }

        _logger.LogInformation("Generating storyboard for concept: {ConceptTitle} (duration: {Duration}s)",
            request.Concept.Title, request.TargetDurationSeconds);

        try
        {
            var prompt = BuildStoryboardPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for storyboard, concept: {ConceptTitle}", request.Concept.Title);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var scenes = ParseStoryboardResponse(response, request.TargetDurationSeconds);

            _logger.LogInformation("Successfully generated {Count} storyboard scenes for concept: {ConceptTitle}",
                scenes.Count, request.Concept.Title);

            return new StoryboardResponse(
                Scenes: scenes,
                ConceptTitle: request.Concept.Title,
                TotalDurationSeconds: scenes.Sum(s => s.DurationSeconds),
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Storyboard generation operation was cancelled for concept: {ConceptTitle}", request.Concept.Title);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating storyboard for concept: {ConceptTitle}", request.Concept.Title);
            throw;
        }
    }

    /// <summary>
    /// Refine a concept based on user direction
    /// </summary>
    public async Task<RefineConceptResponse> RefineConceptAsync(
        RefineConceptRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.Concept, nameof(request.Concept));

        if (string.IsNullOrWhiteSpace(request.RefinementDirection))
        {
            throw new ArgumentException("RefinementDirection cannot be null or empty", nameof(request));
        }

        var validDirections = new[] { "expand", "simplify", "adjust-audience", "merge" };
        if (!validDirections.Contains(request.RefinementDirection.ToLowerInvariant()))
        {
            throw new ArgumentException($"RefinementDirection must be one of: {string.Join(", ", validDirections)}", nameof(request));
        }

        _logger.LogInformation("Refining concept: {ConceptTitle} with direction: {Direction}",
            request.Concept.Title, request.RefinementDirection);

        try
        {
            var prompt = BuildRefineConceptPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for refine concept: {ConceptTitle}", request.Concept.Title);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var (refinedConcept, changesSummary) = ParseRefineConceptResponse(
                response,
                request.Concept,
                request.RefinementDirection);

            _logger.LogInformation("Successfully refined concept: {ConceptTitle}", request.Concept.Title);

            return new RefineConceptResponse(
                RefinedConcept: refinedConcept,
                ChangesSummary: changesSummary
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Refine concept operation was cancelled for concept: {ConceptTitle}", request.Concept.Title);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining concept: {ConceptTitle}", request.Concept.Title);
            throw;
        }
    }

    /// <summary>
    /// Get clarifying questions about the brief
    /// </summary>
    public async Task<QuestionsResponse> GetClarifyingQuestionsAsync(
        QuestionsRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            throw new ArgumentException("ProjectId cannot be null or empty", nameof(request));
        }

        _logger.LogInformation("Generating clarifying questions for project: {ProjectId}", request.ProjectId);

        try
        {
            var prompt = BuildQuestionsPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for clarifying questions, project: {ProjectId}", request.ProjectId);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var questions = ParseQuestionsResponse(response);

            _logger.LogInformation("Successfully generated {Count} clarifying questions for project: {ProjectId}",
                questions.Count, request.ProjectId);

            return new QuestionsResponse(
                Questions: questions,
                Context: "These questions will help create a better video concept"
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get clarifying questions operation was cancelled for project: {ProjectId}", request.ProjectId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating clarifying questions for project: {ProjectId}", request.ProjectId);
            throw;
        }
    }

    /// <summary>
    /// Convert freeform idea into structured brief with multiple variants
    /// </summary>
    public async Task<IdeaToBriefResponse> IdeaToBriefAsync(
        IdeaToBriefRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.Idea))
        {
            throw new ArgumentException("Idea cannot be null or empty", nameof(request));
        }

        if (request.Idea.Length > MaxTopicLength * 2) // Ideas can be longer than topics
        {
            throw new ArgumentException($"Idea length exceeds maximum of {MaxTopicLength * 2} characters", nameof(request));
        }

        var variantCount = Math.Clamp(request.VariantCount ?? 3, 2, 4);

        _logger.LogInformation("Converting idea to brief: {Idea} (variant count: {VariantCount})",
            request.Idea, variantCount);

        try
        {
            var prompt = BuildIdeaToBriefPrompt(request, variantCount);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for idea to brief: {Idea}", request.Idea);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            // Parse the response into structured brief variants
            var variants = ParseIdeaToBriefResponse(response, request);

            _logger.LogInformation("Successfully converted idea to {Count} brief variants", variants.Count);

            return new IdeaToBriefResponse(
                Variants: variants,
                OriginalIdea: request.Idea,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Idea to brief operation was cancelled for idea: {Idea}", request.Idea);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting idea to brief: {Idea}", request.Idea);
            throw;
        }
    }

    // --- Prompt Building Methods ---

    private async Task<string> BuildBrainstormPromptAsync(BrainstormRequest request, int conceptCount, RagContext? ragContext, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Generate exactly {conceptCount} unique, creative, and actionable video concept ideas for the topic: '{request.Topic}'");
        sb.AppendLine();

        // Include RAG context if available to provide specific information about the topic
        if (ragContext != null && ragContext.Chunks.Count > 0)
        {
            sb.AppendLine("=== RELEVANT CONTEXT ABOUT THIS TOPIC ===");
            sb.AppendLine("The following information has been retrieved from project documents and should inform your concept generation:");
            sb.AppendLine();
            foreach (var chunk in ragContext.Chunks.Take(5))
            {
                sb.AppendLine($"- {chunk.Content}");
                if (chunk.CitationNumber > 0)
                {
                    sb.AppendLine($"  [Source: {chunk.Source}]");
                }
            }
            sb.AppendLine();
            sb.AppendLine("CRITICAL: Use the above context to generate SPECIFIC, DETAILED, FACTUALLY-GROUNDED concepts. Do NOT use generic placeholders.");
            sb.AppendLine("Every concept must be directly relevant to the actual information provided above.");
            sb.AppendLine();
        }

        // Add intelligent context about trending topics and content opportunities
        if (_trendingTopicsService != null)
        {
            try
            {
                var trendingTopics = await _trendingTopicsService.GetTrendingTopicsAsync(
                    request.Topic,
                    maxResults: 5,
                    forceRefresh: false,
                    ct).ConfigureAwait(false);

                if (trendingTopics.Count > 0)
                {
                    sb.AppendLine("=== TRENDING CONTEXT ===");
                    sb.AppendLine("The following topics are currently trending (from real-time web data) and may inform your concept generation:");
                    foreach (var topic in trendingTopics.Take(3))
                    {
                        var trendInfo = $"Trend Score: {topic.TrendScore:F1}/100";
                        if (topic.TrendVelocity.HasValue)
                        {
                            trendInfo += $" (Velocity: {topic.TrendVelocity.Value:+#.0;-#.0})";
                        }
                        if (topic.EstimatedAudience.HasValue)
                        {
                            trendInfo += $" | Est. Audience: {FormatNumber(topic.EstimatedAudience.Value)}";
                        }
                        sb.AppendLine($"- {topic.Topic}: {trendInfo}");
                        if (!string.IsNullOrWhiteSpace(topic.SearchVolume))
                        {
                            sb.AppendLine($"  Search Volume: {topic.SearchVolume} | Competition: {topic.Competition ?? "Unknown"}");
                        }
                    }
                    sb.AppendLine();
                    sb.AppendLine("Consider how your concepts can leverage or relate to these trending topics for better engagement.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve trending topics for ideation, continuing without trend data");
            }
        }

        // Add competitive intelligence if web search is available
        if (_webSearchService != null && !string.IsNullOrWhiteSpace(request.Topic))
        {
            try
            {
                var competitiveIntel = await _webSearchService.GetCompetitiveIntelligenceAsync(
                    request.Topic,
                    maxResults: 15,
                    ct).ConfigureAwait(false);

                if (competitiveIntel.TopDomains.Any())
                {
                    sb.AppendLine("=== COMPETITIVE INTELLIGENCE ===");
                    sb.AppendLine($"Market Saturation: {competitiveIntel.SaturationLevel}");
                    sb.AppendLine($"Average Content Relevance: {competitiveIntel.AverageRelevance:F1}%");
                    sb.AppendLine();
                    sb.AppendLine("Top Content Creators in this space:");
                    foreach (var domain in competitiveIntel.TopDomains.Take(5))
                    {
                        sb.AppendLine($"- {domain.Domain}: {domain.ContentCount} pieces (Avg Relevance: {domain.AverageRelevance:F1}%)");
                    }
                    sb.AppendLine();
                    sb.AppendLine("Content Type Distribution:");
                    foreach (var type in competitiveIntel.ContentTypeDistribution.OrderByDescending(kv => kv.Value).Take(5))
                    {
                        sb.AppendLine($"- {type.Key}: {type.Value} pieces");
                    }
                    sb.AppendLine();
                    if (competitiveIntel.CommonKeywords.Any())
                    {
                        sb.AppendLine("Common Keywords (consider for SEO):");
                        sb.AppendLine(string.Join(", ", competitiveIntel.CommonKeywords.Take(10).Select(kv => $"{kv.Key} ({kv.Value})")));
                        sb.AppendLine();
                    }
                    sb.AppendLine("Use this intelligence to identify unique angles and avoid oversaturated approaches.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to gather competitive intelligence, continuing without competitive data");
            }
        }

        sb.AppendLine("=== CRITICAL: JSON FORMAT REQUIREMENT ===");
        sb.AppendLine("You MUST respond with ONLY valid JSON. No markdown, no code blocks, no explanations, no additional text.");
        sb.AppendLine("Start your response immediately with the opening brace { and end with the closing brace }.");
        sb.AppendLine();
        sb.AppendLine("REQUIRED JSON STRUCTURE (copy this exactly):");
        sb.AppendLine("{");
        sb.AppendLine("  \"concepts\": [");
        for (int i = 0; i < conceptCount; i++)
        {
            sb.AppendLine("    {");
            sb.AppendLine("      \"title\": \"Catchy, engaging title SPECIFIC to the topic\",");
            sb.AppendLine("      \"description\": \"Detailed 2-3 sentence description SPECIFIC to this topic and concept angle\",");
            sb.AppendLine("      \"angle\": \"One of: Tutorial, Narrative, Case Study, Comparison, Interview, Documentary, Behind-the-Scenes, Expert Analysis, Beginner's Guide, Deep Dive\",");
            sb.AppendLine("      \"targetAudience\": \"Specific description of the intended audience for THIS topic\",");
            sb.AppendLine("      \"pros\": [\"Specific advantage 1 related to THIS topic\", \"Specific advantage 2 related to THIS topic\", \"Specific advantage 3 related to THIS topic\"],");
            sb.AppendLine("      \"cons\": [\"Specific challenge 1 related to THIS topic\", \"Specific challenge 2 related to THIS topic\", \"Specific challenge 3 related to THIS topic\"],");
            sb.AppendLine("      \"hook\": \"Compelling opening hook SPECIFIC to this topic and concept (15 seconds max)\",");
            sb.AppendLine("      \"talkingPoints\": [\"Specific talking point 1 about THIS topic\", \"Specific talking point 2 about THIS topic\", \"Specific talking point 3 about THIS topic\", \"Specific talking point 4 about THIS topic\", \"Specific talking point 5 about THIS topic\"],");
            sb.AppendLine("      \"appealScore\": 85");
            if (i < conceptCount - 1)
                sb.AppendLine("    },");
            else
                sb.AppendLine("    }");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"IMPORTANT: Generate exactly {conceptCount} concepts in the array above.");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(request.Audience))
        {
            sb.AppendLine($"Target Audience Preference: {request.Audience}");
        }

        if (!string.IsNullOrEmpty(request.Tone))
        {
            sb.AppendLine($"Tone Preference: {request.Tone}");
        }

        if (request.TargetDuration.HasValue)
        {
            sb.AppendLine($"Target Duration: {request.TargetDuration} seconds");
        }

        if (!string.IsNullOrEmpty(request.Platform))
        {
            sb.AppendLine($"Platform: {request.Platform}");
        }

        sb.AppendLine();
        sb.AppendLine("CRITICAL REQUIREMENTS:");
        sb.AppendLine();
        sb.AppendLine($"1. TOPIC-SPECIFIC CONTENT: ALL fields must be SPECIFIC to the topic '{request.Topic}'. Do NOT use generic placeholders like:");
        sb.AppendLine("   - ❌ BAD: \"Introduction\", \"Key concepts\", \"Practical examples\", \"Common mistakes\", \"Next steps\"");
        sb.AppendLine("   - ✅ GOOD: For 'how to eat sushi': \"Proper use of chopsticks for picking up nigiri\", \"Understanding the difference between nigiri, sashimi, and maki\", \"Etiquette for using soy sauce and wasabi\", \"How to eat sushi in one bite\", \"Order of eating: start with lighter fish, end with heavier\"");
        sb.AppendLine();
        sb.AppendLine("2. TALKING POINTS: Must be SPECIFIC, ACTIONABLE points about the topic. Each point should:");
        sb.AppendLine($"   - Be directly related to the topic '{request.Topic}'");
        sb.AppendLine("   - Provide concrete, useful information");
        sb.AppendLine("   - Be suitable for the chosen angle (Tutorial, Narrative, etc.)");
        sb.AppendLine("   - Example for 'how to eat sushi' Tutorial: \"Demonstrate proper chopstick technique for picking up nigiri without breaking it\", \"Explain the purpose of ginger (cleansing palate) and when to eat it\", \"Show correct soy sauce application (dip fish side, not rice)\"");
        sb.AppendLine();
        sb.AppendLine("3. PROS AND CONS: Must be SPECIFIC to this topic and concept angle:");
        sb.AppendLine("   - ❌ BAD: \"Engaging\", \"Accessible\", \"Clear value\", \"Suitable for platform\"");
        sb.AppendLine("   - ✅ GOOD: For 'how to eat sushi' Tutorial: \"Visual demonstration makes complex techniques easy to understand\", \"Addresses common mistakes beginners make (over-soy saucing, wrong bite size)\", \"Builds confidence for first-time sushi eaters\"");
        sb.AppendLine();
        sb.AppendLine("4. HOOK: Must be SPECIFIC and compelling for THIS topic:");
        sb.AppendLine("   - ❌ BAD: \"Discover the most tutorial way to understand [topic]\"");
        sb.AppendLine("   - ✅ GOOD: For 'how to eat sushi': \"Did you know that 90% of people eat sushi wrong? In the next 5 minutes, I'll show you the proper way to enjoy sushi like a Japanese chef, and you'll never make these embarrassing mistakes again.\"");
        sb.AppendLine();
        sb.AppendLine("5. UNIQUENESS: Make each concept genuinely unique with different angles and approaches");
        sb.AppendLine("6. APPEAL SCORES: Should realistically range from 65-95 based on concept viability");
        sb.AppendLine("7. FORMAT: Return ONLY the JSON object, no additional text or markdown formatting");
        sb.AppendLine();
        sb.AppendLine("=== FINAL REMINDER ===");
        sb.AppendLine("1. Start your response with { (opening brace)");
        sb.AppendLine($"2. Include exactly {conceptCount} concepts in the \"concepts\" array");
        sb.AppendLine("3. End your response with } (closing brace)");
        sb.AppendLine("4. Do NOT include ```json or ``` markdown code blocks");
        sb.AppendLine("5. Do NOT include any text before or after the JSON");
        sb.AppendLine($"6. Every field must contain SPECIFIC information about '{request.Topic}'. Generic placeholders are NOT acceptable.");
        sb.AppendLine();
        sb.AppendLine("Now generate the JSON response:");

        return sb.ToString();
    }

    private string BuildExpandBriefPrompt(ExpandBriefRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are helping a creator develop their video brief.");
        sb.AppendLine();
        sb.AppendLine($"Current Topic: {request.CurrentBrief.Topic}");

        if (!string.IsNullOrEmpty(request.CurrentBrief.Goal))
            sb.AppendLine($"Goal: {request.CurrentBrief.Goal}");

        if (!string.IsNullOrEmpty(request.CurrentBrief.Audience))
            sb.AppendLine($"Audience: {request.CurrentBrief.Audience}");

        if (!string.IsNullOrEmpty(request.CurrentBrief.Tone))
            sb.AppendLine($"Tone: {request.CurrentBrief.Tone}");

        sb.AppendLine();

        if (!string.IsNullOrEmpty(request.UserMessage))
        {
            sb.AppendLine($"User's message: {request.UserMessage}");
            sb.AppendLine();
        }

        sb.AppendLine("Ask 3-5 thoughtful clarifying questions that will help refine the concept.");
        sb.AppendLine("Focus on questions about target audience, desired outcome, unique angle, and creative direction.");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"questions\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"question\": \"Specific question text\",");
        sb.AppendLine("      \"context\": \"Why this question is being asked\",");
        sb.AppendLine("      \"questionType\": \"open-ended|multiple-choice|yes-no\",");
        sb.AppendLine("      \"suggestedAnswers\": [\"Option 1\", \"Option 2\", \"Option 3\"] // Only for multiple-choice questions");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"aiResponse\": \"Your helpful response or summary text\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildGapAnalysisPrompt(GapAnalysisRequest request, ContentGapAnalysisResult? webGapAnalysis = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze content gaps and opportunities.");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(request.Niche))
        {
            sb.AppendLine($"Niche: {request.Niche}");
        }

        if (request.ExistingTopics?.Any() == true)
        {
            sb.AppendLine($"Existing topics covered: {string.Join(", ", request.ExistingTopics)}");
        }

        if (request.CompetitorTopics?.Any() == true)
        {
            sb.AppendLine($"Competitor topics: {string.Join(", ", request.CompetitorTopics)}");
        }

        // Include real-time web intelligence if available
        if (webGapAnalysis != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== REAL-TIME WEB INTELLIGENCE ===");
            sb.AppendLine($"Content Gap Opportunity Score: {webGapAnalysis.OpportunityScore:F1}/100");
            sb.AppendLine();

            if (webGapAnalysis.GapKeywords.Any())
            {
                sb.AppendLine("Identified Content Gaps (from current web search):");
                foreach (var gap in webGapAnalysis.GapKeywords.Take(10))
                {
                    sb.AppendLine($"- {gap}");
                }
                sb.AppendLine();
            }

            if (webGapAnalysis.OversaturatedTopics.Any())
            {
                sb.AppendLine("Oversaturated Topics (high competition, consider avoiding):");
                foreach (var topic in webGapAnalysis.OversaturatedTopics.Take(10))
                {
                    sb.AppendLine($"- {topic}");
                }
                sb.AppendLine();
            }

            if (webGapAnalysis.UniqueAngles.Any())
            {
                sb.AppendLine("Unique Content Angles (less explored approaches):");
                foreach (var angle in webGapAnalysis.UniqueAngles.Take(5))
                {
                    sb.AppendLine($"- {angle}");
                }
                sb.AppendLine();
            }

            if (webGapAnalysis.RecommendedFocus.Any())
            {
                sb.AppendLine($"Recommended Focus Areas: {string.Join(", ", webGapAnalysis.RecommendedFocus)}");
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Based on the above information, identify:");
        sb.AppendLine("1. Missing topics that should be covered");
        sb.AppendLine("2. High-opportunity topics with low competition");
        sb.AppendLine("3. Oversaturated topics to avoid");
        sb.AppendLine("4. Unique angles for popular topics");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"missingTopics\": [\"Topic 1\", \"Topic 2\", \"Topic 3\"],");
        sb.AppendLine("  \"opportunities\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"topic\": \"High-opportunity topic name\",");
        sb.AppendLine("      \"trendScore\": 85,");
        sb.AppendLine("      \"searchVolume\": \"10K/month\",");
        sb.AppendLine("      \"competition\": \"Low\",");
        sb.AppendLine("      \"lifecycle\": \"Rising\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"oversaturatedTopics\": [\"Topic 1\", \"Topic 2\"],");
        sb.AppendLine("  \"uniqueAngles\": {");
        sb.AppendLine("    \"Popular Topic 1\": [\"Unique angle 1\", \"Unique angle 2\"],");
        sb.AppendLine("    \"Popular Topic 2\": [\"Unique angle 1\"]");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildResearchPrompt(ResearchRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Gather key facts, statistics, and examples for the topic: '{request.Topic}'");
        sb.AppendLine();
        sb.AppendLine($"Provide {request.MaxFindings ?? 10} research findings that include:");
        sb.AppendLine("1. The fact or statistic");
        sb.AppendLine("2. A real-world example illustrating the concept");
        sb.AppendLine("3. Why this is relevant to the topic");
        sb.AppendLine();
        sb.AppendLine("Focus on interesting, credible, and engaging information that will make the video compelling.");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"findings\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"fact\": \"The specific fact or statistic\",");
        sb.AppendLine("      \"source\": \"Source URL or citation (if available)\",");
        sb.AppendLine("      \"credibilityScore\": 85,");
        sb.AppendLine("      \"relevanceScore\": 90,");
        sb.AppendLine("      \"example\": \"Real-world example illustrating this fact\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildStoryboardPrompt(StoryboardRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Create a visual storyboard for the video concept: '{request.Concept.Title}'");
        sb.AppendLine();
        sb.AppendLine($"Description: {request.Concept.Description}");
        sb.AppendLine($"Angle: {request.Concept.Angle}");
        sb.AppendLine($"Target Duration: {request.TargetDurationSeconds} seconds");
        sb.AppendLine();
        sb.AppendLine("Break the video into 5-8 scenes. For each scene provide:");
        sb.AppendLine("1. Scene description");
        sb.AppendLine("2. Visual style (cinematography, camera angles, lighting)");
        sb.AppendLine("3. Duration in seconds");
        sb.AppendLine("4. Purpose (hook, context, explanation, example, call-to-action, etc.)");
        sb.AppendLine("5. Shot list (3-5 specific shots)");
        sb.AppendLine();
        sb.AppendLine("Ensure the first scene has a strong hook.");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"scenes\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"sceneNumber\": 1,");
        sb.AppendLine("      \"description\": \"Detailed scene description\",");
        sb.AppendLine("      \"visualStyle\": \"Visual style description (cinematography, camera angles, lighting)\",");
        sb.AppendLine("      \"durationSeconds\": 10,");
        sb.AppendLine("      \"purpose\": \"hook|context|explanation|example|call-to-action\",");
        sb.AppendLine("      \"shotList\": [\"Shot 1\", \"Shot 2\", \"Shot 3\"],");
        sb.AppendLine("      \"transitionType\": \"Smooth fade\" // Optional");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildRefineConceptPrompt(RefineConceptRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Refine the video concept: '{request.Concept.Title}'");
        sb.AppendLine();
        sb.AppendLine($"Original Description: {request.Concept.Description}");
        sb.AppendLine($"Angle: {request.Concept.Angle}");
        sb.AppendLine();

        switch (request.RefinementDirection.ToLowerInvariant())
        {
            case "expand":
                sb.AppendLine("Add more depth and detail to this concept. Include additional sections, examples, and nuances.");
                break;
            case "simplify":
                sb.AppendLine("Simplify this concept. Make it more focused and easier to produce.");
                break;
            case "adjust-audience":
                sb.AppendLine($"Adapt this concept for a new target audience: {request.TargetAudience}");
                break;
            case "merge":
                if (request.SecondConcept != null)
                {
                    sb.AppendLine($"Merge this concept with: '{request.SecondConcept.Title}'");
                    sb.AppendLine($"Second concept: {request.SecondConcept.Description}");
                }
                break;
        }

        sb.AppendLine();
        sb.AppendLine("Provide the refined concept with a clear summary of what changed.");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"refinedConcept\": {");
        sb.AppendLine("    \"title\": \"Refined concept title\",");
        sb.AppendLine("    \"description\": \"Refined concept description\",");
        sb.AppendLine("    \"angle\": \"Concept angle\",");
        sb.AppendLine("    \"targetAudience\": \"Target audience description\",");
        sb.AppendLine("    \"pros\": [\"Pro 1\", \"Pro 2\", \"Pro 3\"],");
        sb.AppendLine("    \"cons\": [\"Con 1\", \"Con 2\"],");
        sb.AppendLine("    \"appealScore\": 85,");
        sb.AppendLine("    \"hook\": \"Refined hook\",");
        sb.AppendLine("    \"talkingPoints\": [\"Point 1\", \"Point 2\", \"Point 3\"]");
        sb.AppendLine("  },");
        sb.AppendLine("  \"changesSummary\": \"Clear summary of what changed and why\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildQuestionsPrompt(QuestionsRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are helping a creator develop their video idea.");
        sb.AppendLine();

        if (request.CurrentBrief != null)
        {
            sb.AppendLine($"Topic: {request.CurrentBrief.Topic}");

            if (!string.IsNullOrEmpty(request.CurrentBrief.Audience))
                sb.AppendLine($"Audience: {request.CurrentBrief.Audience}");
        }

        sb.AppendLine();
        sb.AppendLine("Generate 5 clarifying questions that will help refine the video concept.");
        sb.AppendLine("Questions should cover:");
        sb.AppendLine("- Target audience and their pain points");
        sb.AppendLine("- Desired outcome or goal");
        sb.AppendLine("- Unique angle or perspective");
        sb.AppendLine("- Style and tone preferences");
        sb.AppendLine("- Key messages to convey");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"questions\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"question\": \"Specific question text\",");
        sb.AppendLine("      \"context\": \"Why this question is being asked\",");
        sb.AppendLine("      \"questionType\": \"open-ended|multiple-choice|yes-no\",");
        sb.AppendLine("      \"suggestedAnswers\": [\"Option 1\", \"Option 2\"] // Only for multiple-choice questions, null otherwise");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Return ONLY the JSON object, no additional text or markdown formatting.");

        return sb.ToString();
    }

    private string BuildIdeaToBriefPrompt(IdeaToBriefRequest request, int variantCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Convert this freeform idea into {variantCount} structured video brief variants with different creative approaches:");
        sb.AppendLine();
        sb.AppendLine($"Idea: {request.Idea}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(request.TargetPlatform))
        {
            sb.AppendLine($"Target Platform: {request.TargetPlatform}");
        }

        if (!string.IsNullOrEmpty(request.Audience))
        {
            sb.AppendLine($"Target Audience: {request.Audience}");
        }

        if (!string.IsNullOrEmpty(request.PreferredApproaches))
        {
            sb.AppendLine($"User's Creative Direction: {request.PreferredApproaches}");
        }

        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"variants\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"approach\": \"Freeform creative approach description (e.g., 'detective-style investigation', 'philosophical deep-dive', 'fun educational journey')\",");
        sb.AppendLine("      \"topic\": \"Specific, focused topic derived from the idea\",");
        sb.AppendLine("      \"audience\": \"Detailed target audience description\",");
        sb.AppendLine("      \"goal\": \"What the video should achieve (can be any goal, not limited to preset options)\",");
        sb.AppendLine("      \"tone\": \"Freeform tone description (e.g., 'Casual and witty', 'Authoritative yet approachable', 'Mysterious and intriguing')\",");
        sb.AppendLine("      \"targetDurationSeconds\": 60,");
        sb.AppendLine("      \"pacing\": \"Fast|Conversational|Deliberate\",");
        sb.AppendLine("      \"density\": \"Sparse|Balanced|Dense\",");
        sb.AppendLine("      \"style\": \"Freeform style description (e.g., 'Documentary-style', 'Step-by-step tutorial', 'Narrative storytelling with examples')\",");
        sb.AppendLine("      \"explanation\": \"2-3 sentences explaining why this approach works for the original idea\",");
        sb.AppendLine("      \"suitabilityScore\": 85,");
        sb.AppendLine("      \"strengths\": [\"Strength 1\", \"Strength 2\", \"Strength 3\"],");
        sb.AppendLine("      \"considerations\": [\"Consideration 1\", \"Consideration 2\"]");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Requirements:");
        sb.AppendLine($"- Generate exactly {variantCount} genuinely different variants with unique creative approaches");
        sb.AppendLine("- Be creative and open-minded - interpret the user's ideas and preferences in interesting ways");
        sb.AppendLine("- Don't limit yourself to standard categories - create unique, compelling approaches");
        sb.AppendLine("- If user specified preferred approaches, honor their creative direction while adding your own interpretation");
        sb.AppendLine("- Ensure each variant is practical and achievable");
        sb.AppendLine("- Make recommendations specific to the platform if provided");
        sb.AppendLine("- Duration should be appropriate for the platform context");
        sb.AppendLine("- Suitability scores should realistically range from 70-95");
        sb.AppendLine("- Return ONLY the JSON object, no additional text or markdown formatting");

        return sb.ToString();
    }

    // --- Response Parsing Methods ---

    private List<ConceptIdea> ParseBrainstormResponse(string response, string originalTopic, int desiredConceptCount)
    {
        var concepts = new List<ConceptIdea>();

        try
        {
            // Clean the response - remove markdown code blocks and other LLM formatting artifacts
            var cleanedResponse = CleanJsonResponse(response);

            if (string.IsNullOrWhiteSpace(cleanedResponse))
            {
                _logger.LogWarning("Cleaned response is empty for topic: {Topic}. Original response length: {Length}",
                    originalTopic, response.Length);
            }
            else
            {
                // Parse JSON response
                var jsonDoc = JsonDocument.Parse(cleanedResponse);
                if (jsonDoc.RootElement.TryGetProperty("concepts", out var conceptsArray))
                {
                    foreach (var conceptElement in conceptsArray.EnumerateArray())
                    {
                        try
                        {
                            var title = GetStringPropertySafe(conceptElement, "title", "Untitled Concept");
                            var description = GetStringPropertySafe(conceptElement, "description", "");
                            var angle = GetStringPropertySafe(conceptElement, "angle", "Tutorial");
                            var targetAudience = GetStringPropertySafe(conceptElement, "targetAudience", "General audience");
                            var hook = GetStringPropertySafe(conceptElement, "hook", "");
                            var appealScore = GetDoublePropertySafe(conceptElement, "appealScore", 75.0);

                            // Parse new high-value fields
                            var uniqueValue = GetStringPropertySafe(conceptElement, "uniqueValue", null!);
                            var contentGap = GetStringPropertySafe(conceptElement, "contentGap", null!);
                            var monetizationPotential = GetStringPropertySafe(conceptElement, "monetizationPotential", null!);
                            var viralityScore = GetDoublePropertySafe(conceptElement, "viralityScore", 0.0);

                    var pros = new List<string>();
                    if (conceptElement.TryGetProperty("pros", out var prosArray))
                    {
                        foreach (var pro in prosArray.EnumerateArray())
                        {
                            var proText = pro.GetString();
                            if (!string.IsNullOrEmpty(proText))
                            {
                                pros.Add(proText);
                            }
                        }
                    }

                    var cons = new List<string>();
                    if (conceptElement.TryGetProperty("cons", out var consArray))
                    {
                        foreach (var con in consArray.EnumerateArray())
                        {
                            var conText = con.GetString();
                            if (!string.IsNullOrEmpty(conText))
                            {
                                cons.Add(conText);
                            }
                        }
                    }

                    var talkingPoints = new List<string>();
                    if (conceptElement.TryGetProperty("talkingPoints", out var talkingPointsArray))
                    {
                        foreach (var point in talkingPointsArray.EnumerateArray())
                        {
                            var pointText = point.GetString();
                            if (!string.IsNullOrEmpty(pointText))
                            {
                                talkingPoints.Add(pointText);
                            }
                        }
                    }

                    // Parse new array fields
                    var keyInsights = new List<string>();
                    if (conceptElement.TryGetProperty("keyInsights", out var keyInsightsArray))
                    {
                        foreach (var insight in keyInsightsArray.EnumerateArray())
                        {
                            var insightText = insight.GetString();
                            if (!string.IsNullOrEmpty(insightText))
                            {
                                keyInsights.Add(insightText);
                            }
                        }
                    }

                    var visualSuggestions = new List<string>();
                    if (conceptElement.TryGetProperty("visualSuggestions", out var visualSuggestionsArray))
                    {
                        foreach (var visual in visualSuggestionsArray.EnumerateArray())
                        {
                            var visualText = visual.GetString();
                            if (!string.IsNullOrEmpty(visualText))
                            {
                                visualSuggestions.Add(visualText);
                            }
                        }
                    }

                    // Quality validation: skip concepts with too-short descriptions
                    // Reduced threshold for local LLMs like Ollama which may produce shorter but valid content
                    if (description.Length < 30)
                    {
                        _logger.LogWarning("Skipping concept with very short description: {Title} (length: {Length})", title, description.Length);
                        continue;
                    }

                    concepts.Add(new ConceptIdea(
                        ConceptId: Guid.NewGuid().ToString(),
                        Title: title,
                        Description: description,
                        Angle: angle,
                        TargetAudience: targetAudience,
                        Pros: pros,
                        Cons: cons,
                        AppealScore: appealScore,
                        Hook: hook,
                        TalkingPoints: talkingPoints.Count > 0 ? talkingPoints : null,
                        CreatedAt: DateTime.UtcNow,
                        UniqueValue: string.IsNullOrEmpty(uniqueValue) ? null : uniqueValue,
                        ContentGap: string.IsNullOrEmpty(contentGap) ? null : contentGap,
                        KeyInsights: keyInsights.Count > 0 ? keyInsights : null,
                        VisualSuggestions: visualSuggestions.Count > 0 ? visualSuggestions : null,
                        MonetizationPotential: string.IsNullOrEmpty(monetizationPotential) ? null : monetizationPotential,
                        ViralityScore: viralityScore > 0 ? viralityScore : null
                    ));
                        }
                        catch (Exception elementEx)
                        {
                            _logger.LogWarning(elementEx, "Failed to parse one concept element, skipping");
                            continue;
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Failed to parse JSON response from LLM for topic: {Topic}. " +
                "Response length: {Length}. " +
                "JSON error: {JsonError}. " +
                "Response preview: {Preview}",
                originalTopic,
                response.Length,
                ex.Message,
                response.Substring(0, Math.Min(500, response.Length)));

            // Log the cleaned response for debugging
            try
            {
                var cleaned = CleanJsonResponse(response);
                if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != response)
                {
                    _logger.LogDebug("Cleaned response (length: {Length}): {CleanedPreview}",
                        cleaned.Length, cleaned.Substring(0, Math.Min(300, cleaned.Length)));
                }
            }
            catch
            {
                // Ignore errors in logging
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unexpected error parsing LLM response for topic: {Topic}. " +
                "Response length: {Length}. " +
                "Error: {Error}. " +
                "Response preview: {Preview}",
                originalTopic,
                response.Length,
                ex.Message,
                response.Substring(0, Math.Min(500, response.Length)));
        }

        // Fallback: If parsing failed or no concepts were generated, try to extract meaningful data from raw response
        if (concepts.Count == 0)
        {
            _logger.LogWarning(
                "No concepts parsed from LLM response. " +
                "Response length: {Length}. " +
                "Attempting to extract meaningful content from raw response.",
                response.Length);

            // Try to extract concepts from non-JSON response using pattern matching
            var extractedConcepts = TryExtractConceptsFromText(response, originalTopic, desiredConceptCount);
            if (extractedConcepts.Count > 0)
            {
                _logger.LogInformation("Successfully extracted {Count} concepts from raw LLM response", extractedConcepts.Count);
                concepts.AddRange(extractedConcepts);
            }
            else
            {
                // Last resort: Log the full response for debugging and throw an error
                var responsePreview = response.Length > 0
                    ? response.Substring(0, Math.Min(1000, response.Length))
                    : "(empty response)";

                _logger.LogError(
                    "Failed to parse or extract concepts from LLM response for topic: {Topic}. " +
                    "Response length: {Length}. " +
                    "Full response preview: {Preview}",
                    originalTopic, response.Length, responsePreview);

                // Provide more helpful error message
                var errorMessage = response.Length < 50
                    ? $"LLM returned an incomplete response ({response.Length} characters). The response is too short to contain valid JSON. Please try again or check your LLM provider configuration."
                    : $"Failed to generate concepts from LLM response. The response could not be parsed as JSON and no meaningful content could be extracted. " +
                      $"Response length: {response.Length} characters. " +
                      $"Please check your LLM provider configuration and try again. " +
                      $"Response preview: {response.Substring(0, Math.Min(200, response.Length))}...";

                throw new InvalidOperationException(errorMessage);
            }
        }

        // Ensure we return the requested number of concepts
        return concepts.Take(desiredConceptCount).ToList();
    }

    private (List<ClarifyingQuestion>?, string) ParseExpandBriefResponse(string response)
    {
        var questions = new List<ClarifyingQuestion>();
        string aiResponse = response;

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            // Extract AI response if present
            if (jsonDoc.RootElement.TryGetProperty("aiResponse", out var aiResponseElement))
            {
                aiResponse = aiResponseElement.GetString() ?? response;
            }

            // Parse questions
            if (jsonDoc.RootElement.TryGetProperty("questions", out var questionsArray))
            {
                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionText = questionElement.GetProperty("question").GetString() ?? "";
                    var context = questionElement.GetProperty("context").GetString() ?? "";
                    var questionType = questionElement.GetProperty("questionType").GetString() ?? "open-ended";

                    List<string>? suggestedAnswers = null;
                    if (questionElement.TryGetProperty("suggestedAnswers", out var answersArray) && answersArray.ValueKind == JsonValueKind.Array)
                    {
                        suggestedAnswers = new List<string>();
                        foreach (var answer in answersArray.EnumerateArray())
                        {
                            var answerText = answer.GetString();
                            if (!string.IsNullOrEmpty(answerText))
                            {
                                suggestedAnswers.Add(answerText);
                            }
                        }
                        if (suggestedAnswers.Count == 0)
                        {
                            suggestedAnswers = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(questionText))
                    {
                        questions.Add(new ClarifyingQuestion(
                            QuestionId: Guid.NewGuid().ToString(),
                            Question: questionText,
                            Context: context,
                            SuggestedAnswers: suggestedAnswers,
                            QuestionType: questionType
                        ));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for expand brief, using fallback");
        }

        // Fallback: If parsing failed or no questions were generated, create generic questions
        if (questions.Count == 0)
        {
            _logger.LogWarning("No questions parsed from LLM response, generating fallback questions");
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "Who is your ideal viewer for this video?",
                Context: "Understanding your target audience helps tailor the content and tone",
                SuggestedAnswers: new List<string> { "Beginners", "Intermediate learners", "Experts", "General public" },
                QuestionType: "multiple-choice"
            ));
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What action do you want viewers to take after watching?",
                Context: "Defining the desired outcome shapes the video's call-to-action",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            ));
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What makes your perspective on this topic unique?",
                Context: "Your unique angle differentiates your content from competitors",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            ));
        }

        return (questions, aiResponse);
    }


    private (List<string>, List<TrendingTopic>, List<string>, Dictionary<string, List<string>>)
        ParseGapAnalysisResponse(string response)
    {
        var missingTopics = new List<string>();
        var opportunities = new List<TrendingTopic>();
        var oversaturated = new List<string>();
        var uniqueAngles = new Dictionary<string, List<string>>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            // Parse missing topics
            if (jsonDoc.RootElement.TryGetProperty("missingTopics", out var missingTopicsArray))
            {
                foreach (var topic in missingTopicsArray.EnumerateArray())
                {
                    var topicText = topic.GetString();
                    if (!string.IsNullOrEmpty(topicText))
                    {
                        missingTopics.Add(topicText);
                    }
                }
            }

            // Parse opportunities
            if (jsonDoc.RootElement.TryGetProperty("opportunities", out var opportunitiesArray))
            {
                foreach (var oppElement in opportunitiesArray.EnumerateArray())
                {
                    var topic = oppElement.GetProperty("topic").GetString() ?? "Untitled Topic";
                    var trendScore = oppElement.TryGetProperty("trendScore", out var scoreElement) ? scoreElement.GetDouble() : 75.0;
                    var searchVolume = oppElement.TryGetProperty("searchVolume", out var volumeElement) ? volumeElement.GetString() : null;
                    var competition = oppElement.TryGetProperty("competition", out var compElement) ? compElement.GetString() : null;
                    var lifecycle = oppElement.TryGetProperty("lifecycle", out var lifecycleElement) ? lifecycleElement.GetString() : null;

                    opportunities.Add(new TrendingTopic(
                        TopicId: Guid.NewGuid().ToString(),
                        Topic: topic,
                        TrendScore: trendScore,
                        SearchVolume: searchVolume,
                        Competition: competition,
                        Seasonality: null,
                        Lifecycle: lifecycle,
                        RelatedTopics: null,
                        DetectedAt: DateTime.UtcNow
                    ));
                }
            }

            // Parse oversaturated topics
            if (jsonDoc.RootElement.TryGetProperty("oversaturatedTopics", out var oversaturatedArray))
            {
                foreach (var topic in oversaturatedArray.EnumerateArray())
                {
                    var topicText = topic.GetString();
                    if (!string.IsNullOrEmpty(topicText))
                    {
                        oversaturated.Add(topicText);
                    }
                }
            }

            // Parse unique angles
            if (jsonDoc.RootElement.TryGetProperty("uniqueAngles", out var uniqueAnglesObject))
            {
                foreach (var prop in uniqueAnglesObject.EnumerateObject())
                {
                    var topicName = prop.Name;
                    var angles = new List<string>();
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var angle in prop.Value.EnumerateArray())
                        {
                            var angleText = angle.GetString();
                            if (!string.IsNullOrEmpty(angleText))
                            {
                                angles.Add(angleText);
                            }
                        }
                    }
                    if (angles.Count > 0)
                    {
                        uniqueAngles[topicName] = angles;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for gap analysis, using fallback");
        }

        // Fallback: If parsing failed or no data was generated, create generic data
        if (missingTopics.Count == 0 && opportunities.Count == 0 && oversaturated.Count == 0)
        {
            _logger.LogWarning("No data parsed from LLM response, generating fallback data");
            missingTopics.Add("Beginner-friendly introductions");
            missingTopics.Add("Advanced techniques and workflows");
            missingTopics.Add("Real-world case studies");
            missingTopics.Add("Common mistakes to avoid");

            opportunities.Add(new TrendingTopic(
                TopicId: Guid.NewGuid().ToString(),
                Topic: "Emerging tools and technologies",
                TrendScore: 90,
                SearchVolume: "50K/month",
                Competition: "Low",
                Seasonality: "Year-round",
                Lifecycle: "Rising",
                RelatedTopics: null,
                DetectedAt: DateTime.UtcNow
            ));

            oversaturated.Add("Basic introductions covered by many creators");

            uniqueAngles["Popular Topic"] = new List<string>
            {
                "Focus on uncommon use cases",
                "Interview experts in the field",
                "Behind-the-scenes perspective"
            };
        }

        return (missingTopics, opportunities, oversaturated, uniqueAngles);
    }

    private List<ResearchFinding> ParseResearchResponse(string response, string topic)
    {
        var findings = new List<ResearchFinding>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            if (jsonDoc.RootElement.TryGetProperty("findings", out var findingsArray))
            {
                foreach (var findingElement in findingsArray.EnumerateArray())
                {
                    var fact = findingElement.GetProperty("fact").GetString() ?? "";
                    var source = findingElement.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() : null;
                    var credibilityScore = findingElement.TryGetProperty("credibilityScore", out var credElement) ? credElement.GetDouble() : 80.0;
                    var relevanceScore = findingElement.TryGetProperty("relevanceScore", out var relElement) ? relElement.GetDouble() : 85.0;
                    var example = findingElement.TryGetProperty("example", out var exampleElement) ? exampleElement.GetString() : null;

                    if (!string.IsNullOrEmpty(fact))
                    {
                        findings.Add(new ResearchFinding(
                            FindingId: Guid.NewGuid().ToString(),
                            Fact: fact,
                            Source: source,
                            CredibilityScore: credibilityScore,
                            RelevanceScore: relevanceScore,
                            Example: example,
                            GatheredAt: DateTime.UtcNow
                        ));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for research, using fallback");
        }

        // Fallback: If parsing failed or no findings were generated, create generic findings
        if (findings.Count == 0)
        {
            _logger.LogWarning("No findings parsed from LLM response, generating fallback findings");
            for (int i = 0; i < 5; i++)
            {
                findings.Add(new ResearchFinding(
                    FindingId: Guid.NewGuid().ToString(),
                    Fact: $"Key finding {i + 1} about {topic}",
                    Source: "Industry research and expert analysis",
                    CredibilityScore: 85,
                    RelevanceScore: 90,
                    Example: $"A real-world example demonstrating this concept in action",
                    GatheredAt: DateTime.UtcNow
                ));
            }
        }

        return findings;
    }

    private List<StoryboardScene> ParseStoryboardResponse(string response, int targetDuration)
    {
        var scenes = new List<StoryboardScene>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            if (jsonDoc.RootElement.TryGetProperty("scenes", out var scenesArray))
            {
                foreach (var sceneElement in scenesArray.EnumerateArray())
                {
                    var sceneNumber = sceneElement.TryGetProperty("sceneNumber", out var numElement) ? numElement.GetInt32() : scenes.Count + 1;
                    var description = sceneElement.GetProperty("description").GetString() ?? "";
                    var visualStyle = sceneElement.GetProperty("visualStyle").GetString() ?? "Clear, focused";
                    var durationSeconds = sceneElement.TryGetProperty("durationSeconds", out var durElement) ? durElement.GetInt32() : targetDuration / 6;
                    var purpose = sceneElement.GetProperty("purpose").GetString() ?? "Main Content";
                    var transitionType = sceneElement.TryGetProperty("transitionType", out var transElement) ? transElement.GetString() : null;

                    List<string>? shotList = null;
                    if (sceneElement.TryGetProperty("shotList", out var shotListArray) && shotListArray.ValueKind == JsonValueKind.Array)
                    {
                        shotList = new List<string>();
                        foreach (var shot in shotListArray.EnumerateArray())
                        {
                            var shotText = shot.GetString();
                            if (!string.IsNullOrEmpty(shotText))
                            {
                                shotList.Add(shotText);
                            }
                        }
                        if (shotList.Count == 0)
                        {
                            shotList = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(description))
                    {
                        scenes.Add(new StoryboardScene(
                            SceneNumber: sceneNumber,
                            Description: description,
                            VisualStyle: visualStyle,
                            DurationSeconds: durationSeconds,
                            Purpose: purpose,
                            ShotList: shotList,
                            TransitionType: transitionType
                        ));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for storyboard, using fallback");
        }

        // Fallback: If parsing failed or no scenes were generated, create generic scenes
        if (scenes.Count == 0)
        {
            _logger.LogWarning("No scenes parsed from LLM response, generating fallback scenes");
            var sceneCount = 6;
            var sceneDuration = targetDuration / sceneCount;
            var scenePurposes = new[] { "Hook", "Context", "Main Content", "Example", "Deep Dive", "Call to Action" };

            for (int i = 0; i < sceneCount; i++)
            {
                scenes.Add(new StoryboardScene(
                    SceneNumber: i + 1,
                    Description: $"Scene {i + 1}: {scenePurposes[i]} - Visual storytelling element",
                    VisualStyle: i == 0 ? "Dynamic, attention-grabbing" : "Clear, focused",
                    DurationSeconds: sceneDuration,
                    Purpose: scenePurposes[i],
                    ShotList: new List<string>
                    {
                        "Establishing shot",
                        "Medium close-up",
                        "Detail shot",
                        "Transition element"
                    },
                    TransitionType: i < sceneCount - 1 ? "Smooth fade" : null
                ));
            }
        }

        return scenes;
    }

    private (ConceptIdea, string) ParseRefineConceptResponse(
        string response,
        ConceptIdea originalConcept,
        string direction)
    {
        ConceptIdea refined = originalConcept;
        string summary = $"Refined concept based on '{direction}' direction. Enhanced clarity and focus.";

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            if (jsonDoc.RootElement.TryGetProperty("refinedConcept", out var refinedConceptElement))
            {
                var title = refinedConceptElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : originalConcept.Title;
                var description = refinedConceptElement.TryGetProperty("description", out var descElement) ? descElement.GetString() : originalConcept.Description;
                var angle = refinedConceptElement.TryGetProperty("angle", out var angleElement) ? angleElement.GetString() : originalConcept.Angle;
                var targetAudience = refinedConceptElement.TryGetProperty("targetAudience", out var audienceElement) ? audienceElement.GetString() : originalConcept.TargetAudience;
                var appealScore = refinedConceptElement.TryGetProperty("appealScore", out var scoreElement) ? scoreElement.GetDouble() : originalConcept.AppealScore;
                var hook = refinedConceptElement.TryGetProperty("hook", out var hookElement) ? hookElement.GetString() : originalConcept.Hook;

                var pros = originalConcept.Pros;
                if (refinedConceptElement.TryGetProperty("pros", out var prosArray) && prosArray.ValueKind == JsonValueKind.Array)
                {
                    pros = new List<string>();
                    foreach (var pro in prosArray.EnumerateArray())
                    {
                        var proText = pro.GetString();
                        if (!string.IsNullOrEmpty(proText))
                        {
                            pros.Add(proText);
                        }
                    }
                }

                var cons = originalConcept.Cons;
                if (refinedConceptElement.TryGetProperty("cons", out var consArray) && consArray.ValueKind == JsonValueKind.Array)
                {
                    cons = new List<string>();
                    foreach (var con in consArray.EnumerateArray())
                    {
                        var conText = con.GetString();
                        if (!string.IsNullOrEmpty(conText))
                        {
                            cons.Add(conText);
                        }
                    }
                }

                List<string>? talkingPoints = originalConcept.TalkingPoints;
                if (refinedConceptElement.TryGetProperty("talkingPoints", out var talkingPointsArray) && talkingPointsArray.ValueKind == JsonValueKind.Array)
                {
                    talkingPoints = new List<string>();
                    foreach (var point in talkingPointsArray.EnumerateArray())
                    {
                        var pointText = point.GetString();
                        if (!string.IsNullOrEmpty(pointText))
                        {
                            talkingPoints.Add(pointText);
                        }
                    }
                    if (talkingPoints.Count == 0)
                    {
                        talkingPoints = null;
                    }
                }

                refined = originalConcept with
                {
                    ConceptId = Guid.NewGuid().ToString(),
                    Title = title ?? originalConcept.Title,
                    Description = description ?? originalConcept.Description,
                    Angle = angle ?? originalConcept.Angle,
                    TargetAudience = targetAudience ?? originalConcept.TargetAudience,
                    Pros = pros,
                    Cons = cons,
                    AppealScore = appealScore,
                    Hook = hook ?? originalConcept.Hook,
                    TalkingPoints = talkingPoints
                };
            }

            if (jsonDoc.RootElement.TryGetProperty("changesSummary", out var summaryElement))
            {
                summary = summaryElement.GetString() ?? summary;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for refine concept, using fallback");
            // Use fallback refined concept
            refined = originalConcept with
            {
                ConceptId = Guid.NewGuid().ToString(),
                Title = $"{originalConcept.Title} (Refined)",
                Description = $"{originalConcept.Description} [Refined based on {direction}]",
                AppealScore = Math.Min(100, originalConcept.AppealScore + 5)
            };
        }

        return (refined, summary);
    }

    private List<ClarifyingQuestion> ParseQuestionsResponse(string response)
    {
        var questions = new List<ClarifyingQuestion>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);

            if (jsonDoc.RootElement.TryGetProperty("questions", out var questionsArray))
            {
                foreach (var questionElement in questionsArray.EnumerateArray())
                {
                    var questionText = questionElement.GetProperty("question").GetString() ?? "";
                    var context = questionElement.GetProperty("context").GetString() ?? "";
                    var questionType = questionElement.GetProperty("questionType").GetString() ?? "open-ended";

                    List<string>? suggestedAnswers = null;
                    if (questionElement.TryGetProperty("suggestedAnswers", out var answersArray) && answersArray.ValueKind == JsonValueKind.Array)
                    {
                        suggestedAnswers = new List<string>();
                        foreach (var answer in answersArray.EnumerateArray())
                        {
                            var answerText = answer.GetString();
                            if (!string.IsNullOrEmpty(answerText))
                            {
                                suggestedAnswers.Add(answerText);
                            }
                        }
                        if (suggestedAnswers.Count == 0)
                        {
                            suggestedAnswers = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(questionText))
                    {
                        questions.Add(new ClarifyingQuestion(
                            QuestionId: Guid.NewGuid().ToString(),
                            Question: questionText,
                            Context: context,
                            SuggestedAnswers: suggestedAnswers,
                            QuestionType: questionType
                        ));
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM for questions, using fallback");
        }

        // Fallback: If parsing failed or no questions were generated, create generic questions
        if (questions.Count == 0)
        {
            _logger.LogWarning("No questions parsed from LLM response, generating fallback questions");
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What specific problem does this video solve for your audience?",
                Context: "Understanding the problem helps create targeted, valuable content",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            ));
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "How much prior knowledge should viewers have?",
                Context: "This determines the depth and pacing of explanations",
                SuggestedAnswers: new List<string> { "Complete beginner", "Some familiarity", "Advanced" },
                QuestionType: "multiple-choice"
            ));
            questions.Add(new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What emotions should viewers feel while watching?",
                Context: "Emotional tone guides the creative direction",
                SuggestedAnswers: new List<string> { "Inspired", "Educated", "Entertained", "Empowered" },
                QuestionType: "multiple-choice"
            ));
        }

        return questions;
    }

    private List<BriefVariant> ParseIdeaToBriefResponse(string response, IdeaToBriefRequest request)
    {
        var variants = new List<BriefVariant>();

        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            if (jsonDoc.RootElement.TryGetProperty("variants", out var variantsArray))
            {
                foreach (var variantElement in variantsArray.EnumerateArray())
                {
                    var approach = variantElement.GetProperty("approach").GetString() ?? "educational";
                    var topic = variantElement.GetProperty("topic").GetString() ?? request.Idea;
                    var audience = variantElement.GetProperty("audience").GetString() ?? request.Audience ?? "General";
                    var goal = variantElement.GetProperty("goal").GetString() ?? "Educate";
                    var tone = variantElement.GetProperty("tone").GetString() ?? "Professional";
                    var durationSec = variantElement.GetProperty("targetDurationSeconds").GetInt32();
                    var pacingStr = variantElement.GetProperty("pacing").GetString() ?? "Conversational";
                    var densityStr = variantElement.GetProperty("density").GetString() ?? "Balanced";
                    var style = variantElement.GetProperty("style").GetString() ?? "Tutorial";
                    var explanation = variantElement.GetProperty("explanation").GetString() ?? "";
                    var suitabilityScore = variantElement.GetProperty("suitabilityScore").GetDouble();

                    var strengths = new List<string>();
                    if (variantElement.TryGetProperty("strengths", out var strengthsArray))
                    {
                        foreach (var strength in strengthsArray.EnumerateArray())
                        {
                            strengths.Add(strength.GetString() ?? "");
                        }
                    }

                    var considerations = new List<string>();
                    if (variantElement.TryGetProperty("considerations", out var considerationsArray))
                    {
                        foreach (var consideration in considerationsArray.EnumerateArray())
                        {
                            considerations.Add(consideration.GetString() ?? "");
                        }
                    }

                    // Convert pacing and density strings to enums
                    var pacing = Enum.TryParse<Pacing>(pacingStr, ignoreCase: true, out var pacingEnum)
                        ? pacingEnum : Pacing.Conversational;
                    var density = Enum.TryParse<Density>(densityStr, ignoreCase: true, out var densityEnum)
                        ? densityEnum : Density.Balanced;

                    var brief = new Brief(
                        Topic: topic,
                        Audience: audience,
                        Goal: goal,
                        Tone: tone,
                        Language: request.Language ?? "en-US",
                        Aspect: Aspect.Widescreen16x9
                    );

                    var planSpec = new PlanSpec(
                        TargetDuration: TimeSpan.FromSeconds(durationSec),
                        Pacing: pacing,
                        Density: density,
                        Style: style
                    );

                    variants.Add(new BriefVariant(
                        VariantId: Guid.NewGuid().ToString(),
                        Approach: approach,
                        Brief: brief,
                        PlanSpec: planSpec,
                        Explanation: explanation,
                        SuitabilityScore: suitabilityScore,
                        Strengths: strengths,
                        Considerations: considerations
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response for idea-to-brief, using fallback");
        }

        // Fallback: Generate default variants if parsing failed
        if (variants.Count == 0)
        {
            variants.Add(CreateDefaultBriefVariant(request, "educational", 120));
            variants.Add(CreateDefaultBriefVariant(request, "storytelling", 90));
            variants.Add(CreateDefaultBriefVariant(request, "practical", 60));
        }

        return variants.Take(Math.Min(request.VariantCount ?? 3, 4)).ToList();
    }

    private BriefVariant CreateDefaultBriefVariant(IdeaToBriefRequest request, string approach, int durationSec)
    {
        // Map approach to goal and tone in an open-ended way
        var (goal, tone, style) = approach switch
        {
            "educational" => ("Educate and inform viewers", "Clear and accessible", "Step-by-step explanation"),
            "storytelling" => ("Inspire through narrative", "Engaging and emotive", "Narrative-driven journey"),
            "practical" => ("Demonstrate real-world application", "Direct and actionable", "Hands-on demonstration"),
            _ => ("Inform and engage", "Professional yet approachable", "Balanced informational")
        };

        var brief = new Brief(
            Topic: request.Idea,
            Audience: request.Audience ?? "General audience seeking to understand this topic",
            Goal: goal,
            Tone: tone,
            Language: request.Language ?? "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var pacing = durationSec <= 60 ? Pacing.Fast : Pacing.Conversational;
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(durationSec),
            Pacing: pacing,
            Density: Density.Balanced,
            Style: style
        );

        var approachDescription = approach switch
        {
            "educational" => "Clear, structured educational approach",
            "storytelling" => "Narrative-driven storytelling approach",
            "practical" => "Hands-on practical demonstration",
            _ => $"Balanced {approach} approach"
        };

        return new BriefVariant(
            VariantId: Guid.NewGuid().ToString(),
            Approach: approachDescription,
            Brief: brief,
            PlanSpec: planSpec,
            Explanation: $"This {approachDescription} presents the concept in a way that resonates with the target audience while remaining true to the core idea.",
            SuitabilityScore: 80,
            Strengths: new List<string>
            {
                $"Matches the {approach} creative direction",
                "Appropriate pacing for the platform",
                "Accessible to target audience"
            },
            Considerations: new List<string>
            {
                "Consider adding specific examples relevant to your audience",
                "Visual aids can enhance understanding"
            }
        );
    }

    /// <summary>
    /// Enhance/improve a video topic description using AI
    /// </summary>
    public async Task<EnhanceTopicResponse> EnhanceTopicAsync(
        EnhanceTopicRequest request,
        CancellationToken ct = default)
    {
        // Input validation
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic cannot be null or empty", nameof(request));
        }

        if (request.Topic.Length > MaxTopicLength)
        {
            throw new ArgumentException($"Topic length exceeds maximum of {MaxTopicLength} characters", nameof(request));
        }

        _logger.LogInformation("Enhancing topic: {Topic} (length: {Length})", request.Topic, request.Topic.Length);

        try
        {

            var prompt = BuildEnhanceTopicPrompt(request);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for enhance topic: {Topic}", request.Topic);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            // Extract enhanced topic from response
            var enhancedTopic = ExtractEnhancedTopic(response, request.Topic);
            var improvements = ExtractImprovements(response);

            _logger.LogInformation("Successfully enhanced topic: {OriginalTopic} -> {EnhancedTopic}",
                request.Topic, enhancedTopic);

            return new EnhanceTopicResponse(
                EnhancedTopic: enhancedTopic,
                OriginalTopic: request.Topic,
                Improvements: improvements,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Enhance topic operation was cancelled for topic: {Topic}", request.Topic);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing topic: {Topic}", request.Topic);
            throw;
        }
    }

    private string BuildEnhanceTopicPrompt(EnhanceTopicRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert video content strategist. Your task is to enhance and improve the following video topic description to make it more specific, engaging, and effective for video creation.");
        sb.AppendLine();
        sb.AppendLine($"Original topic: {request.Topic}");

        if (!string.IsNullOrWhiteSpace(request.VideoType))
        {
            sb.AppendLine($"Video type: {request.VideoType}");
        }
        if (!string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            sb.AppendLine($"Target audience: {request.TargetAudience}");
        }
        if (!string.IsNullOrWhiteSpace(request.KeyMessage))
        {
            sb.AppendLine($"Key message: {request.KeyMessage}");
        }

        sb.AppendLine();
        sb.AppendLine("Please provide an enhanced version of the topic that:");
        sb.AppendLine("1. Is more specific and detailed");
        sb.AppendLine("2. Includes actionable elements");
        sb.AppendLine("3. Is engaging and clear");
        sb.AppendLine("4. Maintains the original intent");
        sb.AppendLine("5. Is optimized for video content creation");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: Respond with ONLY the enhanced topic description. Do NOT include:");
        sb.AppendLine("- The system prompt or instructions");
        sb.AppendLine("- Explanations or commentary");
        sb.AppendLine("- Markdown formatting");
        sb.AppendLine("- The phrase 'Original topic:' or 'Enhanced topic:'");
        sb.AppendLine();
        sb.AppendLine("Just return the improved topic text, keeping it between 50-500 characters.");

        return sb.ToString();
    }

    private string ExtractEnhancedTopic(string llmResponse, string originalTopic)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            return originalTopic;
        }

        // CRITICAL FIX: Remove the system prompt if it was included in the response
        // The LLM sometimes echoes back the prompt, so we need to strip it out
        var systemPromptMarker = "You are an expert video content strategist";
        var originalTopicMarker = $"Original topic: {originalTopic}";

        // If the response contains the system prompt, extract everything after it
        if (llmResponse.Contains(systemPromptMarker))
        {
            // Find where the actual response starts (after the prompt)
            var promptEndMarkers = new[]
            {
                "Respond with ONLY the enhanced topic description",
                "Keep it between 50-500 characters",
                originalTopicMarker
            };

            int startIndex = 0;
            foreach (var marker in promptEndMarkers)
            {
                var markerIndex = llmResponse.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex >= 0)
                {
                    // Find the end of this line and start from the next line
                    var lineEnd = llmResponse.IndexOf('\n', markerIndex);
                    if (lineEnd >= 0 && lineEnd < llmResponse.Length - 1)
                    {
                        startIndex = lineEnd + 1;
                        break;
                    }
                }
            }

            if (startIndex > 0)
            {
                llmResponse = llmResponse.Substring(startIndex).Trim();
            }
        }

        // Try to extract the enhanced topic from the LLM response
        // Remove markdown formatting, quotes, and extra whitespace
        var cleaned = llmResponse
            .Replace("```", "")
            .Replace("**", "")
            .Replace("*", "")
            .Trim();

        // Remove common prefixes
        var prefixes = new[] {
            "Enhanced topic:",
            "Topic:",
            "Here's the enhanced version:",
            "Enhanced:",
            "Here is the enhanced topic:",
            "The enhanced topic is:"
        };
        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
            }
        }

        // Remove quotes if present
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
        }

        // If the response contains the original prompt text, try to find just the enhanced part
        if (cleaned.Contains("You are an expert") || cleaned.Contains("Original topic:"))
        {
            // Try to find the last sentence or paragraph that doesn't contain prompt markers
            var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var validLines = lines
                .Where(line =>
                    !line.Contains("You are an expert") &&
                    !line.Contains("Original topic:") &&
                    !line.Contains("Please provide") &&
                    !line.Contains("Respond with") &&
                    line.Trim().Length > 10)
                .ToList();

            if (validLines.Any())
            {
                cleaned = string.Join(" ", validLines).Trim();
            }
        }

        // If the response is too long or seems to contain explanations, try to extract just the topic
        if (cleaned.Length > 500 || cleaned.Contains("\n\n"))
        {
            // Take first paragraph or first 500 chars
            var paragraphs = cleaned.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length > 0)
            {
                var firstParagraph = paragraphs[0].Trim();
                if (firstParagraph.Length <= 500 && firstParagraph.Length > 10)
                {
                    cleaned = firstParagraph;
                }
                else
                {
                    // Take first line if paragraph is too long
                    var firstLine = cleaned.Split('\n')[0].Trim();
                    if (firstLine.Length <= 500 && firstLine.Length > 10)
                    {
                        cleaned = firstLine;
                    }
                    else
                    {
                        cleaned = cleaned.Substring(0, Math.Min(500, cleaned.Length));
                    }
                }
            }
        }

        // If cleaned response is too short, same as original, or still contains prompt markers, return original
        if (cleaned.Length < 10 ||
            cleaned.Equals(originalTopic, StringComparison.OrdinalIgnoreCase) ||
            cleaned.Contains("You are an expert") ||
            cleaned.Contains("Original topic:"))
        {
            _logger.LogWarning("Failed to extract enhanced topic from response, returning original. Response: {Response}", llmResponse);
            return originalTopic;
        }

        return cleaned;
    }

    private string? ExtractImprovements(string llmResponse)
    {
        // Try to extract improvement notes if present
        if (llmResponse.Contains("Improvements:") || llmResponse.Contains("Changes:"))
        {
            var lines = llmResponse.Split('\n');
            var improvements = new List<string>();
            bool capturing = false;
            bool foundEmptyLineAfterCapture = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check if this line starts a new section (common section markers)
                if (capturing && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Stop capturing if we encounter a new section header
                    var lowerLine = trimmedLine.ToLowerInvariant();
                    if (lowerLine.StartsWith("note:") ||
                        lowerLine.StartsWith("summary:") ||
                        lowerLine.StartsWith("conclusion:") ||
                        lowerLine.StartsWith("next steps:") ||
                        lowerLine.StartsWith("additional") ||
                        (trimmedLine.Length > 0 && char.IsUpper(trimmedLine[0]) && trimmedLine.EndsWith(":")))
                    {
                        break; // Stop capturing at section boundary
                    }
                }

                if (trimmedLine.Contains("Improvements:") || trimmedLine.Contains("Changes:"))
                {
                    capturing = true;
                    foundEmptyLineAfterCapture = false;
                    continue;
                }

                if (capturing)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // If we've already captured some improvements and hit an empty line,
                        // stop capturing to avoid including unrelated content
                        if (improvements.Count > 0)
                        {
                            foundEmptyLineAfterCapture = true;
                        }
                    }
                    else if (foundEmptyLineAfterCapture)
                    {
                        // We hit an empty line after capturing, and now we have content again
                        // This likely means we've moved to a new section, so stop capturing
                        break;
                    }
                    else
                    {
                        // Capture this improvement line
                        improvements.Add(trimmedLine);
                    }
                }
            }

            if (improvements.Count > 0)
            {
                return string.Join("; ", improvements);
            }
        }

        return null;
    }

    /// <summary>
    /// Analyze prompt quality for video creation using LLM-based analysis
    /// </summary>
    public async Task<AnalyzePromptQualityResponse> AnalyzePromptQualityAsync(
        AnalyzePromptQualityRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic cannot be null or empty", nameof(request));
        }

        _logger.LogInformation("Analyzing prompt quality for topic: {Topic} (length: {Length})",
            request.Topic, request.Topic.Length);

        // Try to retrieve RAG context if enabled
        RagContext? ragContext = null;
        if (_ragContextBuilder != null && request.RagConfiguration != null && request.RagConfiguration.Enabled)
        {
            try
            {
                var ragConfig = new RagConfig
                {
                    Enabled = true,
                    TopK = request.RagConfiguration.TopK,
                    MinimumScore = request.RagConfiguration.MinimumScore,
                    MaxContextTokens = request.RagConfiguration.MaxContextTokens,
                    IncludeCitations = request.RagConfiguration.IncludeCitations
                };
                ragContext = await _ragContextBuilder.BuildContextAsync(request.Topic, ragConfig, ct).ConfigureAwait(false);
                if (ragContext.Chunks.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} RAG chunks for prompt quality analysis", ragContext.Chunks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve RAG context for prompt quality analysis, continuing without RAG");
            }
        }

        try
        {
            var prompt = BuildPromptQualityAnalysisPrompt(request, ragContext);

            if (prompt.Length > MaxPromptLength)
            {
                _logger.LogWarning("Generated prompt exceeds maximum length ({Length} > {MaxLength}), truncating",
                    prompt.Length, MaxPromptLength);
                prompt = prompt.Substring(0, MaxPromptLength);
            }

            var response = await GenerateWithLlmAsync(prompt, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("LLM returned empty response for prompt quality analysis");
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            var analysis = ParsePromptQualityAnalysisResponse(response, request);

            _logger.LogInformation("Successfully analyzed prompt quality. Score: {Score}, Level: {Level}",
                analysis.Score, analysis.Level);

            return analysis;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Prompt quality analysis operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing prompt quality");
            throw;
        }
    }

    private string BuildPromptQualityAnalysisPrompt(AnalyzePromptQualityRequest request, RagContext? ragContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert video content strategist and prompt engineer. Your task is to analyze a video creation prompt for quality, providing specific scores and actionable suggestions.");
        sb.AppendLine();
        sb.AppendLine("VIDEO PROMPT TO ANALYZE:");
        sb.AppendLine($"Topic: {request.Topic}");

        if (!string.IsNullOrWhiteSpace(request.VideoType))
        {
            sb.AppendLine($"Video Type: {request.VideoType}");
        }
        if (!string.IsNullOrWhiteSpace(request.TargetAudience))
        {
            sb.AppendLine($"Target Audience: {request.TargetAudience}");
        }
        if (!string.IsNullOrWhiteSpace(request.KeyMessage))
        {
            sb.AppendLine($"Key Message: {request.KeyMessage}");
        }

        // Add RAG context if available
        if (ragContext != null && ragContext.Chunks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("RELEVANT CONTEXT FROM KNOWLEDGE BASE:");
            foreach (var chunk in ragContext.Chunks.Take(3))
            {
                sb.AppendLine($"- {chunk.Content}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("ANALYSIS REQUIREMENTS:");
        sb.AppendLine("Analyze this prompt across 6 dimensions (each scored 0-100):");
        sb.AppendLine("1. LENGTH: Is the prompt detailed enough (20-100 words optimal)? Does it provide sufficient context?");
        sb.AppendLine("2. SPECIFICITY: Does it avoid vague terms? Are concrete details, examples, or actions specified?");
        sb.AppendLine("3. CLARITY: Is the intent clear? Will the creator understand exactly what to make?");
        sb.AppendLine("4. ACTIONABILITY: Does it include actionable elements? Can it be translated directly into video scenes?");
        sb.AppendLine("5. ENGAGEMENT: Will this create engaging content? Does it consider audience needs and interests?");
        sb.AppendLine("6. ALIGNMENT: Do topic, audience, key message, and video type work well together?");

        sb.AppendLine();
        sb.AppendLine("You MUST respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"score\": 75,");
        sb.AppendLine("  \"level\": \"good\",");
        sb.AppendLine("  \"metrics\": {");
        sb.AppendLine("    \"length\": 80,");
        sb.AppendLine("    \"specificity\": 75,");
        sb.AppendLine("    \"clarity\": 85,");
        sb.AppendLine("    \"actionability\": 70,");
        sb.AppendLine("    \"engagement\": 80,");
        sb.AppendLine("    \"alignment\": 75");
        sb.AppendLine("  },");
        sb.AppendLine("  \"suggestions\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"type\": \"tip\",");
        sb.AppendLine("      \"message\": \"Specific, actionable suggestion text\"");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        sb.AppendLine();
        sb.AppendLine("RULES:");
        sb.AppendLine("- score: Overall quality score 0-100 (weighted average of metrics)");
        sb.AppendLine("- level: \"excellent\" (80-100), \"good\" (60-79), \"fair\" (40-59), or \"poor\" (0-39)");
        sb.AppendLine("- metrics: All values 0-100");
        sb.AppendLine("- suggestions: Array of 2-5 specific, actionable suggestions");
        sb.AppendLine("  - type: \"success\" (strength), \"warning\" (issue), \"info\" (note), or \"tip\" (improvement)");
        sb.AppendLine("  - message: Clear, specific, actionable text (1-2 sentences max)");
        sb.AppendLine("- Respond ONLY with valid JSON, no additional text or explanation");

        return sb.ToString();
    }

    private AnalyzePromptQualityResponse ParsePromptQualityAnalysisResponse(
        string llmResponse,
        AnalyzePromptQualityRequest request)
    {
        try
        {
            // Try to extract JSON from response (might have markdown code blocks)
            var jsonText = llmResponse;

            // Remove markdown code blocks if present
            if (jsonText.Contains("```json"))
            {
                var startIndex = jsonText.IndexOf("```json") + 7;
                var endIndex = jsonText.IndexOf("```", startIndex);
                if (endIndex > startIndex)
                {
                    jsonText = jsonText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            else if (jsonText.Contains("```"))
            {
                var startIndex = jsonText.IndexOf("```") + 3;
                var endIndex = jsonText.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    jsonText = jsonText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            // Parse JSON
            using var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var score = root.TryGetProperty("score", out var scoreElement)
                ? scoreElement.GetInt32()
                : CalculateFallbackScore(request);

            var level = root.TryGetProperty("level", out var levelElement)
                ? levelElement.GetString()?.ToLowerInvariant() ?? "fair"
                : DetermineLevel(score);

            // Parse metrics
            var metrics = new Dictionary<string, int>();
            if (root.TryGetProperty("metrics", out var metricsElement))
            {
                foreach (var prop in metricsElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        metrics[prop.Name] = Math.Clamp(prop.Value.GetInt32(), 0, 100);
                    }
                }
            }

            // Ensure all required metrics are present with fallback values
            var requiredMetrics = new[] { "length", "specificity", "clarity", "actionability", "engagement", "alignment" };
            foreach (var metric in requiredMetrics)
            {
                if (!metrics.ContainsKey(metric))
                {
                    metrics[metric] = CalculateFallbackMetric(metric, request);
                }
            }

            // Parse suggestions
            var suggestions = new List<QualitySuggestion>();
            if (root.TryGetProperty("suggestions", out var suggestionsElement) &&
                suggestionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var suggestionElement in suggestionsElement.EnumerateArray())
                {
                    var type = suggestionElement.TryGetProperty("type", out var typeElement)
                        ? typeElement.GetString() ?? "info"
                        : "info";
                    var message = suggestionElement.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString() ?? ""
                        : "";

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        suggestions.Add(new QualitySuggestion(type, message));
                    }
                }
            }

            // Add fallback suggestions if none provided
            if (suggestions.Count == 0)
            {
                suggestions.AddRange(GenerateFallbackSuggestions(request, score));
            }

            return new AnalyzePromptQualityResponse(
                Score: score,
                Level: level,
                Metrics: metrics,
                Suggestions: suggestions,
                GeneratedAt: DateTime.UtcNow
            );
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response for prompt quality analysis, using fallback");
            return CreateFallbackAnalysis(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing prompt quality analysis response, using fallback");
            return CreateFallbackAnalysis(request);
        }
    }

    private AnalyzePromptQualityResponse CreateFallbackAnalysis(AnalyzePromptQualityRequest request)
    {
        var score = CalculateFallbackScore(request);
        return new AnalyzePromptQualityResponse(
            Score: score,
            Level: DetermineLevel(score),
            Metrics: new Dictionary<string, int>
            {
                ["length"] = CalculateFallbackMetric("length", request),
                ["specificity"] = CalculateFallbackMetric("specificity", request),
                ["clarity"] = CalculateFallbackMetric("clarity", request),
                ["actionability"] = CalculateFallbackMetric("actionability", request),
                ["engagement"] = CalculateFallbackMetric("engagement", request),
                ["alignment"] = CalculateFallbackMetric("alignment", request),
            },
            Suggestions: GenerateFallbackSuggestions(request, score),
            GeneratedAt: DateTime.UtcNow
        );
    }

    private int CalculateFallbackScore(AnalyzePromptQualityRequest request)
    {
        var wordCount = request.Topic.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var hasAudience = !string.IsNullOrWhiteSpace(request.TargetAudience) && request.TargetAudience.Length > 5;
        var hasKeyMessage = !string.IsNullOrWhiteSpace(request.KeyMessage) && request.KeyMessage.Length > 5;
        var hasVideoType = !string.IsNullOrWhiteSpace(request.VideoType);

        var score = 0;
        score += (int)Math.Min((wordCount / 30.0) * 20, 20); // Length
        score += wordCount > 15 ? 15 : 5; // Specificity
        score += (hasAudience && hasKeyMessage) ? 20 : 10; // Clarity
        score += wordCount > 20 ? 15 : 8; // Actionability
        score += (hasVideoType && hasAudience) ? 15 : 8; // Engagement
        score += (hasKeyMessage && hasAudience) ? 15 : 5; // Alignment

        return Math.Clamp(score, 0, 100);
    }

    private int CalculateFallbackMetric(string metricName, AnalyzePromptQualityRequest request)
    {
        var wordCount = request.Topic.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var hasAudience = !string.IsNullOrWhiteSpace(request.TargetAudience) && request.TargetAudience.Length > 5;
        var hasKeyMessage = !string.IsNullOrWhiteSpace(request.KeyMessage) && request.KeyMessage.Length > 5;
        var hasVideoType = !string.IsNullOrWhiteSpace(request.VideoType);

        return metricName switch
        {
            "length" => Math.Clamp((int)((wordCount / 50.0) * 100), 0, 100),
            "specificity" => wordCount > 15 ? 75 : 40,
            "clarity" => (hasAudience && hasKeyMessage) ? 85 : 50,
            "actionability" => wordCount > 20 ? 70 : 40,
            "engagement" => (hasVideoType && hasAudience) ? 80 : 50,
            "alignment" => (hasKeyMessage && hasAudience) ? 75 : 40,
            _ => 50
        };
    }

    private string DetermineLevel(int score) => score switch
    {
        >= 80 => "excellent",
        >= 60 => "good",
        >= 40 => "fair",
        _ => "poor"
    };

    private List<QualitySuggestion> GenerateFallbackSuggestions(AnalyzePromptQualityRequest request, int score)
    {
        var suggestions = new List<QualitySuggestion>();
        var wordCount = request.Topic.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        if (score >= 80)
        {
            suggestions.Add(new QualitySuggestion("success", "Great prompt! You have a clear topic, audience, and message."));
        }
        else
        {
            if (wordCount < 15)
            {
                suggestions.Add(new QualitySuggestion("warning", "Add more detail to your prompt for better results. Include specific examples or context."));
            }
            if (string.IsNullOrWhiteSpace(request.TargetAudience) || request.TargetAudience.Length < 5)
            {
                suggestions.Add(new QualitySuggestion("info", "A well-defined target audience helps create more targeted content."));
            }
            if (string.IsNullOrWhiteSpace(request.KeyMessage) || request.KeyMessage.Length < 10)
            {
                suggestions.Add(new QualitySuggestion("tip", "A clear key message helps focus your video content."));
            }
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new QualitySuggestion("info", "Consider adding more specific details to improve video quality."));
        }

        return suggestions;
    }

    /// <summary>
    /// Helper method to execute LLM generation (backward compatibility - no parameters)
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        string prompt,
        CancellationToken ct)
    {
        return await GenerateWithLlmAsync(prompt, (BrainstormRequest?)null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method to execute LLM generation with proper parameter handling for all providers
    /// Supports model override, temperature, and other LLM parameters
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        string prompt,
        BrainstormRequest? request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prompt, nameof(prompt));

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }

        if (prompt.Length > MaxPromptLength)
        {
            _logger.LogWarning("Prompt length ({Length}) exceeds maximum ({MaxLength}), truncating",
                prompt.Length, MaxPromptLength);
            prompt = prompt.Substring(0, MaxPromptLength);
        }

        var providerType = _llmProvider.GetType().Name;
        var llmParams = request?.LlmParameters;

        _logger.LogInformation(
            "Calling LLM provider for ideation (Provider: {Provider}, ModelOverride: {ModelOverride}, Temperature: {Temperature})",
            providerType, llmParams?.ModelOverride ?? "default", llmParams?.Temperature?.ToString() ?? "default");

        try
        {
            var startTime = DateTime.UtcNow;
            string response;

            // Use provider-specific parameter handling if LLM parameters are provided
            if (request != null && llmParams != null)
            {

                // Use DraftScriptAsync for proper parameter support if any parameters are specified
                if (!string.IsNullOrWhiteSpace(llmParams.ModelOverride) ||
                    llmParams.Temperature.HasValue || llmParams.TopP.HasValue ||
                    llmParams.TopK.HasValue || llmParams.MaxTokens.HasValue)
                {
                    // LLM parameters specified - use provider-specific handling
                    response = await GenerateWithDraftScriptAsync(prompt, request, ct).ConfigureAwait(false);
                }
                else
                {
                    // No special parameters - use CompleteAsync for simplicity
                    _logger.LogDebug("Using CompleteAsync (no special parameters)");
                    response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
                }
            }
            else
            {
                // No request or LLM parameters - use CompleteAsync for simplicity
                _logger.LogDebug("Using CompleteAsync (no LLM parameters provided)");
                response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogDebug("LLM provider completed in {Duration}ms", duration.TotalMilliseconds);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("LLM provider ({Provider}) returned empty response", providerType);
                throw new InvalidOperationException("LLM provider returned an empty response. Please try again.");
            }

            // Validate response quality - check for common error patterns
            var trimmedResponse = response.Trim();
            _logger.LogDebug("LLM provider ({Provider}) returned response of length {Length} characters",
                providerType, response.Length);

            if (trimmedResponse.Length < 50)
            {
                _logger.LogWarning(
                    "LLM response is suspiciously short ({Length} chars) from provider {Provider}. " +
                    "Response: {Response}",
                    trimmedResponse.Length,
                    providerType,
                    trimmedResponse.Substring(0, Math.Min(100, trimmedResponse.Length)));
            }
            else
            {
                // Log a preview of the response for debugging
                var preview = trimmedResponse.Substring(0, Math.Min(200, trimmedResponse.Length));
                _logger.LogDebug("LLM response preview: {Preview}...", preview);
            }

            return response;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("LLM generation timed out for provider {Provider}", providerType);
            throw new TimeoutException("LLM generation timed out. Please try again.", ex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("LLM generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM provider {Provider}: {ErrorMessage}", providerType, ex.Message);

            // Provide helpful error message based on provider type
            var errorMessage = providerType switch
            {
                "OllamaLlmProvider" => "Failed to generate concepts with Ollama. Please ensure Ollama is running and the model is available.",
                "OpenAiLlmProvider" => "Failed to generate concepts with OpenAI. Please check your API key and network connection.",
                "GeminiLlmProvider" => "Failed to generate concepts with Gemini. Please check your API key and network connection.",
                _ => "Failed to generate concepts. Please try again or check your LLM provider configuration."
            };

            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Generate using provider-specific direct API calls with LLM parameters
    /// This ensures proper model override, temperature, and other parameter handling
    /// </summary>
    private async Task<string> GenerateWithDraftScriptAsync(
        string prompt,
        BrainstormRequest request,
        CancellationToken ct)
    {
        var providerType = _llmProvider.GetType();
        var providerTypeName = providerType.Name;
        var llmParams = request.LlmParameters;

        // Note: GenerateWithDraftScriptAsync is for script generation, not ideation
        // It uses a single combined prompt, so we can't use GenerateWithOllamaDirectAsync
        // which expects separate systemPrompt and userPrompt. Use CompleteAsync instead.

        // For other providers, we need to use CompleteAsync as DraftScriptAsync builds its own prompts
        // This means LLM parameters may not be fully applied for non-Ollama providers
        // However, model override might still work if the provider supports it via configuration
        _logger.LogWarning(
            "Using CompleteAsync for {Provider} - LLM parameters (model override, temperature, etc.) may not be fully applied. " +
            "Model override: {ModelOverride}, Temperature: {Temperature}. " +
            "For full parameter support, consider using Ollama provider.",
            providerTypeName, llmParams?.ModelOverride ?? "default", llmParams?.Temperature?.ToString() ?? "default");

        // Fallback to CompleteAsync - parameters may not be applied
        return await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate using Ollama API directly with /api/chat endpoint (similar to script generation approach)
    /// This bypasses CompositeLlmProvider fallback logic and ensures we use Ollama when available
    /// Uses /api/chat with format=json for ideation (requires structured JSON output)
    /// </summary>
    private async Task<string> GenerateWithOllamaDirectAsync(
        string systemPrompt,
        string userPrompt,
        LlmParameters parameters,
        BrainstormRequest request,
        CancellationToken ct)
    {
        // Use reflection to access Ollama provider's internal HttpClient and configuration
        // This allows us to call Ollama API directly with proper parameters (like script generation)
        var providerType = _llmProvider.GetType();
        System.Net.Http.HttpClient? httpClient = null;
        string baseUrl = "http://127.0.0.1:11434";
        string? defaultModel = null; // No hardcoded fallback - must get from provider configuration
        // Use a reasonable timeout - 3 minutes max for ideation to prevent indefinite hangs
        // If Ollama is unresponsive, we want to fail fast and fall back to CompositeLlmProvider
        TimeSpan timeout = TimeSpan.FromMinutes(3);
        int maxRetries = 3;

        // Check availability first (like script generation does)
        ILlmProvider? ollamaProvider = null;

        // Try to get Ollama provider - handle both direct OllamaLlmProvider and CompositeLlmProvider
        if (providerType.Name == "OllamaLlmProvider")
        {
            // Direct Ollama provider - get fields via reflection
            var httpClientField = providerType.GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var baseUrlField = providerType.GetField("_baseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var modelField = providerType.GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeoutField = providerType.GetField("_timeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxRetriesField = providerType.GetField("_maxRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (httpClientField != null && baseUrlField != null && modelField != null)
            {
                httpClient = (System.Net.Http.HttpClient?)httpClientField.GetValue(_llmProvider);
                baseUrl = (string?)baseUrlField.GetValue(_llmProvider) ?? baseUrl;
                defaultModel = (string?)modelField.GetValue(_llmProvider); // Get from provider, no fallback
                timeout = timeoutField?.GetValue(_llmProvider) as TimeSpan? ?? timeout;
                maxRetries = (int)(maxRetriesField?.GetValue(_llmProvider) ?? maxRetries);
            }
        }
        else if (providerType.Name == "CompositeLlmProvider")
        {
            // Composite provider - try to get Ollama provider from its internal providers
            try
            {
                // First, try to get providers via GetProviders() method to ensure they're initialized
                var getProvidersMethod = providerType.GetMethod("GetProviders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Collections.Generic.Dictionary<string, ILlmProvider>? providers = null;

                if (getProvidersMethod != null)
                {
                    // Call GetProviders(false) to get cached providers without forcing refresh
                    var providersResult = getProvidersMethod.Invoke(_llmProvider, new object[] { false });
                    providers = providersResult as System.Collections.Generic.Dictionary<string, ILlmProvider>;
                }

                // Fallback: try direct field access if method doesn't work
                if (providers == null)
                {
                    var providersField = providerType.GetField("_cachedProviders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (providersField != null)
                    {
                        providers = providersField.GetValue(_llmProvider) as System.Collections.Generic.Dictionary<string, ILlmProvider>;
                    }
                }

                if (providers != null && providers.TryGetValue("Ollama", out ollamaProvider) && ollamaProvider != null)
                {
                    var ollamaProviderType = ollamaProvider.GetType();
                    var httpClientField = ollamaProviderType.GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var baseUrlField = ollamaProviderType.GetField("_baseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var modelField = ollamaProviderType.GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var timeoutField = ollamaProviderType.GetField("_timeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var maxRetriesField = ollamaProviderType.GetField("_maxRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (httpClientField != null && baseUrlField != null && modelField != null)
                    {
                        httpClient = (System.Net.Http.HttpClient?)httpClientField.GetValue(ollamaProvider);
                        baseUrl = (string?)baseUrlField.GetValue(ollamaProvider) ?? baseUrl;
                        defaultModel = (string?)modelField.GetValue(ollamaProvider) ?? defaultModel; // Get from provider (may be null if not set)
                        timeout = timeoutField?.GetValue(ollamaProvider) as TimeSpan? ?? timeout;
                        maxRetries = (int)(maxRetriesField?.GetValue(ollamaProvider) ?? maxRetries);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract Ollama provider from CompositeLlmProvider via reflection");
            }
        }

        // Check availability first (like script generation does)
        if (ollamaProvider != null)
        {
            try
            {
                var availabilityMethod = ollamaProvider.GetType().GetMethod("IsServiceAvailableAsync",
                    new[] { typeof(CancellationToken), typeof(bool) });
                if (availabilityMethod != null)
                {
                    using var availabilityCts = new System.Threading.CancellationTokenSource();
                    availabilityCts.CancelAfter(TimeSpan.FromSeconds(5));
                    var availabilityTask = (Task<bool>)availabilityMethod.Invoke(ollamaProvider,
                        new object[] { availabilityCts.Token, false })!;
                    var isAvailable = await availabilityTask.ConfigureAwait(false);

                    if (!isAvailable)
                    {
                        _logger.LogError("Ollama is not available for ideation. Please ensure Ollama is running: 'ollama serve'");
                        throw new InvalidOperationException(
                            "Ollama is required for ideation but is not available. " +
                            "Please ensure Ollama is running: 'ollama serve' and verify models are installed: 'ollama list'");
                    }
                    _logger.LogInformation("Ollama availability check passed for ideation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check Ollama availability, proceeding with request attempt");
            }
        }

        // Track if we created the HttpClient so we can dispose it properly
        bool createdHttpClient = false;
        if (httpClient == null)
        {
            _logger.LogInformation("Creating new HttpClient for direct Ollama API call (baseUrl: {BaseUrl})", baseUrl);
            // Use a reasonable timeout - 5 minutes max for ideation (ideation can take time but shouldn't hang forever)
            // This prevents indefinite hangs if Ollama is unresponsive
            var httpTimeout = TimeSpan.FromMinutes(5);
            httpClient = new System.Net.Http.HttpClient
            {
                Timeout = httpTimeout
            };
            createdHttpClient = true;
            // Update the timeout to match HTTP client timeout to avoid confusion
            timeout = httpTimeout;
        }

        // Get LLM parameters with defaults (like script generation does)
        var llmParams = request.LlmParameters;
        
        // Apply user's explicit model override from the request (e.g., from the LLM selector in the UI)
        // Priority: request.LlmModel > llmParams.ModelOverride > defaultModel (from provider config)
        string? requestModelOverride = null;
        if (!string.IsNullOrWhiteSpace(request.LlmModel))
        {
            requestModelOverride = request.LlmModel;
            _logger.LogInformation("Using explicit model override from request: {Model} (Provider: {Provider})",
                request.LlmModel, request.LlmProvider ?? "not specified");
        }
        
        // Validate we have a model - either from request override, llmParams, or provider configuration
        if (string.IsNullOrWhiteSpace(defaultModel) && 
            string.IsNullOrWhiteSpace(llmParams?.ModelOverride) &&
            string.IsNullOrWhiteSpace(requestModelOverride))
        {
            throw new InvalidOperationException(
                "Cannot determine Ollama model for ideation. " +
                "Please ensure Ollama provider is properly configured with a model in Settings, " +
                "or select a model from the AI Model dropdown. " +
                "The model should be configured in Provider Settings (Ollama Model field).");
        }
        
        // Use model in priority order: request.LlmModel > llmParams.ModelOverride > defaultModel
        var modelToUse = !string.IsNullOrWhiteSpace(requestModelOverride)
            ? requestModelOverride
            : !string.IsNullOrWhiteSpace(llmParams?.ModelOverride)
                ? llmParams.ModelOverride
                : defaultModel!; // Safe because we validated above
        var temperature = parameters?.Temperature ?? llmParams?.Temperature ?? 0.7;
        var maxTokens = parameters?.MaxTokens ?? llmParams?.MaxTokens ?? 2048;
        var topP = parameters?.TopP ?? llmParams?.TopP ?? 0.9;
        var topK = parameters?.TopK ?? llmParams?.TopK;

        _logger.LogInformation(
            "Calling Ollama API directly for ideation (Model: {Model}, Temperature: {Temperature}, MaxTokens: {MaxTokens})",
            modelToUse, temperature, maxTokens);

        // Build combined prompt (like script generation does) - combine system and user prompts
        // Script generation uses a single prompt, not messages array
        var combinedPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? userPrompt
            : $"{systemPrompt}\n\n{userPrompt}";

        // Build Ollama API request with parameters (using /api/generate endpoint like script generation)
        var options = new Dictionary<string, object>
        {
            { "temperature", temperature },
            { "top_p", topP },
            { "num_predict", maxTokens }
        };

        if (topK.HasValue)
        {
            options["top_k"] = topK.Value;
        }

        // Use /api/generate endpoint with format=json for ideation (requires structured JSON output)
        // This matches the working script generation implementation
        var requestBody = new
        {
            model = modelToUse,
            prompt = combinedPrompt,
            stream = false,
            format = "json", // Ideation requires JSON format
            options = options
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

        Exception? lastException = null;
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogInformation("Retrying Ollama ideation (attempt {Attempt}/{MaxRetries}) after {Delay}s",
                        attempt + 1, maxRetries + 1, backoffDelay.TotalSeconds);
                    await Task.Delay(backoffDelay, ct).ConfigureAwait(false);
                }

                // CRITICAL FIX: Use independent timeout - don't link to parent token for timeout management
                // This prevents upstream components (frontend, API middleware) from cancelling our long-running operation
                // if they have shorter timeouts. The linked token approach would cancel if ANY upstream has a short timeout.
                using var cts = new System.Threading.CancellationTokenSource();
                cts.CancelAfter(timeout);

                // Still respect explicit user cancellation by checking the parent token
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Ideation was cancelled by user", ct);
                }

                _logger.LogInformation("Sending ideation request to Ollama (attempt {Attempt}/{MaxRetries}, timeout: {Timeout:F1} minutes)",
                    attempt + 1, maxRetries + 1, timeout.TotalMinutes);

                _logger.LogInformation("Request sent to Ollama, awaiting response (timeout: {Timeout:F1} minutes, this may take a while for large models)...",
                    timeout.TotalMinutes);

                // Start periodic heartbeat logging to show the system is still working
                // During a long wait, there's no visibility that the system is working without this
                var requestStartTime = DateTime.UtcNow;
                using var heartbeatCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                var heartbeatTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!heartbeatCts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30), heartbeatCts.Token).ConfigureAwait(false);
                            var elapsed = DateTime.UtcNow - requestStartTime;
                            var remaining = timeout.TotalSeconds - elapsed.TotalSeconds;
                            if (remaining > 0)
                            {
                                _logger.LogInformation(
                                    "Still awaiting Ollama ideation response... ({Elapsed:F0}s elapsed, {Remaining:F0}s remaining before timeout)",
                                    elapsed.TotalSeconds,
                                    remaining);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when request completes or is cancelled
                    }
                }, heartbeatCts.Token);

                // Use /api/generate endpoint (like script generation) - this is the correct endpoint for Ollama
                var response = await httpClient.PostAsync($"{baseUrl}/api/generate", content, cts.Token).ConfigureAwait(false);

                // Check for model not found error - if model doesn't exist, query for available models and use first one
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                    if (errorContent.Contains("model") && errorContent.Contains("not found"))
                    {
                        _logger.LogWarning("Model '{Model}' not found, querying Ollama for available models", modelToUse);
                        
                        // Query Ollama for available models (like script generation does)
                        try
                        {
                            using var tagsCts = new System.Threading.CancellationTokenSource();
                            tagsCts.CancelAfter(TimeSpan.FromSeconds(10));
                            var tagsResponse = await httpClient.GetAsync($"{baseUrl}/api/tags", tagsCts.Token).ConfigureAwait(false);
                            
                            if (tagsResponse.IsSuccessStatusCode)
                            {
                                var tagsContent = await tagsResponse.Content.ReadAsStringAsync(tagsCts.Token).ConfigureAwait(false);
                                var tagsDoc = System.Text.Json.JsonDocument.Parse(tagsContent);
                                
                                if (tagsDoc.RootElement.TryGetProperty("models", out var modelsArray) &&
                                    modelsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var availableModels = new List<string>();
                                    foreach (var modelElement in modelsArray.EnumerateArray())
                                    {
                                        if (modelElement.TryGetProperty("name", out var nameProp))
                                        {
                                            var name = nameProp.GetString();
                                            if (!string.IsNullOrEmpty(name))
                                            {
                                                availableModels.Add(name);
                                            }
                                        }
                                    }
                                    
                                    if (availableModels.Count > 0)
                                    {
                                        // Use the first available model (like script generation would)
                                        var fallbackModel = availableModels[0];
                                        _logger.LogInformation("Model '{RequestedModel}' not found, using first available model: '{FallbackModel}'. Available models: {AllModels}",
                                            modelToUse, fallbackModel, string.Join(", ", availableModels));
                                        modelToUse = fallbackModel;
                                        
                                        // Retry with the available model
                                        requestBody = new
                                        {
                                            model = modelToUse,
                                            prompt = combinedPrompt,
                                            stream = false,
                                            format = "json",
                                            options = options
                                        };
                                        json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                                        content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                                        response = await httpClient.PostAsync($"{baseUrl}/api/generate", content, cts.Token).ConfigureAwait(false);
                                        response.EnsureSuccessStatusCode();
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException(
                                            $"Model '{modelToUse}' not found and no models are available in Ollama. " +
                                            $"Please install a model using: ollama pull <model-name>");
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        $"Model '{modelToUse}' not found. Please pull the model first using: ollama pull {modelToUse}");
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"Model '{modelToUse}' not found. Please pull the model first using: ollama pull {modelToUse}");
                            }
                        }
                        catch (Exception ex) when (!(ex is InvalidOperationException))
                        {
                            _logger.LogError(ex, "Error querying Ollama for available models");
                            throw new InvalidOperationException(
                                $"Model '{modelToUse}' not found. Please pull the model first using: ollama pull {modelToUse}");
                        }
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }

                // Cancel heartbeat since we got a response
                heartbeatCts.Cancel();

                // CRITICAL: Use cts.Token instead of ct for ReadAsStringAsync (like script generation)
                // This prevents upstream components from cancelling our long-running operation
                var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    _logger.LogError("Ollama returned empty JSON response for ideation");
                    throw new InvalidOperationException("Ollama returned an empty JSON response");
                }

                _logger.LogDebug("Ollama API returned JSON response of length {Length} characters", responseJson.Length);

                // Parse and validate response structure (like script generation)
                System.Text.Json.JsonDocument? responseDoc = null;
                try
                {
                    responseDoc = System.Text.Json.JsonDocument.Parse(responseJson);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Ollama JSON response: {Response}",
                        responseJson.Substring(0, Math.Min(500, responseJson.Length)));
                    throw new InvalidOperationException("Ollama returned invalid JSON response", ex);
                }

                // Check for errors in response (like script generation)
                if (responseDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorMessage = errorElement.GetString() ?? "Unknown error";
                    _logger.LogError("Ollama API error: {Error}", errorMessage);
                    throw new InvalidOperationException($"Ollama API error: {errorMessage}");
                }

                // /api/generate returns response in 'response' field (like script generation)
                if (responseDoc.RootElement.TryGetProperty("response", out var responseText))
                {
                    var result = responseText.GetString() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        _logger.LogError("Ollama returned an empty response. Response JSON: {Response}",
                            responseJson.Substring(0, Math.Min(1000, responseJson.Length)));
                        throw new InvalidOperationException("Ollama returned an empty response");
                    }

                    _logger.LogInformation("Ollama ideation succeeded with {Length} characters", result.Length);
                    return result;
                }

                // Log available fields for debugging
                var availableFields = string.Join(", ", responseDoc.RootElement.EnumerateObject().Select(p => p.Name));
                _logger.LogError(
                    "Ollama response did not contain expected 'response' field. Available fields: {Fields}. Response: {Response}",
                    availableFields,
                    responseJson.Substring(0, Math.Min(500, responseJson.Length)));
                throw new InvalidOperationException($"Invalid response structure from Ollama. Expected 'response' field but got: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}");
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Ollama ideation timed out (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Ollama ideation timed out on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Ollama ideation connection failed (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Ollama ideation connection failed on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Error calling Ollama for ideation (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Error calling Ollama for ideation on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate concepts with Ollama after {maxRetries + 1} attempts. Please ensure Ollama is running and model '{modelToUse}' is available.",
            lastException);
    }

    /// <summary>
    /// Format a number with K/M/B suffixes for readability
    /// </summary>
    private static string FormatNumber(long number)
    {
        if (number >= 1_000_000_000)
            return $"{number / 1_000_000_000.0:F1}B";
        if (number >= 1_000_000)
            return $"{number / 1_000_000.0:F1}M";
        if (number >= 1_000)
            return $"{number / 1_000.0:F1}K";
        return number.ToString();
    }

    /// <summary>
    /// Clean JSON response by removing markdown code blocks and other LLM formatting artifacts
    /// </summary>
    private static string CleanJsonResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        var cleanedResponse = response.Trim();

        // Remove markdown code block markers with language specifier (case-insensitive)
        var jsonMarker = "```json";
        var jsonMarkerUpper = "```JSON";
        var codeMarker = "```";

        // Check for markdown code blocks at the start
        if (cleanedResponse.StartsWith(jsonMarker, StringComparison.OrdinalIgnoreCase))
        {
            cleanedResponse = cleanedResponse.Substring(jsonMarker.Length);
        }
        else if (cleanedResponse.StartsWith(codeMarker, StringComparison.OrdinalIgnoreCase))
        {
            cleanedResponse = cleanedResponse.Substring(codeMarker.Length);
        }

        // Remove markdown code blocks at the end
        if (cleanedResponse.EndsWith(codeMarker, StringComparison.OrdinalIgnoreCase))
        {
            cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - codeMarker.Length);
        }

        cleanedResponse = cleanedResponse.Trim();

        // Remove any leading/trailing whitespace or newlines that might interfere
        cleanedResponse = cleanedResponse.TrimStart('\r', '\n', ' ', '\t');
        cleanedResponse = cleanedResponse.TrimEnd('\r', '\n', ' ', '\t');

        // Try to extract JSON if there's text before/after
        // Look for the first opening brace and last closing brace
        var firstBrace = cleanedResponse.IndexOf('{');
        var lastBrace = cleanedResponse.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            cleanedResponse = cleanedResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
        }
        else
        {
            // If no braces found, try to find array brackets
            var firstBracket = cleanedResponse.IndexOf('[');
            var lastBracket = cleanedResponse.LastIndexOf(']');

            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                cleanedResponse = cleanedResponse.Substring(firstBracket, lastBracket - firstBracket + 1);
            }
        }

        // Final cleanup - remove any remaining leading/trailing whitespace
        cleanedResponse = cleanedResponse.Trim();

        return cleanedResponse;
    }

    /// <summary>
    /// Try to extract concepts from a non-JSON text response using pattern matching
    /// This is a fallback when JSON parsing fails but the response contains useful information
    /// </summary>
    private List<ConceptIdea> TryExtractConceptsFromText(string response, string originalTopic, int desiredConceptCount)
    {
        var concepts = new List<ConceptIdea>();

        try
        {
            // Look for numbered lists or concept patterns in the text
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var currentConcept = new Dictionary<string, string>();
            var conceptNumber = 1;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Look for concept indicators
                if (trimmedLine.StartsWith("Concept", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith($"{conceptNumber}.", StringComparison.OrdinalIgnoreCase) ||
                    (trimmedLine.Length > 0 && char.IsDigit(trimmedLine[0])))
                {
                    // If we have a previous concept, save it
                    if (currentConcept.Count > 0)
                    {
                        var concept = BuildConceptFromDict(currentConcept, originalTopic, conceptNumber - 1);
                        if (concept != null)
                        {
                            concepts.Add(concept);
                        }
                        currentConcept.Clear();
                    }

                    // Extract title from the line
                    var titleMatch = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"(?:Concept\s*\d*:?\s*|^\d+\.\s*)(.+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (titleMatch.Success)
                    {
                        currentConcept["title"] = titleMatch.Groups[1].Value.Trim();
                    }
                    else if (trimmedLine.Length > 10)
                    {
                        currentConcept["title"] = trimmedLine;
                    }
                }
                // Look for field indicators
                else if (trimmedLine.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                {
                    currentConcept["title"] = trimmedLine.Substring(6).Trim();
                }
                else if (trimmedLine.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
                {
                    currentConcept["description"] = trimmedLine.Substring(12).Trim();
                }
                else if (trimmedLine.StartsWith("Angle:", StringComparison.OrdinalIgnoreCase))
                {
                    currentConcept["angle"] = trimmedLine.Substring(6).Trim();
                }
                else if (trimmedLine.StartsWith("Hook:", StringComparison.OrdinalIgnoreCase))
                {
                    currentConcept["hook"] = trimmedLine.Substring(5).Trim();
                }
                else if (currentConcept.ContainsKey("description") && string.IsNullOrEmpty(currentConcept["description"]))
                {
                    // Continuation of description
                    currentConcept["description"] = trimmedLine;
                }
            }

            // Add the last concept
            if (currentConcept.Count > 0)
            {
                var concept = BuildConceptFromDict(currentConcept, originalTopic, conceptNumber);
                if (concept != null)
                {
                    concepts.Add(concept);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract concepts from text response");
        }

        return concepts;
    }

    /// <summary>
    /// Build a ConceptIdea from a dictionary of extracted fields
    /// </summary>
    private ConceptIdea? BuildConceptFromDict(Dictionary<string, string> fields, string originalTopic, int index)
    {
        if (!fields.ContainsKey("title") || string.IsNullOrWhiteSpace(fields["title"]))
        {
            return null;
        }

        var title = fields["title"];
        var description = fields.GetValueOrDefault("description",
            $"A video concept about {originalTopic}.");
        var angle = fields.GetValueOrDefault("angle", "Tutorial");
        var hook = fields.GetValueOrDefault("hook",
            $"Discover insights about {originalTopic}");

        return new ConceptIdea(
            ConceptId: Guid.NewGuid().ToString(),
            Title: title,
            Description: description,
            Angle: angle,
            TargetAudience: "General audience",
            Pros: new List<string> { "Relevant to topic", "Engaging format" },
            Cons: new List<string> { "May need refinement" },
            AppealScore: 75.0,
            Hook: hook,
            TalkingPoints: null,
            CreatedAt: DateTime.UtcNow,
            // Fallback values for enhanced fields - these are extracted from text fallback
            UniqueValue: fields.TryGetValue("uniqueValue", out var uv) ? uv : null,
            ContentGap: fields.TryGetValue("contentGap", out var cg) ? cg : null,
            KeyInsights: null,
            VisualSuggestions: null,
            MonetizationPotential: fields.TryGetValue("monetizationPotential", out var mp) ? mp : null,
            ViralityScore: null
        );
    }

    /// <summary>
    /// Safely get a string property from a JSON element
    /// </summary>
    private static string GetStringPropertySafe(JsonElement element, string propertyName, string defaultValue)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.GetString() ?? defaultValue;
            }
        }
        catch
        {
            // Ignore exceptions during property access
        }
        return defaultValue;
    }

    /// <summary>
    /// Safely get a double property from a JSON element
    /// </summary>
    private static double GetDoublePropertySafe(JsonElement element, string propertyName, double defaultValue)
    {
        try
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    return prop.GetDouble();
                }
                if (prop.ValueKind == JsonValueKind.String)
                {
                    if (double.TryParse(prop.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        catch
        {
            // Ignore exceptions during property access
        }
        return defaultValue;
    }
}

