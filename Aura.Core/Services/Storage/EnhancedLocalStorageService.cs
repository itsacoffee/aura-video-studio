using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Aura.Core.Models.Storage;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Enhanced local storage service with workspace organization, quota monitoring, and cache management
/// </summary>
public interface IEnhancedLocalStorageService : IStorageService
{
    // Workspace Management
    Task<string> GetWorkspacePathAsync(string folder, CancellationToken ct = default);
    Task EnsureWorkspaceStructureAsync(CancellationToken ct = default);
    
    // Storage Statistics
    Task<StorageStatistics> GetStorageStatisticsAsync(CancellationToken ct = default);
    Task<DiskSpaceInfo> GetDiskSpaceInfoAsync(CancellationToken ct = default);
    Task<bool> CheckStorageQuotaAsync(long requiredBytes, CancellationToken ct = default);
    
    // Cache Management
    Task<CacheEntry?> GetCacheEntryAsync(string key, CancellationToken ct = default);
    Task<string> AddCacheEntryAsync(string key, string category, Stream data, Dictionary<string, string>? metadata = null, CancellationToken ct = default);
    Task<bool> RemoveCacheEntryAsync(string key, CancellationToken ct = default);
    Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken ct = default);
    Task<CacheCleanupResult> CleanupCacheAsync(bool forceAll = false, CancellationToken ct = default);
    Task<CacheCleanupResult> CleanupCacheByCategoryAsync(string category, CancellationToken ct = default);
    
    // File Operations
    Task<string> SaveProjectFileAsync(Guid projectId, string content, CancellationToken ct = default);
    Task<string?> LoadProjectFileAsync(Guid projectId, CancellationToken ct = default);
    Task<bool> ProjectFileExistsAsync(Guid projectId, CancellationToken ct = default);
    Task<string> CreateBackupAsync(Guid projectId, string? backupName = null, CancellationToken ct = default);
    Task<List<string>> ListBackupsAsync(Guid projectId, CancellationToken ct = default);
    Task<string?> RestoreBackupAsync(Guid projectId, string backupFileName, CancellationToken ct = default);
}

/// <summary>
/// Implementation of enhanced local storage service
/// </summary>
public class EnhancedLocalStorageService : IEnhancedLocalStorageService
{
    private readonly ILogger<EnhancedLocalStorageService> _logger;
    private readonly LocalStorageConfiguration _config;
    private readonly string _storageRoot;
    private readonly ConcurrentDictionary<Guid, ChunkUploadSession> _uploadSessions = new();
    private readonly ConcurrentDictionary<string, CacheEntry> _cacheIndex = new();
    private readonly string _cacheIndexPath;
    
    private class ChunkUploadSession
    {
        public List<string> ChunkPaths { get; set; } = new();
        public string FinalPath { get; set; } = string.Empty;
    }

    public EnhancedLocalStorageService(
        IConfiguration configuration,
        ILogger<EnhancedLocalStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Load configuration
        _config = new LocalStorageConfiguration();
        configuration.GetSection("Storage:Local").Bind(_config);
        
        // Set default storage root if not configured
        if (string.IsNullOrWhiteSpace(_config.StorageRoot))
        {
            var dataRoot = AuraEnvironmentPaths.ResolveDataRoot(null);
            _config.StorageRoot = Path.Combine(dataRoot, "Workspace");
        }
        
        _storageRoot = AuraEnvironmentPaths.EnsureDirectory(_config.StorageRoot);
        _cacheIndexPath = Path.Combine(_storageRoot, WorkspaceFolders.Cache, "index.json");
        
        // Initialize workspace structure
        Task.Run(async () => await EnsureWorkspaceStructureAsync().ConfigureAwait(false)).Wait();
        
        // Load cache index
        Task.Run(async () => await LoadCacheIndexAsync().ConfigureAwait(false)).Wait();
    }

    #region Workspace Management

    public Task<string> GetWorkspacePathAsync(string folder, CancellationToken ct = default)
    {
        var path = Path.Combine(_storageRoot, folder);
        Directory.CreateDirectory(path);
        return Task.FromResult(path);
    }

