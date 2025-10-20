using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class FileVerificationServiceTests
{
    private readonly FileVerificationService _service;
    private readonly Mock<ILogger<FileVerificationService>> _loggerMock;

    public FileVerificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileVerificationService>>();
        _service = new FileVerificationService(_loggerMock.Object);
    }

    [Fact]
    public async Task ComputeSha256Async_ShouldComputeCorrectHash()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello, World!");

        try
        {
            // Act
            var hash = await _service.ComputeSha256Async(tempFile);

            // Assert
            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length); // SHA-256 is 64 hex characters
            Assert.Equal("dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f", hash);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFileAsync_WithMatchingChecksum_ShouldReturnValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello, World!");
        var expectedHash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        try
        {
            // Act
            var result = await _service.VerifyFileAsync(tempFile, expectedHash);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(expectedHash, result.ActualSha256);
            Assert.Equal(expectedHash, result.ExpectedSha256);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFileAsync_WithMismatchedChecksum_ShouldReturnInvalid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello, World!");
        var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        try
        {
            // Act
            var result = await _service.VerifyFileAsync(tempFile, wrongHash);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEqual(wrongHash, result.ActualSha256);
            Assert.Equal(wrongHash, result.ExpectedSha256);
            Assert.Null(result.ErrorMessage);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFileAsync_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        var hash = "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f";

        // Act
        var result = await _service.VerifyFileAsync(nonExistentFile, hash);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyFileAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "Hello, World!");
        var uppercaseHash = "DFFD6021BB2BD5B0AF676290809EC3A53191DD81C7F70A4B28688A362182986F";

        try
        {
            // Act
            var result = await _service.VerifyFileAsync(tempFile, uppercaseHash);

            // Assert
            Assert.True(result.IsValid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyFilesAsync_ShouldVerifyMultipleFiles()
    {
        // Arrange
        var tempFile1 = Path.GetTempFileName();
        var tempFile2 = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile1, "Hello, World!");
        await File.WriteAllTextAsync(tempFile2, "Goodbye, World!");

        var files = new Dictionary<string, string>
        {
            { tempFile1, "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f" },
            { tempFile2, "c80e4cad628d9020a5d9a1d1e4c5a6f1d8f8e2e7a5f1f5b8a8c8e2e7a5f1f5b8" }
        };

        try
        {
            // Act
            var results = await _service.VerifyFilesAsync(files);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.True(results[tempFile1].IsValid);
            Assert.False(results[tempFile2].IsValid); // Wrong hash intentionally
        }
        finally
        {
            File.Delete(tempFile1);
            File.Delete(tempFile2);
        }
    }

    [Fact]
    public async Task ComputeSha256Async_WithEmptyFilePath_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ComputeSha256Async(""));
    }

    [Fact]
    public async Task VerifyFileAsync_WithEmptyExpectedHash_ShouldThrow()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.VerifyFileAsync(tempFile, ""));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
