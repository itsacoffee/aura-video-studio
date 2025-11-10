using Aura.Core.Errors;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Aura.Core.Services.ErrorHandling;

/// <summary>
/// Centralized error logging service with file-based persistence and categorization
/// </summary>
public class ErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;
    private readonly string _errorLogPath;
    private readonly int _maxLogSizeMb;
    private readonly ConcurrentQueue<ErrorLogEntry> _errorQueue;
    private readonly SemaphoreSlim _writeLock;

    public ErrorLoggingService(
        ILogger<ErrorLoggingService> logger,
        string? logPath = null,
        int maxLogSizeMb = 100)
    {
        _logger = logger;
        _maxLogSizeMb = maxLogSizeMb;
        _errorQueue = new ConcurrentQueue<ErrorLogEntry>();
        _writeLock = new SemaphoreSlim(1, 1);

        // Set default log path
        var baseDir = logPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Logs");
        
        Directory.CreateDirectory(baseDir);
        _errorLogPath = Path.Combine(baseDir, $"errors-{DateTime.UtcNow:yyyy-MM}.jsonl");
    }

    /// <summary>
    /// Log an error with full context
    /// </summary>
    public async Task LogErrorAsync(
        Exception exception,
        ErrorCategory category,
        string? correlationId = null,
        Dictionary<string, object>? context = null,
        string? userId = null,
        bool writeImmediately = false)
    {
        var entry = new ErrorLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = category,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException?.ToString(),
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            Context = context ?? new Dictionary<string, object>(),
            UserId = userId,
            Severity = DetermineSeverity(exception, category)
        };

        // Add AuraException-specific details
        if (exception is AuraException auraEx)
        {
            entry.ErrorCode = auraEx.ErrorCode;
            entry.UserMessage = auraEx.UserMessage;
            entry.SuggestedActions = auraEx.SuggestedActions;
            entry.IsTransient = auraEx.IsTransient;
            
            // Merge AuraException context
            foreach (var kvp in auraEx.Context)
            {
                entry.Context[kvp.Key] = kvp.Value;
            }
        }

        // Log to standard logger
        _logger.Log(
            entry.Severity,
            exception,
            "Error logged: {Category} - {ErrorCode} - {Message} [CorrelationId: {CorrelationId}]",
            entry.Category,
            entry.ErrorCode ?? "N/A",
            entry.Message,
            entry.CorrelationId);

        // Queue for file writing
        _errorQueue.Enqueue(entry);

        if (writeImmediately)
        {
            await FlushErrorsAsync();
        }
    }

    /// <summary>
    /// Flush queued errors to file
    /// </summary>
    public async Task FlushErrorsAsync()
    {
        if (_errorQueue.IsEmpty)
            return;

        await _writeLock.WaitAsync();
        try
        {
            var entries = new List<ErrorLogEntry>();
            while (_errorQueue.TryDequeue(out var entry))
            {
                entries.Add(entry);
            }

            if (entries.Count == 0)
                return;

            // Check log size and rotate if needed
            await RotateLogIfNeeded();

            // Write entries as JSONL (JSON Lines)
            var lines = entries.Select(e => JsonSerializer.Serialize(e, new JsonSerializerOptions
            {
                WriteIndented = false
            }));

            await File.AppendAllLinesAsync(_errorLogPath, lines);

            _logger.LogDebug("Flushed {Count} error entries to log file", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush errors to log file");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Get recent errors from log file
    /// </summary>
    public async Task<List<ErrorLogEntry>> GetRecentErrorsAsync(int count = 100, ErrorCategory? category = null)
    {
        if (!File.Exists(_errorLogPath))
            return new List<ErrorLogEntry>();

        try
        {
            var lines = await File.ReadAllLinesAsync(_errorLogPath);
            var errors = new List<ErrorLogEntry>();

            // Read from end of file backwards
            for (int i = lines.Length - 1; i >= 0 && errors.Count < count; i--)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<ErrorLogEntry>(lines[i]);
                    if (entry != null && (!category.HasValue || entry.Category == category.Value))
                    {
                        errors.Add(entry);
                    }
                }
                catch
                {
                    // Skip malformed lines
                    continue;
                }
            }

            errors.Reverse(); // Return in chronological order
            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read error log file");
            return new List<ErrorLogEntry>();
        }
    }

    /// <summary>
    /// Search errors by correlation ID
    /// </summary>
    public async Task<List<ErrorLogEntry>> SearchByCorrelationIdAsync(string correlationId)
    {
        if (!File.Exists(_errorLogPath))
            return new List<ErrorLogEntry>();

        try
        {
            var lines = await File.ReadAllLinesAsync(_errorLogPath);
            var errors = new List<ErrorLogEntry>();

            foreach (var line in lines)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<ErrorLogEntry>(line);
                    if (entry?.CorrelationId == correlationId)
                    {
                        errors.Add(entry);
                    }
                }
                catch
                {
                    continue;
                }
            }

            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search error log file");
            return new List<ErrorLogEntry>();
        }
    }

    /// <summary>
    /// Export errors to a diagnostic file
    /// </summary>
    public async Task<string> ExportDiagnosticsAsync(TimeSpan? timeWindow = null)
    {
        var cutoffTime = timeWindow.HasValue 
            ? DateTime.UtcNow - timeWindow.Value 
            : DateTime.MinValue;

        var diagnosticsPath = Path.Combine(
            Path.GetDirectoryName(_errorLogPath)!,
            $"diagnostics-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json");

        try
        {
            var allErrors = await GetAllErrorsAsync(cutoffTime);
            
            var diagnostics = new
            {
                ExportedAt = DateTime.UtcNow,
                TimeWindow = timeWindow?.ToString() ?? "All time",
                ErrorCount = allErrors.Count,
                Errors = allErrors,
                SystemInfo = new
                {
                    OS = Environment.OSVersion.ToString(),
                    Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    DotNetVersion = Environment.Version.ToString()
                }
            };

            var json = JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(diagnosticsPath, json);
            
            _logger.LogInformation("Exported diagnostics to {Path}", diagnosticsPath);
            return diagnosticsPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export diagnostics");
            throw;
        }
    }

    /// <summary>
    /// Clear old log files beyond retention period
    /// </summary>
    public async Task<int> CleanupOldLogsAsync(TimeSpan retentionPeriod)
    {
        var logDir = Path.GetDirectoryName(_errorLogPath)!;
        var files = Directory.GetFiles(logDir, "errors-*.jsonl");
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        int deletedCount = 0;

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old log file: {File}", file);
            }
        }

        _logger.LogInformation("Cleaned up {Count} old log files", deletedCount);
        return deletedCount;
    }

    private async Task RotateLogIfNeeded()
    {
        if (!File.Exists(_errorLogPath))
            return;

        var fileInfo = new FileInfo(_errorLogPath);
        var sizeMb = fileInfo.Length / (1024 * 1024);

        if (sizeMb >= _maxLogSizeMb)
        {
            var archivePath = _errorLogPath.Replace(".jsonl", $"-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jsonl");
            File.Move(_errorLogPath, archivePath);
            _logger.LogInformation("Rotated error log to {Path}", archivePath);
        }
    }

    private async Task<List<ErrorLogEntry>> GetAllErrorsAsync(DateTime cutoffTime)
    {
        var logDir = Path.GetDirectoryName(_errorLogPath)!;
        var files = Directory.GetFiles(logDir, "errors-*.jsonl")
            .OrderBy(f => f)
            .ToList();

        var allErrors = new List<ErrorLogEntry>();

        foreach (var file in files)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines)
                {
                    try
                    {
                        var entry = JsonSerializer.Deserialize<ErrorLogEntry>(line);
                        if (entry != null && entry.Timestamp >= cutoffTime)
                        {
                            allErrors.Add(entry);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read log file: {File}", file);
            }
        }

        return allErrors;
    }

    private LogLevel DetermineSeverity(Exception exception, ErrorCategory category)
    {
        if (exception is AuraException auraEx && auraEx.IsTransient)
            return LogLevel.Warning;

        return category switch
        {
            ErrorCategory.User => LogLevel.Information,
            ErrorCategory.System => LogLevel.Error,
            ErrorCategory.Provider => exception is ProviderException provEx && provEx.IsTransient 
                ? LogLevel.Warning 
                : LogLevel.Error,
            ErrorCategory.Network => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }
}

/// <summary>
/// Error log entry structure
/// </summary>
public class ErrorLogEntry
{
    public DateTime Timestamp { get; set; }
    public ErrorCategory Category { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? UserMessage { get; set; }
    public string[]? SuggestedActions { get; set; }
    public bool IsTransient { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public string? UserId { get; set; }
    public LogLevel Severity { get; set; }
}

/// <summary>
/// Error categorization for better handling and reporting
/// </summary>
public enum ErrorCategory
{
    /// <summary>User input or configuration errors</summary>
    User,
    
    /// <summary>System-level errors (disk, memory, etc.)</summary>
    System,
    
    /// <summary>Provider/API errors (LLM, TTS, etc.)</summary>
    Provider,
    
    /// <summary>Network connectivity errors</summary>
    Network,
    
    /// <summary>Application logic errors</summary>
    Application
}
