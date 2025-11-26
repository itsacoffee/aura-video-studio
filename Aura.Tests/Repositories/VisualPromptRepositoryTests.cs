using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data.Repositories;
using Aura.Core.Models.Visual;
using Xunit;

namespace Aura.Tests.Repositories;

public class VisualPromptRepositoryTests
{
    [Fact]
    public async Task SaveAsync_StoresPrompt()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var prompt = new StoredVisualPrompt
        {
            ScriptId = "script-123",
            CorrelationId = "corr-456",
            SceneNumber = 1,
            SceneHeading = "Opening Scene",
            DetailedPrompt = "Test prompt",
            CameraAngle = "Wide shot",
            Lighting = "Natural"
        };

        // Act
        var saved = await repo.SaveAsync(prompt);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("script-123", saved.ScriptId);
        Assert.Equal("corr-456", saved.CorrelationId);
        Assert.Equal(1, saved.SceneNumber);
        Assert.NotEmpty(saved.Id);
    }

    [Fact]
    public async Task GetByScriptIdAsync_ReturnsPromptsInOrder()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "s1", 
            CorrelationId = "c1",
            SceneNumber = 2,
            DetailedPrompt = "Scene 2"
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "s1", 
            CorrelationId = "c1",
            SceneNumber = 1,
            DetailedPrompt = "Scene 1"
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "s2", 
            CorrelationId = "c2",
            SceneNumber = 1,
            DetailedPrompt = "Other script"
        });

        // Act
        var prompts = await repo.GetByScriptIdAsync("s1");

        // Assert
        Assert.Equal(2, prompts.Count);
        Assert.Equal(1, prompts[0].SceneNumber);
        Assert.Equal(2, prompts[1].SceneNumber);
        Assert.All(prompts, p => Assert.Equal("s1", p.ScriptId));
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_ReturnsAllPromptsForCorrelation()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var correlationId = "corr-123";
        
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "script-1", 
            CorrelationId = correlationId,
            SceneNumber = 1
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "script-1", 
            CorrelationId = correlationId,
            SceneNumber = 2
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "script-1", 
            CorrelationId = "other-correlation",
            SceneNumber = 3
        });

        // Act
        var prompts = await repo.GetByCorrelationIdAsync(correlationId);

        // Assert
        Assert.Equal(2, prompts.Count);
        Assert.All(prompts, p => Assert.Equal(correlationId, p.CorrelationId));
    }

    [Fact]
    public async Task GetBySceneAsync_ReturnsSpecificScenePrompt()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var scriptId = "script-123";
        
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = scriptId, 
            CorrelationId = "c1",
            SceneNumber = 1,
            DetailedPrompt = "Scene 1"
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = scriptId, 
            CorrelationId = "c1",
            SceneNumber = 2,
            DetailedPrompt = "Scene 2"
        });

        // Act
        var prompt = await repo.GetBySceneAsync(scriptId, 2);

        // Assert
        Assert.NotNull(prompt);
        Assert.Equal(2, prompt.SceneNumber);
        Assert.Equal("Scene 2", prompt.DetailedPrompt);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPrompt()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var prompt = new StoredVisualPrompt
        {
            ScriptId = "script-1",
            CorrelationId = "corr-1",
            SceneNumber = 1,
            DetailedPrompt = "Original",
            CameraAngle = "Wide",
            Lighting = "Natural"
        };
        var saved = await repo.SaveAsync(prompt);

        // Act
        var update = new UpdateVisualPromptRequest(
            DetailedPrompt: "Updated",
            CameraAngle: "Close-up",
            Lighting: "Dramatic"
        );
        var updated = await repo.UpdateAsync(saved.Id, update);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.DetailedPrompt);
        Assert.Equal("Close-up", updated.CameraAngle);
        Assert.Equal("Dramatic", updated.Lighting);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(saved.Id, updated.Id);
        Assert.Equal(saved.ScriptId, updated.ScriptId);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentPrompt_ReturnsNull()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();

        // Act
        var updated = await repo.UpdateAsync("non-existent", new UpdateVisualPromptRequest());

        // Assert
        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPrompt()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var prompt = new StoredVisualPrompt
        {
            ScriptId = "script-1",
            CorrelationId = "corr-1",
            SceneNumber = 1
        };
        var saved = await repo.SaveAsync(prompt);

        // Act
        var deleted = await repo.DeleteAsync(saved.Id);

        // Assert
        Assert.True(deleted);
        var retrieved = await repo.GetByIdAsync(saved.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentPrompt_ReturnsFalse()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();

        // Act
        var deleted = await repo.DeleteAsync("non-existent");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteByScriptIdAsync_RemovesAllScriptPrompts()
    {
        // Arrange
        var repo = new InMemoryVisualPromptRepository();
        var scriptId = "script-123";
        
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = scriptId, 
            CorrelationId = "c1",
            SceneNumber = 1
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = scriptId, 
            CorrelationId = "c1",
            SceneNumber = 2
        });
        await repo.SaveAsync(new StoredVisualPrompt 
        { 
            ScriptId = "other-script", 
            CorrelationId = "c2",
            SceneNumber = 1
        });

        // Act
        var deletedCount = await repo.DeleteByScriptIdAsync(scriptId);

        // Assert
        Assert.Equal(2, deletedCount);
        var remaining = await repo.GetByScriptIdAsync(scriptId);
        Assert.Empty(remaining);
        
        // Verify other script's prompts still exist
        var otherPrompts = await repo.GetByScriptIdAsync("other-script");
        Assert.Single(otherPrompts);
    }
}

