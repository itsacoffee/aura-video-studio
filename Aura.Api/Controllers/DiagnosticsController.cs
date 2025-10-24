using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.HealthChecks;
using Aura.Core.Services.Validation;
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

    public DiagnosticsController(
        ILogger<DiagnosticsController> logger,
        SystemHealthService? healthService = null,
        SystemIntegrityValidator? integrityValidator = null,
        PromptTemplateValidator? promptValidator = null,
        IEnumerable<ILlmProvider>? llmProviders = null,
        IntelligentContentAdvisor? contentAdvisor = null)
    {
        _logger = logger;
        _healthService = healthService;
        _integrityValidator = integrityValidator;
        _promptValidator = promptValidator;
        _llmProviders = llmProviders;
        _contentAdvisor = contentAdvisor;
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
        _logger.LogInformation("Provider test requested: {Provider}", request.ProviderName);

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
            _logger.LogError(ex, "Provider test failed: {Provider}", request.ProviderName);
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
