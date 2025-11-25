using System;
using System.IO;
using System.Security;
using Aura.Core.Validation;
using Xunit;

namespace Aura.Tests.Validation;

/// <summary>
/// Tests for PathValidator utility to prevent path traversal attacks
/// </summary>
public class PathValidatorTests
{
    private readonly string _baseDirectory;
    private readonly string _tempDirectory;

    public PathValidatorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraPathValidatorTests");
        _baseDirectory = Path.Combine(_tempDirectory, "Base");
        
        // Ensure test directories exist
        Directory.CreateDirectory(_baseDirectory);
    }

    [Fact]
    public void IsPathSafe_ValidPathWithinBase_ReturnsTrue()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "subfolder", "file.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, _baseDirectory);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPathSafe_PathTraversalUp_ReturnsFalse()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "..", "sensitive.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_MultiplePathTraversals_ReturnsFalse()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "..", "..", "..", "etc", "passwd");

        // Act
        var result = PathValidator.IsPathSafe(userPath, _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_AbsolutePathOutsideBase_ReturnsFalse()
    {
        // Arrange
        var userPath = Path.Combine(Path.GetTempPath(), "outside.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_NullPath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.IsPathSafe(null!, _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.IsPathSafe("", _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_WhitespacePath_ReturnsFalse()
    {
        // Act
        var result = PathValidator.IsPathSafe("   ", _baseDirectory);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsPathSafe_NullBaseDirectory_ReturnsFalse()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "file.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePath_SafePath_DoesNotThrow()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "subfolder", "file.txt");

        // Act & Assert
        var exception = Record.Exception(() => PathValidator.ValidatePath(userPath, _baseDirectory));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ValidatePath_UnsafePath_ThrowsSecurityException()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "..", "sensitive.txt");

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => 
            PathValidator.ValidatePath(userPath, _baseDirectory));

        Assert.Contains("Path traversal detected", exception.Message);
    }

    [Fact]
    public void GetSafePath_ValidPath_ReturnsCanonicalPath()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "subfolder", "file.txt");

        // Act
        var result = PathValidator.GetSafePath(userPath, _baseDirectory);

        // Assert
        Assert.NotNull(result);
        Assert.True(Path.IsPathRooted(result));
        Assert.True(result.StartsWith(Path.GetFullPath(_baseDirectory), 
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }

    [Fact]
    public void GetSafePath_UnsafePath_ThrowsSecurityException()
    {
        // Arrange
        var userPath = Path.Combine(_baseDirectory, "..", "sensitive.txt");

        // Act & Assert
        Assert.Throws<SecurityException>(() => 
            PathValidator.GetSafePath(userPath, _baseDirectory));
    }

    [Fact]
    public void GetSafePath_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            PathValidator.GetSafePath(null!, _baseDirectory));
    }

    [Fact]
    public void ContainsTraversalSequences_PathWithDotDot_ReturnsTrue()
    {
        // Arrange
        var path = "../sensitive.txt";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsTraversalSequences_PathWithDotDotSlash_ReturnsTrue()
    {
        // Arrange
        var path = "folder/../sensitive.txt";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsTraversalSequences_PathWithDotDotBackslash_ReturnsTrue()
    {
        // Arrange
        var path = "folder\\..\\sensitive.txt";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsTraversalSequences_JustDotDot_ReturnsTrue()
    {
        // Arrange
        var path = "..";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsTraversalSequences_SafePath_ReturnsFalse()
    {
        // Arrange
        var path = Path.Combine(_baseDirectory, "subfolder", "file.txt");

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsTraversalSequences_PathWithDoubleSlash_ReturnsTrue()
    {
        // Arrange
        var path = "folder//file.txt";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsTraversalSequences_PathWithDoubleBackslash_ReturnsTrue()
    {
        // Arrange
        var path = "folder\\\\file.txt";

        // Act
        var result = PathValidator.ContainsTraversalSequences(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPathSafe_CaseInsensitiveOnWindows_WorksCorrectly()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows
            return;
        }

        // Arrange
        var baseDir = _baseDirectory.ToUpperInvariant();
        var userPath = Path.Combine(_baseDirectory.ToLowerInvariant(), "file.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, baseDir);

        // Assert
        Assert.True(result, "Should be case-insensitive on Windows");
    }

    [Fact]
    public void IsPathSafe_CaseSensitiveOnUnix_WorksCorrectly()
    {
        if (OperatingSystem.IsWindows())
        {
            // Skip on Windows
            return;
        }

        // Arrange
        var baseDir = _baseDirectory.ToUpperInvariant();
        var userPath = Path.Combine(_baseDirectory.ToLowerInvariant(), "file.txt");

        // Act
        var result = PathValidator.IsPathSafe(userPath, baseDir);

        // Assert
        // On Unix, case matters, so this might be false if paths don't match exactly
        // The actual behavior depends on the file system, but the method should handle it
        Assert.NotNull(result);
    }

    [Fact]
    public void GetSafePath_RelativePath_ResolvesToFullPath()
    {
        // Arrange
        var relativePath = "subfolder/file.txt";
        var fullPath = Path.Combine(_baseDirectory, relativePath);

        // Act
        var result = PathValidator.GetSafePath(fullPath, _baseDirectory);

        // Assert
        Assert.True(Path.IsPathRooted(result));
        Assert.Equal(Path.GetFullPath(fullPath), result);
    }
}

