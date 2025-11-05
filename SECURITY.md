# Security Policy

## Overview

Aura Video Studio is committed to maintaining the security and integrity of our application. This document outlines our security practices, policies, and procedures.

## Reporting Security Vulnerabilities

If you discover a security vulnerability, please report it by:

1. **Preferred Method**: Use GitHub's Security Advisory feature
   - Go to Repository → Security → Advisories → New draft security advisory
   - This keeps the vulnerability private until it's addressed
2. **Alternative**: Contact the maintainers privately (do not open public issues)
3. **Do not** disclose the vulnerability publicly until it has been addressed
4. Provide detailed information about the vulnerability and steps to reproduce

## Security Features

### 1. Input Validation
- All public APIs validate input parameters
- Null checks on critical parameters
- Type validation for user inputs
- Path traversal prevention
- URL validation for external resources

### 2. File System Security
- SHA-256 checksum verification for file integrity
- Safe file operations with proper error handling
- Temporary file cleanup
- Proper file permission checks
- No exposure of sensitive file paths in user-facing error messages

### 3. Network Security
- HTTP client timeout configurations
- Proper disposal of HTTP resources
- HTTPS recommended for all external communications
- SSL certificate validation enabled
- Cancellation token support to prevent resource exhaustion

### 4. Process Security
- Process execution with `CreateNoWindow = true`
- Standard output/error redirection for controlled execution
- Timeout mechanisms for process execution
- Proper process disposal

### 5. Authentication & Authorization
- API key validation for external service providers
- **Secrets Encryption at Rest**:
  - **Windows**: Data Protection API (DPAPI) encryption with CurrentUser scope
  - **Linux/macOS**: AES-256 encryption with machine-specific key
  - Storage location: `%LOCALAPPDATA%\Aura\secure\apikeys.dat` (Windows) or `$HOME/.local/share/Aura/secure/apikeys.dat` (Linux/macOS)
- **Key Management API**: Comprehensive REST API for set/get/rotate/test/delete operations
- **CLI Support**: Full key management via `aura keys` commands for headless environments
- **Secret Masking**: All API keys automatically masked in logs, diagnostics, error messages, and SSE events
- **Key Validation**: Test keys with real provider connections before saving
- **Migration Support**: Automatic migration from plaintext to encrypted storage (if legacy plaintext detected)
- Preflight validation with secure API key testing
- No hardcoded credentials or secrets in source code

### 6. Error Handling
- Comprehensive try-catch blocks in critical paths
- Proper disposal of resources using `using` statements
- Detailed logging for troubleshooting (sensitive data excluded)
- User-friendly error messages without information disclosure

## Security Analysis

This project undergoes regular security analysis using:
- **CodeQL**: Automated security vulnerability scanning
- **Dependency Scanning**: Regular checks for vulnerable dependencies
- **Code Review**: Manual security-focused code reviews

For detailed security summaries of specific features and implementations, see [docs/security/](docs/security/).

## Best Practices

✅ Input validation on all public methods  
✅ Proper error handling and logging  
✅ Resource disposal with `using` statements  
✅ Cancellation token support  
✅ Timeout mechanisms for long-running operations  
✅ Secure file operations  
✅ No hardcoded credentials or secrets  
✅ Thread-safe operations  
✅ Encrypted secrets at rest with platform-specific encryption  
✅ Automatic secret masking in all logs and diagnostics  
✅ Key validation before storage  
✅ Secure export/import with explicit opt-in for secrets  

## Compliance

The implementation follows:
- OWASP Secure Coding Practices
- Microsoft .NET Security Guidelines
- Industry best practices for desktop application security

## Security Recommendations for Deployment

1. **HTTPS Only**: Ensure all external URLs use HTTPS
2. **Certificate Validation**: Keep SSL certificate validation enabled
3. **Access Control**: Implement proper access control for application directories
4. **Rate Limiting**: Consider implementing rate limiting for API operations
5. **Audit Logging**: Enable audit logging for sensitive operations
6. **Keep Updated**: Regularly update dependencies and runtime versions

## Security Audit History

All security audits and implementation-specific security reviews are documented in [docs/security/](docs/security/).

## Contact

For security concerns or questions, please open an issue on our GitHub repository with the `security` label.

## Model Selection Security

### Model ID Sanitization

Model IDs and provider names are sanitized in logs to prevent injection attacks:
- All model IDs validated against catalog before use
- Provider names restricted to known values (OpenAI, Anthropic, Gemini, Azure, Ollama)
- No user-controlled strings used in API URLs without validation
- Structured logging used to prevent log injection

### API Key Protection

API keys used for model testing are:
- Never logged or persisted
- Passed only in memory
- Used only for the duration of the test request
- Not included in audit trail or selection records

