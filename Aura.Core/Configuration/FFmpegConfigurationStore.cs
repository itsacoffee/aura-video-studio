using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Persistent storage for FFmpeg configuration
/// Stores configuration in %LOCALAPPDATA%\AuraVideoStudio\ffmpeg-config.json
/// </summary>
public class FFmpegConfigurationStore
{
    private readonly ILogger<FFmpegConfigurationStore> _logger;
    private readonly string _configFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public FFmpegConfigurationStore(ILogger<FFmpegConfigurationStore> logger)
    {
        _logger = logger;
        
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(localAppData, "AuraVideoStudio");
        Directory.CreateDirectory(configDir);
        
        _configFilePath = Path.Combine(configDir, "ffmpeg-config.json");
        _logger.LogInformation("FFmpeg configuration file: {Path}", _configFilePath);
    }
    
    /// <summary>
    /// Load configuration from disk, returns default if file doesn't exist
    /// </summary>
    public async Task<FFmpegConfiguration> LoadAsync(CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Configuration file not found, returning default");
                return new FFmpegConfiguration();
            }
            
            var json = await File.ReadAllTextAsync(_configFilePath, ct).ConfigureAwait(false);
            var config = JsonSerializer.Deserialize<FFmpegConfiguration>(json, JsonOptions);
            
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration, returning default");
                return new FFmpegConfiguration();
            }
            
            _logger.LogInformation(
                "Loaded FFmpeg configuration: Mode={Mode}, Path={Path}, Valid={IsValid}",
                config.Mode,
                config.Path ?? "null",
                config.IsValid
            );
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FFmpeg configuration");
            return new FFmpegConfiguration();
        }
        finally
        {
            _fileLock.Release();
        }
    }
    
    /// <summary>
    /// Save configuration to disk
    /// </summary>
    public async Task SaveAsync(FFmpegConfiguration config, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json, ct).ConfigureAwait(false);
            
            _logger.LogInformation(
                "Saved FFmpeg configuration: Mode={Mode}, Path={Path}, Valid={IsValid}",
                config.Mode,
                config.Path ?? "null",
                config.IsValid
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving FFmpeg configuration");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }
    
    /// <summary>
    /// Update specific fields without reloading entire config
    /// </summary>
    public async Task UpdateAsync(
        Action<FFmpegConfiguration> updateAction,
        CancellationToken ct = default)
    {
        var config = await LoadAsync(ct).ConfigureAwait(false);
        updateAction(config);
        await SaveAsync(config, ct).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Clear configuration (reset to default)
    /// </summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
                _logger.LogInformation("Cleared FFmpeg configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing FFmpeg configuration");
            throw;
        }
        finally
        {
            _fileLock.Release();
        }
    }
    
    /// <summary>
    /// Get configuration file path (for diagnostics)
    /// </summary>
    public string GetConfigFilePath() => _configFilePath;
}
