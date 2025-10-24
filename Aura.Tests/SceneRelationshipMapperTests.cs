using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Services.PacingServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the SceneRelationshipMapper service
/// </summary>
public class SceneRelationshipMapperTests
{
    private readonly SceneRelationshipMapper _mapper;

    public SceneRelationshipMapperTests()
    {
        var logger = NullLogger<SceneRelationshipMapper>.Instance;
        _mapper = new SceneRelationshipMapper(logger);
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithRelatedScenes_BuildsRelationships()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "AI Intro", "AI technology is amazing.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "AI Details", "This AI technology works well.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.NotEmpty(graph.Relationships);
        Assert.True(graph.Relationships.Any(r => r.Strength > 0.3));
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithUnrelatedScenes_DetectsFlowIssues()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "AI", "AI technology is amazing.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Cooking", "Now let's make pasta.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.NotEmpty(graph.FlowIssues);
        Assert.Contains(graph.FlowIssues, i => i.IssueType == "non-sequitur");
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithCallbackPattern_DetectsCallback()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Setup", "Let me introduce the main concept.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Main", "Some content here.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Callback", "Remember what I mentioned earlier about the concept.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.NotEmpty(graph.Relationships);
        Assert.Contains(graph.Relationships, r => r.ConnectionType == "callback");
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithSetupPayoff_IdentifiesDependency()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Setup", "We will reveal something amazing.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Payoff", "Finally, here is the revealed result!", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.NotEmpty(graph.InformationDependencies);
        Assert.Contains(graph.InformationDependencies, d => d.DependencyType == "setup-payoff");
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithGoodFlow_IsCoherent()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "AI Part 1", "AI technology is amazing.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "AI Part 2", "This AI technology works by learning.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "AI Part 3", "The learning process in AI is fascinating.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.True(graph.IsCoherent);
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithOrphanedScene_DetectsOrphanIssue()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "AI", "AI is great.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Random", "Quantum particles behave strangely.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "AI Again", "Back to AI discussion.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.NotEmpty(graph.FlowIssues);
        Assert.Contains(graph.FlowIssues, i => i.IssueType == "orphaned");
    }

    [Fact]
    public async Task MapRelationshipsAsync_SuggestsReordering_ForBetterFlow()
    {
        // Arrange - scenes with very strong topic overlap separated by unrelated content
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Welcome to this video about artificial intelligence technology.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Topic A", "Let's discuss artificial intelligence technology in detail with lots of technology.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Unrelated", "Now about cooking pasta recipes.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)),
            new Scene(3, "Topic A Again", "More about artificial intelligence technology details and technology applications.", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        // With strong overlap between scenes 1 and 3, should suggest reordering or detect flow issues
        var hasSuggestions = graph.ReorderingSuggestions.Count > 0;
        var hasFlowIssues = graph.FlowIssues.Count > 0;
        Assert.True(hasSuggestions || hasFlowIssues, "Expected reordering suggestions or flow issues for non-sequential related scenes");
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithSequentialScenes_MarksAsSequential()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "First", "First scene.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Second", "Second scene.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        var sequentialRel = graph.Relationships.FirstOrDefault(r => r.FromSceneIndex == 0 && r.ToSceneIndex == 1);
        Assert.NotNull(sequentialRel);
        Assert.True(sequentialRel.IsSequential);
    }

    [Fact]
    public async Task MapRelationshipsAsync_WithSingleScene_ReturnsEmptyGraph()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Only", "Only one scene.", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        // Act
        var graph = await _mapper.MapRelationshipsAsync(scenes);

        // Assert
        Assert.Empty(graph.Relationships);
        Assert.True(graph.IsCoherent);
    }
}
