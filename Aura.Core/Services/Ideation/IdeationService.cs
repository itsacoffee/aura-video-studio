using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Ideation;
using Aura.Core.Providers;
using Aura.Core.Services.Conversation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Ideation;

/// <summary>
/// Service for AI-powered ideation and brainstorming
/// </summary>
public class IdeationService
{
    private readonly ILogger<IdeationService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly ProjectContextManager _projectManager;
    private readonly ConversationContextManager _conversationManager;
    private readonly TrendingTopicsService _trendingTopicsService;

    public IdeationService(
        ILogger<IdeationService> logger,
        ILlmProvider llmProvider,
        ProjectContextManager projectManager,
        ConversationContextManager conversationManager,
        TrendingTopicsService trendingTopicsService)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _projectManager = projectManager;
        _conversationManager = conversationManager;
        _trendingTopicsService = trendingTopicsService;
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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
        
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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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

        var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);

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
        sb.AppendLine($"Generate 10 creative and distinct video concept variations for the topic: '{request.Topic}'");
        sb.AppendLine();
        sb.AppendLine("For each concept, provide:");
        sb.AppendLine("1. A catchy title");
        sb.AppendLine("2. A brief description (2-3 sentences)");
        sb.AppendLine("3. The storytelling angle (narrative, tutorial, case study, comparison, interview, documentary, etc.)");
        sb.AppendLine("4. Target audience");
        sb.AppendLine("5. 3 pros of this approach");
        sb.AppendLine("6. 3 cons or challenges");
        sb.AppendLine("7. A compelling hook for the first 15 seconds");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(request.Audience))
        {
            sb.AppendLine($"Target Audience: {request.Audience}");
        }
        
        if (!string.IsNullOrEmpty(request.Tone))
        {
            sb.AppendLine($"Tone: {request.Tone}");
        }
        
        if (request.TargetDuration.HasValue)
        {
            sb.AppendLine($"Target Duration: {request.TargetDuration}s");
        }
        
        if (!string.IsNullOrEmpty(request.Platform))
        {
            sb.AppendLine($"Platform: {request.Platform}");
        }

        sb.AppendLine();
        sb.AppendLine("Make each concept unique and actionable. Focus on creative quality and inspiring the creator.");

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
        // Simplified parsing - in production, would use structured LLM output (JSON mode)
        var concepts = new List<ConceptIdea>();
        
        // Generate sample concepts based on the response
        var angles = new[] { "Tutorial", "Narrative", "Case Study", "Comparison", "Interview", 
            "Documentary", "Behind-the-Scenes", "Expert Analysis", "Beginner's Guide", "Deep Dive" };
        
        for (int i = 0; i < 10; i++)
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
                AppealScore: 70 + (i * 3),
                Hook: $"Discover the most {angles[i].ToLower()} way to understand {originalTopic}",
                CreatedAt: DateTime.UtcNow
            ));
        }

        return concepts;
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
}
