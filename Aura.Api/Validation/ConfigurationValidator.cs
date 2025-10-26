using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Aura.Api.Validation;

/// <summary>
/// Validates application configuration at startup to prevent runtime failures
/// </summary>
public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly List<ConfigurationIssue> _issues = new();

    public ConfigurationValidator(IConfiguration configuration, ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates all configuration settings
    /// </summary>
    public ConfigurationValidationResult Validate()
    {
        _issues.Clear();
        
        _logger.LogInformation("Starting configuration validation...");

        // Validate database configuration
        ValidateDatabaseConfiguration();

        // Validate file paths
        ValidateFilePaths();

        // Validate port configuration
        ValidatePortConfiguration();

        // Validate API keys format (not presence, just format if provided)
        ValidateApiKeysFormat();

        // Validate URLs
        ValidateUrls();

        // Validate numeric ranges
        ValidateNumericRanges();

        var criticalIssues = _issues.Where(i => i.Severity == IssueSeverity.Critical).ToList();
        var warningIssues = _issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

        if (criticalIssues.Any())
        {
            _logger.LogError("Configuration validation failed with {Count} critical issues", criticalIssues.Count);
            foreach (var issue in criticalIssues)
            {
                _logger.LogError("  - {Key}: {Message}", issue.Key, issue.Message);
            }
        }

        if (warningIssues.Any())
        {
            _logger.LogWarning("Configuration validation found {Count} warnings", warningIssues.Count);
            foreach (var issue in warningIssues)
            {
                _logger.LogWarning("  - {Key}: {Message}", issue.Key, issue.Message);
            }
        }

        if (!_issues.Any())
        {
            _logger.LogInformation("Configuration validation passed with no issues");
        }

        return new ConfigurationValidationResult
        {
            IsValid = !criticalIssues.Any(),
            Issues = _issues
        };
    }

    private void ValidateDatabaseConfiguration()
    {
        // Database path is constructed at runtime, so we just check if the directory is writable
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        if (!Directory.Exists(baseDirectory))
        {
            AddIssue("Database:BaseDirectory", $"Base directory does not exist: {baseDirectory}", IssueSeverity.Critical);
        }
        else if (!IsDirectoryWritable(baseDirectory))
        {
            AddIssue("Database:BaseDirectory", $"Base directory is not writable: {baseDirectory}", IssueSeverity.Critical);
        }
    }

    private void ValidateFilePaths()
    {
        // Validate output directory
        var outputDir = _configuration["OutputDirectory"];
        if (!string.IsNullOrEmpty(outputDir))
        {
            ValidateDirectoryPath("OutputDirectory", outputDir, createIfMissing: true);
        }

        // Validate logs directory
        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        ValidateDirectoryPath("Logs:Directory", logsDir, createIfMissing: true);

        // Validate wwwroot directory
        var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            AddIssue("StaticFiles:wwwroot", $"wwwroot directory not found: {wwwrootPath}", IssueSeverity.Warning);
        }
    }

    private void ValidateDirectoryPath(string key, string path, bool createIfMissing = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            AddIssue(key, "Path is empty or null", IssueSeverity.Warning);
            return;
        }

        try
        {
            if (!Directory.Exists(path))
            {
                if (createIfMissing)
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Created directory: {Path}", path);
                }
                else
                {
                    AddIssue(key, $"Directory does not exist: {path}", IssueSeverity.Warning);
                    return;
                }
            }

            if (!IsDirectoryWritable(path))
            {
                AddIssue(key, $"Directory is not writable: {path}", IssueSeverity.Critical);
            }
        }
        catch (Exception ex)
        {
            AddIssue(key, $"Error validating directory path: {ex.Message}", IssueSeverity.Critical);
        }
    }

    private void ValidatePortConfiguration()
    {
        var apiUrl = Environment.GetEnvironmentVariable("AURA_API_URL") 
                     ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
                     ?? "http://127.0.0.1:5005";

        if (Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
        {
            var port = uri.Port;
            
            if (port < 1 || port > 65535)
            {
                AddIssue("ASPNETCORE_URLS:Port", $"Port {port} is out of valid range (1-65535)", IssueSeverity.Critical);
            }
            else if (!IsPortAvailable(port))
            {
                AddIssue("ASPNETCORE_URLS:Port", $"Port {port} is already in use", IssueSeverity.Critical);
            }
        }
        else
        {
            AddIssue("ASPNETCORE_URLS", $"Invalid URL format: {apiUrl}", IssueSeverity.Critical);
        }
    }

    private void ValidateApiKeysFormat()
    {
        // We don't require API keys, but if provided, they should have valid formats
        var openAiKey = _configuration["ApiKeys:OpenAI"];
        if (!string.IsNullOrEmpty(openAiKey) && !openAiKey.StartsWith("sk-"))
        {
            AddIssue("ApiKeys:OpenAI", "OpenAI API key should start with 'sk-'", IssueSeverity.Warning);
        }

        var azureKey = _configuration["Azure:Speech:Key"];
        if (!string.IsNullOrEmpty(azureKey) && azureKey.Length < 32)
        {
            AddIssue("Azure:Speech:Key", "Azure Speech key seems too short", IssueSeverity.Warning);
        }
    }

    private void ValidateUrls()
    {
        var stableDiffusionUrl = _configuration["Providers:StableDiffusion:Url"];
        if (!string.IsNullOrEmpty(stableDiffusionUrl))
        {
            if (!Uri.TryCreate(stableDiffusionUrl, UriKind.Absolute, out var uri))
            {
                AddIssue("Providers:StableDiffusion:Url", $"Invalid URL format: {stableDiffusionUrl}", IssueSeverity.Warning);
            }
        }

        var ollamaUrl = _configuration["Providers:Ollama:Url"];
        if (!string.IsNullOrEmpty(ollamaUrl))
        {
            if (!Uri.TryCreate(ollamaUrl, UriKind.Absolute, out var uri))
            {
                AddIssue("Providers:Ollama:Url", $"Invalid URL format: {ollamaUrl}", IssueSeverity.Warning);
            }
        }
    }

    private void ValidateNumericRanges()
    {
        // Validate any numeric configuration values
        var maxUploadSize = _configuration.GetValue<long?>("MaxUploadSizeMB");
        if (maxUploadSize.HasValue && (maxUploadSize.Value < 1 || maxUploadSize.Value > 10240))
        {
            AddIssue("MaxUploadSizeMB", $"Max upload size {maxUploadSize.Value}MB is out of reasonable range (1-10240MB)", IssueSeverity.Warning);
        }
    }

    private bool IsDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private void AddIssue(string key, string message, IssueSeverity severity)
    {
        _issues.Add(new ConfigurationIssue
        {
            Key = key,
            Message = message,
            Severity = severity
        });
    }
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public IReadOnlyList<ConfigurationIssue> Issues { get; set; } = new List<ConfigurationIssue>();
}

public class ConfigurationIssue
{
    public string Key { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
}

public enum IssueSeverity
{
    Warning,
    Critical
}
