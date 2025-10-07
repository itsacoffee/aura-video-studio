using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

public class DependencyManager
{
    private readonly ILogger<DependencyManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _manifestPath;
    private readonly string _downloadDirectory;

    public DependencyManager(
        ILogger<DependencyManager> logger,
        HttpClient httpClient,
        string manifestPath,
        string downloadDirectory)
    {
        _logger = logger;
        _httpClient = httpClient;
        _manifestPath = manifestPath;
        _downloadDirectory = downloadDirectory;
        
        // Ensure download directory exists
        if (!Directory.Exists(_downloadDirectory))
        {
            Directory.CreateDirectory(_downloadDirectory);
        }
    }
    
    public async Task<DependencyManifest> LoadManifestAsync()
    {
        if (File.Exists(_manifestPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_manifestPath);
                var manifest = JsonSerializer.Deserialize<DependencyManifest>(json);
                return manifest ?? new DependencyManifest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dependency manifest, creating new one");
            }
        }
        
        // Create a new manifest if it doesn't exist or couldn't be loaded
        var newManifest = new DependencyManifest
        {
            Components = new List<DependencyComponent>
            {
                new DependencyComponent
                {
                    Name = "FFmpeg",
                    Version = "6.0",
                    IsRequired = true,
                    Files = new List<DependencyFile>
                    {
                        new DependencyFile
                        {
                            Filename = "ffmpeg.exe",
                            Url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-latest-win64-gpl-6.0.zip",
                            Sha256 = "e25bfb9fc6986e5e42b0bcff64c20433171125243c5ebde1bbee29a4637434a9",
                            ExtractPath = "bin/ffmpeg.exe",
                            SizeBytes = 83558400
                        },
                        new DependencyFile
                        {
                            Filename = "ffprobe.exe",
                            Url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-latest-win64-gpl-6.0.zip",
                            Sha256 = "e25bfb9fc6986e5e42b0bcff64c20433171125243c5ebde1bbee29a4637434a9",
                            ExtractPath = "bin/ffprobe.exe",
                            SizeBytes = 83558400
                        }
                    }
                },
                new DependencyComponent
                {
                    Name = "Ollama",
                    Version = "0.1.19",
                    IsRequired = false,
                    Files = new List<DependencyFile>
                    {
                        new DependencyFile
                        {
                            Filename = "ollama-windows-amd64.zip",
                            Url = "https://github.com/ollama/ollama/releases/download/v0.1.19/ollama-windows-amd64.zip",
                            Sha256 = "f8e4078e510a4062186239fb8721b1a068a74fbe91e7bb7dff882191dff84e8a",
                            ExtractPath = "",
                            SizeBytes = 53620736
                        }
                    }
                }
            }
        };
        
        // Save the new manifest
        await SaveManifestAsync(newManifest);
        
        return newManifest;
    }
    
    private Task SaveManifestAsync(DependencyManifest manifest)
    {
        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        return File.WriteAllTextAsync(_manifestPath, json);
    }
    
    public async Task<bool> IsComponentInstalledAsync(string componentName)
    {
        var manifest = await LoadManifestAsync();
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            return false;
        }
        
        foreach (var file in component.Files)
        {
            string filePath = Path.Combine(_downloadDirectory, file.Filename);
            if (!File.Exists(filePath))
            {
                return false;
            }
            
            // Optionally verify checksum
        }
        
        return true;
    }
    
    public async Task DownloadComponentAsync(
        string componentName, 
        IProgress<DownloadProgress> progress, 
        CancellationToken ct)
    {
        var manifest = await LoadManifestAsync();
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            throw new ArgumentException($"Component {componentName} not found in manifest");
        }
        
        _logger.LogInformation("Downloading component: {Component} v{Version}", 
            component.Name, component.Version);
        
        foreach (var file in component.Files)
        {
            string filePath = Path.Combine(_downloadDirectory, file.Filename);
            
            // Check if file exists and has correct checksum
            if (File.Exists(filePath) && await VerifyChecksumAsync(filePath, file.Sha256))
            {
                _logger.LogInformation("File already exists with correct checksum: {File}", file.Filename);
                continue;
            }
            
            // Download the file
            await DownloadFileAsync(file.Url, filePath, file.SizeBytes, progress, ct);
            
            // Verify checksum
            if (!await VerifyChecksumAsync(filePath, file.Sha256))
            {
                _logger.LogError("Checksum verification failed for {File}", file.Filename);
                File.Delete(filePath);
                throw new Exception($"Checksum verification failed for {file.Filename}");
            }
            
            // Extract if needed
            if (file.Url.EndsWith(".zip"))
            {
                _logger.LogInformation("Extracting zip file: {File}", file.Filename);
                // Extract the file
                // In a real implementation, we would use System.IO.Compression.ZipFile
            }
        }
        
        _logger.LogInformation("Component download completed: {Component}", component.Name);
    }
    
    private async Task DownloadFileAsync(
        string url, 
        string filePath, 
        long expectedSize,
        IProgress<DownloadProgress> progress, 
        CancellationToken ct)
    {
        _logger.LogInformation("Downloading file: {Url} to {FilePath}", url, filePath);
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        // Use HttpClient to download the file
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        
        long totalBytes = response.Content.Headers.ContentLength ?? expectedSize;
        long bytesRead = 0;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        
        var buffer = new byte[8192];
        var lastProgressReport = DateTime.Now;
        
        while (true)
        {
            int read = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read == 0) break;
            
            await fileStream.WriteAsync(buffer, 0, read, ct);
            
            bytesRead += read;
            
            // Report progress no more than once per 100ms
            var now = DateTime.Now;
            if ((now - lastProgressReport).TotalMilliseconds >= 100)
            {
                lastProgressReport = now;
                
                float percentComplete = totalBytes > 0 
                    ? (float)bytesRead / totalBytes * 100 
                    : 0;
                
                progress.Report(new DownloadProgress(
                    bytesRead, 
                    totalBytes, 
                    percentComplete,
                    url));
            }
        }
        
        // Final progress report
        progress.Report(new DownloadProgress(
            bytesRead, 
            totalBytes, 
            100,
            url));
        
        _logger.LogInformation("Download completed: {FilePath}, {Bytes} bytes", filePath, bytesRead);
    }
    
    private async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256)
    {
        if (string.IsNullOrEmpty(expectedSha256))
        {
            _logger.LogWarning("No checksum provided for {File}, skipping verification", filePath);
            return true;
        }
        
        _logger.LogInformation("Verifying checksum for {File}", filePath);
        
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var sha256 = SHA256.Create();
        
        byte[] hashBytes = await sha256.ComputeHashAsync(fs);
        string computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        bool isValid = computedHash.Equals(expectedSha256.ToLowerInvariant());
        
        if (!isValid)
        {
            _logger.LogWarning("Checksum verification failed for {File}. Expected: {Expected}, Actual: {Actual}", 
                filePath, expectedSha256, computedHash);
        }
        
        return isValid;
    }
}

public class DependencyManifest
{
    public List<DependencyComponent> Components { get; set; } = new List<DependencyComponent>();
}

public class DependencyComponent
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsRequired { get; set; }
    public List<DependencyFile> Files { get; set; } = new List<DependencyFile>();
}

public class DependencyFile
{
    public string Filename { get; set; } = "";
    public string Url { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string ExtractPath { get; set; } = "";
    public long SizeBytes { get; set; }
}

public record DownloadProgress(
    long BytesDownloaded, 
    long TotalBytes, 
    float PercentComplete,
    string Url);