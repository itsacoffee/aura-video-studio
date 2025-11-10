using System;
using Aura.Core.Models;
using Aura.Core.Models.Voice;
using Xunit;

namespace Aura.Tests;

public class SSMLParserTests
{
    [Fact]
    public void ExtractPlainText_WithSimpleSSML_ReturnsText()
    {
        // Arrange
        var ssml = @"<?xml version=""1.0""?>
<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
    <voice name=""TestVoice"">
        Hello, this is a test.
    </voice>
</speak>";

        // Act
        var result = SSMLParser.ExtractPlainText(ssml);

        // Assert
        Assert.Contains("Hello, this is a test", result);
        Assert.DoesNotContain("<speak>", result);
        Assert.DoesNotContain("<voice>", result);
    }

    [Fact]
    public void ExtractPlainText_WithProsody_ReturnsTextWithoutTags()
    {
        // Arrange
        var ssml = @"<speak><prosody rate=""fast"">Quick text</prosody></speak>";

        // Act
        var result = SSMLParser.ExtractPlainText(ssml);

        // Assert
        Assert.Contains("Quick text", result);
        Assert.DoesNotContain("prosody", result);
        Assert.DoesNotContain("rate", result);
    }

    [Fact]
    public void ExtractPlainText_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = SSMLParser.ExtractPlainText(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void WrapInSSML_WithSimpleText_CreatesValidSSML()
    {
        // Arrange
        var text = "Hello, world!";

        // Act
        var result = SSMLParser.WrapInSSML(text);

        // Assert
        Assert.Contains("<?xml version=\"1.0\"?>", result);
        Assert.Contains("<speak", result);
        Assert.Contains("</speak>", result);
        Assert.Contains("Hello, world!", result);
    }

    [Fact]
    public void WrapInSSML_WithVoiceName_IncludesVoiceTag()
    {
        // Arrange
        var text = "Test text";
        var voiceName = "TestVoice";

        // Act
        var result = SSMLParser.WrapInSSML(text, voiceName);

        // Assert
        Assert.Contains($"<voice name=\"{voiceName}\">", result);
        Assert.Contains("</voice>", result);
    }

    [Fact]
    public void AddProsody_WithRate_CreatesCorrectSSML()
    {
        // Arrange
        var text = "Test text";

        // Act
        var result = SSMLParser.AddProsody(text, rate: 1.5);

        // Assert
        Assert.Contains("<prosody", result);
        Assert.Contains("rate=", result);
        Assert.Contains("</prosody>", result);
    }

    [Fact]
    public void AddProsody_WithPitch_CreatesCorrectSSML()
    {
        // Arrange
        var text = "Test text";

        // Act
        var result = SSMLParser.AddProsody(text, pitch: 5.0);

        // Assert
        Assert.Contains("<prosody", result);
        Assert.Contains("pitch=\"+5st\"", result);
    }

    [Fact]
    public void AddProsody_WithNegativePitch_FormatsCorrently()
    {
        // Arrange
        var text = "Test text";

        // Act
        var result = SSMLParser.AddProsody(text, pitch: -3.0);

        // Assert
        Assert.Contains("pitch=\"-3st\"", result);
    }

    [Fact]
    public void AddBreak_WithDifferentStyles_ReturnsCorrectTags()
    {
        // Arrange & Act
        var shortBreak = SSMLParser.AddBreak(PauseStyle.Short);
        var naturalBreak = SSMLParser.AddBreak(PauseStyle.Natural);
        var longBreak = SSMLParser.AddBreak(PauseStyle.Long);
        var dramaticBreak = SSMLParser.AddBreak(PauseStyle.Dramatic);

        // Assert
        Assert.Contains("break", shortBreak);
        Assert.Contains("weak", shortBreak);
        
        Assert.Contains("break", naturalBreak);
        Assert.Contains("medium", naturalBreak);
        
        Assert.Contains("break", longBreak);
        Assert.Contains("strong", longBreak);
        
        Assert.Contains("break", dramaticBreak);
        Assert.Contains("time=", dramaticBreak);
    }

    [Fact]
    public void AddBreak_WithTimeSpan_CreatesTimedBreak()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(500);

        // Act
        var result = SSMLParser.AddBreak(duration);

        // Assert
        Assert.Contains("break", result);
        Assert.Contains("time=\"500ms\"", result);
    }

    [Fact]
    public void AddEmphasis_WithDifferentLevels_CreatesCorrectTags()
    {
        // Arrange
        var text = "Important";

        // Act
        var strong = SSMLParser.AddEmphasis(text, EmphasisLevel.Strong);
        var moderate = SSMLParser.AddEmphasis(text, EmphasisLevel.Moderate);
        var reduced = SSMLParser.AddEmphasis(text, EmphasisLevel.Reduced);

        // Assert
        Assert.Contains("level=\"strong\"", strong);
        Assert.Contains("level=\"moderate\"", moderate);
        Assert.Contains("level=\"reduced\"", reduced);
        
        Assert.All(new[] { strong, moderate, reduced }, 
            s => Assert.Contains("Important", s));
    }

