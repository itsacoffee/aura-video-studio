using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Aura.Core.Models.Media;

namespace Aura.Core.Data;

/// <summary>
/// Repository for media library operations
/// </summary>
public interface IMediaRepository
{
    // Media CRUD
    Task<MediaEntity?> GetMediaByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<MediaEntity>> GetAllMediaAsync(CancellationToken ct = default);
    Task<MediaEntity> AddMediaAsync(MediaEntity media, CancellationToken ct = default);
    Task UpdateMediaAsync(MediaEntity media, CancellationToken ct = default);
    Task DeleteMediaAsync(Guid id, CancellationToken ct = default);
    
    // Search and filter
    Task<(List<MediaEntity> Items, int Total)> SearchMediaAsync(
        MediaSearchRequest request, 
        CancellationToken ct = default);
    
    // Collections
    Task<MediaCollectionEntity?> GetCollectionByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<MediaCollectionEntity>> GetAllCollectionsAsync(CancellationToken ct = default);
    Task<MediaCollectionEntity> AddCollectionAsync(MediaCollectionEntity collection, CancellationToken ct = default);
    Task UpdateCollectionAsync(MediaCollectionEntity collection, CancellationToken ct = default);
    Task DeleteCollectionAsync(Guid id, CancellationToken ct = default);
    
    // Tags
    Task<List<string>> GetAllTagsAsync(CancellationToken ct = default);
    Task AddTagsAsync(Guid mediaId, List<string> tags, CancellationToken ct = default);
    Task RemoveTagsAsync(Guid mediaId, List<string> tags, CancellationToken ct = default);
    
    // Usage tracking
    Task TrackUsageAsync(Guid mediaId, string projectId, string? projectName, CancellationToken ct = default);
    Task<List<MediaUsageEntity>> GetUsageHistoryAsync(Guid mediaId, CancellationToken ct = default);
    
    // Statistics
    Task<StorageStats> GetStorageStatsAsync(CancellationToken ct = default);
    
    // Duplicate detection
    Task<MediaEntity?> FindByContentHashAsync(string contentHash, CancellationToken ct = default);
    
    // Upload sessions
    Task<UploadSessionEntity> CreateUploadSessionAsync(UploadSessionEntity session, CancellationToken ct = default);
    Task<UploadSessionEntity?> GetUploadSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task UpdateUploadSessionAsync(UploadSessionEntity session, CancellationToken ct = default);
    Task DeleteExpiredSessionsAsync(CancellationToken ct = default);
}

/// <summary>
/// Implementation of media repository
/// </summary>
public class MediaRepository : IMediaRepository
{
    private readonly AuraDbContext _context;
    private readonly ILogger<MediaRepository> _logger;

