using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// DTO for router provider health status.
/// </summary>
public record RouterProviderHealthDto(
    string ProviderName,
    string State,
    DateTime LastCheckTime,
    int ConsecutiveFailures,
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    double SuccessRate,
    double AverageLatencyMs,
    double HealthScore,
    DateTime? CircuitOpenedAt,
    int? CircuitResetInSeconds);

/// <summary>
/// DTO for router provider performance metrics.
/// </summary>
public record RouterProviderMetricsDto(
    string ProviderName,
    string ModelName,
    double AverageLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    decimal AverageCost,
    double QualityScore,
    int RequestCount,
    DateTime LastUpdated);

/// <summary>
/// DTO for routing decision.
/// </summary>
public record RoutingDecisionDto(
    string ProviderName,
    string ModelName,
    string Reasoning,
    DateTime DecisionTime,
    RoutingMetadataDto Metadata);

/// <summary>
/// DTO for routing metadata.
/// </summary>
public record RoutingMetadataDto(
    int Rank,
    double HealthScore,
    double LatencyScore,
    double CostScore,
    double QualityScore,
    double OverallScore,
    string[] AlternativeProviders);

/// <summary>
/// Request to select a provider for a task.
/// </summary>
public record SelectProviderRequest(
    string TaskType,
    int? RequiredContextLength = null,
    int? MaxLatencyMs = null,
    decimal? MaxCostPerRequest = null,
    bool? RequireDeterminism = null,
    double? MinQualityScore = null);

/// <summary>
/// Request to record a provider request outcome.
/// </summary>
public record RecordRequestRequest(
    string ProviderName,
    string TaskType,
    bool Success,
    double LatencyMs,
    decimal Cost);

/// <summary>
/// Request to mark a provider as unavailable.
/// </summary>
public record MarkProviderUnavailableRequest(
    string ProviderName,
    int? DurationSeconds = null);
