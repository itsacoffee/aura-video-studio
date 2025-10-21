using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class ContentAnalyzerTests
{
    private readonly ContentAnalyzer _analyzer;
    private readonly MockLlmProvider _mockLlmProvider;

    public ContentAnalyzerTests()
    {
        var logger = NullLogger<ContentAnalyzer>.Instance;
        _mockLlmProvider = new MockLlmProvider();
        _analyzer = new ContentAnalyzer(logger, _mockLlmProvider);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_Should_ReturnAnalysis_WithScores()
    {
        // Arrange
        var script = @"# Introduction to AI
## Opening
Hello, welcome to this introduction to Artificial Intelligence.

## Main Content
AI is transforming how we work and live. Let's explore its key concepts.

## Conclusion
Thank you for watching this introduction to AI.";

        _mockLlmProvider.SetResponse(@"
COHERENCE: 85
PACING: 80
ENGAGEMENT: 75
READABILITY: 90
ISSUES:
- Scene transitions could be smoother
- Add more concrete examples
SUGGESTIONS:
- Include a hook in the opening
- Add transition sentences between scenes
");

        // Act
        var analysis = await _analyzer.AnalyzeScriptAsync(script);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(85, analysis.CoherenceScore);
        Assert.Equal(80, analysis.PacingScore);
        Assert.Equal(75, analysis.EngagementScore);
        Assert.Equal(90, analysis.ReadabilityScore);
        Assert.Equal(82.5, analysis.OverallQualityScore); // Average
        Assert.NotEmpty(analysis.Issues);
        Assert.NotEmpty(analysis.Suggestions);
        Assert.NotNull(analysis.Statistics);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_Should_CalculateStatistics_Correctly()
    {
        // Arrange
        var script = "This is a test script with exactly ten words here.";
        _mockLlmProvider.SetResponse("COHERENCE: 80\nPACING: 80\nENGAGEMENT: 80\nREADABILITY: 80\nISSUES:\nSUGGESTIONS:");

        // Act
        var analysis = await _analyzer.AnalyzeScriptAsync(script);

        // Assert
        Assert.NotNull(analysis.Statistics);
        Assert.Equal(10, analysis.Statistics.TotalWordCount);
        Assert.True(analysis.Statistics.EstimatedReadingTime.TotalSeconds > 0);
        Assert.True(analysis.Statistics.ComplexityScore >= 0);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_Should_HandleEmptyScript()
    {
        // Arrange
        var script = "";
        _mockLlmProvider.SetResponse("COHERENCE: 50\nPACING: 50\nENGAGEMENT: 50\nREADABILITY: 50\nISSUES:\nSUGGESTIONS:");

        // Act
        var analysis = await _analyzer.AnalyzeScriptAsync(script);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(0, analysis.Statistics.TotalWordCount);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_Should_ReturnDefault_OnError()
    {
        // Arrange
        var script = "Test script";
        _mockLlmProvider.ShouldThrowError = true;

        // Act
        var analysis = await _analyzer.AnalyzeScriptAsync(script);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(75, analysis.OverallQualityScore); // Default score
        Assert.Contains("Unable to perform detailed analysis", analysis.Issues);
    }

    private sealed class MockLlmProvider : ILlmProvider
    {
        private string _response = "";
        public bool ShouldThrowError { get; set; }

        public void SetResponse(string response)
        {
            _response = response;
        }

        public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
        {
            if (ShouldThrowError)
            {
                throw new InvalidOperationException("Mock error");
            }
            return Task.FromResult(_response);
        }
    }
}
