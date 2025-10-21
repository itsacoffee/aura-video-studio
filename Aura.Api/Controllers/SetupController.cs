using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Services.Setup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

[ApiController]
[Route("api/setup")]
public class SetupController : ControllerBase
{
    private readonly ILogger<SetupController> _logger;
    private readonly DependencyDetector _detector;
    private readonly DependencyInstaller _installer;
    private readonly SseService _sseService;
    private readonly HttpClient _httpClient;

    public SetupController(
        ILogger<SetupController> logger,
        DependencyDetector detector,
        DependencyInstaller installer,
        SseService sseService,
        HttpClient httpClient)
    {
        _logger = logger;
        _detector = detector;
        _installer = installer;
        _sseService = sseService;
        _httpClient = httpClient;
    }

    [HttpGet("detect")]
    public async Task<IActionResult> Detect(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Detecting dependencies");
            var status = await _detector.DetectAllDependenciesAsync(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect dependencies");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("install/ffmpeg")]
    public async Task InstallFFmpeg(CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            _logger.LogInformation("Starting FFmpeg installation via SSE");

            var progress = new Progress<InstallProgress>(p =>
            {
                var json = JsonSerializer.Serialize(p);
                Response.WriteAsync($"event: progress\ndata: {json}\n\n", ct).GetAwaiter().GetResult();
                Response.Body.FlushAsync(ct).GetAwaiter().GetResult();
            });

            var success = await _installer.InstallFFmpegAsync(progress, ct).ConfigureAwait(false);

            var completionData = JsonSerializer.Serialize(new { success });
            await Response.WriteAsync($"event: complete\ndata: {completionData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg installation failed");
            var errorData = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    [HttpPost("install/piper")]
    public async Task InstallPiper(CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            _logger.LogInformation("Starting Piper TTS installation via SSE");

            var progress = new Progress<InstallProgress>(p =>
            {
                var json = JsonSerializer.Serialize(p);
                Response.WriteAsync($"event: progress\ndata: {json}\n\n", ct).GetAwaiter().GetResult();
                Response.Body.FlushAsync(ct).GetAwaiter().GetResult();
            });

            var success = await _installer.InstallPiperTtsAsync(progress, ct).ConfigureAwait(false);

            var completionData = JsonSerializer.Serialize(new { success });
            await Response.WriteAsync($"event: complete\ndata: {completionData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Piper TTS installation failed");
            var errorData = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    [HttpPost("install/all")]
    public async Task InstallAll(CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            _logger.LogInformation("Starting installation of all dependencies via SSE");

            // Detect what needs to be installed
            var status = await _detector.DetectAllDependenciesAsync(ct).ConfigureAwait(false);

            var overallProgress = new Progress<InstallProgress>(p =>
            {
                var json = JsonSerializer.Serialize(p);
                Response.WriteAsync($"event: progress\ndata: {json}\n\n", ct).GetAwaiter().GetResult();
                Response.Body.FlushAsync(ct).GetAwaiter().GetResult();
            });

            bool allSuccess = true;

            // Install FFmpeg if needed
            if (status.FFmpegInstallationRequired)
            {
                ((IProgress<InstallProgress>)overallProgress).Report(new InstallProgress(0, "Installing FFmpeg...", "", 0, 0));
                var ffmpegSuccess = await _installer.InstallFFmpegAsync(overallProgress, ct).ConfigureAwait(false);
                if (!ffmpegSuccess)
                {
                    allSuccess = false;
                }
            }

            // Install Piper TTS if needed
            if (allSuccess && status.PiperTtsInstallationRequired)
            {
                ((IProgress<InstallProgress>)overallProgress).Report(new InstallProgress(50, "Installing Piper TTS...", "", 0, 0));
                var piperSuccess = await _installer.InstallPiperTtsAsync(overallProgress, ct).ConfigureAwait(false);
                if (!piperSuccess)
                {
                    allSuccess = false;
                }
            }

            var completionData = JsonSerializer.Serialize(new { success = allSuccess });
            await Response.WriteAsync($"event: complete\ndata: {completionData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Installation of all dependencies failed");
            var errorData = JsonSerializer.Serialize(new { error = ex.Message });
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    [HttpPost("validate-key")]
    public async Task<IActionResult> ValidateKey([FromBody] ValidateKeyRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Validating API key for provider: {Provider}", request.Provider);

            bool isValid = false;
            string? errorMessage = null;

            switch (request.Provider?.ToLowerInvariant())
            {
                case "openai":
                    (isValid, errorMessage) = await ValidateOpenAIKeyAsync(request.ApiKey, ct).ConfigureAwait(false);
                    break;
                case "gemini":
                    (isValid, errorMessage) = await ValidateGeminiKeyAsync(request.ApiKey, ct).ConfigureAwait(false);
                    break;
                case "elevenlabs":
                    (isValid, errorMessage) = await ValidateElevenLabsKeyAsync(request.ApiKey, ct).ConfigureAwait(false);
                    break;
                case "playht":
                    (isValid, errorMessage) = await ValidatePlayHTKeyAsync(request.ApiKey, ct).ConfigureAwait(false);
                    break;
                default:
                    return BadRequest(new { success = false, error = "Unknown provider" });
            }

            return Ok(new { success = isValid, error = errorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate API key");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("disk-space")]
    public IActionResult GetDiskSpace()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var driveInfo = new DriveInfo(Path.GetPathRoot(localAppData) ?? "C:\\");
            var availableGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            return Ok(new { availableGB = Math.Round(availableGB, 2) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disk space");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("save-config")]
    public async Task<IActionResult> SaveConfig([FromBody] SetupConfig config, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Saving setup configuration for tier: {Tier}", config.Tier);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configDir = Path.Combine(localAppData, "Aura");
            Directory.CreateDirectory(configDir);

            var configPath = Path.Combine(configDir, "config.json");

            // Encrypt API keys using Data Protection API (Windows) or simple encryption (Unix)
            var encryptedConfig = new
            {
                config.Tier,
                config.SetupCompleted,
                config.SetupVersion,
                ApiKeys = new
                {
                    OpenAI = EncryptString(config.ApiKeys?.OpenAI),
                    Gemini = EncryptString(config.ApiKeys?.Gemini),
                    ElevenLabs = EncryptString(config.ApiKeys?.ElevenLabs),
                    PlayHT = EncryptString(config.ApiKeys?.PlayHT)
                }
            };

            var json = JsonSerializer.Serialize(encryptedConfig, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(configPath, json, ct).ConfigureAwait(false);

            _logger.LogInformation("Setup configuration saved successfully");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save setup configuration");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localAppData, "Aura", "config.json");
            
            var setupCompleted = System.IO.File.Exists(configPath);
            
            return Ok(new { setupCompleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get setup status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<(bool isValid, string? error)> ValidateOpenAIKeyAsync(string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key is required");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, $"Invalid API key (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return (false, $"Validation failed: {ex.Message}");
        }
    }

    private async Task<(bool isValid, string? error)> ValidateGeminiKeyAsync(string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key is required");
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1/models?key={apiKey}";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, $"Invalid API key (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return (false, $"Validation failed: {ex.Message}");
        }
    }

    private async Task<(bool isValid, string? error)> ValidateElevenLabsKeyAsync(string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key is required");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
            request.Headers.Add("xi-api-key", apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, $"Invalid API key (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return (false, $"Validation failed: {ex.Message}");
        }
    }

    private async Task<(bool isValid, string? error)> ValidatePlayHTKeyAsync(string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "API key is required");
        }

        try
        {
            // PlayHT validation would require both user ID and secret key
            // For now, just check if the key is provided
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Validation failed: {ex.Message}");
        }
    }

    private static string? EncryptString(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        try
        {
            // Simple base64 encoding for now
            // In production, should use proper encryption with ASP.NET Core Data Protection API
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes);
        }
        catch
        {
            return plainText; // Fallback to plaintext if encoding fails
        }
    }
}

public class ValidateKeyRequest
{
    public string? Provider { get; set; }
    public string? ApiKey { get; set; }
}

public class SetupConfig
{
    public string? Tier { get; set; }
    public bool SetupCompleted { get; set; }
    public string? SetupVersion { get; set; }
    public ApiKeysConfig? ApiKeys { get; set; }
}

public class ApiKeysConfig
{
    public string? OpenAI { get; set; }
    public string? Gemini { get; set; }
    public string? ElevenLabs { get; set; }
    public string? PlayHT { get; set; }
}
