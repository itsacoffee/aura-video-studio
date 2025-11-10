using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for handling desktop setup operations like dependency installation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly ILogger<SetupController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly HttpClient _httpClient;

    public SetupController(
        ILogger<SetupController> logger,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _environment = environment;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for downloads
    }

    /// <summary>
    /// Download and install FFmpeg for the current platform
    /// </summary>
    [HttpPost("install-ffmpeg")]
    public async Task<IActionResult> InstallFFmpeg(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting FFmpeg installation for platform: {Platform}", RuntimeInformation.OSDescription);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await InstallFFmpegWindows(cancellationToken);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Ok(new
                {
                    success = false,
                    message = "Please install FFmpeg using Homebrew: brew install ffmpeg",
                    command = "brew install ffmpeg",
                    url = "https://formulae.brew.sh/formula/ffmpeg"
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Ok(new
                {
                    success = false,
                    message = "Please install FFmpeg using your package manager",
                    commands = new[]
                    {
                        "Ubuntu/Debian: sudo apt-get install ffmpeg",
                        "Fedora: sudo dnf install ffmpeg",
                        "Arch: sudo pacman -S ffmpeg"
                    },
                    url = "https://ffmpeg.org/download.html#build-linux"
                });
            }

            return BadRequest(new { error = "Unsupported platform" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install FFmpeg");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<IActionResult> InstallFFmpegWindows(CancellationToken cancellationToken)
    {
        try
        {
            // Use the official FFmpeg Windows builds from gyan.dev (trusted by the community)
            var downloadUrl = RuntimeInformation.OSArchitecture == Architecture.X64
                ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                : "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

            var dataPath = Environment.GetEnvironmentVariable("AURA_DATA_PATH") ?? 
                           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraVideoStudio");
            var ffmpegDir = Path.Combine(dataPath, "ffmpeg");
            var downloadPath = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");

            Directory.CreateDirectory(ffmpegDir);

            _logger.LogInformation("Downloading FFmpeg from {Url}", downloadUrl);

            // Download FFmpeg with progress reporting
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesDownloaded = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        bytesDownloaded += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((bytesDownloaded * 100) / totalBytes);
                            _logger.LogInformation("Download progress: {Progress}%", progress);
                        }
                    }
                }
            }

            _logger.LogInformation("Extracting FFmpeg to {Path}", ffmpegDir);

            // Extract ZIP
            ZipFile.ExtractToDirectory(downloadPath, ffmpegDir, overwriteFiles: true);

            // Find the ffmpeg.exe in the extracted directory (it's usually in a subdirectory)
            var ffmpegExePath = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (ffmpegExePath == null)
            {
                throw new FileNotFoundException("FFmpeg.exe not found in extracted files");
            }

            // Move ffmpeg.exe to the root of ffmpegDir for easier access
            var targetPath = Path.Combine(ffmpegDir, "ffmpeg.exe");
            if (ffmpegExePath != targetPath)
            {
                System.IO.File.Copy(ffmpegExePath, targetPath, overwrite: true);
            }

            // Also copy ffprobe.exe
            var ffprobeExePath = Directory.GetFiles(ffmpegDir, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (ffprobeExePath != null)
            {
                var ffprobeTargetPath = Path.Combine(ffmpegDir, "ffprobe.exe");
                if (ffprobeExePath != ffprobeTargetPath)
                {
                    System.IO.File.Copy(ffprobeExePath, ffprobeTargetPath, overwrite: true);
                }
            }

            // Cleanup
            System.IO.File.Delete(downloadPath);

            _logger.LogInformation("FFmpeg installed successfully at {Path}", targetPath);

            // Update PATH environment variable for the current process
            var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";
            if (!currentPath.Contains(ffmpegDir))
            {
                Environment.SetEnvironmentVariable("PATH", $"{currentPath};{ffmpegDir}", EnvironmentVariableTarget.Process);
            }

            // Verify installation
            var version = await GetFFmpegVersion(targetPath);

            return Ok(new
            {
                success = true,
                message = "FFmpeg installed successfully",
                path = targetPath,
                version = version
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install FFmpeg on Windows");
            throw;
        }
    }

    private async Task<string?> GetFFmpegVersion(string ffmpegPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse version from first line (e.g., "ffmpeg version 6.0 Copyright...")
            var firstLine = output.Split('\n').FirstOrDefault() ?? "";
            var versionMatch = System.Text.RegularExpressions.Regex.Match(firstLine, @"ffmpeg version ([\d.]+)");
            return versionMatch.Success ? versionMatch.Groups[1].Value : "unknown";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check Ollama availability
    /// </summary>
    [HttpGet("ollama-status")]
    public async Task<IActionResult> GetOllamaStatus(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.GetAsync("http://localhost:11434/api/tags", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return Ok(new
                {
                    available = true,
                    url = "http://localhost:11434",
                    models = content
                });
            }

            return Ok(new { available = false });
        }
        catch
        {
            return Ok(new { available = false });
        }
    }

    /// <summary>
    /// Get system information for setup wizard
    /// </summary>
    [HttpGet("system-info")]
    public IActionResult GetSystemInfo()
    {
        return Ok(new
        {
            platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
                      RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown",
            architecture = RuntimeInformation.OSArchitecture.ToString(),
            frameworkDescription = RuntimeInformation.FrameworkDescription,
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            osDescription = RuntimeInformation.OSDescription,
            paths = new
            {
                userData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                temp = Path.GetTempPath()
            }
        });
    }

    /// <summary>
    /// Validate system requirements
    /// </summary>
    [HttpGet("validate-requirements")]
    public IActionResult ValidateRequirements()
    {
        var requirements = new List<object>();

        // Check RAM
        // Note: This is a simplified check. In production, use performance counters
        var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var totalMemoryGB = totalMemory / (1024.0 * 1024.0 * 1024.0);
        requirements.Add(new
        {
            name = "RAM",
            required = "8 GB",
            detected = $"{totalMemoryGB:F1} GB",
            status = totalMemoryGB >= 8 ? "pass" : "warning",
            message = totalMemoryGB < 8 ? "8GB+ RAM recommended for video generation" : null
        });

        // Check disk space
        var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var drive = new DriveInfo(Path.GetPathRoot(dataPath) ?? "C:\\");
        var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
        requirements.Add(new
        {
            name = "Disk Space",
            required = "10 GB",
            detected = $"{freeSpaceGB:F1} GB free",
            status = freeSpaceGB >= 10 ? "pass" : "warning",
            message = freeSpaceGB < 10 ? "10GB+ free space recommended" : null
        });

        // Check .NET version
        requirements.Add(new
        {
            name = ".NET Runtime",
            required = ".NET 8.0",
            detected = Environment.Version.ToString(),
            status = "pass"
        });

        var allPass = requirements.All(r => ((dynamic)r).status == "pass");

        return Ok(new
        {
            allRequirementsMet = allPass,
            requirements = requirements
        });
    }
}
