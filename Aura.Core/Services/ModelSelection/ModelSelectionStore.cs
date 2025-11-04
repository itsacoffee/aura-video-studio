using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ModelSelection;

/// <summary>
/// Persistent storage for model selections with audit trail.
/// Stores selections in encrypted settings file with per-setting metadata.
/// </summary>
public class ModelSelectionStore
{
    private readonly ILogger<ModelSelectionStore> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly string _storePath;
    private readonly object _lock = new();
    private ModelSelectionData _data;

    public ModelSelectionStore(
        ILogger<ModelSelectionStore> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
        
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _storePath = Path.Combine(auraDataDir, "model-selections.json");
        
        _data = LoadData();
    }

    /// <summary>
    /// Save a model selection
    /// </summary>
    public async Task SaveSelectionAsync(ModelSelection selection, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Saving model selection: {Provider}/{Stage} -> {ModelId} (scope: {Scope}, pinned: {Pinned})",
            selection.Provider, selection.Stage, selection.ModelId, selection.Scope, selection.IsPinned);

        lock (_lock)
        {
            var key = GetSelectionKey(selection.Provider, selection.Stage, selection.Scope);
            
            // Remove existing selection for this key if any
            _data.Selections.RemoveAll(s => GetSelectionKey(s.Provider, s.Stage, s.Scope) == key);
            
            // Add new selection
            _data.Selections.Add(selection);
            
            PersistData();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get a specific model selection
    /// </summary>
    public async Task<ModelSelection?> GetSelectionAsync(
        string provider,
        string stage,
        ModelSelectionScope scope,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var key = GetSelectionKey(provider, stage, scope);
            var selection = _data.Selections.FirstOrDefault(s => 
                GetSelectionKey(s.Provider, s.Stage, s.Scope) == key);
            
            return Task.FromResult(selection).Result;
        }
    }

    /// <summary>
    /// Get all selections for a specific scope
    /// </summary>
    public async Task<List<ModelSelection>> GetAllSelectionsAsync(
        ModelSelectionScope scope,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var selections = _data.Selections
                .Where(s => s.Scope == scope)
                .ToList();
            
            return await Task.FromResult(selections);
        }
    }

    /// <summary>
    /// Clear selections matching criteria
    /// </summary>
    public async Task ClearSelectionsAsync(
        string? provider,
        string? stage,
        ModelSelectionScope? scope,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Clearing selections: provider={Provider}, stage={Stage}, scope={Scope}",
            provider, stage, scope);

        lock (_lock)
        {
            _data.Selections.RemoveAll(s =>
                (provider == null || s.Provider == provider) &&
                (stage == null || s.Stage == stage) &&
                (scope == null || s.Scope == scope));
            
            PersistData();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Record a model resolution for audit trail
    /// </summary>
    public async Task RecordSelectionAsync(ModelResolutionResult resolution, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var audit = new ModelSelectionAudit
            {
                Provider = resolution.Provider,
                Stage = resolution.Stage,
                ModelId = resolution.SelectedModelId ?? string.Empty,
                Source = resolution.Source.ToString(),
                Reasoning = resolution.Reasoning,
                IsPinned = resolution.IsPinned,
                IsBlocked = resolution.IsBlocked,
                BlockReason = resolution.BlockReason,
                Timestamp = resolution.ResolutionTimestamp,
                JobId = resolution.JobId
            };
            
            _data.AuditLog.Add(audit);
            
            // Keep only last 1000 audit entries
            if (_data.AuditLog.Count > 1000)
            {
                _data.AuditLog.RemoveRange(0, _data.AuditLog.Count - 1000);
            }
            
            PersistData();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get audit log entries
    /// </summary>
    public async Task<List<ModelSelectionAudit>> GetAuditLogAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var log = _data.AuditLog
                .OrderByDescending(a => a.Timestamp)
                .ToList();
            
            if (limit.HasValue && limit.Value > 0)
            {
                log = log.Take(limit.Value).ToList();
            }
            
            return await Task.FromResult(log);
        }
    }

    /// <summary>
    /// Get the automatic fallback setting
    /// </summary>
    public async Task<bool> GetAutoFallbackSettingAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return await Task.FromResult(_data.AllowAutomaticFallback);
        }
    }

    /// <summary>
    /// Set the automatic fallback setting
    /// </summary>
    public async Task SetAutoFallbackSettingAsync(bool allow, CancellationToken ct = default)
    {
        _logger.LogInformation("Setting automatic fallback: {Allow}", allow);

        lock (_lock)
        {
            _data.AllowAutomaticFallback = allow;
            PersistData();
        }

        await Task.CompletedTask;
    }

    private string GetSelectionKey(string provider, string stage, ModelSelectionScope scope)
    {
        return $"{provider}:{stage}:{scope}";
    }

    private ModelSelectionData LoadData()
    {
        try
        {
            if (File.Exists(_storePath))
            {
                var json = File.ReadAllText(_storePath);
                var data = JsonSerializer.Deserialize<ModelSelectionData>(json);
                if (data != null)
                {
                    _logger.LogInformation(
                        "Loaded model selections: {Count} selections, {AuditCount} audit entries",
                        data.Selections.Count, data.AuditLog.Count);
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model selections, starting with empty data");
        }

        _logger.LogInformation("Initializing new model selection store");
        return new ModelSelectionData();
    }

    private void PersistData()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_storePath, json);
            
            _logger.LogDebug("Persisted model selections to {Path}", _storePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist model selections to {Path}", _storePath);
        }
    }
}

/// <summary>
/// Data structure for persisting model selections
/// </summary>
internal class ModelSelectionData
{
    public List<ModelSelection> Selections { get; set; } = new();
    public List<ModelSelectionAudit> AuditLog { get; set; } = new();
    public bool AllowAutomaticFallback { get; set; } = false;
}

/// <summary>
/// Audit log entry for model selection resolution
/// </summary>
public class ModelSelectionAudit
{
    public required string Provider { get; set; }
    public required string Stage { get; set; }
    public required string ModelId { get; set; }
    public required string Source { get; set; }
    public required string Reasoning { get; set; }
    public bool IsPinned { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? JobId { get; set; }
}
