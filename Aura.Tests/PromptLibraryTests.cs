using System.Linq;
using Aura.Core.Services.AI;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for PromptLibrary
/// </summary>
public class PromptLibraryTests
{
    private readonly PromptLibrary _library;

    public PromptLibraryTests()
    {
        _library = new PromptLibrary();
    }

    [Fact]
    public void GetAllExamples_ReturnsNonEmptyList()
    {
        // Act
        var examples = _library.GetAllExamples();

        // Assert
        Assert.NotNull(examples);
        Assert.NotEmpty(examples);
    }

    [Fact]
    public void GetAllExamples_ContainsMultipleVideoTypes()
    {
        // Act
        var examples = _library.GetAllExamples();
        var videoTypes = examples.Select(e => e.VideoType).Distinct().ToList();

        // Assert
        Assert.NotEmpty(videoTypes);
        Assert.Contains("Educational", videoTypes);
    }

    [Fact]
    public void GetVideoTypes_ReturnsDistinctTypes()
    {
        // Act
        var types = _library.GetVideoTypes();

        // Assert
        Assert.NotNull(types);
        Assert.NotEmpty(types);
        Assert.Equal(types.Count, types.Distinct().Count());
    }

    [Fact]
    public void GetVideoTypes_ReturnsSortedList()
    {
        // Act
        var types = _library.GetVideoTypes();

        // Assert
        Assert.NotNull(types);
        Assert.Equal(types.OrderBy(t => t).ToList(), types);
    }

    [Fact]
    public void GetExamplesByType_ValidType_ReturnsExamples()
    {
        // Arrange
        var allExamples = _library.GetAllExamples();
        var firstType = allExamples.First().VideoType;

        // Act
        var examples = _library.GetExamplesByType(firstType);

        // Assert
        Assert.NotNull(examples);
        Assert.NotEmpty(examples);
        Assert.All(examples, e => Assert.Equal(firstType, e.VideoType));
    }

    [Fact]
    public void GetExamplesByType_CaseInsensitive_ReturnsExamples()
    {
        // Arrange
        var allExamples = _library.GetAllExamples();
        var firstType = allExamples.First().VideoType;

        // Act
        var examples = _library.GetExamplesByType(firstType.ToUpperInvariant());

        // Assert
        Assert.NotNull(examples);
        Assert.NotEmpty(examples);
    }

    [Fact]
    public void GetExamplesByType_InvalidType_ReturnsEmpty()
    {
        // Act
        var examples = _library.GetExamplesByType("NonExistentType");

        // Assert
        Assert.NotNull(examples);
        Assert.Empty(examples);
    }

    [Fact]
    public void GetExampleByName_ValidName_ReturnsExample()
    {
        // Arrange
        var allExamples = _library.GetAllExamples();
        var firstName = allExamples.First().ExampleName;

        // Act
        var example = _library.GetExampleByName(firstName);

        // Assert
        Assert.NotNull(example);
        Assert.Equal(firstName, example.ExampleName);
    }

    [Fact]
    public void GetExampleByName_CaseInsensitive_ReturnsExample()
    {
        // Arrange
        var allExamples = _library.GetAllExamples();
        var firstName = allExamples.First().ExampleName;

        // Act
        var example = _library.GetExampleByName(firstName.ToLowerInvariant());

        // Assert
        Assert.NotNull(example);
        Assert.Equal(firstName, example.ExampleName);
    }

    [Fact]
    public void GetExampleByName_InvalidName_ReturnsNull()
    {
        // Act
        var example = _library.GetExampleByName("NonExistentExample");

        // Assert
        Assert.Null(example);
    }

    [Fact]
    public void AllExamples_HaveRequiredFields()
    {
        // Act
        var examples = _library.GetAllExamples();

        // Assert
        Assert.All(examples, example =>
        {
            Assert.NotNull(example.VideoType);
            Assert.NotEmpty(example.VideoType);
            Assert.NotNull(example.ExampleName);
            Assert.NotEmpty(example.ExampleName);
            Assert.NotNull(example.Description);
            Assert.NotEmpty(example.Description);
            Assert.NotNull(example.SampleBrief);
            Assert.NotEmpty(example.SampleBrief);
            Assert.NotNull(example.SampleOutput);
            Assert.NotEmpty(example.SampleOutput);
            Assert.NotNull(example.KeyTechniques);
            Assert.NotEmpty(example.KeyTechniques);
        });
    }

    [Fact]
    public void AllExamples_HaveUniqueNames()
    {
        // Act
        var examples = _library.GetAllExamples();
        var names = examples.Select(e => e.ExampleName).ToList();

        // Assert
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void GetExampleByName_EducationalScienceExplainer_ReturnsCorrectExample()
    {
        // Act
        var example = _library.GetExampleByName("Science Explainer");

        // Assert
        Assert.NotNull(example);
        Assert.Equal("Educational", example.VideoType);
        Assert.Equal("Science Explainer", example.ExampleName);
        Assert.NotEmpty(example.KeyTechniques);
    }

    [Fact]
    public void GetExamplesByType_Educational_HasMultipleExamples()
    {
        // Act
        var examples = _library.GetExamplesByType("Educational");

        // Assert
        Assert.NotNull(examples);
        Assert.True(examples.Count > 0, "Educational type should have at least one example");
    }
}
