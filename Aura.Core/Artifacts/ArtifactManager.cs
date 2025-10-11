using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Artifacts;

/// <summary>
/// Manages job artifacts including storage paths, persistence, and retrieval.
/// Stores artifacts under %LOCALAPPDATA%/Aura/jobs/{jobId}/
/// </summary>
public class ArtifactManager
{
    private readonly ILogger<ArtifactManager> _logger;
    private readonly string _baseJobsPath;

    public ArtifactManager(ILogger<ArtifactManager> logger)
    {
        _logger = logger;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _baseJobsPath = Path.Combine(localAppData, "Aura", "jobs");
        
        // Ensure jobs directory exists
        Directory.CreateDirectory(_baseJobsPath);
        _logger.LogInformation("ArtifactManager initialized. Jobs path: {Path}", _baseJobsPath);
    }

    /// <summary>
    /// Gets the job directory path for a specific job.
    /// </summary>
    public string GetJobDirectory(string jobId)
    {
        var path = Path.Combine(_baseJobsPath, jobId);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Saves a job state to disk.
    /// </summary>
    public void SaveJob(Job job)
    {
        try
        {
            var jobDir = GetJobDirectory(job.Id);
            var jobFilePath = Path.Combine(jobDir, "job.json");
            
            var json = JsonSerializer.Serialize(job, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(jobFilePath, json);
            _logger.LogDebug("Saved job {JobId} to {Path}", job.Id, jobFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save job {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Loads a job from disk.
    /// </summary>
    public Job? LoadJob(string jobId)
    {
        try
        {
            var jobFilePath = Path.Combine(_baseJobsPath, jobId, "job.json");
            if (!File.Exists(jobFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(jobFilePath);
            return JsonSerializer.Deserialize<Job>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load job {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Lists all jobs, ordered by most recent first.
    /// </summary>
    public List<Job> ListJobs(int limit = 50)
    {
        try
        {
            var jobs = new List<Job>();
            
            if (!Directory.Exists(_baseJobsPath))
            {
                return jobs;
            }

            var jobDirs = Directory.GetDirectories(_baseJobsPath)
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .Take(limit);

            foreach (var dir in jobDirs)
            {
                var jobId = Path.GetFileName(dir);
                var job = LoadJob(jobId);
                if (job != null)
                {
                    jobs.Add(job);
                }
            }

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list jobs");
            return new List<Job>();
        }
    }

    /// <summary>
    /// Adds an artifact reference to a job.
    /// </summary>
    public JobArtifact CreateArtifact(string jobId, string name, string path, string type)
    {
        var fileInfo = new FileInfo(path);
        return new JobArtifact(
            Name: name,
            Path: path,
            Type: type,
            SizeBytes: fileInfo.Exists ? fileInfo.Length : 0,
            CreatedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Saves logs for a job.
    /// </summary>
    public void SaveLogs(string jobId, List<string> logs)
    {
        try
        {
            var jobDir = GetJobDirectory(jobId);
            var logsPath = Path.Combine(jobDir, "logs.txt");
            File.WriteAllLines(logsPath, logs);
            _logger.LogDebug("Saved logs for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save logs for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Cleans up old jobs beyond a certain limit.
    /// </summary>
    public void CleanupOldJobs(int keepLast = 100)
    {
        try
        {
            if (!Directory.Exists(_baseJobsPath))
            {
                return;
            }

            var jobDirs = Directory.GetDirectories(_baseJobsPath)
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .Skip(keepLast)
                .ToList();

            foreach (var dir in jobDirs)
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    _logger.LogInformation("Cleaned up old job directory: {Dir}", dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete job directory: {Dir}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old jobs");
        }
    }
}
