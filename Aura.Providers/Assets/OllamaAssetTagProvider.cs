using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Assets;

/// <summary>
/// Asset tagging provider that uses Ollama LLM for intelligent asset tagging.
/// Supports image analysis using multimodal models (like llava) when available.
/// </summary>
public class OllamaAssetTagProvider : IAssetTagProvider
{
    private readonly ILogger<OllamaAssetTagProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string _embeddingModel;
    private readonly TimeSpan _timeout;

    // Cache availability check results
    private DateTime _lastAvailabilityCheck = DateTime.MinValue;
    private bool _lastAvailabilityResult;
    private readonly TimeSpan _availabilityCacheDuration = TimeSpan.FromSeconds(30);
    private readonly object _cacheLock = new();

    public string Name => "Ollama";

    public OllamaAssetTagProvider(
        ILogger<OllamaAssetTagProvider> logger,
        HttpClient httpClient,
        string baseUrl = "http://127.0.0.1:11434",
        string model = "llava:7b",
        string embeddingModel = "nomic-embed-text",
        int timeoutSeconds = 120)
    {
        _logger = logger;
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _embeddingModel = embeddingModel;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);

        _logger.LogInformation(
            "OllamaAssetTagProvider initialized: baseUrl={BaseUrl}, model={Model}, embeddingModel={EmbeddingModel}",
            _baseUrl, _model, _embeddingModel);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (DateTime.UtcNow - _lastAvailabilityCheck < _availabilityCacheDuration)
            {
                return _lastAvailabilityResult;
            }
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/version", cts.Token).ConfigureAwait(false);
            var isAvailable = response.IsSuccessStatusCode;

            lock (_cacheLock)
            {
                _lastAvailabilityCheck = DateTime.UtcNow;
                _lastAvailabilityResult = isAvailable;
            }

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama availability check failed");
            
            lock (_cacheLock)
            {
                _lastAvailabilityCheck = DateTime.UtcNow;
                _lastAvailabilityResult = false;
            }
            
            return false;
        }
    }

    public async Task<SemanticAssetMetadata> GenerateTagsAsync(
        string assetPath,
        AssetType assetType,
        AssetMetadata? existingMetadata = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating tags for asset: {Path}, type: {Type}", assetPath, assetType);
        var startTime = DateTime.UtcNow;

        try
        {
            string prompt;
            string? base64Image = null;

            // For images, try to use multimodal capability if file exists
            if (assetType == AssetType.Image && File.Exists(assetPath))
            {
                try
                {
                    var imageBytes = await File.ReadAllBytesAsync(assetPath, ct).ConfigureAwait(false);
                    base64Image = Convert.ToBase64String(imageBytes);
                    prompt = BuildImageAnalysisPrompt();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read image file, falling back to text-based tagging");
                    prompt = BuildTextBasedPrompt(assetPath, assetType, existingMetadata);
                }
            }
            else
            {
                prompt = BuildTextBasedPrompt(assetPath, assetType, existingMetadata);
            }

            var result = await CallOllamaAsync(prompt, base64Image, ct).ConfigureAwait(false);
            var metadata = ParseTaggingResponse(result, assetPath, startTime);

            _logger.LogInformation(
                "Generated {TagCount} tags for asset {Path} in {Duration}ms",
                metadata.Tags.Count,
                assetPath,
                (DateTime.UtcNow - startTime).TotalMilliseconds);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tags for asset {Path}", assetPath);
            
            // Return minimal metadata on failure
            return new SemanticAssetMetadata
            {
                AssetId = Guid.NewGuid(),
                Tags = new List<AssetTag>(),
                TaggedAt = DateTime.UtcNow,
                TaggingProvider = Name,
                ConfidenceScore = 0
            };
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        _logger.LogDebug("Generating embedding for text: {TextPreview}...", 
            text.Length > 50 ? text.Substring(0, 50) : text);

        try
        {
            var requestBody = new
            {
                model = _embeddingModel,
                prompt = text
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeout);

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/embeddings", content, cts.Token).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding request failed with status {Status}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            
            if (doc.RootElement.TryGetProperty("embedding", out var embeddingProp) &&
                embeddingProp.ValueKind == JsonValueKind.Array)
            {
                var embedding = embeddingProp.EnumerateArray()
                    .Select(e => (float)e.GetDouble())
                    .ToArray();
                
                _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);
                return embedding;
            }

            _logger.LogWarning("Embedding response did not contain expected 'embedding' array");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding");
            return null;
        }
    }

    public async Task<List<SemanticSearchResult>> MatchAssetsToSceneAsync(
        string sceneDescription,
        IEnumerable<(Guid AssetId, SemanticAssetMetadata Metadata)> availableAssets,
        int maxResults = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Matching assets to scene: {Description}", 
            sceneDescription.Length > 100 ? sceneDescription.Substring(0, 100) + "..." : sceneDescription);

        var assetList = availableAssets.ToList();
        if (assetList.Count == 0)
        {
            return new List<SemanticSearchResult>();
        }

        try
        {
            // Build a prompt for the LLM to rank assets
            var prompt = BuildAssetMatchingPrompt(sceneDescription, assetList);
            var result = await CallOllamaAsync(prompt, null, ct).ConfigureAwait(false);
            
            return ParseMatchingResponse(result, assetList, maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM-based asset matching failed, falling back to keyword matching");
            return PerformKeywordMatching(sceneDescription, assetList, maxResults);
        }
    }

    private string BuildImageAnalysisPrompt()
    {
        return @"Analyze this image and provide a detailed description in JSON format with the following fields:
{
  ""description"": ""A detailed description of what the image shows"",
  ""subject"": ""The main subject or focus of the image"",
  ""mood"": ""The emotional mood or atmosphere (e.g., cheerful, dramatic, calm, energetic)"",
  ""dominantColor"": ""The dominant color in the image"",
  ""tags"": [
    {""name"": ""tag1"", ""confidence"": 0.9, ""category"": ""Subject""},
    {""name"": ""tag2"", ""confidence"": 0.8, ""category"": ""Style""}
  ]
}

Tag categories can be: Subject, Style, Mood, Color, Setting, Action, Object.
Provide 5-10 relevant tags with confidence scores between 0 and 1.
Return ONLY the JSON object, no additional text.";
    }

    private string BuildTextBasedPrompt(string assetPath, AssetType assetType, AssetMetadata? metadata)
    {
        var filename = Path.GetFileNameWithoutExtension(assetPath);
        var extension = Path.GetExtension(assetPath);
        
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine($"Asset type: {assetType}");
        contextBuilder.AppendLine($"Filename: {filename}");
        contextBuilder.AppendLine($"Format: {extension}");
        
        if (metadata != null)
        {
            if (metadata.Width.HasValue && metadata.Height.HasValue)
            {
                contextBuilder.AppendLine($"Resolution: {metadata.Width}x{metadata.Height}");
            }
            if (metadata.Duration.HasValue)
            {
                contextBuilder.AppendLine($"Duration: {metadata.Duration.Value.TotalSeconds:F1} seconds");
            }
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                contextBuilder.AppendLine($"Description: {metadata.Description}");
            }
        }

        return $@"Based on the following asset information, generate semantic tags for content discovery and matching.

