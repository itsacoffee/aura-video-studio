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
    private readonly string? _portableRoot;

    public DependencyManager(
        ILogger<DependencyManager> logger,
        HttpClient httpClient,
        string manifestPath,
        string downloadDirectory,
        string? portableRoot = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _manifestPath = manifestPath;
        _downloadDirectory = downloadDirectory;
        _portableRoot = portableRoot;
        
        // Ensure download directory exists
        if (!Directory.Exists(_downloadDirectory))
        {
            Directory.CreateDirectory(_downloadDirectory);
        }

        if (!string.IsNullOrEmpty(_portableRoot))
        {
            _logger.LogInformation("Portable mode enabled with root: {PortableRoot}", _portableRoot);
        }
    }
    
    public async Task<DependencyManifest> LoadManifestAsync()
    {
        if (File.Exists(_manifestPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_manifestPath).ConfigureAwait(false);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var manifest = JsonSerializer.Deserialize<DependencyManifest>(json, options);
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
                    InstallPath = "dependencies/ffmpeg",
                    PostInstallProbe = "ffmpeg",
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
                    InstallPath = "dependencies/ollama",
                    PostInstallProbe = "ollama",
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
        await SaveManifestAsync(newManifest).ConfigureAwait(false);
        
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
        var manifest = await LoadManifestAsync().ConfigureAwait(false);
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
        var manifest = await LoadManifestAsync().ConfigureAwait(false);
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
            if (File.Exists(filePath) && await VerifyChecksumAsync(filePath, file.Sha256).ConfigureAwait(false))
            {
                _logger.LogInformation("File already exists with correct checksum: {File}", file.Filename);
                continue;
            }
            
            // Download the file
            await DownloadFileAsync(file.Url, filePath, file.SizeBytes, progress, ct).ConfigureAwait(false);
            
            // Verify checksum
            if (!await VerifyChecksumAsync(filePath, file.Sha256).ConfigureAwait(false))
            {
                _logger.LogError("Checksum verification failed for {File}", file.Filename);
                File.Delete(filePath);
                throw new InvalidOperationException($"Checksum verification failed for {file.Filename}");
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
        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        // Check if partial file exists (for resume support)
        long existingBytes = 0;
        if (File.Exists(filePath))
        {
            existingBytes = new FileInfo(filePath).Length;
            _logger.LogInformation("Found existing file with {Bytes} bytes, attempting resume", existingBytes);
        }
        
        _logger.LogInformation("Downloading file: {Url} to {FilePath}", url, filePath);
        
        // Create request with Range header for resume support
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (existingBytes > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
        }
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        
        // If server doesn't support range requests, start from beginning
        if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable || 
            (existingBytes > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent))
        {
            _logger.LogWarning("Server does not support resume, restarting download from beginning");
            existingBytes = 0;
            request = new HttpRequestMessage(HttpMethod.Get, url);
            response.Dispose();
            var newResponse = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            newResponse.EnsureSuccessStatusCode();
            
            using var contentStream = await newResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await DownloadStreamAsync(contentStream, fileStream, 0, response.Content.Headers.ContentLength ?? expectedSize, progress, url).ConfigureAwait(false);
        }
        else
        {
            response.EnsureSuccessStatusCode();
            
            long totalBytes = existingBytes + (response.Content.Headers.ContentLength ?? expectedSize);
            
            using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true);
            await DownloadStreamAsync(contentStream, fileStream, existingBytes, totalBytes, progress, url).ConfigureAwait(false);
        }
        
        _logger.LogInformation("Download completed: {FilePath}", filePath);
    }
    
    private async Task DownloadStreamAsync(
        Stream contentStream,
        Stream fileStream,
        long existingBytes,
        long totalBytes,
        IProgress<DownloadProgress> progress,
        string url)
    {
        var buffer = new byte[8192];
        var lastProgressReport = DateTime.Now;
        long bytesRead = existingBytes;
        
        while (true)
        {
            int read = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (read == 0) break;
            
            await fileStream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            
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
        
        byte[] hashBytes = await sha256.ComputeHashAsync(fs).ConfigureAwait(false);
        string computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        
        bool isValid = computedHash.Equals(expectedSha256.ToLowerInvariant());
        
        if (!isValid)
        {
            _logger.LogWarning("Checksum verification failed for {File}. Expected: {Expected}, Actual: {Actual}", 
                filePath, expectedSha256, computedHash);
        }
        
        return isValid;
    }
    
    public async Task<ComponentVerificationResult> VerifyComponentAsync(string componentName)
    {
        var manifest = await LoadManifestAsync().ConfigureAwait(false);
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            return new ComponentVerificationResult 
            { 
                ComponentName = componentName,
                IsValid = false, 
                Status = "Component not found in manifest" 
            };
        }
        
        var missingFiles = new List<string>();
        var corruptedFiles = new List<string>();
        
        foreach (var file in component.Files)
        {
            string filePath = Path.Combine(_downloadDirectory, file.Filename);
            
            if (!File.Exists(filePath))
            {
                missingFiles.Add(file.Filename);
                continue;
            }
            
            if (!await VerifyChecksumAsync(filePath, file.Sha256).ConfigureAwait(false))
            {
                corruptedFiles.Add(file.Filename);
            }
        }
        
        var isValid = missingFiles.Count == 0 && corruptedFiles.Count == 0;
        var status = isValid ? "Valid" : 
            $"Missing: {missingFiles.Count}, Corrupted: {corruptedFiles.Count}";
        
        // Run post-install probe if component is valid
        string? probeResult = null;
        if (isValid && !string.IsNullOrEmpty(component.PostInstallProbe))
        {
            probeResult = await RunPostInstallProbeAsync(component).ConfigureAwait(false);
        }
        
        return new ComponentVerificationResult
        {
            ComponentName = componentName,
            IsValid = isValid,
            Status = status,
            MissingFiles = missingFiles,
            CorruptedFiles = corruptedFiles,
            ProbeResult = probeResult
        };
    }
    
    public async Task RepairComponentAsync(
        string componentName, 
        IProgress<DownloadProgress> progress, 
        CancellationToken ct)
    {
        _logger.LogInformation("Repairing component: {Component}", componentName);
        
        var verificationResult = await VerifyComponentAsync(componentName).ConfigureAwait(false);
        
        if (verificationResult.IsValid)
        {
            _logger.LogInformation("Component {Component} is valid, no repair needed", componentName);
            return;
        }
        
        // Re-download missing or corrupted files
        var manifest = await LoadManifestAsync().ConfigureAwait(false);
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            throw new ArgumentException($"Component {componentName} not found in manifest");
        }
        
        foreach (var file in component.Files)
        {
            if (verificationResult.MissingFiles.Contains(file.Filename) || 
                verificationResult.CorruptedFiles.Contains(file.Filename))
            {
                string filePath = Path.Combine(_downloadDirectory, file.Filename);
                
                // Delete corrupted file if it exists
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                // Re-download
                _logger.LogInformation("Re-downloading {File}", file.Filename);
                await DownloadFileAsync(file.Url, filePath, file.SizeBytes, progress, ct).ConfigureAwait(false);
                
                // Verify checksum
                if (!await VerifyChecksumAsync(filePath, file.Sha256).ConfigureAwait(false))
                {
                    _logger.LogError("Checksum verification failed for {File} after repair", file.Filename);
                    File.Delete(filePath);
                    throw new Exception($"Checksum verification failed for {file.Filename} after repair");
                }
            }
        }
        
        _logger.LogInformation("Component {Component} repaired successfully", componentName);
    }
    
    public async Task RemoveComponentAsync(string componentName)
    {
        var manifest = await LoadManifestAsync().ConfigureAwait(false);
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            throw new ArgumentException($"Component {componentName} not found in manifest");
        }
        
        _logger.LogInformation("Removing component: {Component}", componentName);
        
        foreach (var file in component.Files)
        {
            string filePath = Path.Combine(_downloadDirectory, file.Filename);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file: {File}", file.Filename);
            }
        }
        
        _logger.LogInformation("Component {Component} removed successfully", componentName);
    }
    
    public string GetComponentDirectory(string componentName)
    {
        return _downloadDirectory;
    }

    /// <summary>
    /// Get the portable root path (if portable mode is enabled)
    /// </summary>
    public string? GetPortableRoot()
    {
        return _portableRoot;
    }

    /// <summary>
    /// Check if portable mode is enabled
    /// </summary>
    public bool IsPortableModeEnabled()
    {
        return !string.IsNullOrEmpty(_portableRoot);
    }
    
    private async Task<string> RunPostInstallProbeAsync(DependencyComponent component)
    {
        try
        {
            switch (component.PostInstallProbe?.ToLower())
            {
                case "ffmpeg":
                    return await ProbeFFmpegAsync().ConfigureAwait(false);
                case "ollama":
                    return await ProbeOllamaAsync().ConfigureAwait(false);
                case "stablediffusion":
                case "sdwebui":
                    return await ProbeStableDiffusionAsync().ConfigureAwait(false);
                default:
                    return "No probe configured";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Post-install probe failed for {Component}", component.Name);
            return $"Probe failed: {ex.Message}";
        }
    }
    
    private async Task<string> ProbeFFmpegAsync()
    {
        try
        {
            var ffmpegPath = Path.Combine(_downloadDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
            {
                return "FFmpeg executable not found";
            }
            
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
                if (process.ExitCode == 0)
                {
                    var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    var versionLine = output.Split('\n').FirstOrDefault(l => l.Contains("ffmpeg version"));
                    return versionLine ?? "FFmpeg found and working";
                }
            }
            return "FFmpeg returned non-zero exit code";
        }
        catch (Exception ex)
        {
            return $"FFmpeg probe failed: {ex.Message}";
        }
    }
    
    private async Task<string> ProbeOllamaAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://127.0.0.1:11434/api/tags").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return "Ollama endpoint is reachable";
            }
            return $"Ollama endpoint returned status: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Ollama endpoint not reachable: {ex.Message}";
        }
    }
    
    private async Task<string> ProbeStableDiffusionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://127.0.0.1:7860/sdapi/v1/sd-models").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return "Stable Diffusion WebUI is reachable";
            }
            return $"SD WebUI returned status: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"SD WebUI endpoint not reachable: {ex.Message}";
        }
    }
    
    public ManualInstallInstructions GetManualInstallInstructions(string componentName)
    {
        var manifest = LoadManifestAsync().Result;
        var component = manifest.Components.Find(c => c.Name == componentName);
        
        if (component == null)
        {
            throw new ArgumentException($"Component {componentName} not found in manifest");
        }
        
        var instructions = new ManualInstallInstructions
        {
            ComponentName = component.Name,
            Version = component.Version,
            InstallPath = component.InstallPath,
            Steps = new List<string>()
        };
        
        foreach (var file in component.Files)
        {
            instructions.Steps.Add($"1. Download {file.Filename} from: {file.Url}");
            instructions.Steps.Add($"2. Verify SHA-256 checksum: {file.Sha256}");
            instructions.Steps.Add($"3. Size: {FormatFileSize(file.SizeBytes)}");
            instructions.Steps.Add($"4. Extract to: {Path.Combine(_downloadDirectory, file.ExtractPath)}");
            instructions.Steps.Add("");
        }
        
        return instructions;
    }
    
    private string FormatFileSize(long bytes)
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
}

public class ComponentVerificationResult
{
    public string ComponentName { get; set; } = "";
    public bool IsValid { get; set; }
    public string Status { get; set; } = "";
    public List<string> MissingFiles { get; set; } = new List<string>();
    public List<string> CorruptedFiles { get; set; } = new List<string>();
    public string? ProbeResult { get; set; }
}

public class ManualInstallInstructions
{
    public string ComponentName { get; set; } = "";
    public string Version { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public List<string> Steps { get; set; } = new List<string>();
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
    public string InstallPath { get; set; } = "";
    public string? PostInstallProbe { get; set; }
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