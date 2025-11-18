using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ideation;
using Aura.Core.Providers;
using Aura.Core.Services.Conversation;
using Aura.Core.Services.Ideation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class IdeationServiceTests
{
    private readonly Mock<ILogger<IdeationService>> _mockLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly IdeationService _service;

    public IdeationServiceTests()
    {
        _mockLogger = new Mock<ILogger<IdeationService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        
        // Create real instances for context managers with temp paths
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        var mockContextLogger = new Mock<ILogger<ContextPersistence>>();
        var persistence = new ContextPersistence(mockContextLogger.Object, tempPath);
        
        var mockConversationLogger = new Mock<ILogger<ConversationContextManager>>();
        var conversationManager = new ConversationContextManager(mockConversationLogger.Object, persistence);
        
        var mockProjectLogger = new Mock<ILogger<ProjectContextManager>>();
        var projectManager = new ProjectContextManager(mockProjectLogger.Object, persistence);
        
        // Create mocks for TrendingTopicsService dependencies
        var mockTrendingLogger = new Mock<ILogger<TrendingTopicsService>>();
        var mockHttpClientFactory = new Mock<System.Net.Http.IHttpClientFactory>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        
        var trendingTopicsService = new TrendingTopicsService(
            mockTrendingLogger.Object,
            _mockLlmProvider.Object,
            mockHttpClientFactory.Object,
            memoryCache
        );
        
        _service = new IdeationService(
            _mockLogger.Object,
            _mockLlmProvider.Object,
            projectManager,
            conversationManager,
            trendingTopicsService
        );
    }

    [Fact]
    public async Task BrainstormConceptsAsync_DefaultCount_ReturnsThreeConcepts()
    {
        // Arrange
        var request = new BrainstormRequest(
            Topic: "How to start a podcast",
            Audience: "Beginners",
            Tone: "Casual"
        );

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock brainstorming response");

        // Act
        var response = await _service.BrainstormConceptsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Topic, response.OriginalTopic);
        Assert.NotEmpty(response.Concepts);
        Assert.Equal(3, response.Concepts.Count); // Should generate exactly 3 concepts
        Assert.All(response.Concepts, concept =>
        {
            Assert.NotNull(concept.ConceptId);
            Assert.NotNull(concept.Title);
            Assert.NotNull(concept.Description);
            Assert.NotNull(concept.Angle);
            Assert.True(concept.AppealScore >= 0 && concept.AppealScore <= 100);
        });
    }

    [Fact]
    public async Task BrainstormConceptsAsync_WithJsonResponse_ParsesConceptsCorrectly()
    {
        // Arrange
        var request = new BrainstormRequest(
            Topic: "AI in video production",
            Audience: "Content creators"
        );

        var jsonResponse = GetSampleBrainstormJsonResponse();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var response = await _service.BrainstormConceptsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(3, response.Concepts.Count);
        
        var firstConcept = response.Concepts[0];
        Assert.Equal("AI Tools for Video Editing", firstConcept.Title);
        Assert.Equal("Tutorial", firstConcept.Angle);
        Assert.Equal(85, firstConcept.AppealScore);
        Assert.NotNull(firstConcept.TalkingPoints);
        Assert.Equal(5, firstConcept.TalkingPoints.Count);
        Assert.Contains("Introduction to AI editing", firstConcept.TalkingPoints);
        Assert.Equal(3, firstConcept.Pros.Count);
        Assert.Equal(3, firstConcept.Cons.Count);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task BrainstormConceptsAsync_CustomConceptCount_IsHonored(int requestedCount)
    {
        // Arrange
        var request = new BrainstormRequest(
            Topic: "Variable count topic",
            ConceptCount: requestedCount);

        var jsonResponse = CreateSequentialBrainstormJsonResponse(requestedCount + 2);

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var response = await _service.BrainstormConceptsAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(requestedCount, response.Concepts.Count);
    }

    private static string GetSampleBrainstormJsonResponse()
    {
        return @"{
            ""concepts"": [
                {
                    ""title"": ""AI Tools for Video Editing"",
                    ""description"": ""Explore cutting-edge AI tools that automate video editing tasks."",
                    ""angle"": ""Tutorial"",
                    ""targetAudience"": ""Video editors and content creators"",
                    ""pros"": [""Time-saving"", ""Professional results"", ""Easy to learn""],
                    ""cons"": [""Requires subscription"", ""Limited customization"", ""Learning curve""],
                    ""hook"": ""Want to edit videos 10x faster? AI can help!"",
                    ""talkingPoints"": [""Introduction to AI editing"", ""Top AI tools"", ""Hands-on demo"", ""Tips and tricks"", ""Future trends""],
                    ""appealScore"": 85
                },
                {
                    ""title"": ""The Future of AI in Video Production"",
                    ""description"": ""A deep dive into how AI is transforming the video production industry."",
                    ""angle"": ""Documentary"",
                    ""targetAudience"": ""Industry professionals"",
                    ""pros"": [""Insightful"", ""Industry trends"", ""Expert opinions""],
                    ""cons"": [""Complex topic"", ""Longer format"", ""Requires research""],
                    ""hook"": ""AI is revolutionizing video production. Here's how."",
                    ""talkingPoints"": [""Current state of AI"", ""Key innovations"", ""Industry impact"", ""Challenges"", ""What's next""],
                    ""appealScore"": 78
                },
                {
                    ""title"": ""AI vs Traditional Editing: A Comparison"",
                    ""description"": ""Compare AI-powered editing tools with traditional methods to see which is better."",
                    ""angle"": ""Comparison"",
                    ""targetAudience"": ""Professionals and beginners"",
                    ""pros"": [""Clear comparison"", ""Practical insights"", ""Balanced view""],
                    ""cons"": [""May be biased"", ""Requires testing"", ""Rapidly changing field""],
                    ""hook"": ""AI editing vs traditional: which should you choose?"",
                    ""talkingPoints"": [""Traditional workflow"", ""AI workflow"", ""Speed comparison"", ""Quality comparison"", ""Cost analysis""],
                    ""appealScore"": 82
                }
            ]
        }";
    }

    private static string CreateSequentialBrainstormJsonResponse(int conceptCount)
    {
        var sb = new StringBuilder();
        sb.Append("{\"concepts\":[");

        for (int i = 0; i < conceptCount; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append("{");
            sb.Append("\"title\":\"Concept ").Append(i + 1).Append("\",");
            sb.Append("\"description\":\"Description ").Append(i + 1).Append("\",");
            sb.Append("\"angle\":\"Tutorial\",");
            sb.Append("\"targetAudience\":\"Audience ").Append(i + 1).Append("\",");
            sb.Append("\"pros\":[\"Pro ").Append(i + 1).Append("\"],");
            sb.Append("\"cons\":[\"Con ").Append(i + 1).Append("\"],");
            sb.Append("\"hook\":\"Hook ").Append(i + 1).Append("\",");
            sb.Append("\"talkingPoints\":[\"Point ").Append(i + 1).Append("\"],");
            sb.Append("\"appealScore\":").Append(70 + i);
            sb.Append("}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    [Fact]
    public async Task BrainstormConceptsAsync_EmptyTopic_StillReturnsValidResponse()
    {
        // Arrange
        var request = new BrainstormRequest(Topic: "");
        
        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock response");

        // Act
        var response = await _service.BrainstormConceptsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Concepts);
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_ValidRequest_ReturnsTopicsWithMetadata()
    {
        // Arrange
        var request = new TrendingTopicsRequest(Niche: "Technology", MaxResults: 5);

        // Act
        var response = await _service.GetTrendingTopicsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Topics);
        Assert.True(response.Topics.Count <= 5);
        Assert.All(response.Topics, topic =>
        {
            Assert.NotNull(topic.TopicId);
            Assert.NotNull(topic.Topic);
            Assert.True(topic.TrendScore >= 0 && topic.TrendScore <= 100);
        });
    }

    [Fact]
    public async Task GatherResearchAsync_ValidTopic_ReturnsResearchFindings()
    {
        // Arrange
        var request = new ResearchRequest(Topic: "Artificial Intelligence", MaxFindings: 5);
        
        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock research response");

        // Act
        var response = await _service.GatherResearchAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Topic, response.Topic);
        Assert.NotEmpty(response.Findings);
        Assert.All(response.Findings, finding =>
        {
            Assert.NotNull(finding.FindingId);
            Assert.NotNull(finding.Fact);
            Assert.True(finding.CredibilityScore >= 0 && finding.CredibilityScore <= 100);
            Assert.True(finding.RelevanceScore >= 0 && finding.RelevanceScore <= 100);
        });
    }

    [Fact]
    public async Task GenerateStoryboardAsync_ValidConcept_ReturnsScenes()
    {
        // Arrange
        var concept = new ConceptIdea(
            ConceptId: "test-id",
            Title: "Test Concept",
            Description: "Test description",
            Angle: "Tutorial",
            TargetAudience: "Beginners",
            Pros: new System.Collections.Generic.List<string> { "Easy to understand" },
            Cons: new System.Collections.Generic.List<string> { "May be too basic" },
            AppealScore: 75,
            Hook: "Learn the basics in 5 minutes"
        );

        var request = new StoryboardRequest(Concept: concept, TargetDurationSeconds: 60);
        
        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock storyboard response");

        // Act
        var response = await _service.GenerateStoryboardAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(concept.Title, response.ConceptTitle);
        Assert.NotEmpty(response.Scenes);
        Assert.All(response.Scenes, scene =>
        {
            Assert.True(scene.SceneNumber > 0);
            Assert.NotNull(scene.Description);
            Assert.NotNull(scene.VisualStyle);
            Assert.True(scene.DurationSeconds > 0);
            Assert.NotNull(scene.Purpose);
        });
    }

    [Fact]
    public async Task RefineConceptAsync_ExpandDirection_ReturnsRefinedConcept()
    {
        // Arrange
        var originalConcept = new ConceptIdea(
            ConceptId: "original-id",
            Title: "Original Concept",
            Description: "Original description",
            Angle: "Tutorial",
            TargetAudience: "Beginners",
            Pros: new System.Collections.Generic.List<string> { "Simple" },
            Cons: new System.Collections.Generic.List<string> { "Basic" },
            AppealScore: 70,
            Hook: "Quick intro"
        );

        var request = new RefineConceptRequest(
            Concept: originalConcept,
            RefinementDirection: "expand"
        );
        
        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock refinement response");

        // Act
        var response = await _service.RefineConceptAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.RefinedConcept);
        Assert.NotNull(response.ChangesSummary);
        Assert.NotEqual(originalConcept.ConceptId, response.RefinedConcept.ConceptId);
        Assert.True(response.RefinedConcept.AppealScore >= originalConcept.AppealScore);
    }

    [Fact]
    public async Task GetClarifyingQuestionsAsync_ValidProjectId_ReturnsQuestions()
    {
        // Arrange
        var request = new QuestionsRequest(
            ProjectId: "test-project-123",
            CurrentBrief: new BriefRequirements(
                Topic: "Video Marketing",
                Goal: null,
                Audience: null,
                Tone: null,
                DurationSeconds: null,
                Platform: null,
                Keywords: null
            )
        );
        
        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock questions response");

        // Act
        var response = await _service.GetClarifyingQuestionsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Questions);
        Assert.NotNull(response.Context);
        Assert.All(response.Questions, question =>
        {
            Assert.NotNull(question.QuestionId);
            Assert.NotNull(question.Question);
            Assert.NotNull(question.Context);
            Assert.NotNull(question.QuestionType);
        });
    }

    [Fact]
    public async Task IdeaToBriefAsync_ValidRequest_ReturnsVariants()
    {
        // Arrange
        var request = new IdeaToBriefRequest(
            Idea: "Explain quantum computing to my grandmother",
            TargetPlatform: "YouTube",
            Audience: "Non-technical seniors",
            PreferredApproaches: "make it warm and relatable, like telling a story"
        );

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock LLM response");

        // Act
        var response = await _service.IdeaToBriefAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Idea, response.OriginalIdea);
        Assert.NotEmpty(response.Variants);
        Assert.Equal(3, response.Variants.Count); // Default variant count
        Assert.All(response.Variants, variant =>
        {
            Assert.NotNull(variant.VariantId);
            Assert.NotNull(variant.Approach);
            Assert.NotNull(variant.Brief);
            Assert.NotNull(variant.PlanSpec);
            Assert.NotNull(variant.Explanation);
            Assert.True(variant.SuitabilityScore >= 0 && variant.SuitabilityScore <= 100);
        });
    }

    [Fact]
    public async Task IdeaToBriefAsync_WithJsonResponse_ParsesVariantsCorrectly()
    {
        // Arrange
        var request = new IdeaToBriefRequest(
            Idea: "Video about climate change solutions",
            VariantCount: 2
        );

        var mockJsonResponse = @"{
            ""variants"": [
                {
                    ""approach"": ""Hopeful and solution-focused storytelling"",
                    ""topic"": ""Innovative Climate Solutions Making a Real Difference"",
                    ""audience"": ""Environmentally conscious individuals seeking actionable information"",
                    ""goal"": ""Inspire viewers with practical climate solutions"",
                    ""tone"": ""Optimistic yet grounded"",
                    ""targetDurationSeconds"": 180,
                    ""pacing"": ""Conversational"",
                    ""density"": ""Balanced"",
                    ""style"": ""Solution-focused documentary"",
                    ""explanation"": ""This approach focuses on empowering viewers with practical solutions rather than dwelling on problems, creating a positive and actionable viewing experience."",
                    ""suitabilityScore"": 92,
                    ""strengths"": [""Empowering message"", ""Actionable content"", ""Positive tone""],
                    ""considerations"": [""Balance optimism with realism"", ""Include credible sources""]
                },
                {
                    ""approach"": ""Scientific deep-dive with accessible explanations"",
                    ""topic"": ""The Science Behind Emerging Climate Technologies"",
                    ""audience"": ""Science enthusiasts and informed citizens"",
                    ""goal"": ""Educate viewers on the technical aspects of climate solutions"",
                    ""tone"": ""Authoritative yet accessible"",
                    ""targetDurationSeconds"": 240,
                    ""pacing"": ""Deliberate"",
                    ""density"": ""Dense"",
                    ""style"": ""Educational analysis with visual aids"",
                    ""explanation"": ""This approach takes a more technical route, explaining the science behind climate technologies while keeping it accessible for non-experts."",
                    ""suitabilityScore"": 85,
                    ""strengths"": [""In-depth coverage"", ""Credible information"", ""Appeals to curious minds""],
                    ""considerations"": [""May be complex for some viewers"", ""Requires strong visual support""]
                }
            ]
        }";

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockJsonResponse);

        // Act
        var response = await _service.IdeaToBriefAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Variants.Count);
        
        var firstVariant = response.Variants[0];
        Assert.Equal("Hopeful and solution-focused storytelling", firstVariant.Approach);
        Assert.Equal("Innovative Climate Solutions Making a Real Difference", firstVariant.Brief.Topic);
        Assert.Equal("Inspire viewers with practical climate solutions", firstVariant.Brief.Goal);
        Assert.Equal(92, firstVariant.SuitabilityScore);
        Assert.Equal(3, firstVariant.Strengths!.Count);
        Assert.Equal(2, firstVariant.Considerations!.Count);
    }

    [Fact]
    public async Task IdeaToBriefAsync_OpenEndedApproach_AllowsCreativeInterpretation()
    {
        // Arrange - Test that freeform approaches work
        var request = new IdeaToBriefRequest(
            Idea: "History of pizza",
            PreferredApproaches: "make it like a detective mystery solving the origin"
        );

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Core.Models.Brief>(), It.IsAny<Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mock response");

        // Act
        var response = await _service.IdeaToBriefAsync(request, CancellationToken.None);

        // Assert - Verify it generates variants without constraining to preset approaches
        Assert.NotNull(response);
        Assert.NotEmpty(response.Variants);
        Assert.All(response.Variants, variant =>
        {
            // Approach should be a descriptive string, not limited to preset values
            Assert.NotEmpty(variant.Approach);
            Assert.True(variant.Approach.Length > 5); // Should be descriptive
        });
    }
}
