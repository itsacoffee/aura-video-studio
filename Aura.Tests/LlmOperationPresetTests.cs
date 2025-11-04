using Aura.Core.AI.Orchestration;
using Xunit;

namespace Aura.Tests;

public class LlmOperationPresetTests
{
    [Fact]
    public void GetPreset_ForPlanning_ReturnsCorrectPreset()
    {
        var preset = LlmOperationPresets.GetPreset(LlmOperationType.Planning);
        
        Assert.Equal(LlmOperationType.Planning, preset.OperationType);
        Assert.Equal(0.7, preset.Temperature);
        Assert.True(preset.RequiresJsonMode);
        Assert.Equal(2000, preset.MaxTokens);
        Assert.Equal(60, preset.TimeoutSeconds);
    }
    
    [Fact]
    public void GetPreset_ForScripting_ReturnsCorrectPreset()
    {
        var preset = LlmOperationPresets.GetPreset(LlmOperationType.Scripting);
        
        Assert.Equal(LlmOperationType.Scripting, preset.OperationType);
        Assert.Equal(0.8, preset.Temperature);
        Assert.False(preset.RequiresJsonMode);
        Assert.Equal(4000, preset.MaxTokens);
    }
    
    [Fact]
    public void GetPreset_ForVisualPrompts_ReturnsHighTemperature()
    {
        var preset = LlmOperationPresets.GetPreset(LlmOperationType.VisualPrompts);
        
        Assert.Equal(0.9, preset.Temperature);
        Assert.True(preset.RequiresJsonMode);
    }
    
    [Fact]
    public void GetPreset_ForSceneAnalysis_ReturnsLowTemperature()
    {
        var preset = LlmOperationPresets.GetPreset(LlmOperationType.SceneAnalysis);
        
        Assert.Equal(0.3, preset.Temperature);
        Assert.True(preset.RequiresJsonMode);
    }
    
    [Fact]
    public void CreateCustomPreset_WithOverrides_ReturnsCustomizedPreset()
    {
        var custom = LlmOperationPresets.CreateCustomPreset(
            LlmOperationType.Planning,
            temperature: 0.5,
            maxTokens: 3000);
        
        Assert.Equal(0.5, custom.Temperature);
        Assert.Equal(3000, custom.MaxTokens);
        Assert.Equal(0.9, custom.TopP);
        Assert.True(custom.RequiresJsonMode);
    }
    
    [Fact]
    public void GetAllPresets_ReturnsAllOperationTypes()
    {
        var presets = LlmOperationPresets.GetAllPresets();
        
        Assert.True(presets.Count >= 12);
        Assert.Contains(LlmOperationType.Planning, presets.Keys);
        Assert.Contains(LlmOperationType.Scripting, presets.Keys);
        Assert.Contains(LlmOperationType.VisualPrompts, presets.Keys);
        Assert.Contains(LlmOperationType.SceneAnalysis, presets.Keys);
    }
}
