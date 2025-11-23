using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Aura.Core.Data;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;

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
    private readonly AuraDbContext _dbContext;
    private readonly IFfmpegConfigurationService _ffmpegConfigService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GitHubReleaseResolver _releaseResolver;

    public SetupController(
        ILogger<SetupController> logger,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        AuraDbContext dbContext,
        IFfmpegConfigurationService ffmpegConfigService,
        ILoggerFactory loggerFactory,
        GitHubReleaseResolver releaseResolver)
    {
        _logger = logger;
        _environment = environment;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for downloads
        _dbContext = dbContext;
        _ffmpegConfigService = ffmpegConfigService;
        _loggerFactory = loggerFactory;
        _releaseResolver = releaseResolver;
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
                return await InstallFFmpegWindows(cancellationToken).ConfigureAwait(false);
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
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesDownloaded = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
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
            var version = await GetFFmpegVersion(targetPath).ConfigureAwait(false);

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
            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

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
            var response = await client.GetAsync("http://localhost:11434/api/tags", cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Get current system setup status
    /// </summary>
    [HttpGet("system-status")]
    public async Task<IActionResult> GetSystemStatus(CancellationToken cancellationToken)
    {
        try
        {
            // Query the database for setup status (default user)
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == "default", cancellationToken).ConfigureAwait(false);

            if (userSetup == null || !userSetup.Completed)
            {
                return Ok(new
                {
                    isComplete = false,
                    ffmpegPath = (string?)null,
                    outputDirectory = (string?)null
                });
            }

            // Parse wizard state JSON for paths if available
            string? ffmpegPath = null;
            string? outputDirectory = null;

            if (!string.IsNullOrEmpty(userSetup.WizardState))
            {
                try
                {
                    var state = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(userSetup.WizardState);
                    if (state != null)
                    {
                        if (state.TryGetValue("ffmpegPath", out var ffPath))
                        {
                            ffmpegPath = ffPath?.ToString();
                        }
                        if (state.TryGetValue("outputDirectory", out var outDir))
                        {
                            outputDirectory = outDir?.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse wizard state JSON");
                }
            }

            return Ok(new
            {
                isComplete = true,
                ffmpegPath = ffmpegPath,
                outputDirectory = outputDirectory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system status");
            return StatusCode(500, new { error = "Failed to retrieve system status", detail = ex.Message });
        }
    }

    /// <summary>
    /// Check FFmpeg installation status using FFmpegResolver
    /// </summary>
    [HttpGet("check-ffmpeg")]
    public async Task<IActionResult> CheckFFmpeg(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Checking FFmpeg installation status", correlationId);

            // Get effective FFmpeg configuration (uses FFmpegResolver internally)
            var config = await _ffmpegConfigService.GetEffectiveConfigurationAsync(cancellationToken).ConfigureAwait(false);

            if (config != null && config.LastValidationResult == FFmpegValidationResult.Ok && !string.IsNullOrEmpty(config.Path))
            {
                _logger.LogInformation("[{CorrelationId}] FFmpeg found at: {Path}, Version: {Version}",
                    correlationId, config.Path, config.Version ?? "unknown");

                return Ok(new
                {
                    isInstalled = true,
                    path = config.Path,
                    version = config.Version,
                    error = (string?)null,
                    source = config.Source,
                    correlationId
                });
            }
            else
            {
                var errorMessage = config?.LastValidationError ?? "FFmpeg not found. Install FFmpeg or configure the path in Settings.";
                _logger.LogWarning("[{CorrelationId}] FFmpeg not found or invalid: {Error}", correlationId, errorMessage);

                return Ok(new
                {
                    isInstalled = false,
                    path = (string?)null,
                    version = (string?)null,
                    error = errorMessage,
                    source = config?.Source ?? "None",
                    correlationId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error checking FFmpeg status", correlationId);
            return Ok(new
            {
                isInstalled = false,
                path = (string?)null,
                version = (string?)null,
                error = $"Error checking FFmpeg: {ex.Message}",
                source = "None",
                correlationId
            });
        }
    }

    /// <summary>
    /// Complete the first-run setup wizard
    /// Idempotent - multiple calls with same configuration result in success
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteSetup(
        [FromBody] SetupCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Starting setup completion, FFmpegPath: {FFmpegPath}, OutputDirectory: {OutputDirectory}",
                correlationId, request.FFmpegPath ?? "(none)", request.OutputDirectory ?? "(none)");

            var errors = new List<string>();

            // Validate output directory if provided
            string? expandedOutputDirectory = null;
            if (!string.IsNullOrEmpty(request.OutputDirectory))
            {
                if (!ValidateAndCreateDirectory(request.OutputDirectory, createIfMissing: true, out expandedOutputDirectory, out var dirError))
                {
                    errors.Add(dirError ?? "Output directory validation failed");
                    _logger.LogWarning("[{CorrelationId}] Output directory validation failed: {Error}", correlationId, dirError);
                }
                else
                {
                    _logger.LogInformation("[{CorrelationId}] Output directory validated successfully: {Path}", correlationId, expandedOutputDirectory);
                }
            }

            // Validate FFmpeg path if provided (allow null for patience policy)
            if (!string.IsNullOrEmpty(request.FFmpegPath))
            {
                if (!System.IO.File.Exists(request.FFmpegPath))
                {
                    errors.Add($"FFmpeg executable not found at: {request.FFmpegPath}");
                    _logger.LogWarning("[{CorrelationId}] FFmpeg validation failed: file not found", correlationId);
                }
                else
                {
                    // Test FFmpeg execution
                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = request.FFmpegPath,
                                Arguments = "-version",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        process.Start();
                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                        if (process.ExitCode != 0)
                        {
                            errors.Add("FFmpeg validation failed: executable returned error");
                            _logger.LogWarning("[{CorrelationId}] FFmpeg validation failed: exit code {ExitCode}", correlationId, process.ExitCode);
                        }
                        else
                        {
                            _logger.LogInformation("[{CorrelationId}] FFmpeg validated successfully", correlationId);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"FFmpeg validation failed: {ex.Message}");
                        _logger.LogWarning(ex, "[{CorrelationId}] FFmpeg validation failed: execution error", correlationId);
                    }
                }
            }
            else
            {
                _logger.LogInformation("[{CorrelationId}] FFmpeg path not provided, setup will complete without FFmpeg configuration", correlationId);
            }

            if (errors.Any())
            {
                _logger.LogWarning("[{CorrelationId}] Setup validation failed with {ErrorCount} error(s): {Errors}",
                    correlationId, errors.Count, string.Join("; ", errors));
                return Ok(new { success = false, errors = errors.ToArray(), correlationId });
            }

            // Find or create user setup record (idempotent operation)
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == "default", cancellationToken).ConfigureAwait(false);

            var isNewSetup = userSetup == null;

            if (userSetup == null)
            {
                userSetup = new UserSetupEntity
                {
                    UserId = "default",
                    Completed = true,
                    CompletedAt = DateTime.UtcNow,
                    Version = "1.0.0",
                    LastStep = 6,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.UserSetups.Add(userSetup);
                _logger.LogInformation("[{CorrelationId}] Creating new setup record for user 'default'", correlationId);
            }
            else
            {
                // Update existing record (idempotent - safe to call multiple times)
                userSetup.Completed = true;
                userSetup.CompletedAt = DateTime.UtcNow;
                userSetup.Version = "1.0.0";
                userSetup.LastStep = 6;
                userSetup.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("[{CorrelationId}] Updating existing setup record for user 'default' (idempotent operation)", correlationId);
            }

            // Store paths in wizard state JSON (use expanded paths)
            var wizardState = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(request.FFmpegPath))
            {
                wizardState["ffmpegPath"] = request.FFmpegPath;
            }
            if (!string.IsNullOrEmpty(expandedOutputDirectory))
            {
                wizardState["outputDirectory"] = expandedOutputDirectory;
            }
            userSetup.WizardState = System.Text.Json.JsonSerializer.Serialize(wizardState);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("[{CorrelationId}] Setup completed successfully for user 'default', IsNewSetup: {IsNewSetup}, FFmpegConfigured: {FFmpegConfigured}, WorkspaceConfigured: {WorkspaceConfigured}",
                correlationId, isNewSetup, !string.IsNullOrEmpty(request.FFmpegPath), !string.IsNullOrEmpty(request.OutputDirectory));

            return Ok(new { success = true, errors = Array.Empty<string>(), correlationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to complete setup", correlationId);
            return StatusCode(500, new {
                success = false,
                errors = new[] { "Failed to complete setup. Please try again or contact support if the problem persists." },
                correlationId
            });
        }
    }

    /// <summary>
    /// Check if a directory is valid and writable
    /// </summary>
    [HttpPost("check-directory")]
    public IActionResult CheckDirectory(
        [FromBody] DirectoryCheckRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Checking directory: {Path}", correlationId, request.Path);

            if (string.IsNullOrEmpty(request.Path))
            {
                return Ok(new { isValid = false, error = "Path cannot be empty", correlationId });
            }

            // Validate and create directory if needed
            if (!ValidateAndCreateDirectory(request.Path, createIfMissing: true, out var expandedPath, out var error))
            {
                _logger.LogWarning("[{CorrelationId}] Directory validation failed: {Error}", correlationId, error);
                return Ok(new { isValid = false, error = error, expandedPath = (string?)null, correlationId });
            }

            _logger.LogInformation("[{CorrelationId}] Directory validated successfully: {ExpandedPath}", correlationId, expandedPath);
            return Ok(new { isValid = true, error = (string?)null, expandedPath = expandedPath, correlationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to check directory", correlationId);
            return Ok(new { isValid = false, error = $"Failed to check directory: {ex.Message}", expandedPath = (string?)null, correlationId });
        }
    }

    /// <summary>
    /// Save wizard progress (for resume on interruption)
    /// </summary>
    [HttpPost("wizard/save-progress")]
    public async Task<IActionResult> SaveWizardProgress(
        [FromBody] WizardProgressRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Saving wizard progress for user: {UserId}, Step: {Step}, CorrelationId: {CorrelationId}",
                request.UserId ?? "default", request.CurrentStep, request.CorrelationId);

            var userId = request.UserId ?? "default";
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken).ConfigureAwait(false);

            if (userSetup == null)
            {
                userSetup = new UserSetupEntity
                {
                    UserId = userId,
                    LastStep = request.CurrentStep,
                    UpdatedAt = DateTime.UtcNow,
                    WizardState = System.Text.Json.JsonSerializer.Serialize(request.State)
                };
                _dbContext.UserSetups.Add(userSetup);
            }
            else
            {
                userSetup.LastStep = request.CurrentStep;
                userSetup.UpdatedAt = DateTime.UtcNow;
                userSetup.WizardState = System.Text.Json.JsonSerializer.Serialize(request.State);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                message = "Wizard progress saved successfully",
                correlationId = request.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wizard progress, CorrelationId: {CorrelationId}",
                request.CorrelationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to save wizard progress",
                detail = ex.Message,
                correlationId = request.CorrelationId
            });
        }
    }

    /// <summary>
    /// Get setup status (alias for wizard/status for backward compatibility)
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSetupStatus(
        [FromQuery] string? userId,
        CancellationToken cancellationToken)
    {
        return await GetWizardStatus(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get wizard status and saved progress
    /// </summary>
    [HttpGet("wizard/status")]
    public async Task<IActionResult> GetWizardStatus(
        [FromQuery] string? userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectiveUserId = userId ?? "default";
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == effectiveUserId, cancellationToken).ConfigureAwait(false);

            if (userSetup == null)
            {
                return Ok(new
                {
                    completed = false,
                    currentStep = 0,
                    state = (object?)null,
                    canResume = false,
                    lastUpdated = (DateTime?)null
                });
            }

            object? parsedState = null;
            if (!string.IsNullOrEmpty(userSetup.WizardState))
            {
                try
                {
                    parsedState = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(userSetup.WizardState);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse wizard state for user: {UserId}", effectiveUserId);
                }
            }

            return Ok(new
            {
                completed = userSetup.Completed,
                currentStep = userSetup.LastStep,
                state = parsedState,
                canResume = !userSetup.Completed && userSetup.LastStep > 0,
                lastUpdated = userSetup.UpdatedAt,
                completedAt = userSetup.CompletedAt,
                version = userSetup.Version
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wizard status for user: {UserId}", userId);
            return StatusCode(500, new
            {
                error = "Failed to retrieve wizard status",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Mark wizard as complete
    /// </summary>
    [HttpPost("wizard/complete")]
    public async Task<IActionResult> CompleteWizard(
        [FromBody] WizardCompleteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Completing wizard for user: {UserId}, CorrelationId: {CorrelationId}",
                request.UserId ?? "default", request.CorrelationId);

            var userId = request.UserId ?? "default";
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken).ConfigureAwait(false);

            if (userSetup == null)
            {
                userSetup = new UserSetupEntity
                {
                    UserId = userId,
                    Completed = true,
                    CompletedAt = DateTime.UtcNow,
                    Version = request.Version ?? "1.0.0",
                    LastStep = request.FinalStep,
                    UpdatedAt = DateTime.UtcNow,
                    SelectedTier = request.SelectedTier,
                    WizardState = System.Text.Json.JsonSerializer.Serialize(request.FinalState ?? new())
                };
                _dbContext.UserSetups.Add(userSetup);
            }
            else
            {
                userSetup.Completed = true;
                userSetup.CompletedAt = DateTime.UtcNow;
                userSetup.Version = request.Version ?? "1.0.0";
                userSetup.LastStep = request.FinalStep;
                userSetup.UpdatedAt = DateTime.UtcNow;
                userSetup.SelectedTier = request.SelectedTier;
                userSetup.WizardState = System.Text.Json.JsonSerializer.Serialize(request.FinalState ?? new());
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Wizard completed successfully for user: {UserId}", userId);

            return Ok(new
            {
                success = true,
                message = "Wizard completed successfully",
                correlationId = request.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete wizard, CorrelationId: {CorrelationId}",
                request.CorrelationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to complete wizard",
                detail = ex.Message,
                correlationId = request.CorrelationId
            });
        }
    }

    /// <summary>
    /// Reset wizard state (for testing or re-running wizard)
    /// </summary>
    [HttpPost("wizard/reset")]
    public async Task<IActionResult> ResetWizard(
        [FromBody] WizardResetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Resetting wizard for user: {UserId}, CorrelationId: {CorrelationId}",
                request.UserId ?? "default", request.CorrelationId);

            var userId = request.UserId ?? "default";
            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken).ConfigureAwait(false);

            if (userSetup != null)
            {
                if (request.PreserveData)
                {
                    userSetup.Completed = false;
                    userSetup.CompletedAt = null;
                    userSetup.LastStep = 0;
                    userSetup.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _dbContext.UserSetups.Remove(userSetup);
                }

                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Wizard reset successfully for user: {UserId}", userId);

            return Ok(new
            {
                success = true,
                message = "Wizard reset successfully",
                correlationId = request.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset wizard, CorrelationId: {CorrelationId}",
                request.CorrelationId);
            return StatusCode(500, new
            {
                success = false,
                error = "Failed to reset wizard",
                detail = ex.Message,
                correlationId = request.CorrelationId
            });
        }
    }

    /// <summary>
    /// Request models for setup endpoints
    /// </summary>
    public class SetupCompleteRequest
    {
        public string? FFmpegPath { get; set; }
        public string? OutputDirectory { get; set; }
    }

    public class DirectoryCheckRequest
    {
        public required string Path { get; set; }
    }

    public class WizardProgressRequest
    {
        public string? UserId { get; set; }
        public int CurrentStep { get; set; }
        public Dictionary<string, object> State { get; set; } = new();
        public string? CorrelationId { get; set; }
    }

    public class WizardCompleteRequest
    {
        public string? UserId { get; set; }
        public int FinalStep { get; set; }
        public string? Version { get; set; }
        public string? SelectedTier { get; set; }
        public Dictionary<string, object>? FinalState { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class WizardResetRequest
    {
        public string? UserId { get; set; }
        public bool PreserveData { get; set; } = false;
        public string? CorrelationId { get; set; }
    }

    public class SaveSetupApiKeysRequest
    {
        public required List<ApiKeyConfigDto> ApiKeys { get; set; }
        public bool AllowInvalid { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class ApiKeyConfigDto
    {
        public required string Provider { get; set; }
        public required string Key { get; set; }
        public bool IsValidated { get; set; }
    }

    /// <summary>
    /// Save API keys with optional validation bypass
    /// Allows users to save invalid keys with explicit acknowledgment
    /// </summary>
    [HttpPost("save-api-keys")]
    public async Task<IActionResult> SaveApiKeys(
        [FromBody] SaveSetupApiKeysRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = request.CorrelationId ?? HttpContext.TraceIdentifier;
            _logger.LogInformation("Saving API keys, AllowInvalid: {AllowInvalid}, Count: {Count}, CorrelationId: {CorrelationId}",
                request.AllowInvalid, request.ApiKeys.Count, correlationId);

            var warnings = new List<string>();
            var userId = "default";

            var userSetup = await _dbContext.UserSetups
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken).ConfigureAwait(false);

            if (userSetup == null)
            {
                userSetup = new UserSetupEntity
                {
                    UserId = userId,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.UserSetups.Add(userSetup);
            }

            var apiKeyState = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(userSetup.WizardState))
            {
                try
                {
                    var existing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(userSetup.WizardState);
                    if (existing != null)
                    {
                        apiKeyState = existing;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse existing wizard state");
                }
            }

            var apiKeysData = new Dictionary<string, Dictionary<string, object>>();
            if (apiKeyState.TryGetValue("apiKeys", out var existingKeysObj))
            {
                var existingKeysJson = System.Text.Json.JsonSerializer.Serialize(existingKeysObj);
                apiKeysData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(existingKeysJson)
                    ?? new Dictionary<string, Dictionary<string, object>>();
            }

            foreach (var keyConfig in request.ApiKeys)
            {
                var isValid = keyConfig.IsValidated;

                if (!isValid && !request.AllowInvalid)
                {
                    _logger.LogWarning("API key for {Provider} is invalid and AllowInvalid is false", keyConfig.Provider);
                    return BadRequest(new
                    {
                        success = false,
                        errorMessage = $"API key for {keyConfig.Provider} is invalid. Enable 'Allow Invalid' to save anyway.",
                        provider = keyConfig.Provider,
                        correlationId = correlationId
                    });
                }

                if (!isValid)
                {
                    warnings.Add($"{keyConfig.Provider}: Key saved but not validated");
                    _logger.LogInformation("Saving unvalidated key for {Provider}", keyConfig.Provider);
                }

                apiKeysData[keyConfig.Provider] = new Dictionary<string, object>
                {
                    ["key"] = keyConfig.Key,
                    ["isValidated"] = isValid,
                    ["savedAt"] = DateTime.UtcNow.ToString("o")
                };
            }

            apiKeyState["apiKeys"] = apiKeysData;
            userSetup.WizardState = System.Text.Json.JsonSerializer.Serialize(apiKeyState);
            userSetup.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("API keys saved successfully. Warnings: {WarningCount}, CorrelationId: {CorrelationId}",
                warnings.Count, correlationId);

            return Ok(new
            {
                success = true,
                warnings = warnings.Count > 0 ? warnings : null,
                correlationId = correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save API keys, CorrelationId: {CorrelationId}",
                request.CorrelationId ?? HttpContext.TraceIdentifier);
            return StatusCode(500, new
            {
                success = false,
                errorMessage = "Failed to save API keys",
                detail = ex.Message,
                correlationId = request.CorrelationId ?? HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get configuration status for the application
    /// </summary>
    [HttpGet("configuration-status")]
    public async Task<IActionResult> GetConfigurationStatus(CancellationToken cancellationToken)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("Getting configuration status, CorrelationId: {CorrelationId}", correlationId);

            var status = new
            {
                isConfigured = false,
                lastChecked = DateTime.UtcNow.ToString("o"),
                checks = new
                {
                    providerConfigured = false,
                    providerValidated = false,
                    workspaceCreated = true,
                    ffmpegDetected = false,
                    apiKeysValid = false
                },
                details = new
                {
                    configuredProviders = new List<string>(),
                    ffmpegPath = (string?)null,
                    ffmpegVersion = (string?)null,
                    workspacePath = (string?)null,
                    diskSpaceAvailable = 0,
                    gpuAvailable = false
                },
                issues = new List<object>()
            };

            await Task.CompletedTask.ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration status");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error getting configuration status",
                Status = 500,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Check if FFmpeg exists at the specified path and return version information
    /// </summary>
    [HttpPost("check-ffmpeg")]
    public async Task<IActionResult> CheckFFmpegPath(
        [FromBody] FFmpegPathRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Checking FFmpeg path: {Path}",
                correlationId, request.Path);

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return Ok(new {
                    found = false,
                    error = "Path cannot be empty",
                    correlationId
                });
            }

            var path = request.Path.Trim();

            // Check if file exists
            if (!System.IO.File.Exists(path))
            {
                _logger.LogInformation("[{CorrelationId}] FFmpeg not found at path: {Path}",
                    correlationId, path);
                return Ok(new {
                    found = false,
                    error = $"File not found at: {path}",
                    correlationId
                });
            }

            // Execute ffmpeg -version to verify it's valid
            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return Ok(new {
                    found = false,
                    error = "Failed to start FFmpeg process",
                    correlationId
                });
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("[{CorrelationId}] FFmpeg execution failed with exit code {ExitCode}: {Error}",
                    correlationId, process.ExitCode, errorOutput);
                return Ok(new {
                    found = false,
                    error = $"FFmpeg validation failed with exit code {process.ExitCode}",
                    correlationId
                });
            }

            var version = ParseFFmpegVersionString(output);

            _logger.LogInformation("[{CorrelationId}] FFmpeg validated successfully: {Path} (version: {Version})",
                correlationId, path, version);

            return Ok(new {
                found = true,
                path = path,
                version = version ?? "unknown",
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to check FFmpeg path: {Path}",
                correlationId, request.Path);
            return Ok(new {
                found = false,
                error = $"Error checking FFmpeg: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Save FFmpeg path to persistent configuration
    /// </summary>
    [HttpPost("save-ffmpeg-path")]
    public async Task<IActionResult> SaveFFmpegPath(
        [FromBody] SaveFFmpegPathRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Saving FFmpeg path to configuration: {Path}",
                correlationId, request.Path);

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new {
                    success = false,
                    error = "Path cannot be empty",
                    correlationId
                });
            }

            // Validate that the path exists and is executable
            if (!System.IO.File.Exists(request.Path))
            {
                return BadRequest(new {
                    success = false,
                    error = $"File not found at: {request.Path}",
                    correlationId
                });
            }

            // Get current configuration and update path
            var config = await _ffmpegConfigService.GetEffectiveConfigurationAsync(cancellationToken).ConfigureAwait(false);
            config.Path = request.Path;
            config.Mode = FFmpegMode.Custom;
            config.Source = "Configured";

            // Update configuration
            await _ffmpegConfigService.UpdateConfigurationAsync(config, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("[{CorrelationId}] FFmpeg path saved successfully: {Path}",
                correlationId, request.Path);

            return Ok(new {
                success = true,
                path = request.Path,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to save FFmpeg path", correlationId);
            return StatusCode(500, new {
                success = false,
                error = $"Failed to save FFmpeg path: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Configure FFmpeg path with validation and persistence (called by Electron after detection)
    /// </summary>
    [HttpPost("configure-ffmpeg")]
    public async Task<IActionResult> ConfigureFFmpeg(
        [FromBody] ConfigureFFmpegRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Configuring FFmpeg path: {Path}, Source: {Source}",
                correlationId, request.Path, request.Source ?? "Unknown");

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid FFmpeg Path",
                    Status = 400,
                    Detail = "Path cannot be empty",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            // Validate that the path exists
            if (!System.IO.File.Exists(request.Path))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "FFmpeg Not Found",
                    Status = 400,
                    Detail = $"File not found at: {request.Path}",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            // Test FFmpeg execution
            string? version = null;
            string? validationOutput = null;
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = request.Path,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "FFmpeg Validation Failed",
                        Status = 400,
                        Detail = "Failed to start FFmpeg process",
                        Extensions = { ["correlationId"] = correlationId }
                    });
                }

                validationOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogWarning("[{CorrelationId}] FFmpeg validation failed with exit code {ExitCode}: {Error}",
                        correlationId, process.ExitCode, errorOutput);

                    return BadRequest(new ProblemDetails
                    {
                        Title = "FFmpeg Validation Failed",
                        Status = 400,
                        Detail = $"FFmpeg validation failed with exit code {process.ExitCode}",
                        Extensions = { ["correlationId"] = correlationId }
                    });
                }

                version = ParseFFmpegVersionString(validationOutput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Failed to validate FFmpeg executable", correlationId);
                return BadRequest(new ProblemDetails
                {
                    Title = "FFmpeg Validation Error",
                    Status = 400,
                    Detail = $"Error validating FFmpeg: {ex.Message}",
                    Extensions = { ["correlationId"] = correlationId }
                });
            }

            // Determine detection source based on request source
            var detectionSource = request.Source?.ToLowerInvariant() switch
            {
                "electrondetection" => DetectionSource.ElectronDetection,
                "environment" => DetectionSource.Environment,
                "userconfigured" => DetectionSource.UserConfigured,
                "managed" => DetectionSource.Managed,
                "system" => DetectionSource.System,
                _ => DetectionSource.System
            };

            // Create configuration with validation results
            var config = new FFmpegConfiguration
            {
                Mode = FFmpegMode.Custom,
                Path = request.Path,
                Version = version,
                LastValidatedAt = DateTime.UtcNow,
                LastValidationResult = FFmpegValidationResult.Ok,
                Source = request.Source ?? "Configured",
                DetectionSourceType = detectionSource,
                LastDetectedPath = request.Path,
                LastDetectedAt = DateTime.UtcNow,
                ValidationOutput = validationOutput
            };

            // Persist configuration
            await _ffmpegConfigService.UpdateConfigurationAsync(config, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("[{CorrelationId}] FFmpeg configured successfully: {Path} (version: {Version})",
                correlationId, request.Path, version ?? "unknown");

            return Ok(new
            {
                success = true,
                path = request.Path,
                version = version ?? "unknown",
                source = request.Source,
                correlationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to configure FFmpeg", correlationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Configuration Error",
                Status = 500,
                Detail = $"Failed to configure FFmpeg: {ex.Message}",
                Extensions = { ["correlationId"] = correlationId }
            });
        }
    }

    /// <summary>
    /// Parse FFmpeg version from -version output
    /// </summary>
    private static string? ParseFFmpegVersionString(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        // Parse version from first line (e.g., "ffmpeg version 6.0 Copyright...")
        var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var versionMatch = System.Text.RegularExpressions.Regex.Match(firstLine, @"ffmpeg version ([\d.]+[\w-]*)");
        return versionMatch.Success ? versionMatch.Groups[1].Value : null;
    }

    /// <summary>
    /// Expand environment variables in a path string, supporting both Windows (%VAR%) and Unix (~, $VAR) syntax
    /// </summary>
    private static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var expandedPath = path;

        // Handle Unix home directory expansion (~)
        if (expandedPath.StartsWith("~/") || expandedPath == "~")
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (expandedPath == "~")
            {
                expandedPath = homeDir;
            }
            else if (expandedPath.Length >= 2)
            {
                expandedPath = Path.Combine(homeDir, expandedPath[2..]);
            }
        }

        // Expand Windows environment variables (%USERPROFILE%) and Unix variables ($HOME)
        expandedPath = Environment.ExpandEnvironmentVariables(expandedPath);

        // Normalize path separators for the current platform
        expandedPath = Path.GetFullPath(expandedPath);

        return expandedPath;
    }

    /// <summary>
    /// Validate and optionally create a directory path
    /// </summary>
    /// <param name="path">The path to validate (may contain environment variables)</param>
    /// <param name="createIfMissing">Whether to create the directory if it doesn't exist</param>
    /// <param name="expandedPath">The expanded path that was validated</param>
    /// <param name="error">Error message if validation fails</param>
    /// <returns>True if path is valid and writable, false otherwise</returns>
    private bool ValidateAndCreateDirectory(string path, bool createIfMissing, out string expandedPath, out string? error)
    {
        error = null;
        expandedPath = path;
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            // Expand environment variables
            expandedPath = ExpandEnvironmentVariables(path);

            _logger.LogInformation("[{CorrelationId}] Validating directory path. Original: {OriginalPath}, Expanded: {ExpandedPath}",
                correlationId, path, expandedPath);

            // Check if directory exists
            if (!Directory.Exists(expandedPath))
            {
                if (createIfMissing)
                {
                    // Try to create the directory
                    try
                    {
                        Directory.CreateDirectory(expandedPath);
                        _logger.LogInformation("[{CorrelationId}] Created directory: {Path}", correlationId, expandedPath);
                    }
                    catch (Exception ex)
                    {
                        error = $"Failed to create directory '{expandedPath}': {ex.Message}";
                        _logger.LogWarning(ex, "[{CorrelationId}] Failed to create directory: {Path}", correlationId, expandedPath);
                        return false;
                    }
                }
                else
                {
                    error = $"Directory does not exist: {expandedPath}";
                    if (expandedPath != path)
                    {
                        error += $" (expanded from: {path})";
                    }
                    return false;
                }
            }

            // Test write permissions
            try
            {
                var testFile = Path.Combine(expandedPath, $".aura-test-{Guid.NewGuid()}.tmp");
                System.IO.File.WriteAllText(testFile, "test");
                System.IO.File.Delete(testFile);
                _logger.LogInformation("[{CorrelationId}] Directory write test successful: {Path}", correlationId, expandedPath);
            }
            catch (Exception ex)
            {
                error = $"Directory is not writable: {ex.Message}";
                _logger.LogWarning(ex, "[{CorrelationId}] Directory write test failed: {Path}", correlationId, expandedPath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to validate directory: {ex.Message}";
            _logger.LogError(ex, "[{CorrelationId}] Unexpected error validating directory: {Path}", correlationId, path);
            return false;
        }
    }

    /// <summary>
    /// Request models for setup endpoints
    /// </summary>
    public class FFmpegPathRequest
    {
        public required string Path { get; set; }
    }

    public class SaveFFmpegPathRequest
    {
        public required string Path { get; set; }
    }

    public class ConfigureFFmpegRequest
    {
        public required string Path { get; set; }
        public string? Source { get; set; }
    }

    /// <summary>
    /// Install Piper TTS for Windows
    /// </summary>
    [HttpPost("install-piper")]
    public async Task<IActionResult> InstallPiper(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Starting Piper TTS installation for platform: {Platform}",
                correlationId, RuntimeInformation.OSDescription);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Ok(new
                {
                    success = false,
                    message = "Piper TTS managed installation is currently only available for Windows. Please install manually from https://github.com/rhasspy/piper/releases",
                    url = "https://github.com/rhasspy/piper/releases"
                });
            }

            return await InstallPiperWindows(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to install Piper TTS", correlationId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<IActionResult> InstallPiperWindows(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            // Resolve latest Piper TTS release URL dynamically using GitHub API
            var downloadUrl = await _releaseResolver.ResolveLatestAssetUrlAsync(
                "rhasspy/piper",
                "*windows*amd64*.tar.gz",
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(downloadUrl))
            {
                // Fallback to manual instructions if API resolution fails
                _logger.LogWarning("[{CorrelationId}] Failed to resolve Piper TTS download URL from GitHub API, providing manual instructions", correlationId);
                return Ok(new
                {
                    success = false,
                    message = "Unable to automatically resolve the latest Piper TTS download URL. Please download manually.",
                    requiresManualInstall = true,
                    downloadUrl = "https://github.com/rhasspy/piper/releases/latest",
                    instructions = new[]
                    {
                        "1. Visit https://github.com/rhasspy/piper/releases/latest",
                        "2. Download piper_windows_amd64.tar.gz (or the latest Windows x64 release)",
                        "3. Extract the archive using 7-Zip, WinRAR, or Windows 11's built-in extraction",
                        "4. Copy piper.exe to the installation directory",
                        "5. Click 'Re-scan' to detect the installation"
                    }
                });
            }

            var dataPath = Environment.GetEnvironmentVariable("AURA_DATA_PATH") ??
                           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraVideoStudio");
            var piperDir = Path.Combine(dataPath, "piper");
            var downloadPath = Path.Combine(Path.GetTempPath(), "piper.tar.gz");

            Directory.CreateDirectory(piperDir);

            _logger.LogInformation("[{CorrelationId}] Resolved Piper TTS download URL: {Url}", correlationId, downloadUrl);

            // Download Piper
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesDownloaded = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                        bytesDownloaded += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)((bytesDownloaded * 100) / totalBytes);
                            _logger.LogInformation("[{CorrelationId}] Download progress: {Progress}%", correlationId, progress);
                        }
                    }
                }
            }

            _logger.LogInformation("[{CorrelationId}] Extracting Piper TTS to {Path}", correlationId, piperDir);

            // Extract TAR.GZ using SharpCompress (if available) or provide manual instructions
            // For Windows 11, we'll try to use built-in PowerShell or provide clear instructions
            string? piperExePath = null;

            try
            {
                // Try using PowerShell to extract TAR.GZ (Windows 10 1803+ and Windows 11 have built-in tar)
                using var tarProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $"-xzf \"{downloadPath}\" -C \"{piperDir}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = piperDir
                    }
                };

                tarProcess.Start();
                var tarOutput = await tarProcess.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                var tarError = await tarProcess.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                await tarProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (tarProcess.ExitCode == 0)
                {
                    _logger.LogInformation("[{CorrelationId}] Successfully extracted using tar command", correlationId);
                    piperExePath = Directory.GetFiles(piperDir, "piper.exe", SearchOption.AllDirectories).FirstOrDefault();
                }
                else
                {
                    _logger.LogWarning("[{CorrelationId}] tar command failed: {Error}", correlationId, tarError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{CorrelationId}] tar command not available, will provide manual instructions", correlationId);
            }

            // If extraction failed, provide manual instructions with download link
            if (piperExePath == null)
            {
                // Cleanup failed extraction attempt and downloaded file
                try
                {
                    if (Directory.Exists(piperDir))
                    {
                        Directory.Delete(piperDir, recursive: true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                // Cleanup downloaded file
                try
                {
                    if (System.IO.File.Exists(downloadPath))
                    {
                        System.IO.File.Delete(downloadPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{CorrelationId}] Failed to cleanup downloaded file: {Path}", correlationId, downloadPath);
                }

                return Ok(new
                {
                    success = false,
                    message = "Automatic extraction is not available on this system. Please download and extract Piper manually.",
                    requiresManualInstall = true,
                    downloadUrl = "https://github.com/rhasspy/piper/releases/latest",
                    instructions = new[]
                    {
                        "1. Download piper_windows_amd64.tar.gz from the GitHub releases page",
                        "2. Extract the archive using 7-Zip, WinRAR, or Windows 11's built-in extraction",
                        $"3. Copy piper.exe to: {piperDir}",
                        "4. Click 'Re-scan' to detect the installation"
                    },
                    installPath = piperDir
                });
            }

            // Find piper.exe
            piperExePath = Directory.GetFiles(piperDir, "piper.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (piperExePath == null)
            {
                throw new FileNotFoundException("piper.exe not found in extracted files");
            }

            // Move piper.exe to root of piperDir for easier access
            var targetPath = Path.Combine(piperDir, "piper.exe");
            if (piperExePath != targetPath)
            {
                System.IO.File.Copy(piperExePath, targetPath, overwrite: true);
            }

            // Download a default voice model (en_US-lessac-medium is a good default)
            var voiceModelUrl = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx";
            var voiceModelDir = Path.Combine(piperDir, "voices");
            Directory.CreateDirectory(voiceModelDir);
            var voiceModelPath = Path.Combine(voiceModelDir, "en_US-lessac-medium.onnx");

            _logger.LogInformation("[{CorrelationId}] Downloading default voice model", correlationId);

            try
            {
                using (var response = await _httpClient.GetAsync(voiceModelUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (var fileStream = new FileStream(voiceModelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await contentStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{CorrelationId}] Failed to download voice model, user can download manually", correlationId);
                // Continue without voice model - user can download it manually
            }

            // Cleanup
            if (System.IO.File.Exists(downloadPath))
            {
                System.IO.File.Delete(downloadPath);
            }

            // Save configuration
            var providerSettings = new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>());
            providerSettings.SetPiperPaths(targetPath, System.IO.File.Exists(voiceModelPath) ? voiceModelPath : null);

            // Force file system flush by reading back the settings file
            // This ensures the write is committed to disk before the check endpoint is called
            try
            {
                var settingsPath = Path.Combine(providerSettings.GetAuraDataDirectory(), "settings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    System.IO.File.ReadAllText(settingsPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{CorrelationId}] Failed to verify settings file write", correlationId);
            }

            _logger.LogInformation("[{CorrelationId}] Piper TTS installed successfully at {Path}", correlationId, targetPath);

            return Ok(new
            {
                success = true,
                message = "Piper TTS installed successfully",
                path = targetPath,
                voiceModelPath = System.IO.File.Exists(voiceModelPath) ? voiceModelPath : null,
                voiceModelDownloaded = System.IO.File.Exists(voiceModelPath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to install Piper TTS on Windows", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Install Mimic3 TTS (Docker-based or guide user to install)
    /// </summary>
    [HttpPost("install-mimic3")]
    public async Task<IActionResult> InstallMimic3(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Starting Mimic3 TTS installation check", correlationId);

            // Mimic3 is typically run via Docker or Python
            // For Windows, we'll check if Docker is available and guide the user

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check if Docker is available
                try
                {
                    using var dockerProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "docker",
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    dockerProcess.Start();
                    await dockerProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                    if (dockerProcess.ExitCode == 0)
                    {
                        // Docker is available - start Mimic3 container
                        return await StartMimic3Docker(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Docker not found
                }

                // Docker not available - provide installation guide
                return Ok(new
                {
                    success = false,
                    message = "Docker is required for Mimic3 TTS on Windows. Please install Docker Desktop first.",
                    requiresDocker = true,
                    dockerUrl = "https://www.docker.com/products/docker-desktop",
                    alternativeInstructions = "Alternatively, you can install Mimic3 via Python: pip install mycroft-mimic3-tts"
                });
            }
            else
            {
                // Linux/macOS - try Docker first
                try
                {
                    using var dockerProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "docker",
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    dockerProcess.Start();
                    await dockerProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                    if (dockerProcess.ExitCode == 0)
                    {
                        return await StartMimic3Docker(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Docker not found
                }

                return Ok(new
                {
                    success = false,
                    message = "Please install Mimic3 manually. Options:\n1. Docker: docker run -p 59125:59125 mycroftai/mimic3\n2. Python: pip install mycroft-mimic3-tts",
                    requiresManualInstall = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to install Mimic3 TTS", correlationId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<IActionResult> StartMimic3Docker(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation("[{CorrelationId}] Starting Mimic3 Docker container", correlationId);

            // Check if container already exists
            using var checkProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps -a --filter name=mimic3 --format {{.Names}}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            checkProcess.Start();
            var output = await checkProcess.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await checkProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var containerExists = output.Contains("mimic3");

            if (containerExists)
            {
                // Start existing container
                using var startProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "start mimic3",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                startProcess.Start();
                await startProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                if (startProcess.ExitCode == 0)
                {
                    // Save configuration
                    var providerSettings = new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>());
                    providerSettings.SetMimic3BaseUrl("http://127.0.0.1:59125");

                    return Ok(new
                    {
                        success = true,
                        message = "Mimic3 container started successfully",
                        baseUrl = "http://127.0.0.1:59125",
                        wasExisting = true
                    });
                }
            }

            // Create and start new container
            using var runProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "run -d --name mimic3 -p 59125:59125 --restart unless-stopped mycroftai/mimic3:latest",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            runProcess.Start();
            var runOutput = await runProcess.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var runError = await runProcess.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await runProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (runProcess.ExitCode != 0)
            {
                throw new Exception($"Failed to start Mimic3 container: {runError}");
            }

            // Wait for container to start and become ready (with retries)
            var maxRetries = 10;
            var retryDelay = 1000; // 1 second
            var isReady = false;

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);

                try
                {
                    using var testClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    var testResponse = await testClient.GetAsync("http://127.0.0.1:59125/api/voices", cancellationToken).ConfigureAwait(false);
                    if (testResponse.IsSuccessStatusCode)
                    {
                        isReady = true;
                        _logger.LogInformation("[{CorrelationId}] Mimic3 server is ready after {Attempts} attempts", correlationId, i + 1);
                        break;
                    }
                }
                catch
                {
                    // Server not ready yet, continue retrying
                    _logger.LogInformation("[{CorrelationId}] Mimic3 server not ready yet, attempt {Attempt}/{MaxRetries}", correlationId, i + 1, maxRetries);
                }
            }

            if (!isReady)
            {
                _logger.LogWarning("[{CorrelationId}] Mimic3 server did not become ready within {Timeout} seconds, but container is running", correlationId, maxRetries);
            }

            // Save configuration
            var settings = new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>());
            settings.SetMimic3BaseUrl("http://127.0.0.1:59125");

            _logger.LogInformation("[{CorrelationId}] Mimic3 Docker container started successfully", correlationId);

            return Ok(new
            {
                success = true,
                message = isReady
                    ? "Mimic3 TTS installed and started successfully"
                    : "Mimic3 TTS container started but server may still be initializing. Please wait a moment and click 'Re-scan'.",
                baseUrl = "http://127.0.0.1:59125",
                containerId = runOutput.Trim(),
                isReady = isReady
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to start Mimic3 Docker container", correlationId);
            throw;
        }
    }

    /// <summary>
    /// Check Piper TTS installation status
    /// </summary>
    [HttpGet("check-piper")]
    public IActionResult CheckPiper()
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var providerSettings = new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>());
            var piperPath = providerSettings.PiperExecutablePath;
            var voiceModelPath = providerSettings.PiperVoiceModelPath;

            if (string.IsNullOrWhiteSpace(piperPath))
            {
                return Ok(new
                {
                    installed = false,
                    path = (string?)null,
                    voiceModelPath = (string?)null,
                    error = "Piper TTS not configured"
                });
            }

            var isInstalled = System.IO.File.Exists(piperPath);
            var hasVoiceModel = !string.IsNullOrWhiteSpace(voiceModelPath) && System.IO.File.Exists(voiceModelPath);

            return Ok(new
            {
                installed = isInstalled && hasVoiceModel,
                path = piperPath,
                voiceModelPath = voiceModelPath,
                executableExists = isInstalled,
                voiceModelExists = hasVoiceModel,
                error = !isInstalled ? "Piper executable not found" : !hasVoiceModel ? "Voice model not found" : (string?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to check Piper TTS status", correlationId);
            return Ok(new
            {
                installed = false,
                path = (string?)null,
                voiceModelPath = (string?)null,
                error = $"Error checking status: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Check Mimic3 TTS installation status
    /// </summary>
    [HttpGet("check-mimic3")]
    public async Task<IActionResult> CheckMimic3(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var providerSettings = new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>());
            var baseUrl = providerSettings.Mimic3BaseUrl ?? "http://127.0.0.1:59125";

            // Try to connect to Mimic3 server
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync($"{baseUrl}/api/voices", cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        installed = true,
                        baseUrl = baseUrl,
                        reachable = true,
                        error = (string?)null
                    });
                }
            }
            catch
            {
                // Server not reachable
            }

            return Ok(new
            {
                installed = false,
                baseUrl = baseUrl,
                reachable = false,
                error = "Mimic3 server is not reachable. Make sure it's running."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to check Mimic3 TTS status", correlationId);
            return Ok(new
            {
                installed = false,
                baseUrl = (string?)null,
                reachable = false,
                error = $"Error checking status: {ex.Message}"
            });
        }
    }
}
