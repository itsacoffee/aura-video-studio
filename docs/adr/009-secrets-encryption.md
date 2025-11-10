# 009. Secrets Encryption Strategy

Date: 2024-03-01
Status: Accepted

## Context

Aura Video Studio handles sensitive user data including:
- OpenAI API keys
- ElevenLabs API keys
- Other third-party service credentials
- User preferences and settings

These secrets must be stored securely on the user's machine. We needed to determine:

1. Where to store secrets
2. How to encrypt them at rest
3. How to handle decryption
4. What to do with secrets in logs and diagnostics

Key requirements:
- Secrets must be encrypted at rest
- Encryption should use OS-provided mechanisms when possible
- No secrets should appear in logs or diagnostics
- Simple API for developers to use
- Cross-platform support (Windows primary, Linux/macOS secondary)

## Decision

We will use **platform-specific encryption** with a common abstraction:

**Windows (Primary Platform):**
- Use Windows Data Protection API (DPAPI)
- User scope encryption (not machine scope)
- Secrets accessible only to the current user
- Stored in `%LOCALAPPDATA%\AuraVideoStudio\secrets.dat`

**Linux/macOS (Secondary Platforms):**
- Use libsecret (Linux) / Keychain (macOS) where available
- Fallback to encrypted file with user-specific key
- Stored in `~/.local/share/AuraVideoStudio/secrets.dat`

**Common Features:**
- Abstraction layer: `ISecretsManager` interface
- Automatic redaction in logs using `[REDACTED]` placeholder
- Privacy mode for support bundles (redact all PII)
- Encryption status indicator in UI

## Consequences

### Positive Consequences

- **Platform-native security**: Leverages OS security features
- **User-scoped**: Secrets isolated to individual users
- **No master password**: Users don't need to remember another password
- **Automatic encryption**: Transparent to application code
- **Log safety**: Automatic redaction prevents leaks
- **Diagnostic safety**: Support bundles don't contain secrets
- **Simple API**: Developers use `GetSecret()` and `SetSecret()` without crypto knowledge

### Negative Consequences

- **Platform-specific code**: Different implementations per OS
- **No cross-device sync**: Secrets don't follow user across machines
- **User access required**: Can't access secrets when running as service
- **Recovery difficulty**: If user profile corrupted, secrets lost
- **No sharing**: Secrets can't be shared between users on same machine

## Alternatives Considered

### Alternative 1: Plain Text Storage

**Description:** Store API keys in plain text configuration files.

**Pros:**
- Simple to implement
- Easy to debug
- No encryption overhead
- Cross-platform identical

**Cons:**
- **Major security risk**: Keys accessible to anyone with file access
- Keys visible in backups
- Risk of accidental commit to version control
- Violates security best practices

**Why Rejected:** Unacceptable security risk. API keys are valuable and could be abused if stolen. Professional software must encrypt sensitive data.

### Alternative 2: Custom Encryption with Master Password

**Description:** Encrypt secrets with AES using user-provided master password.

**Pros:**
- Full control over encryption
- Cross-platform identical code
- Password-protected access

**Cons:**
- Users must remember another password
- Password management complexity (reset, recovery)
- Must implement secure key derivation (PBKDF2/Argon2)
- Must handle password storage securely
- More attack surface (custom crypto is risky)

**Why Rejected:** Adds friction for users (another password) and increases implementation complexity. Platform-native security is more robust and audited.

### Alternative 3: Cloud-Based Secret Storage

**Description:** Store encrypted secrets in cloud service (e.g., AWS Secrets Manager, Azure Key Vault).

**Pros:**
- Centralized management
- Cross-device synchronization
- Professional-grade security
- Audit logging
- Easy rotation

**Cons:**
- Requires internet connection
- Requires cloud account
- Monthly costs
- Privacy concerns (secrets leave user's machine)
- Overkill for desktop application
- Vendor lock-in

**Why Rejected:** Inappropriate for desktop application. Users expect local, offline operation. Cloud dependency would reduce usability and add costs.

### Alternative 4: Environment Variables

**Description:** Store secrets in environment variables, not files.

**Pros:**
- Common in server environments
- No file storage needed
- Process isolation

**Cons:**
- Not persistent (lost on restart)
- Visible in process listings
- Awkward for desktop applications
- No encryption at rest
- Difficult for non-technical users

**Why Rejected:** Environment variables are appropriate for servers but not desktop applications. Users expect settings to persist and be manageable through UI.

## References

- [Windows Data Protection API (DPAPI)](https://docs.microsoft.com/en-us/dotnet/standard/security/how-to-use-data-protection)
- [Linux libsecret](https://wiki.gnome.org/Projects/Libsecret)
- [macOS Keychain Services](https://developer.apple.com/documentation/security/keychain_services)
- [OWASP: Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)

## Notes

This decision prioritizes security without sacrificing usability. Platform-native encryption provides strong security with zero user friction.

Implementation details:

```csharp
// Usage in application code
string apiKey = await secretsManager.GetSecretAsync("openai-api-key");

// Automatic redaction in logs
logger.LogInformation("API key loaded: {Key}", apiKey); 
// Output: "API key loaded: [REDACTED]"
```

The redaction system prevents accidental logging of secrets:
- Redacts known secret names in log messages
- Redacts values that look like API keys (regex patterns)
- Privacy mode redacts all PII in diagnostic bundles

This approach ensures Aura Video Studio can securely handle API keys while maintaining ease of use and providing excellent debugging capabilities without security risks.
