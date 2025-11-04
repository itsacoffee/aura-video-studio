using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// Service for generating perceptual hashes for image/video deduplication
/// Uses a simplified approach based on URL and dimensions
/// </summary>
public class PerceptualHashService
{
    /// <summary>
    /// Generates a perceptual hash for media based on URL and dimensions
    /// </summary>
    public string GenerateHash(string url, int width, int height)
    {
        var normalizedUrl = NormalizeUrl(url);
        var input = $"{normalizedUrl}:{width}x{height}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes)[..16];
    }

    /// <summary>
    /// Calculates similarity between two hashes (0.0 = completely different, 1.0 = identical)
    /// </summary>
    public double CalculateSimilarity(string hash1, string hash2)
    {
        if (string.IsNullOrEmpty(hash1) || string.IsNullOrEmpty(hash2))
            return 0.0;

        if (hash1.Length != hash2.Length)
            return 0.0;

        int matches = 0;
        for (int i = 0; i < hash1.Length; i++)
        {
            if (hash1[i] == hash2[i])
                matches++;
        }

        return (double)matches / hash1.Length;
    }

    /// <summary>
    /// Determines if two hashes represent duplicate content
    /// </summary>
    public bool IsDuplicate(string hash1, string hash2, double threshold = 0.90)
    {
        var similarity = CalculateSimilarity(hash1, hash2);
        return similarity >= threshold;
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        url = url.Trim().ToLowerInvariant();
        
        if (url.StartsWith("http://"))
            url = "https://" + url.Substring(7);
        
        var queryIndex = url.IndexOf('?');
        if (queryIndex > 0)
            url = url.Substring(0, queryIndex);
        
        return url;
    }
}