Note: Model IDs themselves are **not secrets** and may appear in:
- Audit logs (sanitized)
- Error messages (sanitized)
- Settings files (plain text)
- UI displays

### Selection Persistence Security

Model selections are stored in `AuraData/model-selections.json`:
- File permissions: Read/Write by application only
- No sensitive data (API keys, secrets) stored
- Audit log limited to last 1000 entries to prevent unbounded growth
- Regular cleanup of old audit entries

## Key Management and Secrets Encryption

### Encryption at Rest

All API keys and provider secrets are encrypted at rest using platform-specific encryption:

**Windows (Production)**:
- **Method**: Data Protection API (DPAPI)
- **Scope**: CurrentUser
- **Storage**: `%LOCALAPPDATA%\Aura\secure\apikeys.dat`
- **Security**: Keys encrypted with user's Windows credentials, cannot be decrypted by other users or on other machines

**Linux/macOS**:
- **Method**: AES-256-CBC encryption
- **Key**: Machine-specific 256-bit key stored in `$HOME/.local/share/Aura/secure/.machinekey`
- **Storage**: `$HOME/.local/share/Aura/secure/apikeys.dat`
- **Security**: 
  - Keys encrypted with machine-specific AES-256 key
  - File permissions set to 600 (owner read/write only)
  - Initialization vector (IV) prepended to encrypted data for CBC mode
  - Machine key generated from cryptographically secure random number generator
  - Automatic migration from legacy plaintext storage (if detected)

✅ **Production-Ready**: Linux/macOS encryption uses industry-standard AES-256-CBC with proper key management and file permissions. Suitable for production use.

### Key Management API

Comprehensive REST API for secure key management:

#### Endpoints

- **POST /api/keys/set** - Set or update an API key
  - Body: `{ "provider": "string", "apiKey": "string" }`
  - Response: Masked key confirmation
  - Encryption: Automatic encryption before storage

- **GET /api/keys/list** - List configured providers
  - Response: Array of providers with masked keys
  - Security: Never returns actual key values

- **POST /api/keys/test** - Test key with real provider connection
  - Body: `{ "provider": "string", "apiKey": "string?" }`
  - Optional: Provide key to test without saving
  - Default: Tests stored key
  - Validation: Makes actual API call to provider

- **POST /api/keys/rotate** - Rotate an existing key
  - Body: `{ "provider": "string", "newApiKey": "string", "testBeforeSaving": true }`
  - Safety: Tests new key before overwriting old key
  - Atomic: Old key retained if validation fails

- **DELETE /api/keys/{provider}** - Delete a key
  - Permanent deletion of encrypted key

- **GET /api/keys/info** - Get encryption status
  - Returns platform, method, and storage location
  - No sensitive data returned

### CLI Key Management

Full command-line interface for headless environments:

```bash
# Set an API key
aura keys set openai sk-proj-abc123...

# List configured providers (with masked keys)
aura keys list

# Test a key with real API connection
aura keys test openai

# Rotate a key (tests new key before saving)
aura keys rotate openai sk-proj-new456...

# Delete a key (with confirmation)
aura keys delete elevenlabs
```

### Secret Masking and Redaction

**Automatic masking in all output**:
- Logs (Serilog structured logging)
- SSE events (Server-Sent Events for real-time progress)
- Diagnostics bundles and crash reports
- Error messages and stack traces
- API responses

**Masking format**:
- Keys < 12 chars: `***`
- Keys >= 12 chars: `sk-12345...wxyz` (first 8 + last 4 characters)

**Pattern detection**:
- Regex-based detection of API key patterns
- Sensitive field names: `apiKey`, `api_key`, `secret`, `password`, `token`
- Automatic redaction of detected patterns in text

### Export and Import

**Export without secrets** (default):
```bash
POST /api/settings/export
# Returns settings with all secrets redacted
```

**Export with secrets** (explicit opt-in):
```bash
POST /api/settings/export
Body: { "includeSecrets": true, "selectedKeys": ["openai", "anthropic"] }
# Requires per-key checkbox confirmation in UI
# Shows redaction preview before export
# Prominent warnings about secret exposure
```

⚠️ **Security Warning**: Exported files containing secrets should be:
- Stored securely (encrypted filesystem, password manager, secure vault)
- Never committed to version control
- Never shared via insecure channels (email, chat, public cloud)
- Deleted after use

### Migration from Plaintext

**Automatic Migration on Linux/macOS**:
If legacy plaintext storage is detected (from `$HOME/.aura-dev/apikeys.json`):
1. Keys automatically migrated to encrypted storage on first read
2. Original plaintext file securely deleted (overwritten with random data then deleted)
3. Migration logged for audit trail
4. One-time operation per installation

