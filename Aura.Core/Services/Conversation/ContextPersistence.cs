using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Conversation;

/// <summary>
/// Manages persistence of conversation and project contexts to disk
/// </summary>
public class ContextPersistence
{
    private readonly ILogger<ContextPersistence> _logger;
    private readonly string _conversationsDirectory;
    private readonly string _projectContextsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public ContextPersistence(ILogger<ContextPersistence> logger, string baseDirectory)
    {
        _logger = logger;
        _conversationsDirectory = Path.Combine(baseDirectory, "Conversations");
        _projectContextsDirectory = Path.Combine(baseDirectory, "ProjectContexts");
        
        // Ensure directories exist
        Directory.CreateDirectory(_conversationsDirectory);
        Directory.CreateDirectory(_projectContextsDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Save conversation context to disk
    /// </summary>
    public async Task SaveConversationAsync(ConversationContext context, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetConversationFilePath(context.ProjectId);
            var json = JsonSerializer.Serialize(context, _jsonOptions);
            
            // Write to temp file first, then rename for atomic operation
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved conversation context for project {ProjectId}", context.ProjectId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load conversation context from disk
    /// </summary>
    public async Task<ConversationContext?> LoadConversationAsync(string projectId, CancellationToken ct = default)
    {
        var filePath = GetConversationFilePath(projectId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No conversation context found for project {ProjectId}", projectId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var context = JsonSerializer.Deserialize<ConversationContext>(json, _jsonOptions);
            _logger.LogDebug("Loaded conversation context for project {ProjectId}", projectId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load conversation context for project {ProjectId}", projectId);
            return null;
        }
    }

    /// <summary>
    /// Delete conversation context from disk
    /// </summary>
    public async Task DeleteConversationAsync(string projectId, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetConversationFilePath(projectId);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted conversation context for project {ProjectId}", projectId);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Save project context to disk
    /// </summary>
    public async Task SaveProjectContextAsync(ProjectContext context, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetProjectContextFilePath(context.ProjectId);
            var json = JsonSerializer.Serialize(context, _jsonOptions);
            
            // Write to temp file first, then rename for atomic operation
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved project context for project {ProjectId}", context.ProjectId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load project context from disk
    /// </summary>
    public async Task<ProjectContext?> LoadProjectContextAsync(string projectId, CancellationToken ct = default)
    {
        var filePath = GetProjectContextFilePath(projectId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No project context found for project {ProjectId}", projectId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var context = JsonSerializer.Deserialize<ProjectContext>(json, _jsonOptions);
            _logger.LogDebug("Loaded project context for project {ProjectId}", projectId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project context for project {ProjectId}", projectId);
            return null;
        }
    }

    /// <summary>
    /// Delete project context from disk
    /// </summary>
    public async Task DeleteProjectContextAsync(string projectId, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetProjectContextFilePath(projectId);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted project context for project {ProjectId}", projectId);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Get list of all project IDs with saved contexts
    /// </summary>
    public Task<IReadOnlyList<string>> GetAllProjectIdsAsync()
    {
        var projectIds = new List<string>();
        
        // Get from conversations directory
        if (Directory.Exists(_conversationsDirectory))
        {
            foreach (var file in Directory.GetFiles(_conversationsDirectory, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!projectIds.Contains(fileName))
                {
                    projectIds.Add(fileName);
                }
            }
        }
        
        // Get from project contexts directory
        if (Directory.Exists(_projectContextsDirectory))
        {
            foreach (var file in Directory.GetFiles(_projectContextsDirectory, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!projectIds.Contains(fileName))
                {
                    projectIds.Add(fileName);
                }
            }
        }
        
        return Task.FromResult<IReadOnlyList<string>>(projectIds);
    }

    private string GetConversationFilePath(string projectId)
    {
        var safeProjectId = SanitizeFileName(projectId);
        return Path.Combine(_conversationsDirectory, $"{safeProjectId}.json");
    }

    private string GetProjectContextFilePath(string projectId)
    {
        var safeProjectId = SanitizeFileName(projectId);
        return Path.Combine(_projectContextsDirectory, $"{safeProjectId}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
