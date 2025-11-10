using Aura.Core.Monitoring;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Monitoring;

public class MetricsCollectorTests
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly MetricsCollector _collector;

    public MetricsCollectorTests()
    {
        _logger = new LoggerFactory().CreateLogger<MetricsCollector>();
        _collector = new MetricsCollector(_logger);
    }

    [Fact]
    public void RecordGauge_ShouldStoreValue()
    {
        // Arrange
        var metricName = "test.gauge";
        var value = 42.5;

        // Act
        _collector.RecordGauge(metricName, value);
        var result = _collector.GetGaugeValue(metricName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void RecordGauge_WithTags_ShouldStoreSeparately()
    {
        // Arrange
        var metricName = "test.gauge";
        var tags1 = new Dictionary<string, string> { ["env"] = "prod" };
        var tags2 = new Dictionary<string, string> { ["env"] = "dev" };

        // Act
        _collector.RecordGauge(metricName, 100, tags1);
        _collector.RecordGauge(metricName, 200, tags2);

        // Assert
        Assert.Equal(100, _collector.GetGaugeValue(metricName, tags1));
        Assert.Equal(200, _collector.GetGaugeValue(metricName, tags2));
    }

    [Fact]
    public void IncrementCounter_ShouldIncreaseValue()
    {
        // Arrange
        var counterName = "test.counter";

        // Act
        _collector.IncrementCounter(counterName, 1);
        _collector.IncrementCounter(counterName, 5);
        _collector.IncrementCounter(counterName, 3);
        var result = _collector.GetCounterValue(counterName);

        // Assert
        Assert.Equal(9, result);
    }

    [Fact]
    public void RecordHistogram_ShouldCalculateStats()
    {
        // Arrange
        var histogramName = "test.histogram";
        var values = new[] { 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0 };

        // Act
        foreach (var value in values)
        {
            _collector.RecordHistogram(histogramName, value);
        }
        var stats = _collector.GetHistogramStats(histogramName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10, stats.Count);
        Assert.Equal(10.0, stats.Min);
        Assert.Equal(100.0, stats.Max);
        Assert.Equal(55.0, stats.Mean);
        Assert.Equal(50.0, stats.P50);
        Assert.InRange(stats.P90, 89.0, 91.0);
        Assert.InRange(stats.P95, 94.0, 96.0);
        Assert.InRange(stats.P99, 98.0, 100.0);
    }

    [Fact]
    public void MeasureDuration_ShouldRecordElapsedTime()
    {
        // Arrange
        var metricName = "test.duration";

        // Act
        using (var timer = _collector.MeasureDuration(metricName))
        {
            Thread.Sleep(50); // Simulate work
        }
        var stats = _collector.GetHistogramStats($"{metricName}.duration_ms");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Count);
        Assert.InRange(stats.Mean, 40, 100); // Allow some variance
    }

    [Fact]
    public void GetSnapshot_ShouldIncludeAllMetrics()
    {
        // Arrange
        _collector.RecordGauge("gauge1", 10);
        _collector.RecordGauge("gauge2", 20);
        _collector.IncrementCounter("counter1", 5);
        _collector.RecordHistogram("histogram1", 100);

        // Act
        var snapshot = _collector.GetSnapshot();

        // Assert
        Assert.Equal(2, snapshot.Gauges.Count);
        Assert.Single(snapshot.Counters);
        Assert.Single(snapshot.Histograms);
    }

    [Fact]
    public void ResetCounters_ShouldClearAllCounters()
    {
        // Arrange
        _collector.IncrementCounter("counter1", 10);
        _collector.IncrementCounter("counter2", 20);

        // Act
        _collector.ResetCounters();

        // Assert
        Assert.Equal(0, _collector.GetCounterValue("counter1"));
        Assert.Equal(0, _collector.GetCounterValue("counter2"));
    }

    [Fact]
    public void Histogram_ShouldLimitStoredValues()
    {
        // Arrange
        var histogramName = "test.large.histogram";

        // Act - Add more than 1000 values
        for (int i = 0; i < 1500; i++)
        {
            _collector.RecordHistogram(histogramName, i);
        }
        var stats = _collector.GetHistogramStats(histogramName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(1000, stats.Count); // Should be limited to 1000
    }
}
