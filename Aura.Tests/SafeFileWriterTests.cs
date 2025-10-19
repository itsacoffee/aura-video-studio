using Xunit;
using Aura.Core.IO;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Tests;

public class SafeFileWriterTests
{
    private readonly string _tempDir;

    public SafeFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task WriteFileAsync_Success_CreatesNonZeroFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test.txt");
        var content = "Hello, World!";
        
        // Act
        await SafeFileWriter.WriteFileAsync(filePath, async stream =>
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes);
        });
        
        // Assert
        Assert.True(File.Exists(filePath), "Final file should exist");
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, actualContent);
        
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "File should not be zero bytes");
        
        // Cleanup
        File.Delete(filePath);
    }
    
    [Fact]
    public async Task WriteFileAsync_Exception_NoFinalFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-exception.txt");
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await SafeFileWriter.WriteFileAsync(filePath, async stream =>
            {
                var bytes = Encoding.UTF8.GetBytes("Test");
                await stream.WriteAsync(bytes);
                throw new InvalidOperationException("Simulated error");
            });
        });
        
        // Assert
        Assert.False(File.Exists(filePath), "Final file should not exist after exception");
        Assert.False(File.Exists($"{filePath}.tmp"), "Temp file should be cleaned up");
    }
    
    [Fact]
    public async Task WriteFileAsync_EmptyWrite_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-empty.txt");
        
        // Act & Assert
        await Assert.ThrowsAsync<IOException>(async () =>
        {
            await SafeFileWriter.WriteFileAsync(filePath, async stream =>
            {
                // Write nothing
                await Task.CompletedTask;
            });
        });
        
        // Assert
        Assert.False(File.Exists(filePath), "Zero-byte file should not be created");
    }
    
    [Fact]
    public async Task WriteBytesAsync_Success_WritesCorrectData()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-bytes.bin");
        var data = new byte[] { 1, 2, 3, 4, 5 };
        
        // Act
        await SafeFileWriter.WriteBytesAsync(filePath, data);
        
        // Assert
        Assert.True(File.Exists(filePath));
        var actualData = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(data, actualData);
        
        // Cleanup
        File.Delete(filePath);
    }
    
    [Fact]
    public async Task WriteTextAsync_Success_WritesCorrectText()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-text.txt");
        var text = "Test content\nLine 2\nLine 3";
        
        // Act
        await SafeFileWriter.WriteTextAsync(filePath, text);
        
        // Assert
        Assert.True(File.Exists(filePath));
        var actualText = await File.ReadAllTextAsync(filePath);
        Assert.Equal(text, actualText);
        
        // Cleanup
        File.Delete(filePath);
    }
    
    [Fact]
    public async Task CopyFileAsync_Success_CopiesFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_tempDir, "source.txt");
        var destPath = Path.Combine(_tempDir, "dest.txt");
        var content = "Source content";
        
        await File.WriteAllTextAsync(sourcePath, content);
        
        // Act
        await SafeFileWriter.CopyFileAsync(sourcePath, destPath);
        
        // Assert
        Assert.True(File.Exists(destPath));
        var actualContent = await File.ReadAllTextAsync(destPath);
        Assert.Equal(content, actualContent);
        
        // Cleanup
        File.Delete(sourcePath);
        File.Delete(destPath);
    }
    
    [Fact]
    public async Task WriteFileAsync_Overwrite_ReplacesExistingFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-overwrite.txt");
        await File.WriteAllTextAsync(filePath, "Old content");
        
        // Act
        await SafeFileWriter.WriteTextAsync(filePath, "New content");
        
        // Assert
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal("New content", actualContent);
        
        // Cleanup
        File.Delete(filePath);
    }
    
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