    public Task EnsureWorkspaceStructureAsync(CancellationToken ct = default)
    {
        try
        {
            // Create all workspace folders
            var folders = new[]
            {
                WorkspaceFolders.Projects,
                WorkspaceFolders.Exports,
                WorkspaceFolders.Cache,
                WorkspaceFolders.Temp,
                WorkspaceFolders.Media,
                WorkspaceFolders.Thumbnails,
                WorkspaceFolders.Backups,
                WorkspaceFolders.Previews
            };

            foreach (var folder in folders)
            {
                var path = Path.Combine(_storageRoot, folder);
                Directory.CreateDirectory(path);
            }
            
            // Create cache subfolders
            var cacheSubfolders = new[] { "Thumbnails", "Previews", "Renders", "Temp" };
            foreach (var subfolder in cacheSubfolders)
            {
                var path = Path.Combine(_storageRoot, WorkspaceFolders.Cache, subfolder);
                Directory.CreateDirectory(path);
            }

            _logger.LogInformation("Workspace structure initialized at: {StorageRoot}", _storageRoot);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workspace structure");
            throw;
        }
    }

    #endregion

    #region Storage Statistics

    public async Task<StorageStatistics> GetStorageStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var stats = new StorageStatistics
            {
                QuotaBytes = _config.StorageQuotaBytes,
                LastUpdated = DateTime.UtcNow,
                FolderSizes = new Dictionary<string, long>()
            };

            var folders = new[]
            {
                WorkspaceFolders.Projects,
                WorkspaceFolders.Exports,
                WorkspaceFolders.Cache,
                WorkspaceFolders.Temp,
                WorkspaceFolders.Media,
                WorkspaceFolders.Thumbnails,
                WorkspaceFolders.Backups,
                WorkspaceFolders.Previews
            };

            long totalSize = 0;
            int totalFiles = 0;

