namespace Aura.Core.Logging;

/// <summary>
/// Provides distributed trace context for correlation across services and operations
/// </summary>
public class TraceContext
{
    private static readonly AsyncLocal<TraceContext?> _current = new();

    /// <summary>
    /// Gets or sets the current trace context
    /// </summary>
    public static TraceContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the trace ID that identifies the entire distributed trace
    /// </summary>
    public string TraceId { get; private set; }

    /// <summary>
    /// Gets the span ID that identifies this specific operation within the trace
    /// </summary>
    public string SpanId { get; private set; }

    /// <summary>
    /// Gets the parent span ID if this is a child span
    /// </summary>
    public string? ParentSpanId { get; private set; }

    /// <summary>
    /// Gets the user ID associated with this trace
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets the operation name for this trace
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Gets additional metadata for this trace
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Initializes a new trace context with a new trace ID
    /// </summary>
    public TraceContext()
    {
        TraceId = GenerateId();
        SpanId = GenerateId();
    }

    /// <summary>
    /// Initializes a trace context with an existing trace ID (for propagation)
    /// </summary>
    public TraceContext(string traceId, string? parentSpanId = null)
    {
        TraceId = traceId;
        SpanId = GenerateId();
        ParentSpanId = parentSpanId;
    }

    /// <summary>
    /// Creates a child span within the current trace
    /// </summary>
    public TraceContext CreateChildSpan(string? operationName = null)
    {
        var childContext = new TraceContext(TraceId, SpanId)
        {
            UserId = UserId,
            OperationName = operationName ?? OperationName
        };

        foreach (var kvp in Metadata)
        {
            childContext.Metadata[kvp.Key] = kvp.Value;
        }

        return childContext;
    }

    /// <summary>
    /// Starts a new trace context scope
    /// </summary>
    public static TraceContextScope BeginScope(TraceContext context)
    {
        return new TraceContextScope(context);
    }

    /// <summary>
    /// Starts a new trace context scope with a new trace ID
    /// </summary>
    public static TraceContextScope BeginNewScope(string? operationName = null)
    {
        var context = new TraceContext
        {
            OperationName = operationName
        };
        return new TraceContextScope(context);
    }

    private static string GenerateId()
    {
        return Guid.NewGuid().ToString("N")[..16]; // 16 character hex string
    }
}

/// <summary>
/// Represents a trace context scope that automatically manages the current context
/// </summary>
public sealed class TraceContextScope : IDisposable
{
    private readonly TraceContext? _previousContext;
    private bool _disposed;

    internal TraceContextScope(TraceContext context)
    {
        _previousContext = TraceContext.Current;
        TraceContext.Current = context;
    }

    public void Dispose()
    {
        if (_disposed) return;

        TraceContext.Current = _previousContext;
        _disposed = true;
    }
}
