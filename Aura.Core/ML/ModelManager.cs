using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.ML;

/// <summary>
/// Manages ML model deployment, versioning, and fallback
/// Provides atomic model swapping with backup and rollback support
/// </summary>
public class ModelManager
{
    private readonly ILogger<ModelManager> _logger;
    private readonly string _modelDirectory;
    private const string DefaultModelFileName = "frame-importance-model-default.zip";
    private const string ActiveModelFileName = "frame-importance-model.zip";
    private const string BackupSuffix = ".backup";

    public ModelManager(ILogger<ModelManager> logger, string? modelDirectory = null)
    {
        _logger = logger;
        _modelDirectory = modelDirectory ?? Path.Combine(AppContext.BaseDirectory, "ML", "PretrainedModels");
    }

    /// <summary>
    /// Get the path to the active model with fallback to default
    /// </summary>
    public async Task<string> GetActiveModelPathAsync(CancellationToken cancellationToken = default)
    {
        var activeModelPath = Path.Combine(_modelDirectory, ActiveModelFileName);
        var defaultModelPath = Path.Combine(_modelDirectory, DefaultModelFileName);

        if (File.Exists(activeModelPath))
        {
            if (await ValidateModelAsync(activeModelPath, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Using active model at {Path}", activeModelPath);
                return activeModelPath;
            }

            _logger.LogWarning("Active model at {Path} is invalid, falling back to default", activeModelPath);
        }

        if (File.Exists(defaultModelPath))
        {
            _logger.LogInformation("Using default model at {Path}", defaultModelPath);
            return defaultModelPath;
        }

        throw new FileNotFoundException("No valid model found (neither active nor default)");
    }

    /// <summary>
    /// Deploy a new model atomically with backup of the previous one
    /// </summary>
    public async Task<bool> DeployModelAsync(
        string sourceModelPath, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceModelPath))
        {
            throw new FileNotFoundException($"Source model not found at {sourceModelPath}");
        }

        Directory.CreateDirectory(_modelDirectory);

        var targetModelPath = Path.Combine(_modelDirectory, ActiveModelFileName);
        var backupModelPath = targetModelPath + BackupSuffix;
        var tempModelPath = targetModelPath + ".tmp";

        try
        {
            _logger.LogInformation("Deploying model from {Source} to {Target}", sourceModelPath, targetModelPath);

            if (!await ValidateModelAsync(sourceModelPath, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogError("Source model at {Path} failed validation", sourceModelPath);
                return false;
            }

            File.Copy(sourceModelPath, tempModelPath, overwrite: true);

            if (File.Exists(targetModelPath))
            {
                _logger.LogInformation("Backing up existing model to {BackupPath}", backupModelPath);
                File.Copy(targetModelPath, backupModelPath, overwrite: true);
            }

            File.Move(tempModelPath, targetModelPath, overwrite: true);

            _logger.LogInformation("Model deployed successfully to {Path}", targetModelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy model");
            
            if (File.Exists(tempModelPath))
            {
                try
                {
                    File.Delete(tempModelPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temporary file {Path}", tempModelPath);
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Revert to the default model by removing the active model
    /// </summary>
    public Task<bool> RevertToDefaultAsync(CancellationToken cancellationToken = default)
    {
        var activeModelPath = Path.Combine(_modelDirectory, ActiveModelFileName);
        
        if (!File.Exists(activeModelPath))
        {
            _logger.LogInformation("No active model to revert, already using default");
            return Task.FromResult(true);
        }

        try
        {
            _logger.LogInformation("Reverting to default model by removing {Path}", activeModelPath);
            File.Delete(activeModelPath);
            _logger.LogInformation("Successfully reverted to default model");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert to default model");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Restore from backup if available
    /// </summary>
    public Task<bool> RestoreFromBackupAsync(CancellationToken cancellationToken = default)
    {
        var activeModelPath = Path.Combine(_modelDirectory, ActiveModelFileName);
        var backupModelPath = activeModelPath + BackupSuffix;

        if (!File.Exists(backupModelPath))
        {
            _logger.LogWarning("No backup model found at {Path}", backupModelPath);
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogInformation("Restoring model from backup {BackupPath}", backupModelPath);
            File.Copy(backupModelPath, activeModelPath, overwrite: true);
            _logger.LogInformation("Successfully restored model from backup");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Validate that a model file is valid (basic checks)
    /// </summary>
    private async Task<bool> ValidateModelAsync(string modelPath, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(modelPath))
            {
                _logger.LogWarning("Model file does not exist at {Path}", modelPath);
                return false;
            }

            var fileInfo = new FileInfo(modelPath);
            if (fileInfo.Length == 0)
            {
                _logger.LogWarning("Model file at {Path} is empty", modelPath);
                return false;
            }

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            
            _logger.LogDebug("Model at {Path} passed basic validation", modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating model at {Path}", modelPath);
            return false;
        }
    }
}
