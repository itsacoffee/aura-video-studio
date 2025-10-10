using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Planner;

/// <summary>
/// Interface for LLM-based planner providers
/// Generates comprehensive recommendations including outline, B-roll, SEO, etc.
/// </summary>
public interface ILlmPlannerProvider
{
    /// <summary>
    /// Generate planner recommendations using LLM
    /// </summary>
    Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default);
}
