using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Data;

/// <summary>
/// Repository for managing project versions and content blobs
/// </summary>
public class ProjectVersionRepository
{
    private readonly AuraDbContext _context;
    private readonly ILogger<ProjectVersionRepository> _logger;

    public ProjectVersionRepository(AuraDbContext context, ILogger<ProjectVersionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new version for a project
    /// </summary>
    public async Task<ProjectVersionEntity> CreateVersionAsync(
        ProjectVersionEntity version,
        CancellationToken ct = default)
    {
        if (version.Id == Guid.Empty)
        {
            version.Id = Guid.NewGuid();
        }

        var maxVersion = await _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == version.ProjectId && !v.IsDeleted)
            .MaxAsync(v => (int?)v.VersionNumber, ct).ConfigureAwait(false) ?? 0;

        version.VersionNumber = maxVersion + 1;

        _context.Set<ProjectVersionEntity>().Add(version);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        return version;
    }

    /// <summary>
    /// Get all versions for a project (excluding deleted)
    /// </summary>
    public async Task<List<ProjectVersionEntity>> GetVersionsAsync(
        Guid projectId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == projectId);

        if (!includeDeleted)
        {
            query = query.Where(v => !v.IsDeleted);
        }

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a specific version by ID
    /// </summary>
    public async Task<ProjectVersionEntity?> GetVersionByIdAsync(
        Guid versionId,
        CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a specific version by project ID and version number
    /// </summary>
    public async Task<ProjectVersionEntity?> GetVersionByNumberAsync(
        Guid projectId,
        int versionNumber,
        CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.VersionNumber == versionNumber && !v.IsDeleted, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the latest version for a project
    /// </summary>
    public async Task<ProjectVersionEntity?> GetLatestVersionAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == projectId && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Update version metadata (name, description, important flag)
    /// </summary>
    public async Task UpdateVersionMetadataAsync(
        Guid versionId,
        string? name,
        string? description,
        bool? isMarkedImportant,
        CancellationToken ct = default)
    {
        var version = await GetVersionByIdAsync(versionId, ct).ConfigureAwait(false);
        if (version == null)
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        if (name != null)
        {
            version.Name = name;
        }

        if (description != null)
        {
            version.Description = description;
        }

        if (isMarkedImportant.HasValue)
        {
            version.IsMarkedImportant = isMarkedImportant.Value;
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Soft delete a version
    /// </summary>
    public async Task DeleteVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await GetVersionByIdAsync(versionId, ct).ConfigureAwait(false);
        if (version == null)
        {
            return;
        }

        version.IsDeleted = true;
        version.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get total storage used by all versions of a project
    /// </summary>
    public async Task<long> GetProjectStorageSizeAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == projectId && !v.IsDeleted)
            .SumAsync(v => v.StorageSizeBytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get or create a content blob with deduplication
    /// </summary>
    public async Task<string> StoreContentBlobAsync(
        string content,
        string contentType,
        CancellationToken ct = default)
    {
        var hash = ComputeHash(content);
        var blob = await _context.Set<ContentBlobEntity>()
            .FirstOrDefaultAsync(b => b.ContentHash == hash, ct).ConfigureAwait(false);

        if (blob == null)
        {
            blob = new ContentBlobEntity
            {
                ContentHash = hash,
                Content = content,
                ContentType = contentType,
                SizeBytes = Encoding.UTF8.GetByteCount(content),
                CreatedAt = DateTime.UtcNow,
                LastReferencedAt = DateTime.UtcNow,
                ReferenceCount = 1
            };
            _context.Set<ContentBlobEntity>().Add(blob);
        }
        else
        {
            blob.ReferenceCount++;
            blob.LastReferencedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return hash;
    }

    /// <summary>
    /// Get content blob by hash
    /// </summary>
    public async Task<ContentBlobEntity?> GetContentBlobAsync(
        string contentHash,
        CancellationToken ct = default)
    {
        return await _context.Set<ContentBlobEntity>()
            .FirstOrDefaultAsync(b => b.ContentHash == contentHash, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Decrement reference count when a version is deleted
    /// </summary>
    public async Task DecrementBlobReferenceAsync(string contentHash, CancellationToken ct = default)
    {
        var blob = await GetContentBlobAsync(contentHash, ct).ConfigureAwait(false);
        if (blob != null)
        {
            blob.ReferenceCount--;
            if (blob.ReferenceCount <= 0)
            {
                _context.Set<ContentBlobEntity>().Remove(blob);
            }
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Get versions by type (Manual, Autosave, RestorePoint)
    /// </summary>
    public async Task<List<ProjectVersionEntity>> GetVersionsByTypeAsync(
        Guid projectId,
        string versionType,
        CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == projectId && v.VersionType == versionType && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get autosave versions older than a specific date (for cleanup)
    /// </summary>
    public async Task<List<ProjectVersionEntity>> GetOldAutosavesAsync(
        Guid projectId,
        DateTime olderThan,
        CancellationToken ct = default)
    {
        return await _context.Set<ProjectVersionEntity>()
            .Where(v => v.ProjectId == projectId 
                && v.VersionType == "Autosave" 
                && !v.IsMarkedImportant 
                && !v.IsDeleted
                && v.CreatedAt < olderThan)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Compute SHA256 hash of content for deduplication
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