{contextBuilder}

Return a JSON object with the following structure:
{{
  ""description"": ""A brief description based on the filename and metadata"",
  ""subject"": ""The likely main subject"",
  ""mood"": ""The likely mood or atmosphere"",
  ""tags"": [
    {{""name"": ""tag1"", ""confidence"": 0.7, ""category"": ""Subject""}},
    {{""name"": ""tag2"", ""confidence"": 0.6, ""category"": ""Style""}}
  ]
}}

Tag categories: Subject, Style, Mood, Color, Setting, Action, Object.
Return ONLY the JSON object.";
    }

    private string BuildAssetMatchingPrompt(string sceneDescription, List<(Guid AssetId, SemanticAssetMetadata Metadata)> assets)
    {
        var assetDescriptions = new StringBuilder();
        for (int i = 0; i < assets.Count; i++)
        {
            var (id, metadata) = assets[i];
            var tags = string.Join(", ", metadata.Tags.Take(5).Select(t => t.Name));
            assetDescriptions.AppendLine($"Asset {i}: ID={id}, Tags=[{tags}], Mood={metadata.Mood ?? "unknown"}, Subject={metadata.Subject ?? "unknown"}");
        }

        return $@"Match the following assets to a video scene. Return the best matching assets ranked by relevance.

Scene Description: {sceneDescription}

Available Assets:
{assetDescriptions}

Return a JSON array of matches:
[
  {{""index"": 0, ""score"": 0.9, ""reason"": ""Why this asset matches""}},
  {{""index"": 2, ""score"": 0.7, ""reason"": ""Why this asset matches""}}
]

Return the top 5 matches ordered by score (highest first).
Return ONLY the JSON array.";
    }

    private async Task<string> CallOllamaAsync(string prompt, string? base64Image, CancellationToken ct)
    {
        object requestBody;
        
        if (!string.IsNullOrEmpty(base64Image))
        {
            // Multimodal request with image
            requestBody = new
            {
                model = _model,
                prompt = prompt,
                images = new[] { base64Image },
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    num_predict = 1024
                }
            };
        }
        else
        {
            // Text-only request
            requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                format = "json",
                options = new
                {
                    temperature = 0.3,
                    num_predict = 1024
                }
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("response", out var responseProp))
        {
            return responseProp.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("Ollama response did not contain 'response' field");
    }

    private SemanticAssetMetadata ParseTaggingResponse(string response, string assetPath, DateTime startTime)
    {
        var tags = new List<AssetTag>();
        string? description = null;
        string? subject = null;
        string? mood = null;
        string? dominantColor = null;
        float confidence = 0.5f;

        try
        {
            // Try to extract JSON from the response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("description", out var descProp))
                {
                    description = descProp.GetString();
                }
                
                if (root.TryGetProperty("subject", out var subProp))
                {
                    subject = subProp.GetString();
                }
                
                if (root.TryGetProperty("mood", out var moodProp))
                {
                    mood = moodProp.GetString();
                }
                
                if (root.TryGetProperty("dominantColor", out var colorProp))
                {
                    dominantColor = colorProp.GetString();
                }

                if (root.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tagElement in tagsProp.EnumerateArray())
                    {
                        var name = tagElement.TryGetProperty("name", out var nameProp) 
                            ? nameProp.GetString() ?? "" 
                            : "";
                        
                        var tagConfidence = tagElement.TryGetProperty("confidence", out var confProp)
                            ? (float)confProp.GetDouble()
                            : 0.7f;
                        
                        var categoryStr = tagElement.TryGetProperty("category", out var catProp)
                            ? catProp.GetString() ?? "Subject"
                            : "Subject";
                        
                        if (!Enum.TryParse<TagCategory>(categoryStr, ignoreCase: true, out var category))
                        {
                            category = TagCategory.Subject;
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            tags.Add(new AssetTag(name, tagConfidence, category));
                        }
                    }
                }

                confidence = tags.Count > 0 ? tags.Average(t => t.Confidence) : 0.5f;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse tagging response as JSON, extracting tags from text");
            tags = ExtractTagsFromText(response);
        }

        return new SemanticAssetMetadata
        {
            AssetId = Guid.NewGuid(),
            Tags = tags,
            Description = description,
            Subject = subject,
            Mood = mood,
            DominantColor = dominantColor,
            TaggedAt = DateTime.UtcNow,
            TaggingProvider = Name,
            ConfidenceScore = confidence
        };
    }

    private List<AssetTag> ExtractTagsFromText(string text)
    {
        var tags = new List<AssetTag>();
        
        // Extract words that might be tags
        var words = text.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ':', ';', '\n', '\r', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3 && w.Length <= 25)
            .Distinct()
            .Take(10);

        foreach (var word in words)
        {
            tags.Add(new AssetTag(word, 0.5f, TagCategory.Subject));
        }

        return tags;
    }

    private List<SemanticSearchResult> ParseMatchingResponse(
        string response,
        List<(Guid AssetId, SemanticAssetMetadata Metadata)> assets,
        int maxResults)
    {
        var results = new List<SemanticSearchResult>();

        try
        {
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonString);
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var index = element.TryGetProperty("index", out var indexProp) 
                        ? indexProp.GetInt32() 
                        : -1;
                    
                    if (index < 0 || index >= assets.Count) continue;
                    
                    var score = element.TryGetProperty("score", out var scoreProp)
                        ? (float)scoreProp.GetDouble()
                        : 0.5f;
                    
                    var reason = element.TryGetProperty("reason", out var reasonProp)
                        ? reasonProp.GetString()
                        : null;

                    var (assetId, metadata) = assets[index];
                    results.Add(new SemanticSearchResult
                    {
                        AssetId = assetId,
                        SimilarityScore = score,
                        MatchedTags = metadata.Tags.Take(5).Select(t => t.Name).ToList(),
                        MatchReason = reason
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse matching response");
        }

        return results.Take(maxResults).ToList();
    }

    private List<SemanticSearchResult> PerformKeywordMatching(
        string sceneDescription,
        List<(Guid AssetId, SemanticAssetMetadata Metadata)> assets,
        int maxResults)
    {
        var queryWords = sceneDescription.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToHashSet();

        var results = new List<SemanticSearchResult>();

        foreach (var (assetId, metadata) in assets)
        {
            var assetWords = new HashSet<string>();
            
            foreach (var tag in metadata.Tags)
            {
                assetWords.Add(tag.Name);
            }
            
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                foreach (var word in metadata.Description.ToLowerInvariant().Split(' '))
                {
                    if (word.Length > 2) assetWords.Add(word);
                }
            }

            var matchedWords = queryWords.Intersect(assetWords).ToList();
            
            if (matchedWords.Count > 0)
            {
                var score = (float)matchedWords.Count / Math.Max(queryWords.Count, 1);
                results.Add(new SemanticSearchResult
                {
                    AssetId = assetId,
                    SimilarityScore = Math.Min(score, 1.0f),
                    MatchedTags = matchedWords,
                    MatchReason = $"Matched keywords: {string.Join(", ", matchedWords)}"
                });
            }
        }

        return results.OrderByDescending(r => r.SimilarityScore).Take(maxResults).ToList();
    }
}
