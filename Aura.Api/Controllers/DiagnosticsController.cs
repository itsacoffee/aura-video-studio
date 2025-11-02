using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.HealthChecks;
using Aura.Core.Services.Orchestration;
using Aura.Core.Services.Validation;
using Aura.Core.Services.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Diagnostics and system health endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly SystemHealthService? _healthService;
    private readonly SystemIntegrityValidator? _integrityValidator;
    private readonly PromptTemplateValidator? _promptValidator;
    private readonly IEnumerable<ILlmProvider>? _llmProviders;
    private readonly IntelligentContentAdvisor? _contentAdvisor;
    private readonly IHardwareDetector? _hardwareDetector;
    private readonly PipelineHealthCheck? _pipelineHealthCheck;
    private readonly IResourceTracker? _resourceTracker;
    private readonly ErrorAggregationService? _errorAggregation;
    private readonly PerformanceTrackingService? _performanceTracking;
    private readonly DiagnosticReportGenerator? _reportGenerator;

    public DiagnosticsController(
        ILogger<DiagnosticsController> logger,
        SystemHealthService? healthService = null,
        SystemIntegrityValidator? integrityValidator = null,
        PromptTemplateValidator? promptValidator = null,
        IEnumerable<ILlmProvider>? llmProviders = null,
        IntelligentContentAdvisor? contentAdvisor = null,
        IHardwareDetector? hardwareDetector = null,
        PipelineHealthCheck? pipelineHealthCheck = null,
        IResourceTracker? resourceTracker = null,
        ErrorAggregationService? errorAggregation = null,
        PerformanceTrackingService? performanceTracking = null,
        DiagnosticReportGenerator? reportGenerator = null)
    {
        _logger = logger;
        _healthService = healthService;
        _integrityValidator = integrityValidator;
        _promptValidator = promptValidator;
        _llmProviders = llmProviders;
        _contentAdvisor = contentAdvisor;
        _hardwareDetector = hardwareDetector;
        _pipelineHealthCheck = pipelineHealthCheck;
        _resourceTracker = resourceTracker;
        _errorAggregation = errorAggregation;
        _performanceTracking = performanceTracking;
        _reportGenerator = reportGenerator;
    }

    /// <summary>
    /// Get overall system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<SystemHealthReport>> GetHealth(CancellationToken ct = default)
    {
        _logger.LogInformation("Health check requested");

        if (_healthService == null)
        {
            return Ok(new
            {
                Status = "Degraded",
                Message = "Health service not configured",
                Timestamp = DateTime.UtcNow
            });
        }

        try
        {
            var report = await _healthService.CheckHealthAsync(ct);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test specific LLM provider
    /// </summary>
    [HttpPost("test-provider")]
    public async Task<ActionResult<object>> TestProvider(
        [FromBody] ProviderTestRequest request,
        CancellationToken ct = default)
    {
        // Sanitize input for logging to prevent log forging
        var sanitizedProviderName = request.ProviderName?.Replace("\n", "").Replace("\r", "") ?? "null";
        _logger.LogInformation("Provider test requested: {Provider}", sanitizedProviderName);

        if (_llmProviders == null || !_llmProviders.Any())
        {
            return BadRequest(new { Error = "No LLM providers configured" });
        }

        var provider = _llmProviders.FirstOrDefault(p => 
            p.GetType().Name.Contains(request.ProviderName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            return NotFound(new { Error = $"Provider '{request.ProviderName}' not found" });
        }

        try
        {
            var testBrief = new Brief(
                Topic: "Diagnostic Test",
                Audience: null,
                Goal: null,
                Tone: "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var testSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "diagnostic test"
            );

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var startTime = DateTime.UtcNow;
            var response = await provider.DraftScriptAsync(testBrief, testSpec, cts.Token);
            var duration = DateTime.UtcNow - startTime;

            return Ok(new
            {
                Provider = provider.GetType().Name,
                Status = "Success",
                ResponseLength = response?.Length ?? 0,
                Duration = duration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new
            {
                Provider = provider.GetType().Name,
                Status = "Timeout",
                Error = "Provider test timed out after 30 seconds"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider test failed: {Provider}", sanitizedProviderName);
            return StatusCode(500, new
            {
                Provider = provider.GetType().Name,
                Status = "Failed",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Run quality analysis test
    /// </summary>
    [HttpPost("test-quality-analysis")]
    public async Task<ActionResult<ContentQualityAnalysis>> TestQualityAnalysis(
        [FromBody] QualityTestRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Quality analysis test requested");

        if (_contentAdvisor == null)
        {
            return BadRequest(new { Error = "Content advisor not configured" });
        }

        try
        {
            var testBrief = new Brief(
                Topic: request.Topic ?? "Test Topic",
                Audience: request.Audience,
                Goal: request.Goal,
                Tone: request.Tone ?? "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var testSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "test"
            );

            var analysis = await _contentAdvisor.AnalyzeContentQualityAsync(
                request.Script,
                testBrief,
                testSpec,
                ct
            );

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quality analysis test failed");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Verify system configuration
    /// </summary>
    [HttpGet("configuration")]
    public async Task<ActionResult<object>> GetConfiguration(CancellationToken ct = default)
    {
        _logger.LogInformation("Configuration check requested");

        var config = new
        {
            HealthServiceConfigured = _healthService != null,
            IntegrityValidatorConfigured = _integrityValidator != null,
            PromptValidatorConfigured = _promptValidator != null,
            ContentAdvisorConfigured = _contentAdvisor != null,
            LlmProvidersCount = _llmProviders?.Count() ?? 0,
            LlmProviders = _llmProviders?.Select(p => p.GetType().Name).ToList() ?? new List<string>(),
            Timestamp = DateTime.UtcNow
        };

        await Task.CompletedTask;
        return Ok(config);
    }

    /// <summary>
    /// Run integration tests
    /// </summary>
    [HttpPost("run-integration-tests")]
    public async Task<ActionResult<object>> RunIntegrationTests(CancellationToken ct = default)
    {
        _logger.LogInformation("Integration tests requested");

        var results = new List<object>();

        // Test 1: System Integrity
        if (_integrityValidator != null)
        {
            try
            {
                var integrityResult = await _integrityValidator.ValidateAsync(ct);
                results.Add(new
                {
                    Test = "System Integrity",
                    Status = integrityResult.IsValid ? "Passed" : "Failed",
                    Details = integrityResult
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    Test = "System Integrity",
                    Status = "Error",
                    Error = ex.Message
                });
            }
        }

        // Test 2: Prompt Templates
        if (_promptValidator != null)
        {
            try
            {
                var promptResult = await _promptValidator.ValidateAsync(ct);
                results.Add(new
                {
                    Test = "Prompt Templates",
                    Status = promptResult.IsValid ? "Passed" : "Failed",
                    Details = promptResult
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    Test = "Prompt Templates",
                    Status = "Error",
                    Error = ex.Message
                });
            }
        }

        // Test 3: Health Checks
        if (_healthService != null)
        {
            try
            {
                var healthResult = await _healthService.CheckHealthAsync(ct);
                results.Add(new
                {
                    Test = "Health Checks",
                    Status = healthResult.Status == HealthStatus.Healthy ? "Passed" : "Degraded",
                    Details = healthResult
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    Test = "Health Checks",
                    Status = "Error",
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => ((dynamic)r).Status == "Passed"),
            Results = results,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get hardware information for export acceleration detection
    /// </summary>
    [HttpGet("hardware")]
    public async Task<ActionResult<SystemProfile>> GetHardware()
    {
        _logger.LogInformation("Hardware detection requested");

        if (_hardwareDetector == null)
        {
            return Ok(new
            {
                LogicalCores = 4,
                PhysicalCores = 2,
                RamGB = 8,
                Gpu = (object?)null,
                Tier = "Budget",
                EnableNVENC = false,
                EnableSD = false,
                Message = "Hardware detector not configured"
            });
        }

        try
        {
            var profile = await _hardwareDetector.DetectSystemAsync();
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hardware detection");
            return StatusCode(500, new
            {
                Error = ex.Message,
                Message = "Hardware detection failed"
            });
        }
    }

    /// <summary>
    /// Get pipeline orchestration status and health
    /// </summary>
    [HttpGet("pipeline-status")]
    public async Task<ActionResult<object>> GetPipelineStatus(CancellationToken ct = default)
    {
        _logger.LogInformation("Pipeline status requested");

        if (_pipelineHealthCheck == null)
        {
            return Ok(new
            {
                Status = "NotConfigured",
                Message = "Pipeline health check service not configured",
                Timestamp = DateTime.UtcNow
            });
        }

        try
        {
            var healthResult = await _pipelineHealthCheck.CheckHealthAsync(ct);

            return Ok(new
            {
                Status = healthResult.IsHealthy ? "Healthy" : "Degraded",
                IsHealthy = healthResult.IsHealthy,
                ServiceAvailability = healthResult.ServiceAvailability,
                AvailableServices = healthResult.ServiceAvailability.Count(kvp => kvp.Value),
                TotalServices = healthResult.ServiceAvailability.Count,
                MissingRequiredServices = healthResult.MissingRequiredServices,
                Warnings = healthResult.Warnings,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pipeline status");
            return StatusCode(500, new
            {
                Status = "Error",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get resource usage metrics (memory, file handles, processes)
    /// </summary>
    [HttpGet("resources")]
    public async Task<ActionResult<ResourceMetrics>> GetResources(CancellationToken ct = default)
    {
        _logger.LogInformation("Resource metrics requested");

        if (_resourceTracker == null)
        {
            return Ok(new
            {
                OpenFileHandles = -1,
                ActiveProcesses = -1,
                AllocatedMemoryBytes = -1,
                WorkingSetBytes = -1,
                ThreadCount = -1,
                Timestamp = DateTime.UtcNow,
                Warnings = new[] { "Resource tracker not configured" }
            });
        }

        try
        {
            var metrics = await _resourceTracker.GetMetricsAsync(ct);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource metrics");
            return StatusCode(500, new
            {
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Force resource cleanup (garbage collection, etc.)
    /// </summary>
    [HttpPost("resources/cleanup")]
    public async Task<ActionResult<object>> CleanupResources(CancellationToken ct = default)
    {
        _logger.LogInformation("Resource cleanup requested");

        if (_resourceTracker == null)
        {
            return BadRequest(new { Error = "Resource tracker not configured" });
        }

        try
        {
            await _resourceTracker.CleanupAsync(ct);
            var metricsAfter = await _resourceTracker.GetMetricsAsync(ct);
            
            return Ok(new
            {
                Status = "Cleanup completed",
                MetricsAfter = metricsAfter,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource cleanup");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get aggregated error information
    /// </summary>
    [HttpGet("errors")]
    public ActionResult<object> GetErrors([FromQuery] string? since = null)
    {
        _logger.LogInformation("Error aggregation requested with since: {Since}", since ?? "all time");

        if (_errorAggregation == null)
        {
            return Ok(new
            {
                Message = "Error aggregation service not configured",
                Errors = new List<object>()
            });
        }

        try
        {
            TimeSpan? timeSpan = null;
            if (!string.IsNullOrEmpty(since))
            {
                // Parse time span (e.g., "24h", "7d", "1h")
                timeSpan = ParseTimeSpan(since);
            }

            var errors = _errorAggregation.GetAggregatedErrors(timeSpan, limit: 10);
            var statistics = _errorAggregation.GetStatistics(timeSpan);

            return Ok(new
            {
                Statistics = statistics,
                TopErrors = errors,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregated errors");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    [HttpGet("performance")]
    public ActionResult<object> GetPerformanceMetrics()
    {
        _logger.LogInformation("Performance metrics requested");

        if (_performanceTracking == null)
        {
            return Ok(new
            {
                Message = "Performance tracking service not configured",
                Metrics = new List<object>()
            });
        }

        try
        {
            var metrics = _performanceTracking.GetMetrics();
            var slowOps = _performanceTracking.GetSlowOperations(limit: 20);

            return Ok(new
            {
                Metrics = metrics,
                SlowOperations = slowOps,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Generate comprehensive diagnostic report as ZIP file
    /// </summary>
    [HttpPost("report")]
    public async Task<ActionResult<object>> GenerateDiagnosticReport(CancellationToken ct = default)
    {
        _logger.LogInformation("Diagnostic report generation requested");

        if (_reportGenerator == null)
        {
            return BadRequest(new { Error = "Diagnostic report generator not configured" });
        }

        try
        {
            var result = await _reportGenerator.GenerateReportAsync(ct);

            return Ok(new
            {
                ReportId = result.ReportId,
                FileName = result.FileName,
                GeneratedAt = result.GeneratedAt,
                ExpiresAt = result.ExpiresAt,
                SizeBytes = result.SizeBytes,
                DownloadUrl = $"/api/diagnostics/report/{result.ReportId}/download"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diagnostic report");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Download diagnostic report by ID
    /// </summary>
    [HttpGet("report/{reportId}/download")]
    public ActionResult DownloadDiagnosticReport(string reportId)
    {
        _logger.LogInformation("Diagnostic report download requested: {ReportId}", reportId);

        if (_reportGenerator == null)
        {
            return BadRequest(new { Error = "Diagnostic report generator not configured" });
        }

        try
        {
            var reportPath = _reportGenerator.GetReportPath(reportId);

            if (reportPath == null || !System.IO.File.Exists(reportPath))
            {
                return NotFound(new { Error = "Report not found or has expired" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(reportPath);
            var fileName = Path.GetFileName(reportPath);

            return File(fileBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading diagnostic report");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Parse time span from string (e.g., "24h", "7d", "1h")
    /// </summary>
    private TimeSpan ParseTimeSpan(string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(value, @"^(\d+)([hdm])$");
        if (!match.Success)
        {
            return TimeSpan.FromHours(24); // Default to 24 hours
        }

        var number = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value;

        return unit switch
        {
            "h" => TimeSpan.FromHours(number),
            "d" => TimeSpan.FromDays(number),
            "m" => TimeSpan.FromMinutes(number),
            _ => TimeSpan.FromHours(24)
        };
    }
}

/// <summary>
/// Request model for provider testing
/// </summary>
public class ProviderTestRequest
{
    public string ProviderName { get; set; } = string.Empty;
}

/// <summary>
/// Request model for quality analysis testing
/// </summary>
public class QualityTestRequest
{
    public string Script { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public string? Audience { get; set; }
    public string? Goal { get; set; }
    public string? Tone { get; set; }
}
