using System.Collections.Generic;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Result of a budget check operation
/// </summary>
public record BudgetCheckResult
{
    /// <summary>
    /// Whether the estimated cost is within budget
    /// </summary>
    public required bool IsWithinBudget { get; init; }

    /// <summary>
    /// Whether generation should be blocked (hard limit exceeded)
    /// </summary>
    public required bool ShouldBlock { get; init; }

    /// <summary>
    /// Warning messages about budget status
    /// </summary>
    public required List<string> Warnings { get; init; }

    /// <summary>
    /// Current total monthly cost
    /// </summary>
    public required decimal CurrentMonthlyCost { get; init; }

    /// <summary>
    /// Estimated new total if this operation proceeds
    /// </summary>
    public required decimal EstimatedNewTotal { get; init; }
}
