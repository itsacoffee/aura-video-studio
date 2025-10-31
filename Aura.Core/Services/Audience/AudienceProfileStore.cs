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
    /// Search profiles by name or tags
    /// </summary>
    public Task<List<AudienceProfile>> SearchAsync(string query, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var lowerQuery = query.ToLowerInvariant();
            var results = _profiles.Values
                .Where(p => 
                    p.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Description?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) == true ||
                    p.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.UpdatedAt)
                .ToList();

            return Task.FromResult(results);
        }
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
