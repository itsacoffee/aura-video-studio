using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for managing visual asset selection persistence and decisions
/// </summary>
public class VisualSelectionService
{
    private readonly ILogger<VisualSelectionService> _logger;
    private readonly ImageSelectionService _imageSelectionService;
    private readonly Dictionary<string, Dictionary<int, SceneVisualSelection>> _selections = new();
    private readonly object _lock = new();

    public VisualSelectionService(
        ILogger<VisualSelectionService> logger,
        ImageSelectionService imageSelectionService)
    {
        _logger = logger;
        _imageSelectionService = imageSelectionService;
    }

    /// <summary>
    /// Get selection for a specific scene
    /// </summary>
    public Task<SceneVisualSelection?> GetSelectionAsync(
        string jobId,
        int sceneIndex,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_selections.TryGetValue(jobId, out var jobSelections) &&
                jobSelections.TryGetValue(sceneIndex, out var selection))
            {
                return Task.FromResult<SceneVisualSelection?>(selection);
            }

            return Task.FromResult<SceneVisualSelection?>(null);
        }
    }

    /// <summary>
    /// Get all selections for a job
    /// </summary>
    public Task<IReadOnlyList<SceneVisualSelection>> GetSelectionsForJobAsync(
        string jobId,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_selections.TryGetValue(jobId, out var jobSelections))
            {
                return Task.FromResult<IReadOnlyList<SceneVisualSelection>>(
                    jobSelections.Values.OrderBy(s => s.SceneIndex).ToList());
            }

            return Task.FromResult<IReadOnlyList<SceneVisualSelection>>(Array.Empty<SceneVisualSelection>());
        }
    }

    /// <summary>
    /// Save or update a selection
    /// </summary>
    public Task<SceneVisualSelection> SaveSelectionAsync(
        SceneVisualSelection selection,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_selections.ContainsKey(selection.JobId))
            {
                _selections[selection.JobId] = new Dictionary<int, SceneVisualSelection>();
            }

            _selections[selection.JobId][selection.SceneIndex] = selection;

            _logger.LogInformation(
                "Saved selection for job {JobId}, scene {SceneIndex}, state {State}, score {Score:F1}",
                selection.JobId,
                selection.SceneIndex,
                selection.State,
                selection.SelectedCandidate?.OverallScore ?? 0);

            return Task.FromResult(selection);
        }
    }

    /// <summary>
    /// Accept a candidate for a scene
    /// </summary>
    public async Task<SceneVisualSelection> AcceptCandidateAsync(
        string jobId,
        int sceneIndex,
        ImageCandidate candidate,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existing = await GetSelectionAsync(jobId, sceneIndex, ct);

        var selection = new SceneVisualSelection
        {
            JobId = jobId,
            SceneIndex = sceneIndex,
            SelectedCandidate = candidate,
            Candidates = existing?.Candidates ?? Array.Empty<ImageCandidate>(),
            State = SelectionState.Accepted,
            SelectedAt = DateTime.UtcNow,
            SelectedBy = userId,
            Prompt = existing?.Prompt,
            Metadata = existing?.Metadata ?? new SelectionMetadata()
        };

        return await SaveSelectionAsync(selection, ct);
    }

    /// <summary>
    /// Reject current selection with reason
    /// </summary>
    public async Task<SceneVisualSelection> RejectSelectionAsync(
        string jobId,
        int sceneIndex,
        string rejectionReason,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existing = await GetSelectionAsync(jobId, sceneIndex, ct);

        if (existing == null)
        {
            throw new InvalidOperationException($"No selection found for job {jobId}, scene {sceneIndex}");
        }

        var selection = existing with
        {
            State = SelectionState.Rejected,
            RejectionReason = rejectionReason,
            SelectedAt = DateTime.UtcNow,
            SelectedBy = userId
        };

        _logger.LogInformation(
            "Rejected selection for job {JobId}, scene {SceneIndex}, reason: {Reason}",
            jobId, sceneIndex, rejectionReason);

        return await SaveSelectionAsync(selection, ct);
    }

    /// <summary>
    /// Replace current selection with a new candidate
    /// </summary>
    public async Task<SceneVisualSelection> ReplaceSelectionAsync(
        string jobId,
        int sceneIndex,
        ImageCandidate newCandidate,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existing = await GetSelectionAsync(jobId, sceneIndex, ct);

        if (existing == null)
        {
            throw new InvalidOperationException($"No selection found for job {jobId}, scene {sceneIndex}");
        }

        var updatedCandidates = existing.Candidates.Append(newCandidate).ToList();

        var selection = existing with
        {
            SelectedCandidate = newCandidate,
            Candidates = updatedCandidates,
            State = SelectionState.Replaced,
            SelectedAt = DateTime.UtcNow,
            SelectedBy = userId,
            Metadata = existing.Metadata with
            {
                RegenerationCount = existing.Metadata.RegenerationCount + 1
            }
        };

        _logger.LogInformation(
            "Replaced selection for job {JobId}, scene {SceneIndex}, new score: {Score:F1}",
            jobId, sceneIndex, newCandidate.OverallScore);

        return await SaveSelectionAsync(selection, ct);
    }

    /// <summary>
    /// Remove a selection (reset to pending)
    /// </summary>
    public async Task<SceneVisualSelection> RemoveSelectionAsync(
        string jobId,
        int sceneIndex,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existing = await GetSelectionAsync(jobId, sceneIndex, ct);

        if (existing == null)
        {
            throw new InvalidOperationException($"No selection found for job {jobId}, scene {sceneIndex}");
        }

        var selection = existing with
        {
            SelectedCandidate = null,
            State = SelectionState.Pending,
            RejectionReason = null,
            SelectedAt = DateTime.UtcNow,
            SelectedBy = userId
        };

        _logger.LogInformation(
            "Removed selection for job {JobId}, scene {SceneIndex}",
            jobId, sceneIndex);

        return await SaveSelectionAsync(selection, ct);
    }

    /// <summary>
    /// Regenerate candidates for a scene with optional refined prompt
    /// </summary>
    public async Task<SceneVisualSelection> RegenerateCandidatesAsync(
        string jobId,
        int sceneIndex,
        VisualPrompt? refinedPrompt = null,
        ImageSelectionConfig? config = null,
        string? userId = null,
        CancellationToken ct = default)
    {
        var existing = await GetSelectionAsync(jobId, sceneIndex, ct);

        if (existing == null)
        {
            throw new InvalidOperationException($"No selection found for job {jobId}, scene {sceneIndex}");
        }

        var promptToUse = refinedPrompt ?? existing.Prompt;
        if (promptToUse == null)
        {
            throw new InvalidOperationException("No prompt available for regeneration");
        }

        _logger.LogInformation(
            "Regenerating candidates for job {JobId}, scene {SceneIndex}",
            jobId, sceneIndex);

        var result = await _imageSelectionService.SelectImageForSceneAsync(promptToUse, config, ct);

        var selection = existing with
        {
            Candidates = result.Candidates,
            SelectedCandidate = result.SelectedImage,
            State = SelectionState.Pending,
            Prompt = promptToUse,
            SelectedAt = DateTime.UtcNow,
            SelectedBy = userId,
            Metadata = existing.Metadata with
            {
                RegenerationCount = existing.Metadata.RegenerationCount + 1,
                TotalGenerationTimeMs = existing.Metadata.TotalGenerationTimeMs + result.SelectionTimeMs,
                LlmAssistedRefinement = refinedPrompt != null,
                OriginalPrompt = refinedPrompt != null ? existing.Prompt?.DetailedDescription : null
            }
        };

        return await SaveSelectionAsync(selection, ct);
    }

    /// <summary>
    /// Determine if a candidate should be auto-selected
    /// </summary>
    public AutoSelectionDecision EvaluateAutoSelection(
        IReadOnlyList<ImageCandidate> candidates,
        double confidenceThreshold = 85.0)
    {
        if (candidates.Count == 0)
        {
            return new AutoSelectionDecision
            {
                ShouldAutoSelect = false,
                Confidence = 0,
                Reasoning = "No candidates available",
                ThresholdUsed = confidenceThreshold
            };
        }

        var topCandidate = candidates.OrderByDescending(c => c.OverallScore).First();

        var scoreGap = candidates.Count > 1
            ? topCandidate.OverallScore - candidates.OrderByDescending(c => c.OverallScore).Skip(1).First().OverallScore
            : 100.0;

        var confidence = Math.Min(
            topCandidate.OverallScore,
            topCandidate.OverallScore + (scoreGap * 0.1));

        var shouldAutoSelect = confidence >= confidenceThreshold &&
                              topCandidate.OverallScore >= 75.0 &&
                              scoreGap >= 15.0;

        var reasoning = shouldAutoSelect
            ? $"Top candidate has high score ({topCandidate.OverallScore:F1}) with significant lead ({scoreGap:F1} points)"
            : confidence < confidenceThreshold
                ? $"Confidence ({confidence:F1}) below threshold ({confidenceThreshold:F1})"
                : topCandidate.OverallScore < 75.0
                    ? $"Top score ({topCandidate.OverallScore:F1}) below quality threshold (75.0)"
                    : $"Insufficient score gap ({scoreGap:F1}) between top candidates";

        _logger.LogDebug(
            "Auto-selection evaluation: ShouldSelect={ShouldSelect}, Confidence={Confidence:F1}, TopScore={TopScore:F1}",
            shouldAutoSelect, confidence, topCandidate.OverallScore);

        return new AutoSelectionDecision
        {
            ShouldAutoSelect = shouldAutoSelect,
            SelectedCandidate = shouldAutoSelect ? topCandidate : null,
            Confidence = confidence,
            Reasoning = reasoning,
            ThresholdUsed = confidenceThreshold
        };
    }

    /// <summary>
    /// Clear all selections for a job
    /// </summary>
    public Task ClearJobSelectionsAsync(string jobId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_selections.Remove(jobId))
            {
                _logger.LogInformation("Cleared all selections for job {JobId}", jobId);
            }
        }

        return Task.CompletedTask;
    }
}
