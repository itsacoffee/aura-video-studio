using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Azure voice discovery service using the REST API
/// </summary>
public class AzureVoiceDiscovery
{
    private readonly ILogger<AzureVoiceDiscovery> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _region;
    private readonly string? _apiKey;
    
    private List<VoiceDescriptor>? _cachedVoices;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    public AzureVoiceDiscovery(
        ILogger<AzureVoiceDiscovery> logger,
        HttpClient httpClient,
        string region,
        string? apiKey)
    {
        _logger = logger;
        _httpClient = httpClient;
        _region = region;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Get all available Azure voices
    /// </summary>
    public async Task<IReadOnlyList<VoiceDescriptor>> GetVoicesAsync(
        string? locale = null,
        VoiceGender? gender = null,
        VoiceType? voiceType = null,
        CancellationToken ct = default)
    {
        // Check cache
        if (_cachedVoices != null && DateTime.UtcNow < _cacheExpiry)
        {
            _logger.LogDebug("Returning cached Azure voices");
            return FilterVoices(_cachedVoices, locale, gender, voiceType);
        }

        try
        {
            // Call Azure voices/list REST API
            var url = $"https://{_region}.tts.speech.microsoft.com/cognitiveservices/voices/list";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            }

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var voicesJson = await response.Content.ReadFromJsonAsync<List<AzureVoiceInfo>>(cancellationToken: ct);
            
            if (voicesJson == null)
            {
                _logger.LogWarning("Failed to parse Azure voices response");
                return Array.Empty<VoiceDescriptor>();
            }

            // Convert to VoiceDescriptor
            var voices = voicesJson.Select(ConvertToVoiceDescriptor).ToList();
            
            // Cache the results
            _cachedVoices = voices;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
            
            _logger.LogInformation("Retrieved and cached {Count} Azure voices", voices.Count);
            
            return FilterVoices(voices, locale, gender, voiceType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Azure voices from REST API");
            return Array.Empty<VoiceDescriptor>();
        }
    }

    /// <summary>
    /// Get capabilities for a specific voice
    /// </summary>
    public async Task<VoiceDescriptor?> GetVoiceCapabilitiesAsync(string voiceId, CancellationToken ct = default)
    {
        var voices = await GetVoicesAsync(ct: ct);
        return voices.FirstOrDefault(v => v.Id.Equals(voiceId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clear the voice cache (useful for testing or forcing refresh)
    /// </summary>
    public void ClearCache()
    {
        _cachedVoices = null;
        _cacheExpiry = DateTime.MinValue;
        _logger.LogInformation("Azure voice cache cleared");
    }

    private VoiceDescriptor ConvertToVoiceDescriptor(AzureVoiceInfo voiceInfo)
    {
        // Parse gender
        var gender = voiceInfo.Gender?.ToLowerInvariant() switch
        {
            "male" => VoiceGender.Male,
            "female" => VoiceGender.Female,
            _ => VoiceGender.Neutral
        };

        // Determine voice type (Neural vs Standard)
        var voiceType = voiceInfo.ShortName?.Contains("Neural", StringComparison.OrdinalIgnoreCase) == true
            ? VoiceType.Neural
            : VoiceType.Standard;

        // Determine supported features
        var features = VoiceFeatures.Basic | VoiceFeatures.Breaks | VoiceFeatures.Emphasis;
        
        if (voiceType == VoiceType.Neural)
        {
            features |= VoiceFeatures.Prosody | VoiceFeatures.AudioEffects;
        }

        if (voiceInfo.StyleList != null && voiceInfo.StyleList.Length > 0)
        {
            features |= VoiceFeatures.Styles;
        }

        if (voiceInfo.RolePlayList != null && voiceInfo.RolePlayList.Length > 0)
        {
            features |= VoiceFeatures.Roles;
        }

        // All Azure voices support phonemes and say-as
        features |= VoiceFeatures.Phonemes | VoiceFeatures.SayAs;

        return new VoiceDescriptor
        {
            Id = voiceInfo.ShortName ?? voiceInfo.Name ?? "unknown",
            Name = voiceInfo.DisplayName ?? voiceInfo.Name ?? "Unknown",
            Provider = VoiceProvider.Azure,
            Locale = voiceInfo.Locale ?? "en-US",
            Gender = gender,
            VoiceType = voiceType,
            AvailableStyles = voiceInfo.StyleList ?? Array.Empty<string>(),
            AvailableRoles = voiceInfo.RolePlayList ?? Array.Empty<string>(),
            SupportedFeatures = features,
            Description = voiceInfo.LocalName,
            LocalName = voiceInfo.LocalName,
            SampleUrl = voiceInfo.SampleRateHertz != null 
                ? $"https://speech.microsoft.com/cognitiveservices/v1/stream?voice={voiceInfo.ShortName}" 
                : null
        };
    }

    private IReadOnlyList<VoiceDescriptor> FilterVoices(
        List<VoiceDescriptor> voices,
        string? locale,
        VoiceGender? gender,
        VoiceType? voiceType)
    {
        var filtered = voices.AsEnumerable();

        if (!string.IsNullOrEmpty(locale))
        {
            filtered = filtered.Where(v => v.Locale.StartsWith(locale, StringComparison.OrdinalIgnoreCase));
        }

        if (gender.HasValue)
        {
            filtered = filtered.Where(v => v.Gender == gender.Value);
        }

        if (voiceType.HasValue)
        {
            filtered = filtered.Where(v => v.VoiceType == voiceType.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Azure voice information from REST API
    /// </summary>
    private class AzureVoiceInfo
    {
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        public string? DisplayName { get; set; }
        public string? LocalName { get; set; }
        public string? Gender { get; set; }
        public string? Locale { get; set; }
        public string[]? StyleList { get; set; }
        public string[]? RolePlayList { get; set; }
        public int? SampleRateHertz { get; set; }
        public string? VoiceType { get; set; }
        public string? Status { get; set; }
    }
}
