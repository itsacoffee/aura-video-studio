using System.Collections.Generic;
using Aura.Core.AI.SchemaBuilders;
using Aura.Core.Models.OpenAI;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for ScriptSchemaBuilder to ensure correct JSON schema generation
/// </summary>
public class ScriptSchemaBuilderTests
{
    [Fact]
    public void GetScriptSchema_ReturnsValidResponseFormat()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();

        // Assert
        Assert.NotNull(responseFormat);
        Assert.Equal("json_schema", responseFormat.Type);
        Assert.NotNull(responseFormat.JsonSchema);
    }

    [Fact]
    public void GetScriptSchema_HasCorrectSchemaName()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();

        // Assert
        Assert.Equal("video_script", responseFormat.JsonSchema!.Name);
    }

    [Fact]
    public void GetScriptSchema_HasStrictModeEnabled()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();

        // Assert
        Assert.True(responseFormat.JsonSchema!.Strict);
    }

    [Fact]
    public void GetScriptSchema_HasRequiredTopLevelFields()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var schema = responseFormat.JsonSchema!.Schema;

        // Assert
        Assert.Contains("title", schema.Required);
        Assert.Contains("hook", schema.Required);
        Assert.Contains("scenes", schema.Required);
        Assert.Contains("callToAction", schema.Required);
        Assert.Contains("totalDuration", schema.Required);
    }

    [Fact]
    public void GetScriptSchema_HasCorrectTopLevelProperties()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var properties = responseFormat.JsonSchema!.Schema.Properties;

        // Assert
        Assert.True(properties.ContainsKey("title"));
        Assert.True(properties.ContainsKey("hook"));
        Assert.True(properties.ContainsKey("scenes"));
        Assert.True(properties.ContainsKey("callToAction"));
        Assert.True(properties.ContainsKey("totalDuration"));

        Assert.Equal("string", properties["title"].Type);
        Assert.Equal("string", properties["hook"].Type);
        Assert.Equal("array", properties["scenes"].Type);
        Assert.Equal("string", properties["callToAction"].Type);
        Assert.Equal("number", properties["totalDuration"].Type);
    }

    [Fact]
    public void GetScriptSchema_ScenesArrayHasCorrectStructure()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var scenesProperty = responseFormat.JsonSchema!.Schema.Properties["scenes"];

        // Assert
        Assert.NotNull(scenesProperty.Items);
        Assert.Equal("object", scenesProperty.Items.Type);
        Assert.NotNull(scenesProperty.Items.Properties);
    }

    [Fact]
    public void GetScriptSchema_SceneObjectHasRequiredFields()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var sceneItems = responseFormat.JsonSchema!.Schema.Properties["scenes"].Items!;

        // Assert
        Assert.NotNull(sceneItems.Required);
        Assert.Contains("narration", sceneItems.Required);
        Assert.Contains("visualDescription", sceneItems.Required);
        Assert.Contains("duration", sceneItems.Required);
        Assert.Contains("transition", sceneItems.Required);
    }

    [Fact]
    public void GetScriptSchema_SceneObjectHasCorrectProperties()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var sceneProperties = responseFormat.JsonSchema!.Schema.Properties["scenes"].Items!.Properties!;

        // Assert
        Assert.True(sceneProperties.ContainsKey("narration"));
        Assert.True(sceneProperties.ContainsKey("visualDescription"));
        Assert.True(sceneProperties.ContainsKey("duration"));
        Assert.True(sceneProperties.ContainsKey("transition"));

        Assert.Equal("string", sceneProperties["narration"].Type);
        Assert.Equal("string", sceneProperties["visualDescription"].Type);
        Assert.Equal("number", sceneProperties["duration"].Type);
        Assert.Equal("string", sceneProperties["transition"].Type);
    }

    [Fact]
    public void GetScriptSchema_DurationHasCorrectConstraints()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var durationProperty = responseFormat.JsonSchema!.Schema.Properties["scenes"].Items!.Properties!["duration"];

        // Assert
        Assert.NotNull(durationProperty.Minimum);
        Assert.NotNull(durationProperty.Maximum);
        Assert.Equal(1.0, durationProperty.Minimum.Value);
        Assert.Equal(60.0, durationProperty.Maximum.Value);
    }

    [Fact]
    public void GetScriptSchema_TransitionHasValidEnumValues()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var transitionProperty = responseFormat.JsonSchema!.Schema.Properties["scenes"].Items!.Properties!["transition"];

        // Assert
        Assert.NotNull(transitionProperty.Enum);
        Assert.Contains("cut", transitionProperty.Enum);
        Assert.Contains("fade", transitionProperty.Enum);
        Assert.Contains("dissolve", transitionProperty.Enum);
        Assert.Contains("wipe", transitionProperty.Enum);
        Assert.Contains("slide", transitionProperty.Enum);
        Assert.Contains("zoom", transitionProperty.Enum);
    }

    [Fact]
    public void GetScriptSchema_TotalDurationHasMinimumConstraint()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var totalDurationProperty = responseFormat.JsonSchema!.Schema.Properties["totalDuration"];

        // Assert
        Assert.NotNull(totalDurationProperty.Minimum);
        Assert.Equal(10.0, totalDurationProperty.Minimum.Value);
    }

    [Fact]
    public void GetScriptSchema_DisallowsAdditionalProperties()
    {
        // Act
        var responseFormat = ScriptSchemaBuilder.GetScriptSchema();
        var schema = responseFormat.JsonSchema!.Schema;
        var sceneItems = schema.Properties["scenes"].Items!;

        // Assert
        Assert.False(schema.AdditionalProperties);
        Assert.False(sceneItems.AdditionalProperties);
    }
}
