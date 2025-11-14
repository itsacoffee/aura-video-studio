using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// Integration tests for the KeyVault security endpoints
/// </summary>
public class KeyVaultSecurityApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:5005";

    public KeyVaultSecurityApiTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Fact(Skip = "Requires API server to be running")]
    public async Task GetKeyVaultInfo_ReturnsSecurityInformation()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/keys/info").ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<KeyVaultInfoResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        
        Assert.NotNull(result.Encryption);
        Assert.NotNull(result.Encryption.Platform);
        Assert.NotNull(result.Encryption.Method);
        Assert.NotNull(result.Encryption.Scope);
        
        Assert.NotNull(result.Storage);
        Assert.NotNull(result.Storage.Location);
        Assert.True(result.Storage.Encrypted);
        
        Assert.NotNull(result.Metadata);
        Assert.True(result.Metadata.ConfiguredKeysCount >= 0);
        
        Assert.NotNull(result.Status);
    }

    [Fact(Skip = "Requires API server to be running")]
    public async Task RunDiagnostics_ReturnsRedactionCheckResults()
    {
        // Act
        var response = await _httpClient.PostAsync("/api/keys/diagnostics", null).ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<KeyVaultDiagnosticsResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Checks);
        Assert.NotEmpty(result.Checks);
        Assert.NotNull(result.Message);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class KeyVaultInfoResponse
{
    public bool Success { get; set; }
    public KeyVaultEncryptionInfo Encryption { get; set; } = new();
    public KeyVaultStorageInfo Storage { get; set; } = new();
    public KeyVaultMetadata Metadata { get; set; } = new();
    public string Status { get; set; } = "";
}

public class KeyVaultEncryptionInfo
{
    public string Platform { get; set; } = "";
    public string Method { get; set; } = "";
    public string Scope { get; set; } = "";
}

public class KeyVaultStorageInfo
{
    public string Location { get; set; } = "";
    public bool Encrypted { get; set; }
    public bool FileExists { get; set; }
}

public class KeyVaultMetadata
{
    public int ConfiguredKeysCount { get; set; }
    public string? LastModified { get; set; }
}

public class KeyVaultDiagnosticsResponse
{
    public bool Success { get; set; }
    public bool RedactionCheckPassed { get; set; }
    public string[] Checks { get; set; } = Array.Empty<string>();
    public string? Message { get; set; }
}
