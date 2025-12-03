using Aura.Core.Services.Fallback;
using FluentAssertions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for TopicAwareFallbackGenerator
/// </summary>
public class TopicAwareFallbackGeneratorTests
{
    private readonly TopicAwareFallbackGenerator _generator;

    public TopicAwareFallbackGeneratorTests()
    {
        _generator = new TopicAwareFallbackGenerator();
    }

    [Fact]
    public void GenerateFallbackScript_TechnologyTopic_UsesTechTemplate()
    {
        // Arrange
        var topic = "Machine Learning Basics";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("digital landscape");
        script.Should().Contain("Machine Learning Basics");
        script.Should().Contain("Key Concepts");
        script.Should().Contain("technology continues to advance");
    }

    [Fact]
    public void GenerateFallbackScript_BusinessTopic_UsesBusinessTemplate()
    {
        // Arrange
        var topic = "Marketing Strategies";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("essential for modern business success");
        script.Should().Contain("Marketing Strategies");
        script.Should().Contain("Core Principles");
        script.Should().Contain("competitive advantage");
    }

    [Fact]
    public void GenerateFallbackScript_EducationTopic_UsesEducationTemplate()
    {
        // Arrange
        var topic = "Learning Python Tutorial";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("opens new opportunities for growth");
        script.Should().Contain("Learning Python Tutorial");
        script.Should().Contain("Fundamentals");
        script.Should().Contain("more advanced concepts");
    }

    [Fact]
    public void GenerateFallbackScript_HealthTopic_UsesHealthTemplate()
    {
        // Arrange
        var topic = "Fitness and Wellness Guide";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("well-being matters");
        script.Should().Contain("Fitness and Wellness Guide");
        script.Should().Contain("What You Need to Know");
        script.Should().Contain("taking care of your health is a journey");
    }

    [Fact]
    public void GenerateFallbackScript_UnknownTopic_UsesDefaultTemplate()
    {
        // Arrange
        var topic = "Random Topic About Nothing";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("Welcome to this overview");
        script.Should().Contain("Random Topic About Nothing");
        script.Should().Contain("Key Points");
        script.Should().Contain("Thank you for learning");
    }

    [Fact]
    public void GenerateFallbackScript_WithGoalAndAudience_IncludesThem()
    {
        // Arrange
        var topic = "Python Programming";
        var goal = "teach the basics";
        var audience = "beginners";

        // Act
        var script = _generator.GenerateFallbackScript(
            topic,
            goal: goal,
            audience: audience);

        // Assert
        script.Should().Contain("teach the basics");
        script.Should().Contain("beginners");
        script.Should().Contain("Our goal today is to");
        script.Should().Contain("This content is designed for");
    }

    [Fact]
    public void GenerateFallbackScript_WithGoalOnly_IncludesGoal()
    {
        // Arrange
        var topic = "AI Development";
        var goal = "explain key concepts";

        // Act
        var script = _generator.GenerateFallbackScript(topic, goal: goal);

        // Assert
        script.Should().Contain("explain key concepts");
        script.Should().NotContain("This content is designed for");
    }

    [Fact]
    public void GenerateFallbackScript_WithAudienceOnly_IncludesAudience()
    {
        // Arrange
        var topic = "Business Finance";
        var audience = "entrepreneurs";

        // Act
        var script = _generator.GenerateFallbackScript(topic, audience: audience);

        // Assert
        script.Should().Contain("entrepreneurs");
        script.Should().Contain("This content is designed for");
        script.Should().NotContain("Our goal today is to");
    }

    [Theory]
    [InlineData(30, 3)]   // 30 seconds -> 3 scenes (minimum)
    [InlineData(60, 3)]   // 60 seconds -> 3 scenes (2 calculated, minimum is 3)
    [InlineData(90, 3)]   // 90 seconds -> 3 scenes
    [InlineData(120, 4)]  // 120 seconds -> 4 scenes
    [InlineData(180, 6)]  // 180 seconds -> 6 scenes
    [InlineData(300, 8)]  // 300 seconds -> 8 scenes (maximum)
    public void GenerateFallbackScript_VariousDurations_CalculatesCorrectSceneCount(int durationSeconds, int expectedSceneCount)
    {
        // Arrange
        var topic = "General Topic";
        var duration = TimeSpan.FromSeconds(durationSeconds);

        // Act
        var script = _generator.GenerateFallbackScript(topic, targetDuration: duration);

        // Assert
        var sceneCount = CountScenes(script);
        sceneCount.Should().Be(expectedSceneCount);
    }

