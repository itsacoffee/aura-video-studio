using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// In-memory storage for audience profiles with persistence capabilities
/// Production implementation would use database
/// </summary>
public class AudienceProfileStore
{
    private readonly ILogger<AudienceProfileStore> _logger;
    private readonly Dictionary<string, AudienceProfile> _profiles = new();
    private readonly object _lock = new();

    public AudienceProfileStore(ILogger<AudienceProfileStore> logger)
    {
        _logger = logger;
        InitializeWithTemplates();
    }

    /// <summary>
    /// Create a new audience profile
    /// </summary>
    public Task<AudienceProfile> CreateAsync(AudienceProfile profile, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                profile.Id = Guid.NewGuid().ToString();
            }

            if (_profiles.ContainsKey(profile.Id))
            {
                throw new InvalidOperationException($"Profile with ID {profile.Id} already exists");
            }

            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;
            profile.Version = 1;

            _profiles[profile.Id] = profile;
            
            _logger.LogInformation("Created audience profile {ProfileId}: {ProfileName}", 
                profile.Id, profile.Name);

            return Task.FromResult(profile);
        }
    }

    /// <summary>
    /// Get a profile by ID
    /// </summary>
    public Task<AudienceProfile?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _profiles.TryGetValue(id, out var profile);
            return Task.FromResult(profile);
        }
    }

    /// <summary>
    /// Get all profiles with optional filtering
    /// </summary>
    public Task<List<AudienceProfile>> GetAllAsync(
        bool? templatesOnly = null,
        int? skip = null,
        int? take = null,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var query = _profiles.Values.AsEnumerable();

            if (templatesOnly.HasValue)
            {
                query = query.Where(p => p.IsTemplate == templatesOnly.Value);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            var result = query.OrderByDescending(p => p.UpdatedAt).ToList();
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Update an existing profile
    /// </summary>
    public Task<AudienceProfile> UpdateAsync(AudienceProfile profile, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_profiles.ContainsKey(profile.Id))
            {
                throw new InvalidOperationException($"Profile with ID {profile.Id} not found");
            }

            profile.UpdatedAt = DateTime.UtcNow;
            profile.Version++;

            _profiles[profile.Id] = profile;
            
            _logger.LogInformation("Updated audience profile {ProfileId}: {ProfileName} (v{Version})", 
                profile.Id, profile.Name, profile.Version);

            return Task.FromResult(profile);
        }
    }

    /// <summary>
    /// Delete a profile (soft delete)
    /// </summary>
    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var removed = _profiles.Remove(id);
            
            if (removed)
            {
                _logger.LogInformation("Deleted audience profile {ProfileId}", id);
            }

            return Task.FromResult(removed);
        }
    }

    /// <summary>
    /// Get all template profiles
    /// </summary>
    public Task<List<AudienceProfile>> GetTemplatesAsync(CancellationToken ct = default)
    {
        return GetAllAsync(templatesOnly: true, ct: ct);
    }

    /// <summary>
    /// Search profiles by name, description, tags, or full-text across all fields
    /// </summary>
    public Task<List<AudienceProfile>> SearchAsync(string query, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var lowerQuery = query.ToLowerInvariant();
            var results = _profiles.Values
                .Where(p => MatchesSearchQuery(p, lowerQuery))
                .OrderByDescending(p => p.LastUsedAt ?? p.UpdatedAt)
                .ThenByDescending(p => p.UsageCount)
                .ToList();

            return Task.FromResult(results);
        }
    }

    /// <summary>
    /// Toggle favorite status for a profile
    /// </summary>
    public Task<AudienceProfile> ToggleFavoriteAsync(string id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_profiles.TryGetValue(id, out var profile))
            {
                throw new InvalidOperationException($"Profile with ID {id} not found");
            }

            profile.IsFavorite = !profile.IsFavorite;
            profile.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Toggled favorite for profile {ProfileId}: {IsFavorite}", 
                id, profile.IsFavorite);

            return Task.FromResult(profile);
        }
    }

    /// <summary>
    /// Get all favorite profiles
    /// </summary>
    public Task<List<AudienceProfile>> GetFavoritesAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var favorites = _profiles.Values
                .Where(p => p.IsFavorite)
                .OrderByDescending(p => p.LastUsedAt ?? p.UpdatedAt)
                .ToList();

            return Task.FromResult(favorites);
        }
    }

    /// <summary>
    /// Move profile to a folder
    /// </summary>
    public Task<AudienceProfile> MoveToFolderAsync(string id, string? folderPath, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_profiles.TryGetValue(id, out var profile))
            {
                throw new InvalidOperationException($"Profile with ID {id} not found");
            }

            profile.FolderPath = folderPath;
            profile.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Moved profile {ProfileId} to folder: {FolderPath}", 
                id, folderPath ?? "(root)");

            return Task.FromResult(profile);
        }
    }

    /// <summary>
    /// Get profiles by folder path
    /// </summary>
    public Task<List<AudienceProfile>> GetByFolderAsync(string? folderPath, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var profiles = _profiles.Values
                .Where(p => p.FolderPath == folderPath)
                .OrderByDescending(p => p.UpdatedAt)
                .ToList();

            return Task.FromResult(profiles);
        }
    }

    /// <summary>
    /// Get all unique folder paths
    /// </summary>
    public Task<List<string>> GetFoldersAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var folders = _profiles.Values
                .Select(p => p.FolderPath)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            return Task.FromResult(folders!);
        }
    }

    /// <summary>
    /// Record profile usage for analytics
    /// </summary>
    public Task RecordUsageAsync(string id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_profiles.TryGetValue(id, out var profile))
            {
                profile.UsageCount++;
                profile.LastUsedAt = DateTime.UtcNow;

                _logger.LogDebug("Recorded usage for profile {ProfileId}, count: {UsageCount}", 
                    id, profile.UsageCount);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Export profile to JSON string
    /// </summary>
    public Task<string> ExportToJsonAsync(string id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_profiles.TryGetValue(id, out var profile))
            {
                throw new InvalidOperationException($"Profile with ID {id} not found");
            }

            var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Exported profile {ProfileId} to JSON", id);

            return Task.FromResult(json);
        }
    }

    /// <summary>
    /// Import profile from JSON string
    /// </summary>
    public Task<AudienceProfile> ImportFromJsonAsync(string json, CancellationToken ct = default)
    {
        var profile = System.Text.Json.JsonSerializer.Deserialize<AudienceProfile>(json);
        
        if (profile == null)
        {
            throw new InvalidOperationException("Failed to deserialize profile from JSON");
        }

        profile.Id = Guid.NewGuid().ToString();
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.Version = 1;

        return CreateAsync(profile, ct);
    }

    /// <summary>
    /// Get recommended profiles based on topic and goal
    /// </summary>
    public Task<List<AudienceProfile>> GetRecommendedProfilesAsync(
        string topic,
        string? goal = null,
        int maxResults = 5,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            var lowerTopic = topic.ToLowerInvariant();
            var lowerGoal = goal?.ToLowerInvariant() ?? string.Empty;

            var scoredProfiles = _profiles.Values
                .Select(p => new
                {
                    Profile = p,
                    Score = CalculateRecommendationScore(p, lowerTopic, lowerGoal)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Profile.UsageCount)
                .Take(maxResults)
                .Select(x => x.Profile)
                .ToList();

            _logger.LogInformation("Recommended {Count} profiles for topic: {Topic}", 
                scoredProfiles.Count, topic);

            return Task.FromResult(scoredProfiles);
        }
    }

    private double CalculateRecommendationScore(AudienceProfile profile, string lowerTopic, string lowerGoal)
    {
        double score = 0;

        if (profile.Name.Contains(lowerTopic, StringComparison.OrdinalIgnoreCase))
            score += 10;

        if (profile.Description?.Contains(lowerTopic, StringComparison.OrdinalIgnoreCase) == true)
            score += 8;

        if (profile.Tags.Any(t => lowerTopic.Contains(t.ToLowerInvariant())))
            score += 6;

        if (profile.Interests.Any(i => lowerTopic.Contains(i.ToLowerInvariant())))
            score += 5;

        if (!string.IsNullOrEmpty(lowerGoal))
        {
            if (profile.Motivations.Any(m => lowerGoal.Contains(m.ToLowerInvariant())))
                score += 4;

            if (profile.PainPoints.Any(p => lowerGoal.Contains(p.ToLowerInvariant())))
                score += 3;
        }

        if (profile.Industry != null && lowerTopic.Contains(profile.Industry.ToLowerInvariant()))
            score += 7;

        if (profile.Profession != null && lowerTopic.Contains(profile.Profession.ToLowerInvariant()))
            score += 6;

        if (profile.UsageCount > 0)
            score += Math.Min(profile.UsageCount * 0.5, 5);

        return score;
    }

    private bool MatchesSearchQuery(AudienceProfile profile, string lowerQuery)
    {
        return profile.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)
            || profile.Description?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) == true
            || profile.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            || profile.Profession?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) == true
            || profile.Industry?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) == true
            || profile.Interests.Any(i => i.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            || profile.PainPoints.Any(p => p.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            || profile.Motivations.Any(m => m.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get count of all profiles
    /// </summary>
    public Task<int> GetCountAsync(bool? templatesOnly = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var query = _profiles.Values.AsEnumerable();

            if (templatesOnly.HasValue)
            {
                query = query.Where(p => p.IsTemplate == templatesOnly.Value);
            }

            return Task.FromResult(query.Count());
        }
    }

    /// <summary>
    /// Initialize store with template profiles
    /// </summary>
    private void InitializeWithTemplates()
    {
        lock (_lock)
        {
            var templates = AudienceProfileTemplates.GetAllTemplates();
            
            foreach (var template in templates)
            {
                if (!_profiles.ContainsKey(template.Id))
                {
                    _profiles[template.Id] = template;
                }
            }

            _logger.LogInformation("Initialized audience profile store with {TemplateCount} templates", 
                templates.Count);
        }
    }
}
