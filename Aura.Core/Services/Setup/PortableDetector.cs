using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Setup;

/// <summary>
/// Response model for portable status API
/// </summary>
public record PortableStatus(
    bool IsPortableMode,
    string PortableRoot,
    string ToolsDirectory,
    string DataDirectory,
    string CacheDirectory,
    string LogsDirectory,
    bool ConfigExists,
    bool NeedsFirstRunSetup
);

/// <summary>
/// Summary of all dependencies for portable installations
/// </summary>
public record DependencySummary(
    bool FFmpegInstalled,
    string? FFmpegPath,
    bool PiperInstalled,
    string? PiperPath,
    bool OllamaInstalled,
    string? OllamaPath,
    bool StableDiffusionInstalled,
    string? StableDiffusionPath,
    bool AllRequiredInstalled
);

/// <summary>
/// Detects if the application is running in portable mode and provides
/// portable-relative path resolution for dependencies and configuration.
/// </summary>
public class PortableDetector
{
    private readonly ILogger<PortableDetector> _logger;
    private readonly string _portableRoot;
    private readonly bool _isPortableMode;
    private const string PortableMarkerFileName = ".portable";
    private const string ConfigFileName = "config.json";

    /// <summary>
    /// Gets whether the application is running in portable mode.
    /// Portable mode is determined by the presence of a .portable marker file
    /// in the same directory as the executable, or by an environment variable.
    /// </summary>
    public bool IsPortableMode => _isPortableMode;

    /// <summary>
    /// Gets the root directory for the portable installation.
    /// This is the directory containing the executable.
    /// </summary>
    public string PortableRoot => _portableRoot;

    /// <summary>
    /// Gets the Tools directory path (./Tools/)
    /// </summary>
    public string ToolsDirectory => Path.Combine(_portableRoot, "Tools");

    /// <summary>
    /// Gets the Data directory path (./Data/)
    /// </summary>
    public string DataDirectory => Path.Combine(_portableRoot, "Data");

    /// <summary>
    /// Gets the Cache directory path (./Data/cache/)
    /// </summary>
    public string CacheDirectory => Path.Combine(DataDirectory, "cache");

    /// <summary>
    /// Gets the Logs directory path (./Data/logs/)
    /// </summary>
    public string LogsDirectory => Path.Combine(DataDirectory, "logs");

    /// <summary>
    /// Gets the FFmpeg directory (./Tools/ffmpeg/)
    /// </summary>
    public string FFmpegDirectory => Path.Combine(ToolsDirectory, "ffmpeg");

    /// <summary>
    /// Gets the Piper directory (./Tools/piper/)
    /// </summary>
    public string PiperDirectory => Path.Combine(ToolsDirectory, "piper");

    /// <summary>
    /// Gets the Ollama directory (./Tools/ollama/)
    /// </summary>
    public string OllamaDirectory => Path.Combine(ToolsDirectory, "ollama");

    /// <summary>
    /// Gets the Stable Diffusion WebUI directory (./Tools/stable-diffusion-webui/)
    /// </summary>
    public string StableDiffusionDirectory => Path.Combine(ToolsDirectory, "stable-diffusion-webui");

    /// <summary>
    /// Gets the configuration file path (./Data/config.json)
    /// </summary>
    public string ConfigFilePath => Path.Combine(DataDirectory, ConfigFileName);

    public PortableDetector(ILogger<PortableDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Determine portable root from executable location
        _portableRoot = DeterminePortableRoot();
        _isPortableMode = DetectPortableMode();

        _logger.LogInformation(
            "PortableDetector initialized: IsPortableMode={IsPortable}, Root={Root}",
            _isPortableMode, _portableRoot);
    }

    /// <summary>
    /// Determines the portable root directory from the application base directory.
    /// </summary>
    private string DeterminePortableRoot()
    {
        // Check environment variable first
        var envPortableRoot = Environment.GetEnvironmentVariable("AURA_PORTABLE_ROOT");
        if (!string.IsNullOrWhiteSpace(envPortableRoot) && Directory.Exists(envPortableRoot))
        {
            _logger.LogInformation("Using portable root from AURA_PORTABLE_ROOT: {Root}", envPortableRoot);
            return Path.GetFullPath(envPortableRoot);
        }

        // Use application base directory
        var baseDir = AppContext.BaseDirectory;

        // If running from bin folder during development, go up to solution root
        var current = new DirectoryInfo(baseDir);
        if (current.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            current.Parent?.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) == true)
        {
            while (current != null)
            {
                if (current.Name.Equals("Aura.Api", StringComparison.OrdinalIgnoreCase) ||
                    current.Name.Equals("Aura.Core", StringComparison.OrdinalIgnoreCase))
                {
                    current = current.Parent;
                    break;
                }
                current = current.Parent;
            }
        }

