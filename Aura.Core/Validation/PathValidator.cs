using System;
using System.IO;

namespace Aura.Core.Validation;

/// <summary>
/// Utility for validating file paths to prevent path traversal attacks
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// Checks if a user-provided path is safe (doesn't escape the base directory)
    /// </summary>
    /// <param name="userPath">The user-provided path to validate</param>
    /// <param name="baseDirectory">The base directory that the path must stay within</param>
    /// <returns>True if the path is safe, false otherwise</returns>
    public static bool IsPathSafe(string userPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(userPath))
            return false;

        if (string.IsNullOrWhiteSpace(baseDirectory))
            return false;

        try
        {
            // Resolve both paths to their full canonical forms
            var fullPath = Path.GetFullPath(userPath);
            var baseFullPath = Path.GetFullPath(baseDirectory);

            // Ensure the resolved path starts with the base directory
            // Use case-insensitive comparison on Windows, case-sensitive on Unix
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return fullPath.StartsWith(baseFullPath, comparison);
        }
        catch
        {
            // If path resolution fails (invalid characters, etc.), it's not safe
            return false;
        }
    }

    /// <summary>
    /// Validates a path and throws an exception if it's unsafe
    /// </summary>
    /// <param name="userPath">The user-provided path to validate</param>
    /// <param name="baseDirectory">The base directory that the path must stay within</param>
    /// <exception cref="SecurityException">Thrown if the path is unsafe</exception>
    public static void ValidatePath(string userPath, string baseDirectory)
    {
        if (!IsPathSafe(userPath, baseDirectory))
        {
            throw new System.Security.SecurityException(
                $"Path traversal detected: '{userPath}' attempts to escape base directory '{baseDirectory}'");
        }
    }

    /// <summary>
    /// Validates a path and returns a safe, canonical path
    /// </summary>
    /// <param name="userPath">The user-provided path to validate</param>
    /// <param name="baseDirectory">The base directory that the path must stay within</param>
    /// <returns>A safe, canonical path</returns>
    /// <exception cref="SecurityException">Thrown if the path is unsafe</exception>
    /// <exception cref="ArgumentException">Thrown if the path is null or empty</exception>
    public static string GetSafePath(string userPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(userPath))
            throw new ArgumentException("Path cannot be null or empty", nameof(userPath));

        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be null or empty", nameof(baseDirectory));

        ValidatePath(userPath, baseDirectory);

        // Return the canonical path
        return Path.GetFullPath(userPath);
    }

    /// <summary>
    /// Checks if a path contains any path traversal sequences
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path contains traversal sequences, false otherwise</returns>
    public static bool ContainsTraversalSequences(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Check for common path traversal patterns
        var normalized = path.Replace('\\', '/');
        
        return normalized.Contains("../") ||
               normalized.Contains("..\\") ||
               normalized.StartsWith("../", StringComparison.Ordinal) ||
               normalized.StartsWith("..\\", StringComparison.Ordinal) ||
               normalized.Contains("/../") ||
               normalized.Contains("\\..\\") ||
               normalized == ".." ||
               normalized.Contains("//") ||
               normalized.Contains("\\\\");
    }
}

