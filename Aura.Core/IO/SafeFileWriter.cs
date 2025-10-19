using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.IO;

/// <summary>
/// Provides atomic file write operations with automatic cleanup on failure.
/// Writes to a temporary file, syncs to disk, then atomically moves to the final location.
/// </summary>
public static class SafeFileWriter
{
    /// <summary>
    /// Atomically writes data to a file. On failure, cleans up temporary files and never leaves zero-byte finals.
    /// </summary>
    /// <param name="finalPath">The final destination path</param>
    /// <param name="writeAction">Action that writes to the provided stream</param>
    /// <param name="ct">Cancellation token</param>
    public static async Task WriteFileAsync(string finalPath, Func<Stream, Task> writeAction, CancellationToken ct = default)
    {
        var tempPath = $"{finalPath}.tmp";
        
        try
        {
            // Ensure parent directory exists
            var directory = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write to temporary file
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await writeAction(fileStream);
                await fileStream.FlushAsync(ct);
                
                // Sync to disk if supported
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Windows, Flush(true) syncs to disk
                    fileStream.Flush(true);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On Unix-like systems, use fsync
                    var result = Fsync(fileStream.SafeFileHandle.DangerousGetHandle());
                    if (result != 0)
                    {
                        // Log warning but don't fail - fsync errors are rare and usually not fatal
                        System.Diagnostics.Debug.WriteLine($"fsync returned non-zero: {result}");
                    }
                }
            }
            
            // Verify temp file is not empty
            var fileInfo = new FileInfo(tempPath);
            if (fileInfo.Length == 0)
            {
                throw new IOException($"Temporary file {tempPath} is zero bytes - write operation may have failed");
            }
            
            // Atomic move to final location
            // If finalPath already exists, this will replace it atomically
            File.Move(tempPath, finalPath, overwrite: true);
        }
        catch
        {
            // Clean up temporary file on any error
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best effort cleanup - ignore deletion errors
                }
            }
            throw;
        }
    }
    
    /// <summary>
    /// Atomically writes bytes to a file
    /// </summary>
    public static async Task WriteBytesAsync(string finalPath, byte[] data, CancellationToken ct = default)
    {
        await WriteFileAsync(finalPath, async stream =>
        {
            await stream.WriteAsync(data, ct);
        }, ct);
    }
    
    /// <summary>
    /// Atomically writes text to a file
    /// </summary>
    public static async Task WriteTextAsync(string finalPath, string text, CancellationToken ct = default)
    {
        await WriteFileAsync(finalPath, async stream =>
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteAsync(text);
            await writer.FlushAsync();
        }, ct);
    }
    
    /// <summary>
    /// Atomically copies a file
    /// </summary>
    public static async Task CopyFileAsync(string sourcePath, string finalPath, CancellationToken ct = default)
    {
        await WriteFileAsync(finalPath, async stream =>
        {
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(stream, ct);
        }, ct);
    }
    
    [DllImport("libc", SetLastError = true, EntryPoint = "fsync")]
    private static extern int Fsync(IntPtr fd);
}
