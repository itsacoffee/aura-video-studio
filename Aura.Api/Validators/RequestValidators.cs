using Aura.Api.Models.ApiModels.V1;
using FluentValidation;

namespace Aura.Api.Validators;

/// <summary>
/// Validator for ScriptRequest - ensures all required fields are present and valid
/// </summary>
public class ScriptRequestValidator : AbstractValidator<ScriptRequest>
{
    public ScriptRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required")
            .MinimumLength(3).WithMessage("Topic must be at least 3 characters")
            .MaximumLength(500).WithMessage("Topic must not exceed 500 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Topic contains potentially dangerous content");

        RuleFor(x => x.Audience)
            .NotEmpty().WithMessage("Audience is required")
            .MinimumLength(3).WithMessage("Audience must be at least 3 characters")
            .MaximumLength(200).WithMessage("Audience must not exceed 200 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Audience contains potentially dangerous content");

        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage("Goal is required")
            .MinimumLength(3).WithMessage("Goal must be at least 3 characters")
            .MaximumLength(300).WithMessage("Goal must not exceed 300 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Goal contains potentially dangerous content");

        RuleFor(x => x.Tone)
            .NotEmpty().WithMessage("Tone is required")
            .MinimumLength(3).WithMessage("Tone must be at least 3 characters")
            .MaximumLength(100).WithMessage("Tone must not exceed 100 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Tone contains potentially dangerous content");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required")
            .MinimumLength(2).WithMessage("Language must be at least 2 characters")
            .MaximumLength(50).WithMessage("Language must not exceed 50 characters");

        RuleFor(x => x.TargetDurationMinutes)
            .GreaterThan(0).WithMessage("Target duration must be greater than 0")
            .LessThanOrEqualTo(120).WithMessage("Target duration must not exceed 120 minutes");

        RuleFor(x => x.Style)
            .NotEmpty().WithMessage("Style is required")
            .MaximumLength(200).WithMessage("Style must not exceed 200 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Style contains potentially dangerous content");

        When(x => x.PromptModifiers != null, () =>
        {
            RuleFor(x => x.PromptModifiers)
                .SetValidator(new PromptModifiersDtoValidator()!);
        });

        When(x => x.RefinementConfig != null, () =>
        {
            RuleFor(x => x.RefinementConfig)
                .SetValidator(new ScriptRefinementConfigDtoValidator()!);
        });

        When(x => !string.IsNullOrWhiteSpace(x.AudienceProfileId), () =>
        {
            RuleFor(x => x.AudienceProfileId)
                .Must(id => Aura.Api.Security.InputSanitizer.IsValidGuid(id))
                .WithMessage("Audience profile ID must be a valid GUID");
        });
    }
}

/// <summary>
/// Validator for PlanRequest
/// </summary>
public class PlanRequestValidator : AbstractValidator<PlanRequest>
{
    public PlanRequestValidator()
    {
        RuleFor(x => x.TargetDurationMinutes)
            .GreaterThan(0).WithMessage("Target duration must be greater than 0")
            .LessThanOrEqualTo(120).WithMessage("Target duration must not exceed 120 minutes");

        RuleFor(x => x.Style)
            .NotEmpty().WithMessage("Style is required")
            .MaximumLength(200).WithMessage("Style must not exceed 200 characters");
    }
}

/// <summary>
/// Validator for TtsRequest
/// </summary>
public class TtsRequestValidator : AbstractValidator<TtsRequest>
{
    public TtsRequestValidator()
    {
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Lines collection is required")
            .NotEmpty().WithMessage("At least one line is required")
            .Must(lines => lines.Count <= 1000).WithMessage("Cannot process more than 1000 lines at once");

        RuleFor(x => x.VoiceName)
            .NotEmpty().WithMessage("Voice name is required")
            .MaximumLength(100).WithMessage("Voice name must not exceed 100 characters")
            .Must(name => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(name))
            .WithMessage("Voice name contains potentially dangerous content");

        RuleFor(x => x.Rate)
            .InclusiveBetween(0.5, 2.0).WithMessage("Rate must be between 0.5 and 2.0");

        RuleFor(x => x.Pitch)
            .InclusiveBetween(-20, 20).WithMessage("Pitch must be between -20 and 20");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Text)
                .NotEmpty().WithMessage("Line text is required")
                .MaximumLength(5000).WithMessage("Line text must not exceed 5000 characters")
                .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
                .WithMessage("Line text contains potentially dangerous content");

            line.RuleFor(l => l.StartSeconds)
                .GreaterThanOrEqualTo(0).WithMessage("Start time must be non-negative");