        return current?.FullName ?? baseDir;
    }

    /// <summary>
    /// Detects if the application is running in portable mode.
    /// </summary>
    private bool DetectPortableMode()
    {
        // Check environment variable
        var envPortable = Environment.GetEnvironmentVariable("AURA_PORTABLE_MODE");
        if (!string.IsNullOrWhiteSpace(envPortable))
        {
            if (bool.TryParse(envPortable, out var isPortable))
            {
                _logger.LogInformation("Portable mode from AURA_PORTABLE_MODE: {IsPortable}", isPortable);
                return isPortable;
            }
            if (envPortable.Equals("1", StringComparison.Ordinal) ||
                envPortable.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Portable mode enabled via environment variable");
                return true;
            }
        }

        // Check for .portable marker file
        var markerPath = Path.Combine(_portableRoot, PortableMarkerFileName);
        if (File.Exists(markerPath))
        {
            _logger.LogInformation("Portable mode detected via marker file: {Path}", markerPath);
            return true;
        }

        // Check for existing Data folder (indicates portable setup)
        var dataDir = Path.Combine(_portableRoot, "Data");
        if (Directory.Exists(dataDir))
        {
            var configPath = Path.Combine(dataDir, ConfigFileName);
            if (File.Exists(configPath))
            {
                _logger.LogInformation("Portable mode detected via existing Data folder: {Path}", dataDir);
                return true;
            }
        }

        // Check if we're NOT in a standard installation location
        // (e.g., Program Files on Windows)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (!_portableRoot.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) &&
                !_portableRoot.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase))
            {
                // Running from a non-standard location, assume portable
                _logger.LogInformation("Assuming portable mode (not in Program Files)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the complete portable status including all paths and configuration status.
    /// </summary>
    public PortableStatus GetPortableStatus()
    {
        var configExists = File.Exists(ConfigFilePath);
        var needsFirstRun = !configExists;

        return new PortableStatus(
            IsPortableMode: _isPortableMode,
            PortableRoot: _portableRoot,
            ToolsDirectory: ToolsDirectory,
            DataDirectory: DataDirectory,
            CacheDirectory: CacheDirectory,
            LogsDirectory: LogsDirectory,
            ConfigExists: configExists,
            NeedsFirstRunSetup: needsFirstRun
        );
    }

    /// <summary>
    /// Gets a summary of all installed dependencies.
    /// </summary>
    public DependencySummary GetDependencySummary()
    {
        var ffmpegPath = FindFFmpegPath();
        var piperPath = FindPiperPath();
        var ollamaPath = FindOllamaPath();
        var sdPath = FindStableDiffusionPath();

        var ffmpegInstalled = !string.IsNullOrEmpty(ffmpegPath);
        var piperInstalled = !string.IsNullOrEmpty(piperPath);
        var ollamaInstalled = !string.IsNullOrEmpty(ollamaPath);
        var sdInstalled = !string.IsNullOrEmpty(sdPath);

        // FFmpeg is the only required dependency
        var allRequired = ffmpegInstalled;

        return new DependencySummary(
            FFmpegInstalled: ffmpegInstalled,
            FFmpegPath: ffmpegPath,
            PiperInstalled: piperInstalled,
            PiperPath: piperPath,
            OllamaInstalled: ollamaInstalled,
            OllamaPath: ollamaPath,
            StableDiffusionInstalled: sdInstalled,
            StableDiffusionPath: sdPath,
            AllRequiredInstalled: allRequired
        );
    }

    /// <summary>
    /// Ensures all required directories exist.
    /// </summary>
    public void EnsureDirectoriesExist()
    {
        EnsureDirectory(ToolsDirectory);
        EnsureDirectory(DataDirectory);
        EnsureDirectory(CacheDirectory);
        EnsureDirectory(LogsDirectory);
    }

    /// <summary>
    /// Creates the portable marker file to enable portable mode.
    /// </summary>
    public void CreatePortableMarker()
    {
        var markerPath = Path.Combine(_portableRoot, PortableMarkerFileName);
        try
        {
            File.WriteAllText(markerPath, $"Portable installation created at {DateTime.UtcNow:O}");
            _logger.LogInformation("Created portable marker file: {Path}", markerPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create portable marker file: {Path}", markerPath);
        }
    }

    /// <summary>
    /// Converts an absolute path to a portable-relative path if applicable.
    /// </summary>
    public string? ToRelativePath(string? absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath) || !_isPortableMode)
        {
            return absolutePath;
        }

        try
        {
            var fullAbsolutePath = Path.GetFullPath(absolutePath);
            var fullPortableRoot = Path.GetFullPath(_portableRoot);

            if (fullAbsolutePath.StartsWith(fullPortableRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(fullPortableRoot, fullAbsolutePath);
                return "." + Path.DirectorySeparatorChar + relativePath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to convert path to relative: {Path}", absolutePath);
        }

        return absolutePath;
    }

    /// <summary>
    /// Converts a portable-relative path to an absolute path.
    /// </summary>
    public string? ToAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return relativePath;
        }

        // If already absolute, return as-is
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        // Handle ./ and ../ prefixes
        if (relativePath.StartsWith("./", StringComparison.Ordinal) ||
            relativePath.StartsWith(".\\", StringComparison.Ordinal))
        {
            relativePath = relativePath.Substring(2);
        }

        return Path.GetFullPath(Path.Combine(_portableRoot, relativePath));
    }

    /// <summary>
    /// Validates and optionally repairs paths that may have become invalid
    /// after moving the portable folder.
    /// </summary>
    public (bool IsValid, string? RepairedPath) ValidateAndRepairPath(string? configuredPath, string defaultSubPath)
    {
        if (string.IsNullOrEmpty(configuredPath))
        {
            // No path configured, check if default exists
            var defaultPath = Path.Combine(_portableRoot, defaultSubPath);
            if (File.Exists(defaultPath) || Directory.Exists(defaultPath))
            {
                return (true, defaultPath);
            }
            return (false, null);
        }

        // Convert to absolute if relative
        var absolutePath = ToAbsolutePath(configuredPath);

        // Check if path exists
        if (File.Exists(absolutePath) || Directory.Exists(absolutePath))
        {
            return (true, absolutePath);
        }

        // Path is invalid, try to repair by checking default location
        var defaultRepairPath = Path.Combine(_portableRoot, defaultSubPath);
        if (File.Exists(defaultRepairPath) || Directory.Exists(defaultRepairPath))
        {
            _logger.LogInformation(
                "Repaired path from {OldPath} to {NewPath}",
                configuredPath, defaultRepairPath);
            return (true, defaultRepairPath);
        }

        return (false, null);
    }

    /// <summary>
    /// Find FFmpeg executable in portable or system locations.
    /// </summary>
    public string? FindFFmpegPath()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "ffmpeg.exe"
            : "ffmpeg";

        // Check portable Tools directory first
        var portablePaths = new[]
        {
            Path.Combine(FFmpegDirectory, "bin", executableName),
            Path.Combine(FFmpegDirectory, executableName),
            Path.Combine(ToolsDirectory, executableName),
        };

        foreach (var path in portablePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found FFmpeg at portable location: {Path}", path);
                return path;
            }
        }

        // Check system locations as fallback
        return FindInSystemPath(executableName);
    }

    /// <summary>
    /// Find Piper executable in portable or system locations.
    /// </summary>
    public string? FindPiperPath()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "piper.exe"
            : "piper";

        // Check portable Tools directory first
        var portablePaths = new[]
        {
            Path.Combine(PiperDirectory, executableName),
            Path.Combine(ToolsDirectory, executableName),
        };

        foreach (var path in portablePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found Piper at portable location: {Path}", path);
                return path;
            }
        }

        return FindInSystemPath(executableName);
    }

    /// <summary>
    /// Find Ollama executable in portable or system locations.
    /// </summary>
    public string? FindOllamaPath()
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "ollama.exe"
            : "ollama";

        // Check portable Tools directory first
        var portablePaths = new[]
        {
            Path.Combine(OllamaDirectory, executableName),
            Path.Combine(ToolsDirectory, executableName),
        };

        foreach (var path in portablePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found Ollama at portable location: {Path}", path);
                return path;
            }
        }

        // Also check using OllamaService's method
        var systemPath = OllamaService.FindOllamaExecutable();
        if (!string.IsNullOrEmpty(systemPath))
        {
            return systemPath;
        }

        return FindInSystemPath(executableName);
    }

    /// <summary>
    /// Find Stable Diffusion WebUI directory.
    /// </summary>
    public string? FindStableDiffusionPath()
    {
        // Check portable Tools directory
        var portablePath = StableDiffusionDirectory;
        if (Directory.Exists(portablePath))
        {
            // Look for webui.py or webui-user.bat to confirm it's a valid installation
            if (File.Exists(Path.Combine(portablePath, "webui.py")) ||
                File.Exists(Path.Combine(portablePath, "webui-user.bat")))
            {
                _logger.LogDebug("Found Stable Diffusion at portable location: {Path}", portablePath);
                return portablePath;
            }
        }

        return null;
    }

    /// <summary>
    /// Search for an executable in the system PATH.
    /// </summary>
    private string? FindInSystemPath(string executableName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var pathDirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in pathDirs)
        {
            try
            {
                var fullPath = Path.Combine(dir, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Skip invalid path entries
            }
        }

        return null;
    }

    private void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("Created directory: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create directory: {Path}", path);
            }
        }
    }
}