**Legacy Storage Locations** (no longer used):
- Linux/macOS: `$HOME/.aura-dev/apikeys.json` (plaintext, migrated automatically)

**Migration Process**:
- Detects legacy plaintext file on KeyStore initialization
- Reads all keys from plaintext file
- Encrypts and saves each key to new encrypted storage
- Securely overwrites legacy file with random data
- Deletes legacy file
- Logs migration completion with key count

### CI/CD Secrets Enforcement

**Automated Security Guardrails**:

The repository includes automated security checks that run on every commit and pull request:

**1. Secrets Scanner (`scripts/audit/scan-secrets.sh`)**:
- Detects API keys, tokens, and credentials in source code
- Patterns detected:
  - OpenAI keys: `sk-*`, `sk-proj-*`
  - Anthropic keys: `sk-ant-api*`
  - JWT tokens: `eyJ*`
  - Bearer tokens
  - AWS keys: `AKIA*`
  - Google API keys: `AIza*`
  - GitHub tokens: `ghp_*`, `gho_*`, `ghs_*`
  - Generic patterns: `api_key`, `apiKey`, `password`, `token`, `secret`
- Excludes test files and documentation
- Fails CI builds if secrets are detected

**2. Plaintext Key File Scanner (`scripts/audit/check-no-plaintext-keys.sh`)**:
- Blocks known plaintext key filenames:
  - `apikeys.json`, `api-keys.json`, `keys.json`, `secrets.json`
  - `.env.production`, `.env.prod`
- Detects backup files: `*.backup`, `*.bak`, `*_backup`, `*_bak`
- Fails CI if forbidden files are committed

**3. Diagnostics Bundle Redaction**:
- All diagnostic bundles automatically redact secrets
- Comprehensive pattern matching for:
  - API keys and tokens
  - Bearer tokens and JWT tokens
  - Authorization headers
  - JSON key-value pairs containing secrets
- Public `RedactSensitiveData` method with extensive test coverage
- Safe to share diagnostic bundles for troubleshooting

**4. Pre-commit Hook Documentation**:
- Husky pre-commit hooks can be configured to run security scans
- Prevents accidental commits of sensitive data
- Configuration in `.husky/pre-commit` (optional)

**CI Workflow: Secrets Enforcement** (`.github/workflows/secrets-enforcement.yml`):
- Runs on all pushes and pull requests
- Four security check jobs:
  1. **Secrets Scan**: Pattern-based secret detection
  2. **Diagnostics Redaction Test**: Validates bundle scrubbing
  3. **CI Artifacts Check**: Scans workflows and scripts for hardcoded secrets
  4. **Security Summary**: Aggregates results and fails if any violations found
- Blocks PR merges if secrets are detected
- Provides detailed violation reports with file locations and line numbers

**Developer Guidelines**:
- ✅ Use encrypted KeyStore API for all secrets
- ✅ Set API keys via `/api/keys/set` or `aura keys set` CLI
- ✅ Test changes locally with `scripts/audit/scan-secrets.sh` before committing
- ❌ Never commit plaintext API keys or tokens
- ❌ Never commit backup files that may contain sensitive data
- ❌ Never hardcode credentials in source code or configuration files

**Incident Response**:
If a secret is accidentally committed:
1. Rotate the exposed credential immediately
2. Remove the secret from git history using `git filter-branch` or BFG Repo-Cleaner
3. Force-push the cleaned history (coordinate with team)
4. Update all deployments with the new credential
5. Document the incident for security audit trail

### Precedence Enforcement

Model selection precedence is enforced server-side:
- Client cannot bypass precedence rules
- Pinned selections validated on every resolution
- Audit trail records all resolutions for compliance
- No client-side override of pinned selections

### Model Testing Security

The model test endpoint (`POST /api/models/test`):
- Requires API key (not stored, used only for test)
- Rate-limited to prevent abuse
- Timeout enforced (15 seconds max)
- Results not cached with API key
- Test prompts are minimal and safe (no user-controlled content)

### Model Selection Precedence Table

For security and governance, the precedence rules are:

| Priority | Source | Security Level | Override Capability |
|----------|--------|----------------|---------------------|
| 1 | Run Override (Pinned) | User explicit | None - blocks if unavailable |
| 2 | Run Override | User explicit | Auto-fallback if configured |
| 3 | Stage Pinned | User explicit | None - blocks if unavailable |
| 4 | Project Override | User/Admin | Auto-fallback if configured |
| 5 | Global Default | Admin | Auto-fallback if configured |
| 6 | Automatic Fallback | System | Only if explicitly enabled |

**Security Implications**:
- Pinned selections (priority 1, 3) cannot be overridden by system
- All selections require authentication
- Audit trail provides non-repudiation
- System fallback disabled by default for predictability

