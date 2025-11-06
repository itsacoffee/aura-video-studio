using System;
using System.Linq;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Telemetry;
using Aura.Core.Telemetry.Costing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for cost analytics and telemetry-based cost breakdown
/// Integrates with RunTelemetry v1 for accurate post-run cost reporting
/// </summary>
[ApiController]
[Route("api/costing")]
public class CostingController : ControllerBase
{
    private readonly ILogger<CostingController> _logger;
    private readonly CostEstimatorService? _estimatorService;
    private readonly TelemetryCostAnalyzer? _analyzer;
    private readonly BudgetChecker? _budgetChecker;
    
    public CostingController(
        ILogger<CostingController> logger,
        CostEstimatorService? estimatorService = null,
        TelemetryCostAnalyzer? analyzer = null,
        BudgetChecker? budgetChecker = null)
    {
        _logger = logger;
        _estimatorService = estimatorService;
        _analyzer = analyzer;
        _budgetChecker = budgetChecker;
    }
    
    /// <summary>
    /// Get current pricing versions for all providers
    /// </summary>
    [HttpGet("pricing/current")]
    public IActionResult GetCurrentPricing()
    {
        if (_estimatorService == null)
        {
            return Problem("Cost estimator service not available", statusCode: 503);
        }
        
        try
        {
            var pricingTable = _estimatorService.GetPricingTable();
            var currentVersions = pricingTable.GetAllCurrentVersions();
            
            var dtos = currentVersions.Select(v => new PricingVersionDto(
                Version: v.Version,
                ValidFrom: v.ValidFrom,
                ValidUntil: v.ValidUntil,
                ProviderName: v.ProviderName,
                Currency: v.Currency,
                CostPer1KInputTokens: v.CostPer1KInputTokens,
                CostPer1KOutputTokens: v.CostPer1KOutputTokens,
                CostPer1KCachedInputTokens: v.CostPer1KCachedInputTokens,
                CostPer1KCharacters: v.CostPer1KCharacters,
                CostPerImage: v.CostPerImage,
                CostPerComputeSecond: v.CostPerComputeSecond,
                IsFree: v.IsFree,
                Notes: v.Notes
            )).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current pricing");
            return Problem($"Error getting current pricing: {ex.Message}", statusCode: 500);
        }
    }
    
    /// <summary>
    /// Get pricing history for a specific provider
    /// </summary>
    [HttpGet("pricing/{providerName}/history")]
    public IActionResult GetProviderPricingHistory(string providerName)
    {
        if (_estimatorService == null)
        {
            return Problem("Cost estimator service not available", statusCode: 503);
        }
        
        try
        {
            var pricingTable = _estimatorService.GetPricingTable();
            var versions = pricingTable.GetAllVersions(providerName);
            
            if (!versions.Any())
            {
                return NotFound(new { error = $"No pricing history found for provider {providerName}" });
            }
            
            var dtos = versions.Select(v => new PricingVersionDto(
                Version: v.Version,
                ValidFrom: v.ValidFrom,
                ValidUntil: v.ValidUntil,
                ProviderName: v.ProviderName,
                Currency: v.Currency,
                CostPer1KInputTokens: v.CostPer1KInputTokens,
                CostPer1KOutputTokens: v.CostPer1KOutputTokens,
                CostPer1KCachedInputTokens: v.CostPer1KCachedInputTokens,
                CostPer1KCharacters: v.CostPer1KCharacters,
                CostPerImage: v.CostPerImage,
                CostPerComputeSecond: v.CostPerComputeSecond,
                IsFree: v.IsFree,
                Notes: v.Notes
            )).ToList();
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing history for {Provider}", providerName);
            return Problem($"Error getting pricing history: {ex.Message}", statusCode: 500);
        }
    }
    
