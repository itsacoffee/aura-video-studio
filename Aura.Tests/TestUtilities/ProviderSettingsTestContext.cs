using System;
using System.IO;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Tests.TestUtilities;

/// <summary>
/// Provides an isolated ProviderSettings instance backed by a temporary data root.
/// Automatically cleans up on dispose.
/// </summary>
public sealed class ProviderSettingsTestContext : IDisposable
{
    private readonly string? _originalDataRoot;
    public string PortableRoot { get; }
    public ProviderSettings Settings { get; }

    public ProviderSettingsTestContext()
    {
        PortableRoot = Path.Combine(Path.GetTempPath(), "AuraTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(PortableRoot);

        _originalDataRoot = Environment.GetEnvironmentVariable("AURA_DATA_PATH");
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", PortableRoot);

        Settings = new ProviderSettings(NullLogger<ProviderSettings>.Instance);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", _originalDataRoot);

        try
        {
            if (Directory.Exists(PortableRoot))
            {
                Directory.Delete(PortableRoot, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for tests.
        }
    }
}

