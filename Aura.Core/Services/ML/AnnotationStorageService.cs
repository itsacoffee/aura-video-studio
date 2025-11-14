using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// Service for storing and retrieving frame annotations for ML training
/// Uses per-user JSONL storage in AppData/Aura/ML/Annotations/{userId}/
/// </summary>
public class AnnotationStorageService
{
    private readonly ILogger<AnnotationStorageService> _logger;
    private readonly string _baseDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public AnnotationStorageService(ILogger<AnnotationStorageService> logger, string? baseDirectory = null)
    {
        _logger = logger;
        _baseDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aura", "ML", "Annotations");
    }

    /// <summary>
    /// Store annotations for a user
    /// </summary>
    public async Task StoreAnnotationsAsync(
        string userId, 
        IEnumerable<AnnotationRecord> annotations, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        }

        var annotationList = annotations.ToList();
        if (annotationList.Count == 0)
        {
            _logger.LogWarning("No annotations to store for user {UserId}", userId);
            return;
        }

        var userDirectory = GetUserDirectory(userId);
        Directory.CreateDirectory(userDirectory);

        var annotationsFile = Path.Combine(userDirectory, "annotations.jsonl");
        _logger.LogInformation("Storing {Count} annotations for user {UserId} to {Path}", 
            annotationList.Count, userId, annotationsFile);

        try
        {
            await using var writer = new StreamWriter(annotationsFile, append: true);
            foreach (var annotation in annotationList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                ValidateAnnotation(annotation);
                
                var json = JsonSerializer.Serialize(annotation, JsonOptions);
                await writer.WriteLineAsync(json).ConfigureAwait(false);
            }

            _logger.LogInformation("Successfully stored {Count} annotations for user {UserId}", 
                annotationList.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store annotations for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieve all annotations for a user
    /// </summary>
    public async Task<List<AnnotationRecord>> GetAnnotationsAsync(
        string userId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        }

        var annotationsFile = Path.Combine(GetUserDirectory(userId), "annotations.jsonl");
        
        if (!File.Exists(annotationsFile))
        {
            _logger.LogInformation("No annotations file found for user {UserId}", userId);
            return new List<AnnotationRecord>();
        }

        _logger.LogInformation("Loading annotations for user {UserId} from {Path}", userId, annotationsFile);

        var annotations = new List<AnnotationRecord>();
        
        try
        {
            using var reader = new StreamReader(annotationsFile);
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var annotation = JsonSerializer.Deserialize<AnnotationRecord>(line);
                    if (annotation != null)
                    {
                        annotations.Add(annotation);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize annotation line: {Line}", line);
                }
            }

            _logger.LogInformation("Loaded {Count} annotations for user {UserId}", annotations.Count, userId);
            return annotations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load annotations for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get statistics about stored annotations
    /// </summary>
    public async Task<AnnotationStats> GetStatsAsync(
        string userId, 
        CancellationToken cancellationToken = default)
    {
        var annotations = await GetAnnotationsAsync(userId, cancellationToken).ConfigureAwait(false);
        
        if (annotations.Count == 0)
        {
            return new AnnotationStats(
                UserId: userId,
                TotalAnnotations: 0,
                AverageRating: 0.0,
                OldestAnnotation: null,
                NewestAnnotation: null);
        }

        var averageRating = annotations.Average(a => a.Rating);
        var oldestAnnotation = annotations.Min(a => a.Timestamp);
        var newestAnnotation = annotations.Max(a => a.Timestamp);

        return new AnnotationStats(
            UserId: userId,
            TotalAnnotations: annotations.Count,
            AverageRating: averageRating,
            OldestAnnotation: oldestAnnotation,
            NewestAnnotation: newestAnnotation);
    }

    /// <summary>
    /// Clear all annotations for a user
    /// </summary>
    public Task ClearAnnotationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
        }

        var annotationsFile = Path.Combine(GetUserDirectory(userId), "annotations.jsonl");
        
        if (File.Exists(annotationsFile))
        {
            _logger.LogInformation("Clearing annotations for user {UserId}", userId);
            File.Delete(annotationsFile);
        }

        return Task.CompletedTask;
    }

    private string GetUserDirectory(string userId)
    {
        return Path.Combine(_baseDirectory, SanitizeUserId(userId));
    }

    private static string SanitizeUserId(string userId)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", userId.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private void ValidateAnnotation(AnnotationRecord annotation)
    {
        if (string.IsNullOrWhiteSpace(annotation.FramePath))
        {
            throw new ArgumentException("FramePath cannot be null or empty");
        }

        if (annotation.Rating < 0.0 || annotation.Rating > 1.0)
        {
            throw new ArgumentException($"Rating must be between 0.0 and 1.0, got {annotation.Rating}");
        }
    }
}

/// <summary>
/// Record for storing a single annotation
/// </summary>
public record AnnotationRecord(
    string FramePath,
    double Rating,
    DateTime Timestamp,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// Statistics about stored annotations
/// </summary>
public record AnnotationStats(
    string UserId,
    int TotalAnnotations,
    double AverageRating,
    DateTime? OldestAnnotation,
    DateTime? NewestAnnotation);
