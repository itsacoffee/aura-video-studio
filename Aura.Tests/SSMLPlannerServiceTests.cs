using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;
using Aura.Providers.Tts.validators;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class SSMLPlannerServiceTests
{
    private readonly SSMLPlannerService _service;
    private readonly Mock<ILogger<SSMLPlannerService>> _loggerMock;

    public SSMLPlannerServiceTests()
    {
        _loggerMock = new Mock<ILogger<SSMLPlannerService>>();
        
        var mappers = new List<ISSMLMapper>
        {
            new ElevenLabsSSMLMapper(),
            new WindowsSSMLMapper(),
            new PlayHTSSMLMapper(),
            new PiperSSMLMapper(),
            new Mimic3SSMLMapper()
        };
        
        _service = new SSMLPlannerService(_loggerMock.Object, mappers);
    }

    [Fact]
    public async Task PlanSSMLAsync_SimpleText_ReturnsValidSSML()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello, this is a test narration.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };

        var targetDurations = new Dictionary<int, double> { { 0, 3.0 } };
        var voiceSpec = CreateTestVoiceSpec();

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = voiceSpec,
            TargetDurations = targetDurations,
            DurationTolerance = 0.02,
            MaxFittingIterations = 10
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Segments);
        Assert.Contains("<speak>", result.Segments[0].SsmlMarkup);
        Assert.Contains("</speak>", result.Segments[0].SsmlMarkup);
    }

    [Fact]
    public async Task PlanSSMLAsync_MultipleScenes_GeneratesAllSegments()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Welcome to our presentation.", TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Today we will discuss advanced topics in artificial intelligence.", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)),
            new ScriptLine(2, "Let's begin with the fundamentals.", TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(3))
        };

        var targetDurations = new Dictionary<int, double>
        {
            { 0, 2.0 },
            { 1, 5.0 },
            { 2, 3.0 }
        };

        var voiceSpec = CreateTestVoiceSpec();

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = voiceSpec,
            TargetDurations = targetDurations
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        Assert.Equal(3, result.Segments.Count);
        Assert.All(result.Segments, segment =>
        {
            Assert.Contains("<speak>", segment.SsmlMarkup);
            Assert.Contains("</speak>", segment.SsmlMarkup);
        });
    }

    [Fact]
    public async Task PlanSSMLAsync_DurationFitting_AdjustsToTarget()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is a test sentence that needs to be adjusted to fit a specific duration target.", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        var targetDurations = new Dictionary<int, double> { { 0, 5.0 } };
        var voiceSpec = CreateTestVoiceSpec();

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = voiceSpec,
            TargetDurations = targetDurations,
            DurationTolerance = 0.02
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        var segment = result.Segments.First();
        var deviationPercent = Math.Abs(segment.DeviationPercent);
        
        Assert.True(deviationPercent <= 2.0, 
            $"Deviation {deviationPercent:F2}% exceeds 2% tolerance");
    }

    [Fact]
    public async Task PlanSSMLAsync_WithinTolerance_ReportsStats()
    {
        var scriptLines = CreateMultipleScriptLines(5);
        var targetDurations = scriptLines.ToDictionary(
            line => line.SceneIndex,
            line => line.Duration.TotalSeconds
        );

        var voiceSpec = CreateTestVoiceSpec();

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = voiceSpec,
            TargetDurations = targetDurations,
            DurationTolerance = 0.02
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        Assert.NotNull(result.Stats);
        Assert.True(result.Stats.WithinTolerancePercent >= 0 && result.Stats.WithinTolerancePercent <= 100);
        Assert.True(result.Stats.AverageDeviation >= 0);
        Assert.True(result.Stats.TargetDurationSeconds > 0);
        Assert.True(result.Stats.ActualDurationSeconds > 0);
    }

    [Fact]
    public async Task PlanSSMLAsync_InvalidProvider_ThrowsException()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.Azure,
            VoiceSpec = CreateTestVoiceSpec(),
            TargetDurations = new Dictionary<int, double> { { 0, 1.0 } }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.PlanSSMLAsync(request, CancellationToken.None)
        );
    }

    [Fact]
    public async Task PlanSSMLAsync_WindowsProvider_GeneratesValidSAPISSML()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Testing Windows SAPI provider.", TimeSpan.Zero, TimeSpan.FromSeconds(2))
        };

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.WindowsSAPI,
            VoiceSpec = CreateTestVoiceSpec(),
            TargetDurations = new Dictionary<int, double> { { 0, 2.0 } }
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        var ssml = result.Segments.First().SsmlMarkup;
        Assert.Contains("xmlns=\"http://www.w3.org/2001/10/synthesis\"", ssml);
    }

    [Fact]
    public async Task PlanSSMLAsync_PiperProvider_GeneratesSimplifiedSSML()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Testing Piper provider with limited SSML support.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.Piper,
            VoiceSpec = CreateTestVoiceSpec(),
            TargetDurations = new Dictionary<int, double> { { 0, 3.0 } }
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        var ssml = result.Segments.First().SsmlMarkup;
        Assert.DoesNotContain("<prosody", ssml);
    }

    [Fact]
    public async Task PlanSSMLAsync_LongText_ConvergesWithinMaxIterations()
    {
        var longText = string.Join(" ", Enumerable.Repeat("This is a long sentence with many words.", 10));
        
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, longText, TimeSpan.Zero, TimeSpan.FromSeconds(30))
        };

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            TargetDurations = new Dictionary<int, double> { { 0, 30.0 } },
            MaxFittingIterations = 10
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        var segment = result.Segments.First();
        Assert.True(segment.Adjustments.Iterations <= 10);
        Assert.NotEmpty(segment.SsmlMarkup);
    }

    [Fact]
    public async Task PlanSSMLAsync_HighAccuracyRequired_MeetsStrictTolerance()
    {
        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Precision timing test for strict tolerance.", TimeSpan.Zero, TimeSpan.FromSeconds(4))
        };

        var request = new SSMLPlanningRequest
        {
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            TargetDurations = new Dictionary<int, double> { { 0, 4.0 } },
            DurationTolerance = 0.01,
            MaxFittingIterations = 20
        };

        var result = await _service.PlanSSMLAsync(request, CancellationToken.None);

        var deviation = Math.Abs(result.Segments.First().DeviationPercent);
        Assert.True(result.Stats.WithinTolerancePercent >= 80,
            $"Only {result.Stats.WithinTolerancePercent:F1}% within tolerance");
    }

    private VoiceSpec CreateTestVoiceSpec()
    {
        return new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);
    }

    private List<ScriptLine> CreateMultipleScriptLines(int count)
    {
        var lines = new List<ScriptLine>();
        var currentTime = TimeSpan.Zero;

        for (int i = 0; i < count; i++)
        {
            var duration = TimeSpan.FromSeconds(2 + (i % 3));
            lines.Add(new ScriptLine(i, $"This is test scene number {i + 1} with some content to narrate.", currentTime, duration));
            currentTime += duration;
        }

        return lines;
    }
}
