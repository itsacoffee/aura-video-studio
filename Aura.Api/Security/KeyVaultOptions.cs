namespace Aura.Api.Security;

/// <summary>
/// Configuration options for Azure Key Vault integration
/// </summary>
public class KeyVaultOptions
{
    /// <summary>
    /// Enable Azure Key Vault integration
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Azure Key Vault URI (e.g., https://your-vault.vault.azure.net/)
    /// </summary>
    public string? VaultUri { get; set; }

    /// <summary>
    /// Tenant ID for Azure AD authentication
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Client ID for Azure AD authentication (Managed Identity or Service Principal)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret for Service Principal authentication (optional, prefer Managed Identity)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Use Managed Identity for authentication (recommended for Azure deployments)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Cache secrets in memory for this duration (in minutes)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Automatically reload secrets when they change
    /// </summary>
    public bool AutoReload { get; set; } = true;

    /// <summary>
    /// Secret names to load from Key Vault
    /// Map of local configuration key to Key Vault secret name
    /// </summary>
    public Dictionary<string, string> SecretMappings { get; set; } = new()
    {
        ["Providers:OpenAI:ApiKey"] = "OpenAI-ApiKey",
        ["Providers:Anthropic:ApiKey"] = "Anthropic-ApiKey",
        ["Providers:ElevenLabs:ApiKey"] = "ElevenLabs-ApiKey",
        ["Providers:Stability:ApiKey"] = "Stability-ApiKey",
        ["Authentication:JwtSecretKey"] = "JWT-Secret-Key",
        ["ConnectionStrings:Redis"] = "Redis-Connection-String"
    };
}
