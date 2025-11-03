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

    public CostTrackingController(EnhancedCostTrackingService? costTrackingService = null)
    {
        _costTrackingService = costTrackingService;
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
}
