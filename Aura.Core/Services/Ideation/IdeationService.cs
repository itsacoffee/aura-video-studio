using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Ideation;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Aura.Core.Services.Conversation;
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

    public IdeationService(
        ILogger<IdeationService> logger,
        ILlmProvider llmProvider,
        ProjectContextManager projectManager,
        ConversationContextManager conversationManager,
        TrendingTopicsService trendingTopicsService,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _projectManager = projectManager;
        _conversationManager = conversationManager;
        _trendingTopicsService = trendingTopicsService;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Generate creative concept variations from a topic
    /// </summary>
    public async Task<BrainstormResponse> BrainstormConceptsAsync(
        BrainstormRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Brainstorming concepts for topic: {Topic}", request.Topic);

        var prompt = BuildBrainstormPrompt(request);
        
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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);
        
        // Parse the response into structured concepts
        var concepts = ParseBrainstormResponse(response, request.Topic);

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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

        // Store in conversation history
        if (!string.IsNullOrEmpty(request.UserMessage))
        {
            await _conversationManager.AddMessageAsync(
                request.ProjectId,
                "user",
                request.UserMessage,
                ct: ct);
        }

        await _conversationManager.AddMessageAsync(
            request.ProjectId,
            "assistant",
            response,
            ct: ct);

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
            ct);

        return new TrendingTopicsResponse(
            Topics: topics,
            AnalyzedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Analyze content gaps and opportunities
    /// </summary>
    public async Task<GapAnalysisResponse> AnalyzeContentGapsAsync(
        GapAnalysisRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content gaps for niche: {Niche}", request.Niche ?? "general");

        var prompt = BuildGapAnalysisPrompt(request);
        
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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

        var (missingTopics, opportunities, oversaturated, uniqueAngles) = 
            ParseGapAnalysisResponse(response);

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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

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

        var response = await GenerateWithLlmAsync(brief, planSpec, ct);

        var questions = ParseQuestionsResponse(response);

        return new QuestionsResponse(
            Questions: questions,
            Context: "These questions will help create a better video concept"
        );
    }

    // --- Prompt Building Methods ---

    private string BuildBrainstormPrompt(BrainstormRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Generate exactly 3 unique, creative, and actionable video concept ideas for the topic: '{request.Topic}'");
        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"concepts\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"title\": \"Catchy, engaging title\",");
        sb.AppendLine("      \"description\": \"Detailed 2-3 sentence description of the video concept\",");
        sb.AppendLine("      \"angle\": \"One of: Tutorial, Narrative, Case Study, Comparison, Interview, Documentary, Behind-the-Scenes, Expert Analysis, Beginner's Guide, Deep Dive\",");
        sb.AppendLine("      \"targetAudience\": \"Specific description of the intended audience\",");
        sb.AppendLine("      \"pros\": [\"Pro 1\", \"Pro 2\", \"Pro 3\"],");
        sb.AppendLine("      \"cons\": [\"Con 1\", \"Con 2\", \"Con 3\"],");
        sb.AppendLine("      \"hook\": \"Compelling opening hook for the first 15 seconds\",");
        sb.AppendLine("      \"talkingPoints\": [\"Key point 1\", \"Key point 2\", \"Key point 3\", \"Key point 4\", \"Key point 5\"],");
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
        sb.AppendLine("Requirements:");
        sb.AppendLine("- Make each concept genuinely unique with different angles and approaches");
        sb.AppendLine("- Ensure talking points are specific, actionable, and relevant to the concept");
        sb.AppendLine("- Focus on creative quality and practical value for video creators");
        sb.AppendLine("- Appeal scores should realistically range from 65-95 based on concept viability");
        sb.AppendLine("- Return ONLY the JSON object, no additional text or markdown formatting");

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

    private string BuildGapAnalysisPrompt(GapAnalysisRequest request)
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

        sb.AppendLine();
        sb.AppendLine("Identify:");
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

    // --- Response Parsing Methods ---

    private List<ConceptIdea> ParseBrainstormResponse(string response, string originalTopic)
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

        // Fallback: If parsing failed or no concepts were generated, create 3 generic concepts
        if (concepts.Count == 0)
        {
            _logger.LogWarning("No concepts parsed from LLM response, generating fallback concepts");
            
            var angles = new[] { "Tutorial", "Narrative", "Case Study" };
            
            for (int i = 0; i < 3; i++)
            {
                concepts.Add(new ConceptIdea(
                    ConceptId: Guid.NewGuid().ToString(),
                    Title: $"{originalTopic} - {angles[i]} Approach",
                    Description: $"A {angles[i].ToLower()} style video exploring {originalTopic}. " +
                                "This approach provides unique value through its specific perspective and presentation style.",
                    Angle: angles[i],
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
                    Hook: $"Discover the most {angles[i].ToLower()} way to understand {originalTopic}",
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

        // Ensure we return exactly 3 concepts
        return concepts.Take(3).ToList();
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
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
        }
    }
}
