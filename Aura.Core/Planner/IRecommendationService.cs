using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Planner;

/// <summary>
/// Service for generating planner recommendations
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generate recommendations based on brief, plan spec, and constraints
    /// </summary>
    Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default);
}