            foreach (var folder in folders)
            {
                var path = Path.Combine(_storageRoot, folder);
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                    var folderSize = files.Sum(f => f.Length);
                    
                    stats.FolderSizes[folder] = folderSize;
                    totalSize += folderSize;
                    totalFiles += files.Length;
                }
                else
                {
                    stats.FolderSizes[folder] = 0;
                }
            }

            stats.UsedSizeBytes = totalSize;
            stats.AvailableSizeBytes = _config.StorageQuotaBytes - totalSize;
            stats.TotalSizeBytes = _config.StorageQuotaBytes;
            stats.UsagePercentage = _config.StorageQuotaBytes > 0 
                ? (double)totalSize / _config.StorageQuotaBytes * 100 
                : 0;
            stats.TotalFiles = totalFiles;
            stats.IsLowSpace = stats.AvailableSizeBytes < _config.LowSpaceThresholdBytes;

            return await Task.FromResult(stats).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage statistics");
            throw;
        }
    }

    public async Task<DiskSpaceInfo> GetDiskSpaceInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_storageRoot) ?? "/");
            
            var info = new DiskSpaceInfo
            {
                DriveName = driveInfo.Name,
                TotalSize = driveInfo.TotalSize,
                AvailableSpace = driveInfo.AvailableFreeSpace,
                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                UsagePercentage = (double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize * 100,
                IsLowSpace = driveInfo.AvailableFreeSpace < _config.LowSpaceThresholdBytes,
                FormattedTotalSize = FormatBytes(driveInfo.TotalSize),
                FormattedAvailableSpace = FormatBytes(driveInfo.AvailableFreeSpace),
                FormattedUsedSpace = FormatBytes(driveInfo.TotalSize - driveInfo.AvailableFreeSpace)
            };

            return await Task.FromResult(info).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disk space info");
            throw;
        }
    }

    public async Task<bool> CheckStorageQuotaAsync(long requiredBytes, CancellationToken ct = default)
    {
        try
        {
            var stats = await GetStorageStatisticsAsync(ct).ConfigureAwait(false);
            var diskInfo = await GetDiskSpaceInfoAsync(ct).ConfigureAwait(false);
            
            // Check both quota and actual disk space
            var hasQuota = stats.AvailableSizeBytes >= requiredBytes;
            var hasDiskSpace = diskInfo.AvailableSpace >= requiredBytes;
            
            if (!hasQuota)
            {
                _logger.LogWarning("Storage quota exceeded. Required: {Required}MB, Available: {Available}MB",
                    requiredBytes / 1024 / 1024, stats.AvailableSizeBytes / 1024 / 1024);
            }
            
            if (!hasDiskSpace)
            {
                _logger.LogWarning("Disk space low. Required: {Required}MB, Available: {Available}MB",
                    requiredBytes / 1024 / 1024, diskInfo.AvailableSpace / 1024 / 1024);
            }
            
            return hasQuota && hasDiskSpace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check storage quota");
            return false;
        }
    }

    #endregion

    #region Cache Management

    private async Task LoadCacheIndexAsync()
    {
        try
        {
            if (File.Exists(_cacheIndexPath))
            {
                var json = await File.ReadAllTextAsync(_cacheIndexPath).ConfigureAwait(false);
                var entries = JsonSerializer.Deserialize<List<CacheEntry>>(json);
                
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        _cacheIndex[entry.Key] = entry;
                    }
                    
                    _logger.LogInformation("Loaded {Count} cache entries from index", entries.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cache index, starting fresh");
        }
    }

    private async Task SaveCacheIndexAsync()
    {
        try
        {
            var entries = _cacheIndex.Values.ToList();
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cacheIndexPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cache index");
        }
    }

    public Task<CacheEntry?> GetCacheEntryAsync(string key, CancellationToken ct = default)
    {
        if (_cacheIndex.TryGetValue(key, out var entry))
        {
            // Update access statistics
            entry.LastAccessedAt = DateTime.UtcNow;
            entry.AccessCount++;
            
            // Check if file still exists
            if (File.Exists(entry.FilePath))
            {
                return Task.FromResult<CacheEntry?>(entry);
            }
            else
            {
                // Remove stale entry
                _cacheIndex.TryRemove(key, out _);
                _logger.LogWarning("Cache entry file not found: {Key}", key);
            }
        }
        
        return Task.FromResult<CacheEntry?>(null);
    }

    public async Task<string> AddCacheEntryAsync(string key, string category, Stream data, Dictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        try
        {
            // Check if entry already exists
            if (_cacheIndex.TryGetValue(key, out var existingEntry))
            {
                _logger.LogDebug("Cache entry already exists: {Key}", key);
                return existingEntry.FilePath;
            }
            
            // Create category subfolder
            var categoryPath = Path.Combine(_storageRoot, WorkspaceFolders.Cache, category);
            Directory.CreateDirectory(categoryPath);
            
            // Generate unique file name
            var fileId = Guid.NewGuid();
            var filePath = Path.Combine(categoryPath, fileId.ToString());
            
            // Save file
            using (var fileStream = File.Create(filePath))
            {
                await data.CopyToAsync(fileStream, ct).ConfigureAwait(false);
            }
            
            var fileInfo = new FileInfo(filePath);
            
            // Create cache entry
            var entry = new CacheEntry
            {
                Id = fileId,
                Key = key,
                FilePath = filePath,
                Category = category,
                SizeBytes = fileInfo.Length,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                AccessCount = 0,
                ExpiresAt = DateTime.UtcNow.AddDays(_config.CacheTtlDays),
                Metadata = metadata ?? new Dictionary<string, string>()
            };
            
            _cacheIndex[key] = entry;
            await SaveCacheIndexAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Added cache entry: {Key} ({Size} bytes)", key, fileInfo.Length);
            
            // Check cache size and cleanup if needed
            if (_config.EnableAutoCacheCleanup)
            {
                var stats = await GetCacheStatisticsAsync(ct).ConfigureAwait(false);
                if (stats.TotalSizeBytes > _config.MaxCacheSizeBytes)
                {
                    _logger.LogInformation("Cache size exceeded, triggering cleanup");
                    await CleanupCacheAsync(false, ct).ConfigureAwait(false);
                }
            }
            
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cache entry: {Key}", key);
            throw;
        }
    }

    public async Task<bool> RemoveCacheEntryAsync(string key, CancellationToken ct = default)
    {
        try
        {
            if (_cacheIndex.TryRemove(key, out var entry))
            {
                if (File.Exists(entry.FilePath))
                {
                    File.Delete(entry.FilePath);
                }
                
                await SaveCacheIndexAsync().ConfigureAwait(false);
                _logger.LogInformation("Removed cache entry: {Key}", key);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache entry: {Key}", key);
            return false;
        }
    }

    public Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken ct = default)
    {
        var stats = new CacheStatistics
        {
            MaxSizeBytes = _config.MaxCacheSizeBytes,
            TotalEntries = _cacheIndex.Count,
            EntriesByCategory = new Dictionary<string, int>(),
            SizeByCategory = new Dictionary<string, long>()
        };
        
        foreach (var entry in _cacheIndex.Values)
        {
            stats.TotalSizeBytes += entry.SizeBytes;
            stats.HitCount += entry.AccessCount;
            
            if (!stats.EntriesByCategory.TryGetValue(entry.Category, out var value))
            {
                value = 0;
                stats.EntriesByCategory[entry.Category] = value;
                stats.SizeByCategory[entry.Category] = 0;
            }
            
            stats.EntriesByCategory[entry.Category] = ++value;
            stats.SizeByCategory[entry.Category] += entry.SizeBytes;
            
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt < DateTime.UtcNow)
            {
                stats.ExpiredEntries++;
            }
            
            if (stats.OldestEntry == null || entry.CreatedAt < stats.OldestEntry)
            {
                stats.OldestEntry = entry.CreatedAt;
            }
            
            if (stats.NewestEntry == null || entry.CreatedAt > stats.NewestEntry)
            {
                stats.NewestEntry = entry.CreatedAt;
            }
        }
        
        stats.UsagePercentage = stats.MaxSizeBytes > 0 
            ? (double)stats.TotalSizeBytes / stats.MaxSizeBytes * 100 
            : 0;
        
        var totalRequests = stats.HitCount + stats.MissCount;
        stats.HitRate = totalRequests > 0 ? (double)stats.HitCount / totalRequests * 100 : 0;
        
        return Task.FromResult(stats);
    }

    public async Task<CacheCleanupResult> CleanupCacheAsync(bool forceAll = false, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new CacheCleanupResult();
        
        try
        {
            var entriesToRemove = new List<string>();
            
            if (forceAll)
            {
                // Remove all cache entries
                entriesToRemove.AddRange(_cacheIndex.Keys);
            }
            else
            {
                // Remove expired entries
                var expiredEntries = _cacheIndex.Values
                    .Where(e => e.ExpiresAt.HasValue && e.ExpiresAt < DateTime.UtcNow)
                    .Select(e => e.Key)
                    .ToList();
                
                entriesToRemove.AddRange(expiredEntries);
                
                // If still over size limit, remove least recently used entries
                var stats = await GetCacheStatisticsAsync(ct).ConfigureAwait(false);
                if (stats.TotalSizeBytes > _config.MaxCacheSizeBytes)
                {
                    var lruEntries = _cacheIndex.Values
                        .OrderBy(e => e.LastAccessedAt)
                        .TakeWhile(e =>
                        {
                            var shouldRemove = stats.TotalSizeBytes > _config.MaxCacheSizeBytes * 0.8; // Target 80% usage
                            if (shouldRemove)
                            {
                                stats.TotalSizeBytes -= e.SizeBytes;
                            }
                            return shouldRemove;
                        })
                        .Select(e => e.Key)
                        .ToList();
                    
                    entriesToRemove.AddRange(lruEntries);
                }
            }
            
            // Remove entries
            foreach (var key in entriesToRemove.Distinct())
            {
                if (_cacheIndex.TryRemove(key, out var entry))
                {
                    try
                    {
                        if (File.Exists(entry.FilePath))
                        {
                            File.Delete(entry.FilePath);
                            result.BytesFreed += entry.SizeBytes;
                        }
                        
                        result.EntriesRemoved++;
                        
                        if (!result.RemovedCategories.Contains(entry.Category))
                        {
                            result.RemovedCategories.Add(entry.Category);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete cache file: {FilePath}", entry.FilePath);
                    }
                }
            }
            
            await SaveCacheIndexAsync().ConfigureAwait(false);
            
            result.Duration = DateTime.UtcNow - startTime;
            result.FormattedBytesFreed = FormatBytes(result.BytesFreed);
            
            _logger.LogInformation("Cache cleanup completed: {Count} entries removed, {Size} freed in {Duration}ms",
                result.EntriesRemoved, result.FormattedBytesFreed, result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache cleanup failed");
        }
        
        return result;
    }

    public async Task<CacheCleanupResult> CleanupCacheByCategoryAsync(string category, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new CacheCleanupResult();
        
        try
        {
            var entriesToRemove = _cacheIndex.Values
                .Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Key)
                .ToList();
            
            foreach (var key in entriesToRemove)
            {
                if (_cacheIndex.TryRemove(key, out var entry))
                {
                    try
                    {
                        if (File.Exists(entry.FilePath))
                        {
                            File.Delete(entry.FilePath);
                            result.BytesFreed += entry.SizeBytes;
                        }
                        
                        result.EntriesRemoved++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete cache file: {FilePath}", entry.FilePath);
                    }
                }
            }
            
            result.RemovedCategories.Add(category);
            await SaveCacheIndexAsync().ConfigureAwait(false);
            
            result.Duration = DateTime.UtcNow - startTime;
            result.FormattedBytesFreed = FormatBytes(result.BytesFreed);
            
            _logger.LogInformation("Cache cleanup for category '{Category}' completed: {Count} entries removed, {Size} freed",
                category, result.EntriesRemoved, result.FormattedBytesFreed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache cleanup failed for category: {Category}", category);
        }
        
        return result;
    }

    #endregion

    #region Project File Management

    public async Task<string> SaveProjectFileAsync(Guid projectId, string content, CancellationToken ct = default)
    {
        try
        {
            var projectsPath = await GetWorkspacePathAsync(WorkspaceFolders.Projects, ct).ConfigureAwait(false);
            var projectFile = Path.Combine(projectsPath, $"{projectId}.aura");
            
            await File.WriteAllTextAsync(projectFile, content, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Saved project file: {ProjectId}", projectId);
            return projectFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project file: {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<string?> LoadProjectFileAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var projectsPath = await GetWorkspacePathAsync(WorkspaceFolders.Projects, ct).ConfigureAwait(false);
            var projectFile = Path.Combine(projectsPath, $"{projectId}.aura");
            
            if (File.Exists(projectFile))
            {
                return await File.ReadAllTextAsync(projectFile, ct).ConfigureAwait(false);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project file: {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<bool> ProjectFileExistsAsync(Guid projectId, CancellationToken ct = default)
    {
        var projectsPath = await GetWorkspacePathAsync(WorkspaceFolders.Projects, ct).ConfigureAwait(false);
        var projectFile = Path.Combine(projectsPath, $"{projectId}.aura");
        return File.Exists(projectFile);
    }

    public async Task<string> CreateBackupAsync(Guid projectId, string? backupName = null, CancellationToken ct = default)
    {
        try
        {
            var projectContent = await LoadProjectFileAsync(projectId, ct).ConfigureAwait(false);
            if (projectContent == null)
            {
                throw new FileNotFoundException($"Project file not found: {projectId}");
            }
            
            var backupsPath = await GetWorkspacePathAsync(WorkspaceFolders.Backups, ct).ConfigureAwait(false);
            var projectBackupPath = Path.Combine(backupsPath, projectId.ToString());
            Directory.CreateDirectory(projectBackupPath);
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = string.IsNullOrEmpty(backupName)
                ? $"{projectId}_{timestamp}.aura.bak"
                : $"{projectId}_{backupName}_{timestamp}.aura.bak";
            
            var backupFile = Path.Combine(projectBackupPath, fileName);
            await File.WriteAllTextAsync(backupFile, projectContent, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Created backup for project {ProjectId}: {BackupFile}", projectId, fileName);
            return backupFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for project: {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<List<string>> ListBackupsAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var backupsPath = await GetWorkspacePathAsync(WorkspaceFolders.Backups, ct).ConfigureAwait(false);
            var projectBackupPath = Path.Combine(backupsPath, projectId.ToString());
            
            if (!Directory.Exists(projectBackupPath))
            {
                return new List<string>();
            }
            
            var backupFiles = Directory.GetFiles(projectBackupPath, "*.aura.bak")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderByDescending(f => f)
                .ToList();
            
            return backupFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups for project: {ProjectId}", projectId);
            return new List<string>();
        }
    }

    public async Task<string?> RestoreBackupAsync(Guid projectId, string backupFileName, CancellationToken ct = default)
    {
        try
        {
            var backupsPath = await GetWorkspacePathAsync(WorkspaceFolders.Backups, ct).ConfigureAwait(false);
            var backupFile = Path.Combine(backupsPath, projectId.ToString(), backupFileName);
            
            if (!File.Exists(backupFile))
            {
                _logger.LogWarning("Backup file not found: {BackupFile}", backupFile);
                return null;
            }
            
            // Create a backup of current state before restoring
            await CreateBackupAsync(projectId, "pre_restore", ct).ConfigureAwait(false);
            
            // Restore backup
            var backupContent = await File.ReadAllTextAsync(backupFile, ct).ConfigureAwait(false);
            await SaveProjectFileAsync(projectId, backupContent, ct).ConfigureAwait(false);
            
            _logger.LogInformation("Restored backup for project {ProjectId}: {BackupFile}", projectId, backupFileName);
            return backupContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup for project: {ProjectId}", projectId);
            throw;
        }
    }

    #endregion

    #region IStorageService Implementation

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            var fileId = Guid.NewGuid();
            var extension = Path.GetExtension(fileName);
            var safeFileName = $"{fileId}{extension}";
            var mediaPath = await GetWorkspacePathAsync(WorkspaceFolders.Media, ct).ConfigureAwait(false);
            var fullPath = Path.Combine(mediaPath, safeFileName);

            using (var fileStreamOut = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(fileStreamOut, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Uploaded file: {FileName} -> {Path}", fileName, fullPath);
            return $"local://media/{safeFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> UploadChunkAsync(Guid sessionId, int chunkIndex, Stream chunkStream, CancellationToken ct = default)
    {
        try
        {
            if (!_uploadSessions.TryGetValue(sessionId, out var session))
            {
                session = new ChunkUploadSession();
                _uploadSessions[sessionId] = session;
            }

            var tempPath = await GetWorkspacePathAsync(WorkspaceFolders.Temp, ct).ConfigureAwait(false);
            var chunkPath = Path.Combine(tempPath, $"{sessionId}_chunk_{chunkIndex}");
            
            using (var fileStream = File.Create(chunkPath))
            {
                await chunkStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
            }

            session.ChunkPaths.Add(chunkPath);
            _logger.LogDebug("Uploaded chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            
            return chunkPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            throw;
        }
    }

    public async Task<string> CompleteChunkedUploadAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_uploadSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Upload session {sessionId} not found");
            }

            var sortedChunks = session.ChunkPaths.OrderBy(p => p).ToList();
            var fileId = Guid.NewGuid();
            var mediaPath = await GetWorkspacePathAsync(WorkspaceFolders.Media, ct).ConfigureAwait(false);
            var finalPath = Path.Combine(mediaPath, fileId.ToString());
            
            using (var finalStream = File.Create(finalPath))
            {
                foreach (var chunkPath in sortedChunks)
                {
                    using (var chunkStream = File.OpenRead(chunkPath))
                    {
                        await chunkStream.CopyToAsync(finalStream, ct).ConfigureAwait(false);
                    }
                    File.Delete(chunkPath);
                }
            }

            _uploadSessions.TryRemove(sessionId, out _);
            _logger.LogInformation("Completed chunked upload for session {SessionId}", sessionId);

            return $"local://media/{Path.GetFileName(finalPath)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chunked upload for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException($"File not found: {blobUrl}");
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = File.OpenRead(localPath))
            {
                await fileStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task DeleteFileAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
                _logger.LogInformation("Deleted file: {BlobUrl}", blobUrl);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(string blobUrl, TimeSpan expiresIn, CancellationToken ct = default)
    {
        return Task.FromResult(blobUrl);
    }

    public Task<long> GetFileSizeAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            var fileInfo = new FileInfo(localPath);
            return Task.FromResult(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            return Task.FromResult(File.Exists(localPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {BlobUrl}", blobUrl);
            return Task.FromResult(false);
        }
    }

    public async Task<string> CopyFileAsync(string sourceBlobUrl, string destinationFileName, CancellationToken ct = default)
    {
        try
        {
            var sourcePath = ConvertBlobUrlToPath(sourceBlobUrl);
            var destId = Guid.NewGuid();
            var extension = Path.GetExtension(destinationFileName);
            var mediaPath = await GetWorkspacePathAsync(WorkspaceFolders.Media, ct).ConfigureAwait(false);
            var destPath = Path.Combine(mediaPath, $"{destId}{extension}");

            File.Copy(sourcePath, destPath);
            _logger.LogInformation("Copied file from {Source} to {Destination}", sourceBlobUrl, destPath);

            return $"local://media/{Path.GetFileName(destPath)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file from {Source}", sourceBlobUrl);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private string ConvertBlobUrlToPath(string blobUrl)
    {
        if (blobUrl.StartsWith("local://"))
        {
            var relativePath = blobUrl.Substring("local://".Length);
            var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                var folder = parts[0];
                var fileName = string.Join("/", parts.Skip(1));
                
                return folder.ToLower() switch
                {
                    "media" => Path.Combine(_storageRoot, WorkspaceFolders.Media, fileName),
                    "thumbnails" => Path.Combine(_storageRoot, WorkspaceFolders.Thumbnails, fileName),
                    "exports" => Path.Combine(_storageRoot, WorkspaceFolders.Exports, fileName),
                    "previews" => Path.Combine(_storageRoot, WorkspaceFolders.Previews, fileName),
                    _ => Path.Combine(_storageRoot, relativePath)
                };
            }
        }
        
        return blobUrl;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}
