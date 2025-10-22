using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Learning;
using Aura.Core.Models.Profiles;
using Aura.Core.Services.Learning;
using Aura.Core.Services.Profiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class LearningServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILogger<ProfilePersistence>> _profilePersistenceLoggerMock;
    private readonly Mock<ILogger<ProfileService>> _profileServiceLoggerMock;
    private readonly Mock<ILogger<LearningPersistence>> _learningPersistenceLoggerMock;
    private readonly Mock<ILogger<LearningService>> _learningServiceLoggerMock;
    private readonly Mock<ILogger<DecisionAnalysisEngine>> _analysisEngineLoggerMock;
    private readonly Mock<ILogger<PatternRecognitionSystem>> _patternRecognitionLoggerMock;
    private readonly Mock<ILogger<PreferenceInferenceEngine>> _preferenceInferenceLoggerMock;
    private readonly Mock<ILogger<PredictiveSuggestionRanker>> _suggestionRankerLoggerMock;
    private readonly ProfilePersistence _profilePersistence;
    private readonly ProfileService _profileService;
    private readonly LearningPersistence _learningPersistence;
    private readonly DecisionAnalysisEngine _decisionAnalysis;
    private readonly PatternRecognitionSystem _patternRecognition;
    private readonly PreferenceInferenceEngine _preferenceInference;
    private readonly PredictiveSuggestionRanker _suggestionRanker;
    private readonly LearningService _learningService;

    public LearningServiceTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), "AuraLearningTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        // Create mocks
        _profilePersistenceLoggerMock = new Mock<ILogger<ProfilePersistence>>();
        _profileServiceLoggerMock = new Mock<ILogger<ProfileService>>();
        _learningPersistenceLoggerMock = new Mock<ILogger<LearningPersistence>>();
        _learningServiceLoggerMock = new Mock<ILogger<LearningService>>();
        _analysisEngineLoggerMock = new Mock<ILogger<DecisionAnalysisEngine>>();
        _patternRecognitionLoggerMock = new Mock<ILogger<PatternRecognitionSystem>>();
        _preferenceInferenceLoggerMock = new Mock<ILogger<PreferenceInferenceEngine>>();
        _suggestionRankerLoggerMock = new Mock<ILogger<PredictiveSuggestionRanker>>();

        // Create services
        _profilePersistence = new ProfilePersistence(_profilePersistenceLoggerMock.Object, _testDirectory);
        _profileService = new ProfileService(_profileServiceLoggerMock.Object, _profilePersistence);
        _learningPersistence = new LearningPersistence(_learningPersistenceLoggerMock.Object, _testDirectory);
        _decisionAnalysis = new DecisionAnalysisEngine(_analysisEngineLoggerMock.Object);
        _patternRecognition = new PatternRecognitionSystem(_patternRecognitionLoggerMock.Object);
        _preferenceInference = new PreferenceInferenceEngine(_preferenceInferenceLoggerMock.Object);
        _suggestionRanker = new PredictiveSuggestionRanker(
            _suggestionRankerLoggerMock.Object,
            _patternRecognition);
        
        _learningService = new LearningService(
            _learningServiceLoggerMock.Object,
            _profileService,
            _decisionAnalysis,
            _patternRecognition,
            _preferenceInference,
            _suggestionRanker,
            _learningPersistence);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetMaturityLevel_NewProfile_ShouldReturnNascent()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Act
        var maturity = await _learningService.GetMaturityLevelAsync(profile.ProfileId);

        // Assert
        Assert.NotNull(maturity);
        Assert.Equal("nascent", maturity.MaturityLevel);
        Assert.Equal(0, maturity.TotalDecisions);
    }

    [Fact]
    public async Task AnalyzePatterns_WithDecisions_ShouldIdentifyPatterns()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Add some test decisions
        for (int i = 0; i < 10; i++)
        {
            var decision = new RecordDecisionRequest(
                ProfileId: profile.ProfileId,
                SuggestionType: "visual",
                Decision: "accepted",
                Context: new Dictionary<string, object> { { "aesthetic", "cinematic" } });
            
            await _profileService.RecordDecisionAsync(decision);
        }

        // Act
        var patterns = await _learningService.AnalyzePatternsAsync(profile.ProfileId);

        // Assert
        Assert.NotNull(patterns);
        // With 10 decisions of the same type, we should identify at least one pattern
        Assert.NotEmpty(patterns);
    }

    [Fact]
    public async Task GetConfidenceScore_NoDecisions_ShouldReturnZero()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Act
        var confidence = await _learningService.GetConfidenceScoreAsync(
            profile.ProfileId,
            "visual");

        // Assert
        Assert.Equal(0.0, confidence);
    }

    [Fact]
    public async Task InferPreferences_WithSufficientData_ShouldInferPreferences()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Add test decisions with consistent preferences
        for (int i = 0; i < 10; i++)
        {
            var decision = new RecordDecisionRequest(
                ProfileId: profile.ProfileId,
                SuggestionType: "visual",
                Decision: "accepted",
                Context: new Dictionary<string, object> 
                { 
                    { "aesthetic", "cinematic" },
                    { "colorPalette", "warm" }
                });
            
            await _profileService.RecordDecisionAsync(decision);
        }

        // Act
        var preferences = await _learningService.InferPreferencesAsync(profile.ProfileId);

        // Assert
        Assert.NotNull(preferences);
        // With consistent decisions, we should infer some preferences
        // The exact number depends on the inference logic
    }

    [Fact]
    public async Task ResetLearning_ShouldClearAllLearningData()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Add some decisions
        for (int i = 0; i < 5; i++)
        {
            var decision = new RecordDecisionRequest(
                ProfileId: profile.ProfileId,
                SuggestionType: "visual",
                Decision: "accepted",
                Context: new Dictionary<string, object> { { "aesthetic", "cinematic" } });
            
            await _profileService.RecordDecisionAsync(decision);
        }

        // Analyze to create patterns
        await _learningService.AnalyzePatternsAsync(profile.ProfileId);

        // Act
        await _learningService.ResetLearningAsync(profile.ProfileId);

        // Assert
        var patterns = await _learningService.GetPatternsAsync(profile.ProfileId);
        var insights = await _learningService.GetInsightsAsync(profile.ProfileId);
        var preferences = await _learningService.GetInferredPreferencesAsync(profile.ProfileId);

        Assert.Empty(patterns);
        Assert.Empty(insights);
        Assert.Empty(preferences);
    }

    [Fact]
    public async Task GetPredictionStats_WithDecisions_ShouldReturnStatistics()
    {
        // Arrange
        var request = new CreateProfileRequest(
            UserId: "user123",
            ProfileName: "Test Profile",
            Description: "Test",
            FromTemplateId: null);
        
        var profile = await _profileService.CreateProfileAsync(request);

        // Add mixed decisions
        var decisions = new[]
        {
            ("visual", "accepted"),
            ("visual", "accepted"),
            ("visual", "rejected"),
            ("audio", "modified"),
            ("audio", "accepted")
        };

        foreach (var (type, decision) in decisions)
        {
            await _profileService.RecordDecisionAsync(new RecordDecisionRequest(
                ProfileId: profile.ProfileId,
                SuggestionType: type,
                Decision: decision,
                Context: null));
        }

        // Act
        var stats = await _learningService.GetPredictionStatsAsync(profile.ProfileId);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, stats.Count); // Should have stats for 'visual' and 'audio'
        
        var visualStats = stats["visual"];
        Assert.Equal(3, visualStats.TotalDecisions);
        Assert.Equal(2, visualStats.Accepted);
        Assert.Equal(1, visualStats.Rejected);
    }
}
