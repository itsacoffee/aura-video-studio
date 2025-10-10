using System;
using System.Globalization;
using Xunit;
using Aura.Core.Models;
using Aura.Core.Rendering;
using Aura.Core.Timeline.Overlays;

namespace Aura.Tests.Timeline;

public class OverlayTests
{
    [Fact]
    public void OverlayModel_ToDrawTextFilter_Should_BeCultureInvariant()
    {
        var overlay = new OverlayModel(
            id: "overlay1",
            type: OverlayType.Title,
            text: "Test Title",
            inTime: TimeSpan.FromSeconds(1.5),
            outTime: TimeSpan.FromSeconds(5.25),
            alignment: SafeAreaAlignment.TopCenter,
            fontSize: 48,
            fontColor: "white"
        );

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var filter = overlay.ToDrawTextFilter(1920, 1080);

            Assert.Contains("1.500", filter);
            Assert.Contains("5.250", filter);
            Assert.DoesNotContain(",", filter.Split('=')[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void OverlayModel_ToDrawTextFilter_Should_EscapeSpecialCharacters()
    {
        var overlay = new OverlayModel(
            id: "overlay1",
            type: OverlayType.Title,
            text: "Test: 100% Complete\\Special",
            inTime: TimeSpan.FromSeconds(0),
            outTime: TimeSpan.FromSeconds(5),
            fontSize: 48,
            fontColor: "white"
        );

        var filter = overlay.ToDrawTextFilter(1920, 1080);

        Assert.Contains("Test\\: 100\\% Complete\\\\Special", filter);
    }

    [Fact]
    public void OverlayModel_CreateTitle_Should_HaveCorrectDefaults()
    {
        var title = OverlayModel.CreateTitle("My Title", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        Assert.Equal(OverlayType.Title, title.Type);
        Assert.Equal("My Title", title.Text);
        Assert.Equal(SafeAreaAlignment.TopCenter, title.Alignment);
        Assert.Equal(72, title.FontSize);
        Assert.NotNull(title.BackgroundColor);
    }

    [Fact]
    public void OverlayModel_CreateLowerThird_Should_HaveCorrectDefaults()
    {
        var lowerThird = OverlayModel.CreateLowerThird("Speaker Name", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        Assert.Equal(OverlayType.LowerThird, lowerThird.Type);
        Assert.Equal("Speaker Name", lowerThird.Text);
        Assert.Equal(SafeAreaAlignment.BottomLeft, lowerThird.Alignment);
        Assert.Equal(36, lowerThird.FontSize);
    }

    [Fact]
    public void OverlayModel_CreateCallout_Should_HaveCorrectDefaults()
    {
        var callout = OverlayModel.CreateCallout("Important!", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));

        Assert.Equal(OverlayType.Callout, callout.Type);
        Assert.Equal("Important!", callout.Text);
        Assert.Equal(SafeAreaAlignment.MiddleRight, callout.Alignment);
        Assert.Equal(48, callout.FontSize);
        Assert.Equal("yellow", callout.FontColor);
    }

    [Fact]
    public void OverlayModel_GetPosition_Should_ReturnCorrectCoordinates()
    {
        var overlay = new OverlayModel(
            id: "overlay1",
            type: OverlayType.Title,
            text: "Test",
            inTime: TimeSpan.Zero,
            outTime: TimeSpan.FromSeconds(5),
            alignment: SafeAreaAlignment.TopLeft,
            fontSize: 48,
            fontColor: "white"
        );

        var (x, y) = overlay.GetPosition(1920, 1080);

        Assert.Equal(50, x);
        Assert.Equal(50, y);
    }

    [Fact]
    public void OverlayModel_GetPosition_Custom_Should_UseXY()
    {
        var overlay = new OverlayModel(
            id: "overlay1",
            type: OverlayType.Title,
            text: "Test",
            inTime: TimeSpan.Zero,
            outTime: TimeSpan.FromSeconds(5),
            alignment: SafeAreaAlignment.Custom,
            x: 100,
            y: 200,
            fontSize: 48,
            fontColor: "white"
        );

        var (x, y) = overlay.GetPosition(1920, 1080);

        Assert.Equal(100, x);
        Assert.Equal(200, y);
    }

    [Fact]
    public void FFmpegPlanBuilder_BuildFilterGraphWithOverlays_Should_BeInvariant()
    {
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        
        var overlays = new[]
        {
            OverlayModel.CreateTitle("Title 1", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)),
            OverlayModel.CreateLowerThird("Name", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8))
        };

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            var filterGraph = builder.BuildFilterGraphWithOverlays(resolution, overlays);

            Assert.Contains("drawtext", filterGraph);
            Assert.Contains("1.000", filterGraph);
            Assert.Contains("3.000", filterGraph);
            Assert.DoesNotContain(",", filterGraph.Split(':')[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void FFmpegPlanBuilder_BuildFilterGraphWithOverlays_Should_OrderByInTime()
    {
        var builder = new FFmpegPlanBuilder();
        var resolution = new Resolution(1920, 1080);
        
        var overlays = new[]
        {
            new OverlayModel("id2", OverlayType.Title, "Second", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8)),
            new OverlayModel("id1", OverlayType.Title, "First", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))
        };

        var filterGraph = builder.BuildFilterGraphWithOverlays(resolution, overlays);

        var firstIndex = filterGraph.IndexOf("First", StringComparison.Ordinal);
        var secondIndex = filterGraph.IndexOf("Second", StringComparison.Ordinal);

        Assert.True(firstIndex < secondIndex);
    }

    [Fact]
    public void OverlayModel_Duration_Should_CalculateCorrectly()
    {
        var overlay = new OverlayModel(
            id: "overlay1",
            type: OverlayType.Title,
            text: "Test",
            inTime: TimeSpan.FromSeconds(2),
            outTime: TimeSpan.FromSeconds(7),
            fontSize: 48,
            fontColor: "white"
        );

        Assert.Equal(TimeSpan.FromSeconds(5), overlay.Duration);
    }
}
