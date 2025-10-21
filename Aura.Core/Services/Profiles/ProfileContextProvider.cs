using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Profiles;

/// <summary>
/// Provides profile context to AI services for personalized behavior
/// </summary>
public class ProfileContextProvider
{
    private readonly ILogger<ProfileContextProvider> _logger;
    private readonly ProfileService _profileService;

    public ProfileContextProvider(
        ILogger<ProfileContextProvider> logger,
        ProfileService profileService)
    {
        _logger = logger;
        _profileService = profileService;
    }

    /// <summary>
    /// Get context string for AI prompts based on active profile
    /// </summary>
    public async Task<string> GetProfileContextAsync(
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileService.GetActiveProfileAsync(userId, ct);
            if (profile == null)
            {
                _logger.LogWarning("No active profile found for user {UserId}", userId);
                return string.Empty;
            }

            var preferences = await _profileService.GetPreferencesAsync(profile.ProfileId, ct);
            return BuildContextString(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile context for user {UserId}", userId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Get preferences for active profile
    /// </summary>
    public async Task<ProfilePreferences?> GetActivePreferencesAsync(
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileService.GetActiveProfileAsync(userId, ct);
            if (profile == null)
            {
                return null;
            }

            return await _profileService.GetPreferencesAsync(profile.ProfileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active preferences for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Build context string from preferences for AI prompts
    /// </summary>
    private static string BuildContextString(ProfilePreferences preferences)
    {
        var sb = new StringBuilder();

        sb.AppendLine("User Preferences:");
        
        if (!string.IsNullOrEmpty(preferences.ContentType))
        {
            sb.AppendLine($"- Content Type: {preferences.ContentType}");
        }

        if (preferences.Tone != null)
        {
            sb.AppendLine($"- Tone: Formality level {preferences.Tone.Formality}/100, Energy level {preferences.Tone.Energy}/100");
            if (preferences.Tone.PersonalityTraits?.Count > 0)
            {
                sb.AppendLine($"  Personality: {string.Join(", ", preferences.Tone.PersonalityTraits)}");
            }
            if (!string.IsNullOrEmpty(preferences.Tone.CustomDescription))
            {
                sb.AppendLine($"  Description: {preferences.Tone.CustomDescription}");
            }
        }

        if (preferences.Visual != null)
        {
            sb.AppendLine($"- Visual Style: {preferences.Visual.Aesthetic ?? "balanced"}");
            sb.AppendLine($"  Color Palette: {preferences.Visual.ColorPalette ?? "natural"}");
            sb.AppendLine($"  Pacing: {preferences.Visual.PacingPreference ?? "moderate"}");
        }

        if (preferences.Audio != null && preferences.Audio.MusicGenres?.Count > 0)
        {
            sb.AppendLine($"- Music: {string.Join(", ", preferences.Audio.MusicGenres)}");
            sb.AppendLine($"  Energy: {preferences.Audio.MusicEnergy}/100");
        }

        if (preferences.Editing != null)
        {
            sb.AppendLine($"- Editing: Pacing {preferences.Editing.Pacing}/100, Cut frequency {preferences.Editing.CutFrequency}/100");
            sb.AppendLine($"  Style: {preferences.Editing.EditingPhilosophy ?? "balanced"}");
        }

        if (preferences.Platform != null)
        {
            sb.AppendLine($"- Platform: {preferences.Platform.PrimaryPlatform ?? "YouTube"}");
            sb.AppendLine($"  Aspect Ratio: {preferences.Platform.AspectRatio ?? "16:9"}");
            if (preferences.Platform.TargetDurationSeconds.HasValue)
            {
                sb.AppendLine($"  Target Duration: {preferences.Platform.TargetDurationSeconds}s");
            }
        }

        if (preferences.AIBehavior != null)
        {
            sb.AppendLine($"- AI Behavior: Assistance level {preferences.AIBehavior.AssistanceLevel}/100, Creativity {preferences.AIBehavior.CreativityLevel}/100");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Adjust AI parameters based on profile preferences
    /// </summary>
    public static void ApplyPreferencesToPrompt(
        ProfilePreferences preferences,
        ref string prompt)
    {
        var context = BuildContextString(preferences);
        if (!string.IsNullOrEmpty(context))
        {
            prompt = $"{context}\n\n{prompt}";
        }
    }

    /// <summary>
    /// Get tone guidance string for AI prompts
    /// </summary>
    public static string GetToneGuidance(ProfilePreferences preferences)
    {
        if (preferences.Tone == null)
        {
            return "Use a professional, balanced tone.";
        }

        var guidance = new StringBuilder();
        
        // Formality
        if (preferences.Tone.Formality < 30)
        {
            guidance.Append("Use a very casual, conversational tone. ");
        }
        else if (preferences.Tone.Formality < 50)
        {
            guidance.Append("Use a casual but clear tone. ");
        }
        else if (preferences.Tone.Formality < 70)
        {
            guidance.Append("Use a professional tone. ");
        }
        else
        {
            guidance.Append("Use a formal, authoritative tone. ");
        }

        // Energy
        if (preferences.Tone.Energy < 30)
        {
            guidance.Append("Keep energy calm and measured. ");
        }
        else if (preferences.Tone.Energy < 50)
        {
            guidance.Append("Maintain moderate energy. ");
        }
        else if (preferences.Tone.Energy < 70)
        {
            guidance.Append("Use upbeat, engaging energy. ");
        }
        else
        {
            guidance.Append("Use high energy and excitement. ");
        }

        // Personality traits
        if (preferences.Tone.PersonalityTraits?.Count > 0)
        {
            guidance.Append($"Personality: {string.Join(", ", preferences.Tone.PersonalityTraits)}. ");
        }

        // Custom description
        if (!string.IsNullOrEmpty(preferences.Tone.CustomDescription))
        {
            guidance.Append(preferences.Tone.CustomDescription);
        }

        return guidance.ToString();
    }

    /// <summary>
    /// Get pacing guidance based on preferences
    /// </summary>
    public static string GetPacingGuidance(ProfilePreferences preferences)
    {
        if (preferences.Editing == null)
        {
            return "Use moderate pacing with balanced cuts.";
        }

        var pacing = preferences.Editing.Pacing;
        
        if (pacing < 30)
        {
            return "Use slow, deliberate pacing with longer takes and minimal cuts.";
        }
        else if (pacing < 50)
        {
            return "Use moderate pacing with thoughtful transitions.";
        }
        else if (pacing < 70)
        {
            return "Use dynamic pacing with regular cuts to maintain engagement.";
        }
        else
        {
            return "Use fast-paced editing with quick cuts and high energy transitions.";
        }
    }

    /// <summary>
    /// Determine if AI should auto-apply suggestions based on preferences
    /// </summary>
    public static bool ShouldAutoApply(ProfilePreferences preferences)
    {
        return preferences.AIBehavior?.AutoApplySuggestions ?? false;
    }

    /// <summary>
    /// Get suggestion verbosity level
    /// </summary>
    public static string GetVerbosityLevel(ProfilePreferences preferences)
    {
        return preferences.AIBehavior?.SuggestionVerbosity ?? "moderate";
    }

    /// <summary>
    /// Get creativity temperature for AI models
    /// </summary>
    public static double GetCreativityTemperature(ProfilePreferences preferences)
    {
        var creativity = preferences.AIBehavior?.CreativityLevel ?? 50;
        // Map 0-100 to 0.3-1.0 temperature range
        return 0.3 + (creativity / 100.0 * 0.7);
    }
}