    [Fact]
    public void AddSayAs_WithCardinal_CreatesCorrectTag()
    {
        // Arrange
        var text = "123";

        // Act
        var result = SSMLParser.AddSayAs(text, SayAsInterpret.Cardinal);

        // Assert
        Assert.Contains("say-as", result);
        Assert.Contains("interpret-as=\"cardinal\"", result);
        Assert.Contains("123", result);
    }

    [Fact]
    public void AddSayAs_WithDateAndFormat_IncludesFormat()
    {
        // Arrange
        var text = "2024-01-15";

        // Act
        var result = SSMLParser.AddSayAs(text, SayAsInterpret.Date, format: "ymd");

        // Assert
        Assert.Contains("interpret-as=\"date\"", result);
        Assert.Contains("format=\"ymd\"", result);
    }

    [Fact]
    public void AddPhoneme_CreatesCorrectTag()
    {
        // Arrange
        var text = "tomato";
        var phoneme = "təˈmeɪtoʊ";

        // Act
        var result = SSMLParser.AddPhoneme(text, phoneme);

        // Assert
        Assert.Contains("phoneme", result);
        Assert.Contains("alphabet=\"ipa\"", result);
        Assert.Contains($"ph=\"{phoneme}\"", result);
        Assert.Contains("tomato", result);
    }

    [Fact]
    public void Validate_WithValidSSML_ReturnsValid()
    {
        // Arrange
        var validSSML = @"<?xml version=""1.0""?>
<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
    Hello, world!
</speak>";

        // Act
        var result = SSMLParser.Validate(validSSML);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithMissingSpeakTag_ReturnsInvalid()
    {
        // Arrange
        var invalidSSML = @"<?xml version=""1.0""?>
<voice name=""test"">Hello</voice>";

        // Act
        var result = SSMLParser.Validate(invalidSSML);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("<speak>"));
    }

    [Fact]
    public void Validate_WithUnbalancedTags_ReturnsError()
    {
        // Arrange
        var invalidSSML = @"<?xml version=""1.0""?>
<speak>
    <voice name=""test"">Hello
</speak>";

        // Act
        var result = SSMLParser.Validate(invalidSSML);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unbalanced"));
    }

    [Fact]
    public void Validate_WithUnsupportedTags_ReturnsWarning()
    {
        // Arrange
        var ssmlWithWarnings = @"<?xml version=""1.0""?>
<speak>
    <b>Bold text</b>
</speak>";

        // Act
        var result = SSMLParser.Validate(ssmlWithWarnings);

        // Assert
        // May have warnings but should note the issue
        Assert.True(result.Warnings.Length > 0 || result.Errors.Length > 0);
    }

    [Fact]
    public void SSMLBuilder_BuildsCompleteDocument()
    {
        // Arrange & Act
        var builder = new SSMLBuilder("en-US");
        var ssml = builder
            .AddVoice("TestVoice")
            .AddText("Hello, world!")
            .AddBreak(PauseStyle.Short)
            .AddText("How are you?")
            .EndVoice()
            .Build();

        // Assert
        Assert.Contains("<?xml version=\"1.0\"?>", ssml);
        Assert.Contains("<speak", ssml);
        Assert.Contains("</speak>", ssml);
        Assert.Contains("<voice name=\"TestVoice\">", ssml);
        Assert.Contains("</voice>", ssml);
        Assert.Contains("Hello, world!", ssml);
        Assert.Contains("How are you?", ssml);
        Assert.Contains("break", ssml);
    }

    [Fact]
    public void SSMLBuilder_WithProsody_AddsCorrectly()
    {
        // Arrange & Act
        var builder = new SSMLBuilder();
        var ssml = builder
            .AddProsody("Fast speech", rate: 1.5)
            .Build();

        // Assert
        Assert.Contains("prosody", ssml);
        Assert.Contains("Fast speech", ssml);
    }

    [Fact]
    public void SSMLBuilder_WithMultipleElements_BuildsCorrectly()
    {
        // Arrange & Act
        var builder = new SSMLBuilder();
        var ssml = builder
            .AddText("First line")
            .AddBreak(TimeSpan.FromMilliseconds(500))
            .AddEmphasis("Important", EmphasisLevel.Strong)
            .AddBreak(PauseStyle.Long)
            .AddText("Final line")
            .Build();

        // Assert
        Assert.Contains("First line", ssml);
        Assert.Contains("Important", ssml);
        Assert.Contains("Final line", ssml);
        Assert.Contains("emphasis", ssml);
    }
}
