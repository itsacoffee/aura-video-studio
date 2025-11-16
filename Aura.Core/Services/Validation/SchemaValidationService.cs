using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Validation;

/// <summary>
/// Validates JSON script responses against expected schema structure
/// Ensures all required fields are present and valid before processing
/// </summary>
public class SchemaValidationService
{
    private readonly ILogger<SchemaValidationService> _logger;

    public SchemaValidationService(ILogger<SchemaValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a script JSON string against the expected schema structure
    /// </summary>
    /// <param name="scriptJson">JSON string containing the script data</param>
    /// <returns>Validation result with error details if validation fails</returns>
    public ScriptValidationResult ValidateScriptJson(string scriptJson)
    {
        var errors = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(scriptJson))
            {
                errors.Add("Script JSON is null or empty");
                _logger.LogError("Script validation failed: JSON is null or empty");
                return new ScriptValidationResult { IsValid = false, Errors = errors };
            }

            using var document = JsonDocument.Parse(scriptJson);
            var root = document.RootElement;

            // Validate required top-level fields
            if (!ValidateRequiredField(root, "title", JsonValueKind.String, errors))
            {
                _logger.LogWarning("Script validation: Missing or invalid 'title' field");
            }

            if (!ValidateRequiredField(root, "hook", JsonValueKind.String, errors))
            {
                _logger.LogWarning("Script validation: Missing or invalid 'hook' field");
            }

            if (!ValidateRequiredField(root, "callToAction", JsonValueKind.String, errors))
            {
                _logger.LogWarning("Script validation: Missing or invalid 'callToAction' field");
            }

            if (!ValidateRequiredField(root, "totalDuration", JsonValueKind.Number, errors))
            {
                _logger.LogWarning("Script validation: Missing or invalid 'totalDuration' field");
            }

            // Validate scenes array
            if (!root.TryGetProperty("scenes", out var scenesElement))
            {
                errors.Add("Required field 'scenes' is missing");
                _logger.LogError("Script validation failed: Missing 'scenes' array");
            }
            else if (scenesElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Field 'scenes' must be an array");
                _logger.LogError("Script validation failed: 'scenes' is not an array");
            }
            else
            {
                var sceneArray = scenesElement.EnumerateArray().ToList();
                if (sceneArray.Count == 0)
                {
                    errors.Add("Scenes array cannot be empty");
                    _logger.LogWarning("Script validation: 'scenes' array is empty");
                }
                else
                {
                    // Validate each scene
                    for (int i = 0; i < sceneArray.Count; i++)
                    {
                        ValidateScene(sceneArray[i], i, errors);
                    }
                }
            }

            var isValid = errors.Count == 0;
            
            if (isValid)
            {
                _logger.LogInformation("Script validation passed successfully");
            }
            else
            {
                _logger.LogWarning("Script validation failed with {ErrorCount} errors: {Errors}", 
                    errors.Count, string.Join("; ", errors));
            }

            return new ScriptValidationResult
            {
                IsValid = isValid,
                Errors = errors
            };
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
            _logger.LogError(ex, "Script validation failed: Invalid JSON format");
            return new ScriptValidationResult { IsValid = false, Errors = errors };
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error during script validation");
            return new ScriptValidationResult { IsValid = false, Errors = errors };
        }
    }

    private bool ValidateRequiredField(
        JsonElement root, 
        string fieldName, 
        JsonValueKind expectedKind, 
        List<string> errors)
    {
        if (!root.TryGetProperty(fieldName, out var field))
        {
            errors.Add($"Required field '{fieldName}' is missing");
            return false;
        }

        if (field.ValueKind != expectedKind)
        {
            errors.Add($"Field '{fieldName}' must be of type {expectedKind}");
            return false;
        }

        // Additional validation for string fields
        if (expectedKind == JsonValueKind.String)
        {
            var value = field.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Field '{fieldName}' cannot be empty");
                return false;
            }
        }

        return true;
    }

    private void ValidateScene(JsonElement scene, int index, List<string> errors)
    {
        var scenePrefix = $"Scene {index + 1}";

        if (scene.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{scenePrefix}: Must be an object");
            return;
        }

        // Validate required scene fields
        if (!ValidateRequiredField(scene, "narration", JsonValueKind.String, errors))
        {
            _logger.LogWarning("{ScenePrefix}: Missing or invalid 'narration' field", scenePrefix);
        }

        if (!ValidateRequiredField(scene, "visualDescription", JsonValueKind.String, errors))
        {
            _logger.LogWarning("{ScenePrefix}: Missing or invalid 'visualDescription' field", scenePrefix);
        }

        if (!ValidateRequiredField(scene, "duration", JsonValueKind.Number, errors))
        {
            _logger.LogWarning("{ScenePrefix}: Missing or invalid 'duration' field", scenePrefix);
        }
        else if (scene.TryGetProperty("duration", out var durationElement))
        {
            var duration = durationElement.GetDouble();
            if (duration < 1.0 || duration > 60.0)
            {
                errors.Add($"{scenePrefix}: Duration must be between 1.0 and 60.0 seconds (got {duration})");
                _logger.LogWarning("{ScenePrefix}: Duration {Duration} is out of valid range", scenePrefix, duration);
            }
        }

        if (!ValidateRequiredField(scene, "transition", JsonValueKind.String, errors))
        {
            _logger.LogWarning("{ScenePrefix}: Missing or invalid 'transition' field", scenePrefix);
        }
        else if (scene.TryGetProperty("transition", out var transitionElement))
        {
            var transition = transitionElement.GetString();
            var validTransitions = new[] { "cut", "fade", "dissolve", "wipe", "slide", "zoom" };
            if (!validTransitions.Contains(transition, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{scenePrefix}: Invalid transition type '{transition}'. Must be one of: {string.Join(", ", validTransitions)}");
                _logger.LogWarning("{ScenePrefix}: Invalid transition type '{Transition}'", scenePrefix, transition);
            }
        }
    }
}

/// <summary>
/// Result of script JSON validation
/// </summary>
public class ScriptValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors (empty if valid)
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
