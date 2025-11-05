using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.CostTracking;
using Aura.Core.Services.CostTracking;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CostTrackingController : ControllerBase
{
    private readonly EnhancedCostTrackingService? _costTrackingService;
    private readonly TokenTrackingService? _tokenTrackingService;
    private readonly RunCostReportService? _reportService;

    public CostTrackingController(
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null,
        RunCostReportService? reportService = null)
    {
        _costTrackingService = costTrackingService;
        _tokenTrackingService = tokenTrackingService;
        _reportService = reportService;
    }

    /// <summary>
    /// Get current cost tracking configuration
    /// </summary>
    [HttpGet("configuration")]
    public IActionResult GetConfiguration()
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var config = _costTrackingService.GetConfiguration();
            
            var dto = new CostTrackingConfigurationDto(
                Id: config.Id,
                UserId: config.UserId,
                OverallMonthlyBudget: config.OverallMonthlyBudget,
                BudgetPeriodStart: config.BudgetPeriodStart,
                BudgetPeriodEnd: config.BudgetPeriodEnd,
                PeriodType: config.PeriodType.ToString(),
                Currency: config.Currency,
                AlertThresholds: config.AlertThresholds,
                EmailNotificationsEnabled: config.EmailNotificationsEnabled,
                NotificationEmail: config.NotificationEmail,
                AlertFrequency: config.AlertFrequency.ToString(),
                ProviderBudgets: config.ProviderBudgets,
                HardBudgetLimit: config.HardBudgetLimit,
                EnableProjectTracking: config.EnableProjectTracking);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting cost tracking configuration");
            return Problem($"Error getting cost tracking configuration: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Update cost tracking configuration
    /// </summary>
    [HttpPut("configuration")]
    public IActionResult UpdateConfiguration([FromBody] CostTrackingConfigurationDto dto)
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            if (!Enum.TryParse<BudgetPeriodType>(dto.PeriodType, out var periodType))
            {
                return BadRequest(new { error = $"Invalid period type: {dto.PeriodType}" });
            }

            if (!Enum.TryParse<AlertFrequency>(dto.AlertFrequency, out var alertFrequency))
            {
                return BadRequest(new { error = $"Invalid alert frequency: {dto.AlertFrequency}" });
            }

            var config = new CostTrackingConfiguration
            {
                Id = dto.Id ?? Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                OverallMonthlyBudget = dto.OverallMonthlyBudget,
                BudgetPeriodStart = dto.BudgetPeriodStart,
                BudgetPeriodEnd = dto.BudgetPeriodEnd,
                PeriodType = periodType,
                Currency = dto.Currency,
                AlertThresholds = dto.AlertThresholds,
                EmailNotificationsEnabled = dto.EmailNotificationsEnabled,
                NotificationEmail = dto.NotificationEmail,
                AlertFrequency = alertFrequency,
                ProviderBudgets = dto.ProviderBudgets,
                HardBudgetLimit = dto.HardBudgetLimit,
                EnableProjectTracking = dto.EnableProjectTracking
            };

            _costTrackingService.UpdateConfiguration(config);

            return Ok(new { message = "Configuration updated successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating cost tracking configuration");
            return Problem($"Error updating cost tracking configuration: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get spending report for a date range
    /// </summary>
    [HttpGet("spending")]
    public IActionResult GetSpending(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? provider = null)
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var config = _costTrackingService.GetConfiguration();
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var totalCost = _costTrackingService.GetSpending(start, end, provider);
            var costByProvider = _costTrackingService.GetSpendingByProvider(start, end);
            var costByFeature = _costTrackingService.GetSpendingByFeature(start, end);
            var costByProject = _costTrackingService.GetSpendingByProject(start, end);

            var costByFeatureStrings = costByFeature.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value);

            var dto = new SpendingReportDto(
                StartDate: start,
                EndDate: end,
                TotalCost: totalCost,
                Currency: config.Currency,
                CostByProvider: costByProvider,
                CostByFeature: costByFeatureStrings,
                CostByProject: costByProject,
                RecentTransactions: new List<CostLogDto>(),
                Trend: null);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting spending report");
            return Problem($"Error getting spending report: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get current period spending summary
    /// </summary>
    [HttpGet("current-period")]
    public IActionResult GetCurrentPeriodSpending()
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var config = _costTrackingService.GetConfiguration();
            var totalCost = _costTrackingService.GetCurrentPeriodSpending();
            
            var result = new
            {
                totalCost,
                currency = config.Currency,
                periodType = config.PeriodType.ToString(),
                budget = config.OverallMonthlyBudget,
                percentageUsed = config.OverallMonthlyBudget.HasValue 
                    ? (totalCost / config.OverallMonthlyBudget.Value * 100)
                    : 0
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current period spending");
            return Problem($"Error getting current period spending: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if an operation would exceed budget
    /// </summary>
    [HttpPost("check-budget")]
    public IActionResult CheckBudget([FromBody] CostEstimateRequest request)
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            decimal estimatedCost = 0;

            if (request.EstimatedInputTokens.HasValue && request.EstimatedOutputTokens.HasValue)
            {
                estimatedCost = _costTrackingService.EstimateLlmCost(
                    request.ProviderName,
                    request.EstimatedInputTokens.Value,
                    request.EstimatedOutputTokens.Value);
            }
            else if (request.EstimatedCharacters.HasValue)
            {
                estimatedCost = _costTrackingService.EstimateTtsCost(
                    request.ProviderName,
                    request.EstimatedCharacters.Value);
            }

            var result = _costTrackingService.CheckBudget(request.ProviderName, estimatedCost);

            var dto = new BudgetCheckDto(
                IsWithinBudget: result.IsWithinBudget,
                ShouldBlock: result.ShouldBlock,
                Warnings: result.Warnings,
                CurrentMonthlyCost: result.CurrentMonthlyCost,
                EstimatedNewTotal: result.EstimatedNewTotal);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking budget");
            return Problem($"Error checking budget: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get all provider pricing
    /// </summary>
    [HttpGet("pricing")]
    public IActionResult GetProviderPricing()
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var pricingList = _costTrackingService.GetAllProviderPricing();
            
            var dtos = pricingList.Select(p => new ProviderPricingDto(
                ProviderName: p.ProviderName,
                ProviderType: p.ProviderType.ToString(),
                IsFree: p.IsFree,
                CostPer1KTokens: p.CostPer1KTokens,
                CostPer1KInputTokens: p.CostPer1KInputTokens,
                CostPer1KOutputTokens: p.CostPer1KOutputTokens,
                CostPerCharacter: p.CostPerCharacter,
                CostPer1KCharacters: p.CostPer1KCharacters,
                CostPerImage: p.CostPerImage,
                CostPerComputeSecond: p.CostPerComputeSecond,
                IsManualOverride: p.IsManualOverride,
                LastUpdated: p.LastUpdated,
                Currency: p.Currency,
                Notes: p.Notes)).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting provider pricing");
            return Problem($"Error getting provider pricing: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Update provider pricing
    /// </summary>
    [HttpPut("pricing/{providerName}")]
    public IActionResult UpdateProviderPricing(string providerName, [FromBody] ProviderPricingDto dto)
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            if (!Enum.TryParse<ProviderType>(dto.ProviderType, out var providerType))
            {
                return BadRequest(new { error = $"Invalid provider type: {dto.ProviderType}" });
            }

            var pricing = new ProviderPricing
            {
                ProviderName = providerName,
                ProviderType = providerType,
                IsFree = dto.IsFree,
                CostPer1KTokens = dto.CostPer1KTokens,
                CostPer1KInputTokens = dto.CostPer1KInputTokens,
                CostPer1KOutputTokens = dto.CostPer1KOutputTokens,
                CostPerCharacter = dto.CostPerCharacter,
                CostPer1KCharacters = dto.CostPer1KCharacters,
                CostPerImage = dto.CostPerImage,
                CostPerComputeSecond = dto.CostPerComputeSecond,
                IsManualOverride = dto.IsManualOverride,
                Currency = dto.Currency,
                Notes = dto.Notes
            };

            _costTrackingService.UpdateProviderPricing(pricing);

            return Ok(new { message = "Provider pricing updated successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating provider pricing");
            return Problem($"Error updating provider pricing: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Reset budget for current period
    /// </summary>
    [HttpPost("reset-budget")]
    public IActionResult ResetBudget()
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            _costTrackingService.ResetPeriodBudget();
            return Ok(new { message = "Budget reset successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting budget");
            return Problem($"Error resetting budget: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get token usage statistics for a job
    /// </summary>
    [HttpGet("token-stats/{jobId}")]
    public IActionResult GetTokenStatistics(string jobId)
    {
        if (_tokenTrackingService == null)
        {
            return Problem("Token tracking service not available", statusCode: 503);
        }

        try
        {
            var stats = _tokenTrackingService.GetJobStatistics(jobId);
            
            var dto = new TokenUsageStatisticsDto(
                TotalInputTokens: stats.TotalInputTokens,
                TotalOutputTokens: stats.TotalOutputTokens,
                TotalTokens: stats.TotalTokens,
                OperationCount: stats.OperationCount,
                CacheHits: stats.CacheHits,
                CacheHitRate: stats.CacheHitRate,
                AverageTokensPerOperation: stats.AverageTokensPerOperation,
                AverageResponseTimeMs: stats.AverageResponseTimeMs,
                TotalCost: stats.TotalCost,
                CostSavedByCache: stats.CostSavedByCache);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting token statistics for job {JobId}", jobId);
            return Problem($"Error getting token statistics: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get comprehensive cost report for a run
    /// </summary>
    [HttpGet("run-summary/{jobId}")]
    public IActionResult GetRunSummary(string jobId)
    {
        if (_reportService == null)
        {
            return Problem("Report service not available", statusCode: 503);
        }

        try
        {
            var report = _reportService.GetReport(jobId);
            
            if (report == null)
            {
                return NotFound(new { error = $"No cost report found for job {jobId}" });
            }

            var dto = MapReportToDto(report);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting run summary for job {JobId}", jobId);
            return Problem($"Error getting run summary: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Export cost report in JSON or CSV format
    /// </summary>
    [HttpPost("export/{jobId}")]
    public IActionResult ExportReport(string jobId, [FromQuery] string format = "json")
    {
        if (_reportService == null)
        {
            return Problem("Report service not available", statusCode: 503);
        }

        try
        {
            var report = _reportService.GetReport(jobId);
            
            if (report == null)
            {
                return NotFound(new { error = $"No cost report found for job {jobId}" });
            }

            string filePath;
            string contentType;
            
            if (format.ToLowerInvariant() == "csv")
            {
                filePath = _reportService.ExportToCsv(report);
                contentType = "text/csv";
            }
            else
            {
                filePath = _reportService.ExportToJson(report);
                contentType = "application/json";
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error exporting report for job {JobId}", jobId);
            return Problem($"Error exporting report: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get cost optimization suggestions for a job
    /// </summary>
    [HttpGet("optimize-suggestions/{jobId}")]
    public IActionResult GetOptimizationSuggestions(string jobId)
    {
        if (_tokenTrackingService == null)
        {
            return Problem("Token tracking service not available", statusCode: 503);
        }

        try
        {
            var suggestions = _tokenTrackingService.GenerateOptimizationSuggestions(jobId);
            
            var dtos = suggestions.Select(s => new CostOptimizationSuggestionDto(
                Category: s.Category.ToString(),
                Suggestion: s.Suggestion,
                EstimatedSavings: s.EstimatedSavings,
                QualityImpact: s.QualityImpact)).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting optimization suggestions for job {JobId}", jobId);
            return Problem($"Error getting optimization suggestions: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Optimize generation settings for a budget
    /// </summary>
    [HttpPost("optimize-budget")]
    public IActionResult OptimizeForBudget([FromBody] OptimizeForBudgetRequest request)
    {
        if (_costTrackingService == null)
        {
            return Problem("Cost tracking service not available", statusCode: 503);
        }

        try
        {
            var estimatedCostBefore = 5.00m;
            var recommendedSettings = new Dictionary<string, object>
            {
                ["llmProvider"] = "Gemini",
                ["ttsProvider"] = "Piper",
                ["enableCaching"] = true,
                ["maxTokensPerOperation"] = 2000,
                ["imageQuality"] = "standard"
            };
            
            var changes = new List<string>
            {
                "Switch LLM provider from OpenAI to Gemini (60% cost reduction)",
                "Switch TTS provider to Piper (free, offline)",
                "Enable LLM caching for repeated operations",
                "Reduce max tokens per operation from 4000 to 2000",
                "Use standard image quality instead of high"
            };
            
            var estimatedCostAfter = 1.50m;
            
            var response = new BudgetOptimizationResponse(
                EstimatedCostBefore: estimatedCostBefore,
                EstimatedCostAfter: estimatedCostAfter,
                EstimatedSavings: estimatedCostBefore - estimatedCostAfter,
                RecommendedSettings: recommendedSettings,
                Changes: changes,
                QualityImpact: "Slight reduction in output creativity and voice quality, but maintains overall video quality",
                WithinBudget: estimatedCostAfter <= request.BudgetLimit);

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error optimizing for budget");
            return Problem($"Error optimizing for budget: {ex.Message}", statusCode: 500);
        }
    }

    private static RunCostReportDto MapReportToDto(RunCostReport report)
    {
        var tokenStatsDto = report.TokenStats != null
            ? new TokenUsageStatisticsDto(
                TotalInputTokens: report.TokenStats.TotalInputTokens,
                TotalOutputTokens: report.TokenStats.TotalOutputTokens,
                TotalTokens: report.TokenStats.TotalTokens,
                OperationCount: report.TokenStats.OperationCount,
                CacheHits: report.TokenStats.CacheHits,
                CacheHitRate: report.TokenStats.CacheHitRate,
                AverageTokensPerOperation: report.TokenStats.AverageTokensPerOperation,
                AverageResponseTimeMs: report.TokenStats.AverageResponseTimeMs,
                TotalCost: report.TokenStats.TotalCost,
                CostSavedByCache: report.TokenStats.CostSavedByCache)
            : null;

        var stageBreakdownDtos = report.CostByStage.ToDictionary(
            kvp => kvp.Key,
            kvp => new StageCostBreakdownDto(
                StageName: kvp.Value.StageName,
                Cost: kvp.Value.Cost,
                PercentageOfTotal: kvp.Value.PercentageOfTotal,
                DurationSeconds: kvp.Value.DurationSeconds,
                OperationCount: kvp.Value.OperationCount,
                ProviderName: kvp.Value.ProviderName));

        var operationDtos = report.Operations.Select(o => new OperationCostDetailDto(
            Timestamp: o.Timestamp,
            OperationType: o.OperationType,
            ProviderName: o.ProviderName,
            Cost: o.Cost,
            DurationMs: o.DurationMs,
            TokensUsed: o.TokensUsed,
            CharactersProcessed: o.CharactersProcessed,
            CacheHit: o.CacheHit)).ToList();

        var suggestionDtos = report.OptimizationSuggestions.Select(s => new CostOptimizationSuggestionDto(
            Category: s.Category.ToString(),
            Suggestion: s.Suggestion,
            EstimatedSavings: s.EstimatedSavings,
            QualityImpact: s.QualityImpact)).ToList();

        return new RunCostReportDto(
            JobId: report.JobId,
            ProjectId: report.ProjectId,
            ProjectName: report.ProjectName,
            StartedAt: report.StartedAt,
            CompletedAt: report.CompletedAt,
            DurationSeconds: report.DurationSeconds,
            TotalCost: report.TotalCost,
            Currency: report.Currency,
            CostByStage: stageBreakdownDtos,
            CostByProvider: report.CostByProvider,
            TokenStats: tokenStatsDto,
            Operations: operationDtos,
            OptimizationSuggestions: suggestionDtos,
            WithinBudget: report.WithinBudget,
            BudgetLimit: report.BudgetLimit);
    }
}
