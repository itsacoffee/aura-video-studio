using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Aura.Api.Security;

/// <summary>
/// Provides input sanitization methods to prevent security vulnerabilities
/// </summary>
public static class InputSanitizer
{
    private static readonly char[] PathSeparators = new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
    
    // Dangerous characters for file paths
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars()
        .Concat(new[] { '<', '>', ':', '"', '|', '?', '*' })
        .Distinct()
        .ToArray();

    // SQL injection patterns
    private static readonly Regex SqlInjectionPattern = new Regex(
        @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)|('|(--)|;|/\*|\*/|xp_)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // XSS patterns
    private static readonly Regex XssPattern = new Regex(
        @"<script[^>]*>.*?</script>|javascript:|on\w+\s*=|<iframe|<object|<embed",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a file path to prevent directory traversal attacks
    /// </summary>
    /// <param name="filePath">The file path to sanitize</param>
    /// <param name="baseDirectory">The base directory that the path should be within</param>
    /// <returns>Sanitized file path</returns>
    /// <exception cref="ArgumentException">Thrown if path is outside base directory or contains invalid characters</exception>
    public static string SanitizeFilePath(string filePath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("Base directory cannot be empty", nameof(baseDirectory));
        }

        // Remove any path traversal sequences
        var sanitized = filePath.Replace("..", string.Empty);

        // Remove null characters
        sanitized = sanitized.Replace("\0", string.Empty);

        // Check for invalid characters
        if (sanitized.IndexOfAny(InvalidPathChars) >= 0)
        {
            throw new ArgumentException($"File path contains invalid characters: {filePath}", nameof(filePath));
        }

        // Get full paths for comparison
        var fullBasePath = Path.GetFullPath(baseDirectory);
        var fullFilePath = Path.GetFullPath(Path.Combine(fullBasePath, sanitized));

        // Ensure the resolved path is within the base directory
        if (!fullFilePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"File path is outside the allowed directory: {filePath}", nameof(filePath));
        }

        return fullFilePath;
    }

    /// <summary>
    /// Validates and sanitizes a filename (without path)
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        }

        // Remove path separators
        var sanitized = fileName;
        foreach (var sep in PathSeparators)
        {
            sanitized = sanitized.Replace(sep.ToString(), string.Empty);
        }

        // Remove invalid file name characters
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c.ToString(), string.Empty);
        }

        // Remove path traversal attempts
        sanitized = sanitized.Replace("..", string.Empty);

        // Remove null characters
        sanitized = sanitized.Replace("\0", string.Empty);

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ArgumentException($"File name becomes empty after sanitization: {fileName}", nameof(fileName));
        }

        // Limit filename length
        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, 255 - extension.Length) + extension;
        }

        return sanitized;
    }

    /// <summary>
    /// Checks if input contains potential SQL injection patterns
    /// Note: This is a defense-in-depth measure. Always use parameterized queries as the primary defense.
    /// </summary>
    public static bool ContainsSqlInjectionPattern(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return SqlInjectionPattern.IsMatch(input);
    }

    /// <summary>
    /// Checks if input contains potential XSS attack patterns
    /// </summary>
    public static bool ContainsXssPattern(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return XssPattern.IsMatch(input);
    }

    /// <summary>
    /// Sanitizes user input by removing potential XSS attack vectors
    /// </summary>
    public static string SanitizeForXss(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // HTML encode the input to prevent XSS
        var sanitized = HttpUtility.HtmlEncode(input);

        // Additional safety: remove script tags and event handlers
        sanitized = XssPattern.Replace(sanitized, string.Empty);

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a string for safe use in file names or identifiers
    /// </summary>
    public static string SanitizeForIdentifier(string input, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be empty", nameof(input));
        }

        // Remove or replace invalid characters
        var sanitized = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sanitized.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                sanitized.Append('_');
            }
            // Skip other characters
        }

        var result = sanitized.ToString().Trim('_', '-');
        
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new ArgumentException($"Input becomes empty after sanitization: {input}", nameof(input));
        }

        // Limit length
        if (result.Length > maxLength)
        {
            result = result.Substring(0, maxLength);
        }

        return result;
    }

    /// <summary>
    /// Validates that a URL is safe and uses allowed protocols
    /// </summary>
    public static bool IsValidUrl(string url, string[] allowedSchemes = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        allowedSchemes ??= new[] { "http", "https" };

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return allowedSchemes.Contains(uri.Scheme.ToLowerInvariant());
    }

    /// <summary>
    /// Sanitizes a string for use in log messages to prevent log injection
    /// </summary>
    public static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Remove newlines and carriage returns that could be used for log injection
        return input
            .Replace("\r", string.Empty)
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();
    }

    /// <summary>
    /// Validates and sanitizes an email address
    /// </summary>
    public static string SanitizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        email = email.Trim().ToLowerInvariant();

        // Basic email regex pattern
        var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        
        if (!emailPattern.IsMatch(email))
        {
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));
        }

        return email;
    }
}
