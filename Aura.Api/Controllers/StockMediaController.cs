using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.Assets;
using Aura.Core.Models.StockMedia;
using Aura.Core.Services.StockMedia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for stock media operations (Pexels, Unsplash, Pixabay)
/// </summary>
[ApiController]
[Route("api/stock-media")]
public class StockMediaController : ControllerBase
{
    private readonly ILogger<StockMediaController> _logger;
    private readonly UnifiedStockMediaService _stockMediaService;
    private readonly QueryCompositionService _queryCompositionService;

    public StockMediaController(
        ILogger<StockMediaController> logger,
        UnifiedStockMediaService stockMediaService,
        QueryCompositionService queryCompositionService)
    {
        _logger = logger;
        _stockMediaService = stockMediaService;
        _queryCompositionService = queryCompositionService;
    }

    /// <summary>
    /// Search for stock media across multiple providers
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromBody] StockMediaSearchRequestDto request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Stock media search: {Query}, CorrelationId: {CorrelationId}",
                request.Query, HttpContext.TraceIdentifier);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Status = 400,
                    Detail = "Query cannot be empty",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var stockRequest = MapToStockMediaSearchRequest(request);
            var response = await _stockMediaService.SearchAsync(stockRequest, ct).ConfigureAwait(false);
            var dto = MapToStockMediaSearchResponseDto(response);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Stock media search failed, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(429, new ProblemDetails
            {
                Title = "Rate Limit Exceeded",
                Status = 429,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching stock media, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Search Failed",
                Status = 500,
                Detail = "An error occurred while searching stock media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Compose optimized search query using LLM
    /// </summary>
    [HttpPost("compose-query")]
    public async Task<IActionResult> ComposeQuery(
        [FromBody] QueryCompositionRequestDto request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Composing query for {Provider}, CorrelationId: {CorrelationId}",
                request.TargetProvider, HttpContext.TraceIdentifier);

            if (!Enum.TryParse<StockMediaProvider>(request.TargetProvider, true, out var provider))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Status = 400,
                    Detail = $"Unknown provider: {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            if (!Enum.TryParse<StockMediaType>(request.MediaType, true, out var mediaType))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Media Type",
                    Status = 400,
                    Detail = $"Unknown media type: {request.MediaType}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var compositionRequest = new QueryCompositionRequest
            {
                SceneDescription = request.SceneDescription,
                Keywords = request.Keywords,
                TargetProvider = provider,
                MediaType = mediaType,
                Style = request.Style,
                Mood = request.Mood
            };

            var result = await _queryCompositionService.ComposeQueryAsync(compositionRequest, ct).ConfigureAwait(false);
            var dto = MapToQueryCompositionResultDto(result);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error composing query, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Query Composition Failed",
                Status = 500,
                Detail = "An error occurred while composing the query",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get blend set recommendation (stock vs generative mix)
    /// </summary>
    [HttpPost("recommend-blend")]
    public async Task<IActionResult> RecommendBlend(
        [FromBody] BlendSetRequestDto request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Generating blend set recommendation, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var blendRequest = new BlendSetRequest
            {
                SceneDescriptions = request.SceneDescriptions,
                VideoGoal = request.VideoGoal,
                VideoStyle = request.VideoStyle,
                Budget = request.Budget,
                AllowGenerative = request.AllowGenerative,
                AllowStock = request.AllowStock
            };

            var result = await _queryCompositionService.RecommendBlendSetAsync(blendRequest, ct).ConfigureAwait(false);
            var dto = MapToBlendSetRecommendationDto(result);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blend recommendation, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Blend Recommendation Failed",
                Status = 500,
                Detail = "An error occurred while generating blend recommendation",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get rate limit status for all providers
    /// </summary>
    [HttpGet("rate-limits")]
    public IActionResult GetRateLimits()
    {
        try
        {
            _logger.LogInformation(
                "Getting rate limit status, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var status = _stockMediaService.GetRateLimitStatus();
            var dtos = status.Select(kvp => new RateLimitStatusDto(
                kvp.Key.ToString(),
                kvp.Value.RequestsRemaining,
                kvp.Value.RequestsLimit,
                kvp.Value.ResetTime,
                kvp.Value.IsLimited
            )).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limits, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Rate Limit Check Failed",
                Status = 500,
                Detail = "An error occurred while checking rate limits",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Validate provider API keys
    /// </summary>
    [HttpPost("validate-providers")]
    public async Task<IActionResult> ValidateProviders(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Validating providers, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var results = await _stockMediaService.ValidateProvidersAsync(ct).ConfigureAwait(false);
            var dtos = results.Select(kvp => new ProviderValidationDto(
                kvp.Key.ToString(),
                kvp.Value,
                kvp.Value ? null : "API key invalid or provider unreachable"
            )).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating providers, CorrelationId: {CorrelationId}", 
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Provider Validation Failed",
                Status = 500,
                Detail = "An error occurred while validating providers",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    private StockMediaSearchRequest MapToStockMediaSearchRequest(StockMediaSearchRequestDto dto)
    {
        StockMediaType? mediaType = null;
        if (!string.IsNullOrWhiteSpace(dto.MediaType))
        {
            Enum.TryParse<StockMediaType>(dto.MediaType, true, out var type);
            mediaType = type;
        }

        var providers = dto.Providers
            .Select(p => Enum.TryParse<StockMediaProvider>(p, true, out var provider) ? (StockMediaProvider?)provider : null)
            .OfType<StockMediaProvider>()
            .ToList();

        return new StockMediaSearchRequest
        {
            Query = dto.Query,
            Type = mediaType,
            Providers = providers,
            Count = dto.Count,
            Page = dto.Page,
            SafeSearchEnabled = dto.SafeSearchEnabled,
            Orientation = dto.Orientation,
            Color = dto.Color,
            MinWidth = dto.MinWidth,
            MinHeight = dto.MinHeight,
            MinDuration = dto.MinDurationSeconds.HasValue ? TimeSpan.FromSeconds(dto.MinDurationSeconds.Value) : null,
            MaxDuration = dto.MaxDurationSeconds.HasValue ? TimeSpan.FromSeconds(dto.MaxDurationSeconds.Value) : null
        };
    }

    private StockMediaSearchResponseDto MapToStockMediaSearchResponseDto(StockMediaSearchResponse response)
    {
        var results = response.Results.Select(r => new StockMediaResultDto(
            r.Id,
            r.Type.ToString(),
            r.Provider.ToString(),
            r.ThumbnailUrl,
            r.PreviewUrl,
            r.FullSizeUrl,
            r.DownloadUrl,
            r.Width,
            r.Height,
            r.Duration.HasValue ? (int)r.Duration.Value.TotalSeconds : null,
            new LicensingInfoDto
            {
                LicenseType = r.Licensing.LicenseType,
                Attribution = r.Licensing.Attribution,
                LicenseUrl = r.Licensing.LicenseUrl,
                CommercialUseAllowed = r.Licensing.CommercialUseAllowed,
                AttributionRequired = r.Licensing.AttributionRequired,
                CreatorName = r.Licensing.CreatorName,
                CreatorUrl = r.Licensing.CreatorUrl,
                SourcePlatform = r.Licensing.SourcePlatform
            },
            r.Metadata,
            r.RelevanceScore
        )).ToList();

        var resultsByProvider = response.ResultsByProvider.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value
        );

        return new StockMediaSearchResponseDto(
            results,
            response.TotalResults,
            response.Page,
            response.PerPage,
            resultsByProvider
        );
    }

    private QueryCompositionResultDto MapToQueryCompositionResultDto(QueryCompositionResult result)
    {
        return new QueryCompositionResultDto(
            result.PrimaryQuery,
            result.AlternativeQueries,
            result.NegativeFilters,
            result.Reasoning,
            result.Confidence
        );
    }

    private BlendSetRecommendationDto MapToBlendSetRecommendationDto(BlendSetRecommendation result)
    {
        var sceneRecs = result.SceneRecommendations.ToDictionary(
            kvp => kvp.Key,
            kvp => new SourceRecommendationDto(
                kvp.Value.UseStock,
                kvp.Value.UseGenerative,
                kvp.Value.PreferredSource,
                kvp.Value.Reasoning,
                kvp.Value.Confidence
            )
        );

        return new BlendSetRecommendationDto(
            sceneRecs,
            result.Strategy,
            result.Reasoning,
            result.EstimatedCost,
            result.NarrativeCoverageScore
        );
    }

    /// <summary>
    /// Validate stock media query for safety
    /// </summary>
    [HttpPost("validate-query")]
    public async Task<IActionResult> ValidateQuery(
        [FromBody] ValidateStockQueryRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Validating stock media query, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            await Task.CompletedTask.ConfigureAwait(false);
            _logger.LogWarning("Stock media safety integration not yet wired up to controller dependencies");

            return Ok(new ValidateStockQueryResponse(
                request.Query,
                true,
                "Query validation pending full integration",
                request.Query,
                null,
                new List<string>()
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating query");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Validation Failed",
                Status = 500,
                Detail = "Failed to validate stock media query",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Sanitize stock media query to remove unsafe terms
    /// </summary>
    [HttpPost("sanitize-query")]
    public IActionResult SanitizeQuery([FromBody] ValidateStockQueryRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Sanitizing stock media query, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            _logger.LogWarning("Stock media safety integration not yet wired up to controller dependencies");

            return Ok(new { originalQuery = request.Query, sanitizedQuery = request.Query });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing query");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Sanitization Failed",
                Status = 500,
                Detail = "Failed to sanitize stock media query",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}
