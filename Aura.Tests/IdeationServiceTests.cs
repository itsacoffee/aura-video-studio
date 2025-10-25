using System;
using System.Collections.Generic;
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
    public async Task BrainstormConceptsAsync_ValidTopic_ReturnsConceptsWithCorrectCount()
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
        Assert.Equal(10, response.Concepts.Count); // Should generate 10 concepts
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
}