            line.RuleFor(l => l.DurationSeconds)
                .GreaterThan(0).WithMessage("Duration must be greater than 0")
                .LessThanOrEqualTo(300).WithMessage("Duration must not exceed 300 seconds per line");
        });
    }
}

/// <summary>
/// Validator for RenderRequest
/// </summary>
public class RenderRequestValidator : AbstractValidator<RenderRequest>
{
    public RenderRequestValidator()
    {
        RuleFor(x => x.TimelineJson)
            .NotEmpty().WithMessage("Timeline JSON is required")
            .Must(BeValidJson).WithMessage("Timeline must be valid JSON");

        RuleFor(x => x.PresetName)
            .NotEmpty().WithMessage("Preset name is required")
            .MaximumLength(50).WithMessage("Preset name must not exceed 50 characters");

        When(x => x.Settings != null, () =>
        {
            RuleFor(x => x.Settings!.Width)
                .InclusiveBetween(128, 7680).WithMessage("Width must be between 128 and 7680 pixels");

            RuleFor(x => x.Settings!.Height)
                .InclusiveBetween(128, 4320).WithMessage("Height must be between 128 and 4320 pixels");

            RuleFor(x => x.Settings!.Fps)
                .InclusiveBetween(1, 120).WithMessage("FPS must be between 1 and 120");

            RuleFor(x => x.Settings!.QualityLevel)
                .InclusiveBetween(1, 10).WithMessage("Quality level must be between 1 and 10");

            RuleFor(x => x.Settings!.VideoBitrateK)
                .InclusiveBetween(500, 100000).WithMessage("Video bitrate must be between 500 and 100000 kbps");

            RuleFor(x => x.Settings!.AudioBitrateK)
                .InclusiveBetween(64, 512).WithMessage("Audio bitrate must be between 64 and 512 kbps");
        });
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for AssetSearchRequest
/// </summary>
public class AssetSearchRequestValidator : AbstractValidator<AssetSearchRequest>
{
    public AssetSearchRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required")
            .Must(p => new[] { "pexels", "pixabay", "unsplash", "local" }.Contains(p.ToLowerInvariant()))
            .WithMessage("Provider must be one of: pexels, pixabay, unsplash, local");

        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Search query is required")
            .MinimumLength(2).WithMessage("Query must be at least 2 characters")
            .MaximumLength(200).WithMessage("Query must not exceed 200 characters");

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 50).WithMessage("Count must be between 1 and 50");

        When(x => x.Provider.ToLowerInvariant() != "local", () =>
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty().WithMessage("API key is required for non-local providers");
        });
    }
}

/// <summary>
/// Validator for AssetGenerateRequest
/// </summary>
public class AssetGenerateRequestValidator : AbstractValidator<AssetGenerateRequest>
{
    public AssetGenerateRequestValidator()
    {
        RuleFor(x => x.Prompt)
            .NotEmpty().WithMessage("Prompt is required")
            .MinimumLength(3).WithMessage("Prompt must be at least 3 characters")
            .MaximumLength(1000).WithMessage("Prompt must not exceed 1000 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Prompt contains potentially dangerous content");

        When(x => x.Width.HasValue, () =>
        {
            RuleFor(x => x.Width!.Value)
                .InclusiveBetween(128, 2048).WithMessage("Width must be between 128 and 2048 pixels");
        });

        When(x => x.Height.HasValue, () =>
        {
            RuleFor(x => x.Height!.Value)
                .InclusiveBetween(128, 2048).WithMessage("Height must be between 128 and 2048 pixels");
        });

        When(x => x.Steps.HasValue, () =>
        {
            RuleFor(x => x.Steps!.Value)
                .InclusiveBetween(1, 150).WithMessage("Steps must be between 1 and 150");
        });

        When(x => x.CfgScale.HasValue, () =>
        {
            RuleFor(x => x.CfgScale!.Value)
                .InclusiveBetween(1.0, 30.0).WithMessage("CFG scale must be between 1.0 and 30.0");
        });

        When(x => !string.IsNullOrWhiteSpace(x.StableDiffusionUrl), () =>
        {
            RuleFor(x => x.StableDiffusionUrl)
                .Must(url => Aura.Api.Security.InputSanitizer.IsValidUrl(url!))
                .WithMessage("Stable Diffusion URL must be a valid HTTP or HTTPS URL");
        });
    }
}

/// <summary>
/// Validator for AzureTtsSynthesizeRequest
/// </summary>
public class AzureTtsSynthesizeRequestValidator : AbstractValidator<AzureTtsSynthesizeRequest>
{
    public AzureTtsSynthesizeRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required")
            .MaximumLength(10000).WithMessage("Text must not exceed 10000 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Text contains potentially dangerous content");

