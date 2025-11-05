using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for redacting sensitive data with allowlist-based approach (deny by default)
/// </summary>
public class RedactionService
{
    private static readonly HashSet<string> AllowedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Job identifiers (allowed)
        "jobId", "job_id", "correlationId", "correlation_id", "bundleId", "bundle_id",
        "projectId", "project_id", "requestId", "request_id", "traceId", "trace_id",
        
        // Timestamps and durations (allowed)
        "timestamp", "startedAt", "started_at", "endedAt", "ended_at", "completedAt", "completed_at",
        "createdAt", "created_at", "updatedAt", "updated_at", "duration", "latency", "latencyMs", "latency_ms",
        
        // Technical metadata (allowed)
        "stage", "status", "resultStatus", "result_status", "errorCode", "error_code",
        "provider", "model", "modelId", "model_id", "version", "schemaVersion", "schema_version",
        
        // Performance metrics (allowed)
        "tokensIn", "tokens_in", "tokensOut", "tokens_out", "cost", "costEstimate", "cost_estimate",
        "currency", "retries", "cacheHit", "cache_hit",
        
        // System info (allowed - non-PII)
        "osVersion", "processorCount", "dotnetVersion", "ramGB", "gpuVendor", "gpuModel", "tier",
        "logicalCores", "physicalCores",
        
        // Error details (allowed - technical only)
        "errorMessage", "error_message", "errorType", "error_type", "stackTrace", "stack_trace",
        "message", "description", "summary", "title", "type", "confidence",
        
        // FFmpeg and rendering (allowed - technical)
        "command", "exitCode", "exit_code", "codec", "resolution", "frameRate", "frame_rate",
        "format", "sizeBytes", "size_bytes",
        
        // Collections and metadata
        "records", "timeline", "stages", "steps", "actions", "links", "files", "metadata"
    };

    private static readonly List<Regex> SensitivePatterns = new()
    {
        // OpenAI keys
        new Regex(@"sk-[a-zA-Z0-9_-]{20,}", RegexOptions.Compiled),
        new Regex(@"sk-proj-[a-zA-Z0-9_-]{20,}", RegexOptions.Compiled),
        
        // Anthropic keys
        new Regex(@"sk-ant-api[0-9]{2}-[a-zA-Z0-9_-]{20,}", RegexOptions.Compiled),
        
        // Google API keys
        new Regex(@"AIza[a-zA-Z0-9_-]{35}", RegexOptions.Compiled),
        
        // AWS keys
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled),
        
        // GitHub tokens
        new Regex(@"gh[ps]_[a-zA-Z0-9]{36,}", RegexOptions.Compiled),
        
        // Replicate tokens
        new Regex(@"r8_[a-zA-Z0-9]{40,}", RegexOptions.Compiled),
        
        // Bearer tokens
        new Regex(@"Bearer\s+[a-zA-Z0-9\-_\.]{20,}", RegexOptions.Compiled),
        
        // JWT tokens
        new Regex(@"eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled),
        
        // Generic secrets (more than 20 chars of base64-like)
        new Regex(@"\b[A-Za-z0-9+/]{40,}={0,2}\b", RegexOptions.Compiled),
    };

    private static readonly List<Regex> SensitiveFieldPatterns = new()
    {
        // Field name patterns that should always be redacted
        new Regex(@"""(api[_-]?key|apikey)""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"""(password|passwd|pwd)""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"""(token|auth[_-]?token)""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"""(secret|client[_-]?secret)""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"""(authorization)""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    /// <summary>
    /// Redact sensitive data from text content using pattern matching
    /// </summary>
    public static string RedactText(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var redacted = content;

        // Apply sensitive pattern redactions
        foreach (var pattern in SensitivePatterns)
        {
            redacted = pattern.Replace(redacted, "[REDACTED]");
        }

        // Apply sensitive field redactions
        foreach (var pattern in SensitiveFieldPatterns)
        {
            redacted = pattern.Replace(redacted, m =>
            {
                var parts = m.Value.Split(':');
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}: \"[REDACTED]\"";
                }
                return "[REDACTED]";
            });
        }

        return redacted;
    }

    /// <summary>
    /// Redact JSON object using allowlist approach
    /// Fields not in allowlist are redacted unless they contain technical metadata
    /// </summary>
    public static JsonElement RedactJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    if (IsFieldAllowed(property.Name))
                    {
                        obj[property.Name] = ConvertJsonElement(RedactJsonElement(property.Value));
                    }
                    else
                    {
                        obj[property.Name] = "[REDACTED]";
                    }
                }
                return JsonSerializer.SerializeToElement(obj);

            case JsonValueKind.Array:
                var array = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(ConvertJsonElement(RedactJsonElement(item)));
                }
                return JsonSerializer.SerializeToElement(array);

            case JsonValueKind.String:
                var str = element.GetString() ?? string.Empty;
                return JsonSerializer.SerializeToElement(RedactText(str));

            default:
                return element;
        }
    }

    /// <summary>
    /// Check if a field name is allowed to be included
    /// </summary>
    private static bool IsFieldAllowed(string fieldName)
    {
        return AllowedFieldNames.Contains(fieldName);
    }

    /// <summary>
    /// Convert JsonElement to object for serialization
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = ConvertJsonElement(property.Value);
                }
                return obj;

            case JsonValueKind.Array:
                var array = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(ConvertJsonElement(item));
                }
                return array;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Redact log lines based on content and context
    /// </summary>
    public static IEnumerable<string> RedactLogLines(IEnumerable<string> lines, DateTime? failureTime = null, TimeSpan? windowSize = null)
    {
        var redactedLines = new List<string>();
        
        // If we have a failure time and window, filter logs within the window
        if (failureTime.HasValue && windowSize.HasValue)
        {
            var windowStart = failureTime.Value - windowSize.Value;
            var windowEnd = failureTime.Value + windowSize.Value;
            
            foreach (var line in lines)
            {
                var timestamp = ExtractTimestamp(line);
                if (timestamp.HasValue && timestamp.Value >= windowStart && timestamp.Value <= windowEnd)
                {
                    redactedLines.Add(RedactText(line));
                }
            }
        }
        else
        {
            // Redact all lines
            foreach (var line in lines)
            {
                redactedLines.Add(RedactText(line));
            }
        }

        return redactedLines;
    }

    /// <summary>
    /// Extract timestamp from log line (supports common formats)
    /// </summary>
    private static DateTime? ExtractTimestamp(string line)
    {
        // Try ISO 8601 format: 2024-01-15T10:30:45Z or 2024-01-15 10:30:45
        var isoMatch = Regex.Match(line, @"(\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)");
        if (isoMatch.Success && DateTime.TryParse(isoMatch.Groups[1].Value, out var dt))
        {
            return dt;
        }

        return null;
    }

    /// <summary>
    /// Get list of allowed field names for documentation
    /// </summary>
    public static IReadOnlySet<string> GetAllowedFields()
    {
        return AllowedFieldNames;
    }
}
