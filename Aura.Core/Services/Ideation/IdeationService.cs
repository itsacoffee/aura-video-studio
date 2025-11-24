using System;
using System.Collections.Generic;
using System.Linq;
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
        _logger.LogInformation("Brainstorming concepts for topic: {Topic}", request.Topic);

        var desiredConceptCount = Math.Clamp(
            request.ConceptCount ?? MinConceptCount,
            MinConceptCount,
            MaxConceptCount);

        // Try to retrieve RAG context for the topic to provide more specific information
        RagContext? ragContext = null;
        if (_ragContextBuilder != null)
        {
            try
            {
                var ragConfig = new RagConfig
                {
                    Enabled = true,
                    TopK = 5,
                    MinimumScore = 0.5f,
                    MaxContextTokens = 2000,
                    IncludeCitations = false
                };
                ragContext = await _ragContextBuilder.BuildContextAsync(request.Topic, ragConfig, ct).ConfigureAwait(false);
                if (ragContext.Chunks.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} RAG chunks for topic: {Topic}", ragContext.Chunks.Count, request.Topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve RAG context for ideation, continuing without RAG");
            }
        }

        var prompt = await BuildBrainstormPromptAsync(request, desiredConceptCount, ragContext, ct).ConfigureAwait(false);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.Audience ?? "General",
            Goal: "Generate creative video concept variations",
            Tone: request.Tone ?? "Professional",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(request.TargetDuration ?? 60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Creative"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Parse the response into structured concepts
        var concepts = ParseBrainstormResponse(response, request.Topic, desiredConceptCount);

        return new BrainstormResponse(
            Concepts: concepts,
            OriginalTopic: request.Topic,
            GeneratedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Expand brief with AI asking clarifying questions
    /// </summary>
    public async Task<ExpandBriefResponse> ExpandBriefAsync(
        ExpandBriefRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Expanding brief for project: {ProjectId}", request.ProjectId);

        var prompt = BuildExpandBriefPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.CurrentBrief.Audience ?? "General",
            Goal: "Expand and clarify video brief",
            Tone: "Helpful",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Conversational"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Store in conversation history
        if (!string.IsNullOrEmpty(request.UserMessage))
        {
            await _conversationManager.AddMessageAsync(
                request.ProjectId,
                "user",
                request.UserMessage,
                ct: ct).ConfigureAwait(false);
        }

        await _conversationManager.AddMessageAsync(
            request.ProjectId,
            "assistant",
            response,
            ct: ct).ConfigureAwait(false);

        // Parse response for questions or updated brief
        var (questions, aiResponse) = ParseExpandBriefResponse(response);

        return new ExpandBriefResponse(
            UpdatedBrief: null,  // Will be implemented with more sophisticated parsing
            Questions: questions,
            AiResponse: aiResponse
        );
    }

    /// <summary>
    /// Get trending topics for a niche with AI analysis
    /// </summary>
    public async Task<TrendingTopicsResponse> GetTrendingTopicsAsync(
        TrendingTopicsRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing trending topics for niche: {Niche}", request.Niche ?? "general");

        var topics = await _trendingTopicsService.GetTrendingTopicsAsync(
            request.Niche,
            request.MaxResults ?? 10,
            forceRefresh: false,
            ct).ConfigureAwait(false);

        return new TrendingTopicsResponse(
            Topics: topics,
            AnalyzedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Analyze content gaps and opportunities with real-time web intelligence
    /// </summary>
    public async Task<GapAnalysisResponse> AnalyzeContentGapsAsync(
        GapAnalysisRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content gaps for niche: {Niche}", request.Niche ?? "general");

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

        var brief = new Brief(
            Topic: prompt,
            Audience: "Content Creators",
            Goal: "Identify content gaps and opportunities",
            Tone: "Analytical",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analytical"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

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

        return new GapAnalysisResponse(
            MissingTopics: missingTopics,
            Opportunities: opportunities,
            OversaturatedTopics: oversaturated,
            UniqueAngles: uniqueAngles
        );
    }

    /// <summary>
    /// Gather research and facts for a topic
    /// </summary>
    public async Task<ResearchResponse> GatherResearchAsync(
        ResearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Gathering research for topic: {Topic}", request.Topic);

        var prompt = BuildResearchPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: "Researchers",
            Goal: "Gather facts and examples",
            Tone: "Factual",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Factual"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        var findings = ParseResearchResponse(response, request.Topic);

        return new ResearchResponse(
            Findings: findings,
            Topic: request.Topic,
            GatheredAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Generate visual storyboard for a concept
    /// </summary>
    public async Task<StoryboardResponse> GenerateStoryboardAsync(
        StoryboardRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating storyboard for concept: {ConceptTitle}", request.Concept.Title);

        var prompt = BuildStoryboardPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.Concept.TargetAudience,
            Goal: "Create visual storyboard",
            Tone: "Descriptive",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(request.TargetDurationSeconds),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Visual"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        var scenes = ParseStoryboardResponse(response, request.TargetDurationSeconds);

        return new StoryboardResponse(
            Scenes: scenes,
            ConceptTitle: request.Concept.Title,
            TotalDurationSeconds: scenes.Sum(s => s.DurationSeconds),
            GeneratedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Refine a concept based on user direction
    /// </summary>
    public async Task<RefineConceptResponse> RefineConceptAsync(
        RefineConceptRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Refining concept: {ConceptTitle} with direction: {Direction}",
            request.Concept.Title, request.RefinementDirection);

        var prompt = BuildRefineConceptPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.TargetAudience ?? request.Concept.TargetAudience,
            Goal: "Refine video concept",
            Tone: "Creative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Creative"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        var (refinedConcept, changesSummary) = ParseRefineConceptResponse(
            response,
            request.Concept,
            request.RefinementDirection);

        return new RefineConceptResponse(
            RefinedConcept: refinedConcept,
            ChangesSummary: changesSummary
        );
    }

    /// <summary>
    /// Get clarifying questions about the brief
    /// </summary>
    public async Task<QuestionsResponse> GetClarifyingQuestionsAsync(
        QuestionsRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating clarifying questions for project: {ProjectId}", request.ProjectId);

        var prompt = BuildQuestionsPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: "Content Creator",
            Goal: "Ask clarifying questions",
            Tone: "Helpful",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Conversational"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        var questions = ParseQuestionsResponse(response);

        return new QuestionsResponse(
            Questions: questions,
            Context: "These questions will help create a better video concept"
        );
    }

    /// <summary>
    /// Convert freeform idea into structured brief with multiple variants
    /// </summary>
    public async Task<IdeaToBriefResponse> IdeaToBriefAsync(
        IdeaToBriefRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Converting idea to brief: {Idea}", request.Idea);

        var variantCount = Math.Clamp(request.VariantCount ?? 3, 2, 4);
        var prompt = BuildIdeaToBriefPrompt(request, variantCount);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.Audience ?? "General",
            Goal: "Generate structured brief variants from freeform idea",
            Tone: "Professional",
            Language: request.Language ?? "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analytical"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Parse the response into structured brief variants
        var variants = ParseIdeaToBriefResponse(response, request);

        return new IdeaToBriefResponse(
            Variants: variants,
            OriginalIdea: request.Idea,
            GeneratedAt: DateTime.UtcNow
        );
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

        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"concepts\": [");
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
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
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
        sb.AppendLine($"Remember: Every field must contain SPECIFIC information about '{request.Topic}'. Generic placeholders are NOT acceptable.");

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
            if (jsonDoc.RootElement.TryGetProperty("concepts", out var conceptsArray))
            {
                foreach (var conceptElement in conceptsArray.EnumerateArray())
                {
                    var title = conceptElement.GetProperty("title").GetString() ?? "Untitled Concept";
                    var description = conceptElement.GetProperty("description").GetString() ?? "";
                    var angle = conceptElement.GetProperty("angle").GetString() ?? "Tutorial";
                    var targetAudience = conceptElement.GetProperty("targetAudience").GetString() ?? "General audience";
                    var hook = conceptElement.GetProperty("hook").GetString() ?? "";
                    var appealScore = conceptElement.GetProperty("appealScore").GetDouble();

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
                        CreatedAt: DateTime.UtcNow
                    ));
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response from LLM, falling back to generic concepts");
        }

        // Fallback: If parsing failed or no concepts were generated, create generic concepts
        if (concepts.Count == 0)
        {
            _logger.LogWarning("No concepts parsed from LLM response, generating fallback concepts");

            var angles = new[] { "Tutorial", "Narrative", "Case Study" };

            for (int i = 0; i < desiredConceptCount; i++)
            {
                var angle = angles[i % angles.Length];
                concepts.Add(new ConceptIdea(
                    ConceptId: Guid.NewGuid().ToString(),
                    Title: $"{originalTopic} - {angle} Approach",
                    Description: $"A {angle.ToLower()} style video exploring {originalTopic}. " +
                                "This approach provides unique value through its specific perspective and presentation style.",
                    Angle: angle,
                    TargetAudience: "General viewers interested in the topic",
                    Pros: new List<string>
                    {
                        "Engaging and accessible format",
                        "Clear value proposition",
                        "Suitable for target platform"
                    },
                    Cons: new List<string>
                    {
                        "May require specific expertise",
                        "Needs careful pacing",
                        "Competition in this format"
                    },
                    AppealScore: 70 + (i * 5),
                    Hook: $"Discover the most {angle.ToLower()} way to understand {originalTopic}",
                    TalkingPoints: new List<string>
                    {
                        "Introduction to the topic",
                        "Key concepts and fundamentals",
                        "Practical examples and applications",
                        "Common mistakes to avoid",
                        "Next steps and resources"
                    },
                    CreatedAt: DateTime.UtcNow
                ));
            }
        }

        // Ensure we return the requested number of concepts
        return concepts.Take(desiredConceptCount).ToList();
    }

    private (List<ClarifyingQuestion>?, string) ParseExpandBriefResponse(string response)
    {
        // Simplified parsing
        var questions = new List<ClarifyingQuestion>
        {
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "Who is your ideal viewer for this video?",
                Context: "Understanding your target audience helps tailor the content and tone",
                SuggestedAnswers: new List<string> { "Beginners", "Intermediate learners", "Experts", "General public" },
                QuestionType: "multiple-choice"
            ),
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What action do you want viewers to take after watching?",
                Context: "Defining the desired outcome shapes the video's call-to-action",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            ),
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What makes your perspective on this topic unique?",
                Context: "Your unique angle differentiates your content from competitors",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            )
        };

        return (questions, response);
    }


    private (List<string>, List<TrendingTopic>, List<string>, Dictionary<string, List<string>>)
        ParseGapAnalysisResponse(string response)
    {
        // Simplified parsing
        var missingTopics = new List<string>
        {
            "Beginner-friendly introductions",
            "Advanced techniques and workflows",
            "Real-world case studies",
            "Common mistakes to avoid"
        };

        var opportunities = new List<TrendingTopic>
        {
            new TrendingTopic(
                TopicId: Guid.NewGuid().ToString(),
                Topic: "Emerging tools and technologies",
                TrendScore: 90,
                SearchVolume: "50K/month",
                Competition: "Low",
                Seasonality: "Year-round",
                Lifecycle: "Rising",
                RelatedTopics: null,
                DetectedAt: DateTime.UtcNow
            )
        };

        var oversaturated = new List<string>
        {
            "Basic introductions covered by many creators"
        };

        var uniqueAngles = new Dictionary<string, List<string>>
        {
            ["Popular Topic"] = new List<string>
            {
                "Focus on uncommon use cases",
                "Interview experts in the field",
                "Behind-the-scenes perspective"
            }
        };

        return (missingTopics, opportunities, oversaturated, uniqueAngles);
    }

    private List<ResearchFinding> ParseResearchResponse(string response, string topic)
    {
        // Simplified parsing
        var findings = new List<ResearchFinding>();

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

        return findings;
    }

    private List<StoryboardScene> ParseStoryboardResponse(string response, int targetDuration)
    {
        // Simplified parsing
        var scenes = new List<StoryboardScene>();
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

        return scenes;
    }

    private (ConceptIdea, string) ParseRefineConceptResponse(
        string response,
        ConceptIdea originalConcept,
        string direction)
    {
        // Simplified parsing - create refined version
        var refined = originalConcept with
        {
            ConceptId = Guid.NewGuid().ToString(),
            Title = $"{originalConcept.Title} (Refined)",
            Description = $"{originalConcept.Description} [Refined based on {direction}]",
            AppealScore = Math.Min(100, originalConcept.AppealScore + 5)
        };

        var summary = $"Refined concept based on '{direction}' direction. Enhanced clarity and focus.";

        return (refined, summary);
    }

    private List<ClarifyingQuestion> ParseQuestionsResponse(string response)
    {
        // Simplified parsing
        return new List<ClarifyingQuestion>
        {
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What specific problem does this video solve for your audience?",
                Context: "Understanding the problem helps create targeted, valuable content",
                SuggestedAnswers: null,
                QuestionType: "open-ended"
            ),
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "How much prior knowledge should viewers have?",
                Context: "This determines the depth and pacing of explanations",
                SuggestedAnswers: new List<string> { "Complete beginner", "Some familiarity", "Advanced" },
                QuestionType: "multiple-choice"
            ),
            new ClarifyingQuestion(
                QuestionId: Guid.NewGuid().ToString(),
                Question: "What emotions should viewers feel while watching?",
                Context: "Emotional tone guides the creative direction",
                SuggestedAnswers: new List<string> { "Inspired", "Educated", "Entertained", "Empowered" },
                QuestionType: "multiple-choice"
            )
        };
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
        _logger.LogInformation("Enhancing topic: {Topic}", request.Topic);

        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("Topic cannot be empty", nameof(request));
        }

        var prompt = BuildEnhanceTopicPrompt(request);

        var brief = new Brief(
            Topic: prompt,
            Audience: request.TargetAudience ?? "General video audience",
            Goal: "Enhance and improve video topic description",
            Tone: "Professional and engaging",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Clear and descriptive"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Extract enhanced topic from response
        var enhancedTopic = ExtractEnhancedTopic(response, request.Topic);
        var improvements = ExtractImprovements(response);

        return new EnhanceTopicResponse(
            EnhancedTopic: enhancedTopic,
            OriginalTopic: request.Topic,
            Improvements: improvements,
            GeneratedAt: DateTime.UtcNow
        );
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
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
        }
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
}