    public MediaRepository(AuraDbContext context, ILogger<MediaRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MediaEntity?> GetMediaByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MediaItems
            .Include(m => m.Tags)
            .Include(m => m.Collection)
            .FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<List<MediaEntity>> GetAllMediaAsync(CancellationToken ct = default)
    {
        return await _context.MediaItems
            .Include(m => m.Tags)
            .Include(m => m.Collection)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<MediaEntity> AddMediaAsync(MediaEntity media, CancellationToken ct = default)
    {
        await _context.MediaItems.AddAsync(media, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return media;
    }

    public async Task UpdateMediaAsync(MediaEntity media, CancellationToken ct = default)
    {
        _context.MediaItems.Update(media);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteMediaAsync(Guid id, CancellationToken ct = default)
    {
        var media = await _context.MediaItems.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (media != null)
        {
            _context.MediaItems.Remove(media);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<(List<MediaEntity> Items, int Total)> SearchMediaAsync(
        MediaSearchRequest request, 
        CancellationToken ct = default)
    {
        var query = _context.MediaItems
            .Include(m => m.Tags)
            .Include(m => m.Collection)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(m => 
                m.FileName.ToLower().Contains(searchLower) ||
                (m.Description != null && m.Description.ToLower().Contains(searchLower)));
        }

        if (request.Types != null && request.Types.Count != 0)
        {
            var typeStrings = request.Types.Select(t => t.ToString()).ToList();
            query = query.Where(m => typeStrings.Contains(m.Type));
        }

        if (request.Sources != null && request.Sources.Count != 0)
        {
            var sourceStrings = request.Sources.Select(s => s.ToString()).ToList();
            query = query.Where(m => sourceStrings.Contains(m.Source));
        }

        if (request.Tags != null && request.Tags.Count != 0)
        {
            foreach (var tag in request.Tags)
            {
                query = query.Where(m => m.Tags.Any(t => t.Tag == tag));
            }
        }

        if (request.CollectionId.HasValue)
        {
            query = query.Where(m => m.CollectionId == request.CollectionId.Value);
        }

        if (request.CreatedAfter.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= request.CreatedAfter.Value);
        }

        if (request.CreatedBefore.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= request.CreatedBefore.Value);
        }

        // Get total count
        var total = await query.CountAsync(ct).ConfigureAwait(false);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "filename" => request.SortDescending 
                ? query.OrderByDescending(m => m.FileName) 
                : query.OrderBy(m => m.FileName),
            "filesize" => request.SortDescending 
                ? query.OrderByDescending(m => m.FileSize) 
                : query.OrderBy(m => m.FileSize),
            "type" => request.SortDescending 
                ? query.OrderByDescending(m => m.Type) 
                : query.OrderBy(m => m.Type),
            _ => request.SortDescending 
                ? query.OrderByDescending(m => m.CreatedAt) 
                : query.OrderBy(m => m.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<MediaCollectionEntity?> GetCollectionByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.MediaCollections
            .Include(c => c.MediaItems)
            .FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
    }

    public async Task<List<MediaCollectionEntity>> GetAllCollectionsAsync(CancellationToken ct = default)
    {
        return await _context.MediaCollections
            .Include(c => c.MediaItems)
            .OrderBy(c => c.Name)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<MediaCollectionEntity> AddCollectionAsync(MediaCollectionEntity collection, CancellationToken ct = default)
    {
        await _context.MediaCollections.AddAsync(collection, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return collection;
    }

    public async Task UpdateCollectionAsync(MediaCollectionEntity collection, CancellationToken ct = default)
    {
        _context.MediaCollections.Update(collection);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteCollectionAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await _context.MediaCollections.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (collection != null)
        {
            _context.MediaCollections.Remove(collection);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<List<string>> GetAllTagsAsync(CancellationToken ct = default)
    {
        return await _context.MediaTags
            .Select(t => t.Tag)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddTagsAsync(Guid mediaId, List<string> tags, CancellationToken ct = default)
    {
        var existingTags = await _context.MediaTags
            .Where(t => t.MediaId == mediaId)
            .Select(t => t.Tag)
            .ToListAsync(ct).ConfigureAwait(false);

        var newTags = tags
            .Where(t => !existingTags.Contains(t))
            .Select(t => new MediaTagEntity
            {
                Id = Guid.NewGuid(),
                MediaId = mediaId,
                Tag = t,
                CreatedAt = DateTime.UtcNow
            });

        await _context.MediaTags.AddRangeAsync(newTags, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveTagsAsync(Guid mediaId, List<string> tags, CancellationToken ct = default)
    {
        var tagsToRemove = await _context.MediaTags
            .Where(t => t.MediaId == mediaId && tags.Contains(t.Tag))
            .ToListAsync(ct).ConfigureAwait(false);

        _context.MediaTags.RemoveRange(tagsToRemove);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task TrackUsageAsync(Guid mediaId, string projectId, string? projectName, CancellationToken ct = default)
    {
        var usage = new MediaUsageEntity
        {
            Id = Guid.NewGuid(),
            MediaId = mediaId,
            ProjectId = projectId,
            ProjectName = projectName,
            UsedAt = DateTime.UtcNow
        };

        await _context.MediaUsages.AddAsync(usage, ct).ConfigureAwait(false);

        // Update media usage count
        var media = await _context.MediaItems.FindAsync(new object[] { mediaId }, ct).ConfigureAwait(false);
        if (media != null)
        {
            media.UsageCount++;
            media.LastUsedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<MediaUsageEntity>> GetUsageHistoryAsync(Guid mediaId, CancellationToken ct = default)
    {
        return await _context.MediaUsages
            .Where(u => u.MediaId == mediaId)
            .OrderByDescending(u => u.UsedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken ct = default)
    {
        var allMedia = await _context.MediaItems.ToListAsync(ct).ConfigureAwait(false);
        var totalSize = allMedia.Sum(m => m.FileSize);
        var quota = 50L * 1024 * 1024 * 1024; // 50GB default quota

        var stats = new StorageStats
        {
            TotalSizeBytes = totalSize,
            QuotaBytes = quota,
            AvailableBytes = quota - totalSize,
            UsagePercentage = quota > 0 ? (double)totalSize / quota * 100 : 0,
            TotalFiles = allMedia.Count
        };

        foreach (var media in allMedia)
        {
            if (Enum.TryParse<MediaType>(media.Type, out var type))
            {
                if (!stats.FilesByType.TryGetValue(type, out var value))
                {
                    value = 0;
                    stats.FilesByType[type] = value;
                    stats.SizeByType[type] = 0;
                }
                stats.FilesByType[type] = ++value;
                stats.SizeByType[type] += media.FileSize;
            }
        }

        return stats;
    }

    public async Task<MediaEntity?> FindByContentHashAsync(string contentHash, CancellationToken ct = default)
    {
        return await _context.MediaItems
            .FirstOrDefaultAsync(m => m.ContentHash == contentHash, ct).ConfigureAwait(false);
    }

    public async Task<UploadSessionEntity> CreateUploadSessionAsync(UploadSessionEntity session, CancellationToken ct = default)
    {
        await _context.UploadSessions.AddAsync(session, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        return session;
    }

    public async Task<UploadSessionEntity?> GetUploadSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _context.UploadSessions.FindAsync(new object[] { sessionId }, ct).ConfigureAwait(false);
    }

    public async Task UpdateUploadSessionAsync(UploadSessionEntity session, CancellationToken ct = default)
    {
        _context.UploadSessions.Update(session);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteExpiredSessionsAsync(CancellationToken ct = default)
    {
        var expiredSessions = await _context.UploadSessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct).ConfigureAwait(false);

        _context.UploadSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