    [Fact]
    public void GenerateFallbackScript_DefaultDuration_Returns3Scenes()
    {
        // Arrange
        var topic = "Some Topic";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        var sceneCount = CountScenes(script);
        sceneCount.Should().Be(3);
    }

    [Fact]
    public void GenerateFallbackScript_HasProperMarkdownFormat()
    {
        // Arrange
        var topic = "Test Topic";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("## Scene 1: Introduction");
        script.Should().Contain("## Scene 2:");
        script.Should().Contain("Conclusion");
    }

    [Fact]
    public void GenerateFallbackScript_SanitizesTopicWithSpecialCharacters()
    {
        // Arrange
        var topic = "Topic with #special *characters* and [brackets]";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().NotContain("#special");
        script.Should().NotContain("*characters*");
        script.Should().NotContain("[brackets]");
    }

    [Fact]
    public void GenerateFallbackScript_EmptyTopic_UsesDefaultTopic()
    {
        // Arrange
        var topic = "";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("this topic");
    }

    [Fact]
    public void GenerateFallbackScript_WhitespaceOnlyTopic_UsesDefaultTopic()
    {
        // Arrange
        var topic = "   ";

        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain("this topic");
    }

    [Theory]
    [InlineData("AI and Machine Learning", "digital landscape")]
    [InlineData("Software Development Best Practices", "digital landscape")]
    [InlineData("Computer Science Fundamentals", "digital landscape")]
    [InlineData("Digital Marketing", "digital landscape")]
    [InlineData("Mobile App Development", "digital landscape")]
    public void GenerateFallbackScript_TechKeywords_MatchesTechTemplate(string topic, string expectedPhrase)
    {
        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain(expectedPhrase);
    }

    [Theory]
    [InlineData("Startup Funding", "business success")]
    [InlineData("Sales Management", "business success")]
    [InlineData("Entrepreneur Guide", "business success")]
    [InlineData("Finance Basics", "business success")]
    public void GenerateFallbackScript_BusinessKeywords_MatchesBusinessTemplate(string topic, string expectedPhrase)
    {
        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain(expectedPhrase);
    }

    [Theory]
    [InlineData("Student Guide", "opens new opportunities")]
    [InlineData("Skill Training", "opens new opportunities")]
    [InlineData("Teaching Methods", "opens new opportunities")]
    public void GenerateFallbackScript_EducationKeywords_MatchesEducationTemplate(string topic, string expectedPhrase)
    {
        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain(expectedPhrase);
    }

    [Theory]
    [InlineData("Nutrition Tips", "well-being matters")]
    [InlineData("Mental Health", "well-being matters")]
    [InlineData("Exercise Routine", "well-being matters")]
    [InlineData("Medical Advice", "well-being matters")]
    public void GenerateFallbackScript_HealthKeywords_MatchesHealthTemplate(string topic, string expectedPhrase)
    {
        // Act
        var script = _generator.GenerateFallbackScript(topic);

        // Assert
        script.Should().Contain(expectedPhrase);
    }

    [Fact]
    public void GenerateFallbackScript_LongDuration_CapsAt8Scenes()
    {
        // Arrange
        var topic = "Extended Topic";
        var duration = TimeSpan.FromMinutes(10); // 600 seconds

        // Act
        var script = _generator.GenerateFallbackScript(topic, targetDuration: duration);

        // Assert
        var sceneCount = CountScenes(script);
        sceneCount.Should().Be(8);
    }

    [Fact]
    public void GenerateFallbackScript_VeryShortDuration_Minimum3Scenes()
    {
        // Arrange
        var topic = "Quick Topic";
        var duration = TimeSpan.FromSeconds(10);

        // Act
        var script = _generator.GenerateFallbackScript(topic, targetDuration: duration);

        // Assert
        var sceneCount = CountScenes(script);
        sceneCount.Should().Be(3);
    }

    /// <summary>
    /// Helper method to count scenes in a script based on markdown headers.
    /// </summary>
    private static int CountScenes(string script)
    {
        var lines = script.Split('\n');
        return lines.Count(line => line.StartsWith("## Scene"));
    }
}