        RuleFor(x => x.VoiceId)
            .NotEmpty().WithMessage("Voice ID is required")
            .MaximumLength(100).WithMessage("Voice ID must not exceed 100 characters");

        When(x => x.Options != null, () =>
        {
            When(x => x.Options!.Rate.HasValue, () =>
            {
                RuleFor(x => x.Options!.Rate!.Value)
                    .InclusiveBetween(-50, 100).WithMessage("Rate must be between -50 and 100");
            });

            When(x => x.Options!.Pitch.HasValue, () =>
            {
                RuleFor(x => x.Options!.Pitch!.Value)
                    .InclusiveBetween(-50, 50).WithMessage("Pitch must be between -50 and 50");
            });

            When(x => x.Options!.Volume.HasValue, () =>
            {
                RuleFor(x => x.Options!.Volume!.Value)
                    .InclusiveBetween(0, 2).WithMessage("Volume must be between 0 and 2");
            });

            When(x => x.Options!.StyleDegree.HasValue, () =>
            {
                RuleFor(x => x.Options!.StyleDegree!.Value)
                    .InclusiveBetween(0.01, 2.0).WithMessage("Style degree must be between 0.01 and 2.0");
            });
        });
    }
}

/// <summary>
/// Validator for ApiKeysRequest
/// </summary>
public class ApiKeysRequestValidator : AbstractValidator<ApiKeysRequest>
{
    public ApiKeysRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.OpenAiKey), () =>
        {
            RuleFor(x => x.OpenAiKey)
                .Must(key => Aura.Api.Security.InputSanitizer.ValidateApiKeyFormat(key, "openai"))
                .WithMessage("OpenAI API key must start with 'sk-' and be at least 20 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ElevenLabsKey), () =>
        {
            RuleFor(x => x.ElevenLabsKey)
                .Must(key => Aura.Api.Security.InputSanitizer.ValidateApiKeyFormat(key, "elevenlabs"))
                .WithMessage("ElevenLabs API key must be a 32-character hexadecimal string");
        });

        When(x => !string.IsNullOrWhiteSpace(x.StabilityAiKey), () =>
        {
            RuleFor(x => x.StabilityAiKey)
                .Must(key => Aura.Api.Security.InputSanitizer.ValidateApiKeyFormat(key, "stability"))
                .WithMessage("Stability AI API key must start with 'sk-' and be at least 20 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PexelsKey), () =>
        {
            RuleFor(x => x.PexelsKey)
                .MinimumLength(20).WithMessage("Pexels API key must be at least 20 characters")
                .MaximumLength(200).WithMessage("Pexels API key must not exceed 200 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PixabayKey), () =>
        {
            RuleFor(x => x.PixabayKey)
                .MinimumLength(20).WithMessage("Pixabay API key must be at least 20 characters")
                .MaximumLength(200).WithMessage("Pixabay API key must not exceed 200 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.UnsplashKey), () =>
        {
            RuleFor(x => x.UnsplashKey)
                .MinimumLength(20).WithMessage("Unsplash API key must be at least 20 characters")
                .MaximumLength(200).WithMessage("Unsplash API key must not exceed 200 characters");
        });
    }
}

/// <summary>
/// Validator for ProviderPathsRequest
/// </summary>
public class ProviderPathsRequestValidator : AbstractValidator<ProviderPathsRequest>
{
    public ProviderPathsRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.StableDiffusionUrl), () =>
        {
            RuleFor(x => x.StableDiffusionUrl)
                .Must(url => Aura.Api.Security.InputSanitizer.IsValidUrl(url!))
                .WithMessage("Stable Diffusion URL must be a valid HTTP or HTTPS URL");
        });

        When(x => !string.IsNullOrWhiteSpace(x.OllamaUrl), () =>
        {
            RuleFor(x => x.OllamaUrl)
                .Must(url => Aura.Api.Security.InputSanitizer.IsValidUrl(url!))
                .WithMessage("Ollama URL must be a valid HTTP or HTTPS URL");
        });

        When(x => !string.IsNullOrWhiteSpace(x.FfmpegPath), () =>
        {
            RuleFor(x => x.FfmpegPath)
                .Must(path => !path!.Contains("..", StringComparison.OrdinalIgnoreCase))
                .WithMessage("FFmpeg path contains directory traversal attempt")
                .Must(path => Aura.Api.Security.InputSanitizer.IsAllowedFileExtension(path!, new[] { ".exe", "" }))
                .WithMessage("FFmpeg path must point to an executable");
        });

        When(x => !string.IsNullOrWhiteSpace(x.FfprobePath), () =>
        {
            RuleFor(x => x.FfprobePath)
                .Must(path => !path!.Contains("..", StringComparison.OrdinalIgnoreCase))
                .WithMessage("FFprobe path contains directory traversal attempt")
                .Must(path => Aura.Api.Security.InputSanitizer.IsAllowedFileExtension(path!, new[] { ".exe", "" }))
                .WithMessage("FFprobe path must point to an executable");
        });

        When(x => !string.IsNullOrWhiteSpace(x.OutputDirectory), () =>
        {
            RuleFor(x => x.OutputDirectory)
                .Must(path => !path!.Contains("..", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Output directory contains directory traversal attempt");
        });
    }
}

/// <summary>
/// Validator for PromptModifiersDto
/// </summary>
public class PromptModifiersDtoValidator : AbstractValidator<PromptModifiersDto>
{
    public PromptModifiersDtoValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.AdditionalInstructions), () =>
        {
            RuleFor(x => x.AdditionalInstructions)
                .MaximumLength(5000).WithMessage("Additional instructions must not exceed 5000 characters")
                .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text!))
                .WithMessage("Additional instructions contain potentially dangerous content");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ExampleStyle), () =>
        {
            RuleFor(x => x.ExampleStyle)
                .MaximumLength(2000).WithMessage("Example style must not exceed 2000 characters")
                .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text!))
                .WithMessage("Example style contains potentially dangerous content");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PromptVersion), () =>
        {
            RuleFor(x => x.PromptVersion)
                .MaximumLength(50).WithMessage("Prompt version must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\-_]+$").WithMessage("Prompt version can only contain alphanumeric characters, hyphens, and underscores");
        });
    }
}