    /// <summary>
    /// Get comprehensive cost breakdown from telemetry for a completed run
    /// This is the primary source of truth for post-run cost reporting
    /// </summary>
    [HttpGet("breakdown/{jobId}")]
    public IActionResult GetCostBreakdown(string jobId)
    {
        if (_analyzer == null)
        {
            return Problem("Cost analyzer service not available", statusCode: 503);
        }
        
        try
        {
            // This would typically load telemetry from storage
            // For now, return error indicating telemetry is needed
            return Problem(
                "Telemetry-based cost breakdown requires RunTelemetry data. " +
                "Please ensure telemetry collection is enabled for the job.",
                statusCode: 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cost breakdown for job {JobId}", jobId);
            return Problem($"Error generating cost breakdown: {ex.Message}", statusCode: 500);
        }
    }
    
    /// <summary>
    /// Estimate cost for a planned operation before execution
    /// </summary>
    [HttpPost("estimate")]
    public IActionResult EstimateCost([FromBody] CostEstimateRequestDto request)
    {
        if (_estimatorService == null)
        {
            return Problem("Cost estimator service not available", statusCode: 503);
        }
        
        try
        {
            // Create a telemetry record from the request for estimation
            var record = new RunTelemetryRecord
            {
                JobId = "estimate",
                CorrelationId = HttpContext.TraceIdentifier,
                Stage = Enum.Parse<RunStage>(request.Stage, ignoreCase: true),
                Provider = request.ProviderName,
                TokensIn = request.EstimatedInputTokens,
                TokensOut = request.EstimatedOutputTokens,
                CacheHit = request.CacheHit,
                Retries = request.ExpectedRetries,
                LatencyMs = 0,
                ResultStatus = ResultStatus.Ok,
                StartedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow
            };
            
            var estimate = _estimatorService.EstimateCost(record);
            
            var dto = new CostEstimateResponseDto(
                Amount: estimate.Amount,
                Currency: estimate.Currency,
                FormattedAmount: CurrencyFormatter.FormatForDisplay(estimate.Amount, estimate.Currency),
                PricingVersion: estimate.PricingVersion,
                Confidence: estimate.Confidence.ToString(),
                Notes: estimate.Notes
            );
            
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating cost");
            return Problem($"Error estimating cost: {ex.Message}", statusCode: 500);
        }
    }
    
    /// <summary>
    /// Format a currency amount for display
    /// </summary>
    [HttpGet("format")]
    public IActionResult FormatCurrency(
        [FromQuery] decimal amount,
        [FromQuery] string currency = "USD",
        [FromQuery] bool useCode = false)
    {
        try
        {
            var formatted = useCode
                ? CurrencyFormatter.FormatWithCode(amount, currency)
                : CurrencyFormatter.FormatForDisplay(amount, currency);
            
            return Ok(new { formatted, amount, currency });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting currency");
            return Problem($"Error formatting currency: {ex.Message}", statusCode: 500);
        }
    }
}

/// <summary>
/// DTO for pricing version information
/// </summary>
public record PricingVersionDto(
    string Version,
    DateTime ValidFrom,
    DateTime? ValidUntil,
    string ProviderName,
    string Currency,
    decimal? CostPer1KInputTokens,
    decimal? CostPer1KOutputTokens,
    decimal? CostPer1KCachedInputTokens,
    decimal? CostPer1KCharacters,
    decimal? CostPerImage,
    decimal? CostPerComputeSecond,
    bool IsFree,
    string? Notes
);

/// <summary>
/// Request DTO for cost estimation
/// </summary>
public record CostEstimateRequestDto(
    string Stage,
    string ProviderName,
    int? EstimatedInputTokens,
    int? EstimatedOutputTokens,
    bool CacheHit,
    int ExpectedRetries
);

/// <summary>
/// Response DTO for cost estimation
/// </summary>
public record CostEstimateResponseDto(
    decimal Amount,
    string Currency,
    string FormattedAmount,
    string? PricingVersion,
    string Confidence,
    string? Notes
);
