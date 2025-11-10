using Serilog.Core;
using Serilog.Events;

namespace Aura.Core.Logging;

/// <summary>
/// Enriches log events with trace context information
/// </summary>
public class TraceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = TraceContext.Current?.TraceId;
        if (!string.IsNullOrEmpty(traceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        }

        var spanId = TraceContext.Current?.SpanId;
        if (!string.IsNullOrEmpty(spanId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
        }

        var userId = TraceContext.Current?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
        }

        var operationName = TraceContext.Current?.OperationName;
        if (!string.IsNullOrEmpty(operationName))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("OperationName", operationName));
        }
    }
}

/// <summary>
/// Enriches log events with performance metrics
/// </summary>
public class PerformanceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("Duration", out var durationProperty))
        {
            if (durationProperty is ScalarValue { Value: TimeSpan duration })
            {
                // Add performance category based on duration
                var category = duration.TotalMilliseconds switch
                {
                    < 100 => "Fast",
                    < 1000 => "Normal",
                    < 5000 => "Slow",
                    _ => "VerySlow"
                };

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("PerformanceCategory", category));
            }
        }
    }
}

/// <summary>
/// Enriches log events with request context information
/// </summary>
public class RequestContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = RequestContext.Current;
        if (context == null) return;

        if (!string.IsNullOrEmpty(context.RequestId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", context.RequestId));
        }

        if (!string.IsNullOrEmpty(context.ClientIp))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIp", context.ClientIp));
        }

        if (!string.IsNullOrEmpty(context.UserAgent))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", context.UserAgent));
        }

        if (!string.IsNullOrEmpty(context.Method))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HttpMethod", context.Method));
        }

        if (!string.IsNullOrEmpty(context.Path))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("HttpPath", context.Path));
        }
    }
}

/// <summary>
/// Enriches log events with operation context information
/// </summary>
public class OperationContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = OperationContext.Current;
        if (context == null) return;

        if (!string.IsNullOrEmpty(context.OperationType))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("OperationType", context.OperationType));
        }

        if (!string.IsNullOrEmpty(context.ResourceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ResourceId", context.ResourceId));
        }

        if (context.Metadata.Count > 0)
        {
            foreach (var kvp in context.Metadata)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty($"Context_{kvp.Key}", kvp.Value));
            }
        }
    }
}
