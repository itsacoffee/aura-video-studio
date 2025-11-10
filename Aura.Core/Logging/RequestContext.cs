namespace Aura.Core.Logging;

/// <summary>
/// Provides request-specific context information for logging
/// </summary>
public class RequestContext
{
    private static readonly AsyncLocal<RequestContext?> _current = new();

    /// <summary>
    /// Gets or sets the current request context
    /// </summary>
    public static RequestContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the unique request identifier
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets the user agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets the HTTP method
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Gets the request path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets the timestamp when the request started
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides operation-specific context information for logging
/// </summary>
public class OperationContext
{
    private static readonly AsyncLocal<OperationContext?> _current = new();

    /// <summary>
    /// Gets or sets the current operation context
    /// </summary>
    public static OperationContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Gets the type of operation being performed
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the resource ID associated with the operation
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets additional metadata for the operation
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Creates a new operation context scope
    /// </summary>
    public static IDisposable BeginScope(string operationType, string? resourceId = null)
    {
        var context = new OperationContext
        {
            OperationType = operationType,
            ResourceId = resourceId
        };
        return new OperationContextScope(context);
    }

    private sealed class OperationContextScope : IDisposable
    {
        private readonly OperationContext? _previousContext;
        private bool _disposed;

        public OperationContextScope(OperationContext context)
        {
            _previousContext = Current;
            Current = context;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Current = _previousContext;
            _disposed = true;
        }
    }
}