/// <summary>
/// Validator for ScriptRefinementConfigDto
/// </summary>
public class ScriptRefinementConfigDtoValidator : AbstractValidator<ScriptRefinementConfigDto>
{
    public ScriptRefinementConfigDtoValidator()
    {
        RuleFor(x => x.MaxRefinementPasses)
            .InclusiveBetween(1, 5).WithMessage("Max refinement passes must be between 1 and 5");

        RuleFor(x => x.QualityThreshold)
            .InclusiveBetween(0.0, 100.0).WithMessage("Quality threshold must be between 0 and 100");

        RuleFor(x => x.MinimumImprovement)
            .InclusiveBetween(0.0, 50.0).WithMessage("Minimum improvement must be between 0 and 50");

        RuleFor(x => x.PassTimeoutMinutes)
            .InclusiveBetween(1, 10).WithMessage("Pass timeout must be between 1 and 10 minutes");
    }
}

/// <summary>
/// Validator for CaptionsRequest
/// </summary>
public class CaptionsRequestValidator : AbstractValidator<CaptionsRequest>
{
    public CaptionsRequestValidator()
    {
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Lines collection is required")
            .NotEmpty().WithMessage("At least one line is required")
            .Must(lines => lines.Count <= 5000).WithMessage("Cannot process more than 5000 lines at once");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Format is required")
            .Must(f => new[] { "SRT", "VTT", "ASS", "SSA" }.Contains(f.ToUpperInvariant()))
            .WithMessage("Format must be one of: SRT, VTT, ASS, SSA");

        When(x => !string.IsNullOrWhiteSpace(x.OutputPath), () =>
        {
            RuleFor(x => x.OutputPath)
                .Must(path => !path!.Contains("..", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Output path contains directory traversal attempt");
        });
    }
}

/// <summary>
/// Validator for RecommendationsRequestDto
/// </summary>
public class RecommendationsRequestValidator : AbstractValidator<RecommendationsRequestDto>
{
    public RecommendationsRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic is required")
            .MinimumLength(3).WithMessage("Topic must be at least 3 characters")
            .MaximumLength(500).WithMessage("Topic must not exceed 500 characters")
            .Must(text => !Aura.Api.Security.InputSanitizer.ContainsXssPattern(text))
            .WithMessage("Topic contains potentially dangerous content");

        When(x => !string.IsNullOrWhiteSpace(x.Audience), () =>
        {
            RuleFor(x => x.Audience)
                .MaximumLength(200).WithMessage("Audience must not exceed 200 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Goal), () =>
        {
            RuleFor(x => x.Goal)
                .MaximumLength(300).WithMessage("Goal must not exceed 300 characters");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Tone), () =>
        {
            RuleFor(x => x.Tone)
                .MaximumLength(100).WithMessage("Tone must not exceed 100 characters");
        });

        RuleFor(x => x.TargetDurationMinutes)
            .GreaterThan(0).WithMessage("Target duration must be greater than 0")
            .LessThanOrEqualTo(120).WithMessage("Target duration must not exceed 120 minutes");
    }
}
