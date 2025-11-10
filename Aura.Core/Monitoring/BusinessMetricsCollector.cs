using Microsoft.Extensions.Logging;

namespace Aura.Core.Monitoring;

/// <summary>
/// Collects business-level KPIs and metrics
/// </summary>
public class BusinessMetricsCollector
{
    private readonly MetricsCollector _metrics;
    private readonly ILogger<BusinessMetricsCollector> _logger;

    public BusinessMetricsCollector(MetricsCollector metrics, ILogger<BusinessMetricsCollector> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Record job completion
    /// </summary>
    public void RecordJobCompleted(string jobType, bool success, TimeSpan duration, decimal cost = 0)
    {
        var tags = new Dictionary<string, string>
        {
            ["job_type"] = jobType,
            ["status"] = success ? "success" : "failure"
        };

        _metrics.IncrementCounter("jobs.completed", 1, tags);
        _metrics.RecordHistogram("jobs.duration_seconds", duration.TotalSeconds, tags);
        
        if (cost > 0)
        {
            _metrics.RecordHistogram("jobs.cost_usd", (double)cost, tags);
        }

        _logger.LogInformation(
            "Job completed: Type={JobType}, Success={Success}, Duration={Duration}s, Cost=${Cost}",
            jobType, success, duration.TotalSeconds, cost);
    }

    /// <summary>
    /// Record video generation metrics
    /// </summary>
    public void RecordVideoGeneration(int sceneCount, int totalFrames, TimeSpan processingTime, string quality)
    {
        var tags = new Dictionary<string, string>
        {
            ["quality"] = quality
        };

        _metrics.RecordHistogram("video.scenes_per_video", sceneCount, tags);
        _metrics.RecordHistogram("video.frames_generated", totalFrames, tags);
        _metrics.RecordHistogram("video.processing_seconds", processingTime.TotalSeconds, tags);
        _metrics.IncrementCounter("video.generated", 1, tags);
    }

    /// <summary>
    /// Record LLM usage
    /// </summary>
    public void RecordLlmUsage(string provider, string model, int inputTokens, int outputTokens, TimeSpan latency, decimal cost)
    {
        var tags = new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["model"] = model
        };

        _metrics.RecordHistogram("llm.input_tokens", inputTokens, tags);
        _metrics.RecordHistogram("llm.output_tokens", outputTokens, tags);
        _metrics.RecordHistogram("llm.latency_ms", latency.TotalMilliseconds, tags);
        _metrics.RecordHistogram("llm.cost_usd", (double)cost, tags);
        _metrics.IncrementCounter("llm.requests", 1, tags);
    }

    /// <summary>
    /// Record TTS generation
    /// </summary>
    public void RecordTtsGeneration(string provider, int characterCount, TimeSpan duration, decimal cost)
    {
        var tags = new Dictionary<string, string>
        {
            ["provider"] = provider
        };

        _metrics.RecordHistogram("tts.characters", characterCount, tags);
        _metrics.RecordHistogram("tts.generation_seconds", duration.TotalSeconds, tags);
        _metrics.RecordHistogram("tts.cost_usd", (double)cost, tags);
        _metrics.IncrementCounter("tts.requests", 1, tags);
    }

    /// <summary>
    /// Record image generation
    /// </summary>
    public void RecordImageGeneration(string provider, string size, TimeSpan duration, decimal cost)
    {
        var tags = new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["size"] = size
        };

        _metrics.RecordHistogram("image.generation_seconds", duration.TotalSeconds, tags);
        _metrics.RecordHistogram("image.cost_usd", (double)cost, tags);
        _metrics.IncrementCounter("image.generated", 1, tags);
    }

    /// <summary>
    /// Record API request
    /// </summary>
    public void RecordApiRequest(string endpoint, string method, int statusCode, TimeSpan duration)
    {
        var tags = new Dictionary<string, string>
        {
            ["endpoint"] = endpoint,
            ["method"] = method,
            ["status_code"] = statusCode.ToString()
        };

        _metrics.RecordHistogram("api.request_duration_ms", duration.TotalMilliseconds, tags);
        _metrics.IncrementCounter("api.requests", 1, tags);

        if (statusCode >= 500)
        {
            _metrics.IncrementCounter("api.errors.5xx", 1, tags);
        }
        else if (statusCode >= 400)
        {
            _metrics.IncrementCounter("api.errors.4xx", 1, tags);
        }
    }

    /// <summary>
    /// Record cache hit/miss
    /// </summary>
    public void RecordCacheAccess(string cacheType, bool hit)
    {
        var tags = new Dictionary<string, string>
        {
            ["cache_type"] = cacheType,
            ["result"] = hit ? "hit" : "miss"
        };

        _metrics.IncrementCounter("cache.access", 1, tags);
    }

    /// <summary>
    /// Record queue depth
    /// </summary>
    public void RecordQueueDepth(string queueName, int depth)
    {
        var tags = new Dictionary<string, string>
        {
            ["queue"] = queueName
        };

        _metrics.RecordGauge("queue.depth", depth, tags);
    }

    /// <summary>
    /// Record provider health status
    /// </summary>
    public void RecordProviderHealth(string provider, bool healthy, int consecutiveFailures)
    {
        var tags = new Dictionary<string, string>
        {
            ["provider"] = provider
        };

        _metrics.RecordGauge("provider.healthy", healthy ? 1 : 0, tags);
        _metrics.RecordGauge("provider.consecutive_failures", consecutiveFailures, tags);
    }

    /// <summary>
    /// Record cost accumulation
    /// </summary>
    public void RecordCost(string costCategory, decimal amount)
    {
        var tags = new Dictionary<string, string>
        {
            ["category"] = costCategory
        };

        _metrics.RecordHistogram("cost.usd", (double)amount, tags);
        _metrics.IncrementCounter("cost.transactions", 1, tags);
    }
}
