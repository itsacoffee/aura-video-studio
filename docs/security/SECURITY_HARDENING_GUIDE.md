# Security Hardening Guide

## Overview

This guide documents the comprehensive security measures implemented in Aura Video Studio as part of PR #12. The security hardening follows OWASP best practices and implements defense-in-depth strategies.

## Table of Contents

- [Security Architecture](#security-architecture)
- [Authentication & Authorization](#authentication--authorization)
- [Secret Management](#secret-management)
- [Input Validation & Sanitization](#input-validation--sanitization)
- [Security Headers](#security-headers)
- [CSRF Protection](#csrf-protection)
- [Rate Limiting](#rate-limiting)
- [Audit Logging](#audit-logging)
- [HTTPS Enforcement](#https-enforcement)
- [Security Scanning](#security-scanning)
- [Configuration](#configuration)
- [Incident Response](#incident-response)

## Security Architecture

### Defense-in-Depth Layers

1. **Network Layer**: HTTPS enforcement, secure TLS configuration
2. **Application Layer**: Security headers, CSRF protection, input validation
3. **Authentication Layer**: JWT tokens, API keys, multi-factor authentication ready
4. **Authorization Layer**: Role-based access control, resource-based policies
5. **Data Layer**: Encryption at rest, secure secret storage
6. **Monitoring Layer**: Audit logging, security event tracking

### Security Middleware Pipeline

The security middleware is applied in the following order:

1. Security Headers (First - protects all responses)
2. HTTPS Enforcement
3. Cookie Policy
4. CORS
5. Authentication
6. Authorization
7. CSRF Protection (After authentication)
8. Audit Logging
9. Rate Limiting

## Authentication & Authorization

### API Key Authentication

API key authentication is enabled by default for local/development scenarios.

**Configuration**:
```json
{
  "Authentication": {
    "EnableApiKeyAuthentication": true,
    "ApiKeyHeaderName": "X-API-Key",
    "RequireAuthentication": false
  }
}
```

**Usage**:
```bash
curl -H "X-API-Key: your-api-key" https://api.example.com/api/endpoint
```

### JWT Authentication

JWT authentication provides stateless authentication with token validation.

**Configuration**:
```json
{
  "Authentication": {
    "EnableJwtAuthentication": true,
    "JwtSecretKey": "use-key-vault-for-production",
    "JwtIssuer": "AuraVideoStudio",
    "JwtAudience": "AuraApi",
    "JwtExpirationMinutes": 60
  }
}
```

**Token Structure**:
```json
{
  "sub": "user-id",
  "role": "Admin",
  "iat": 1234567890,
  "exp": 1234571490
}
```

### Authorization Policies

#### Available Policies

- `RequireAdmin`: Requires admin role
- `RequireUser`: Requires user or admin role
- `RequireApiKey`: Requires valid API key
- `RequireValidatedProvider`: Requires validated provider configuration

#### Usage in Controllers

```csharp
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public async Task<IActionResult> AdminOnlyEndpoint()
{
    // Only admins can access this
}

[Authorize(Policy = AuthorizationPolicies.RequireApiKey)]
public async Task<IActionResult> ApiKeyProtectedEndpoint()
{
    // Requires valid API key
}
```

### Anonymous Endpoints

The following endpoints are exempt from authentication:

- `/health*` - Health check endpoints
- `/healthz` - Kubernetes health probe
- `/swagger` - API documentation
- `/api-docs` - API documentation

## Secret Management

### Azure Key Vault Integration

Azure Key Vault integration provides secure storage for sensitive configuration values.

#### Configuration

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-vault.vault.azure.net/",
    "UseManagedIdentity": true,
    "CacheExpirationMinutes": 60,
    "AutoReload": true,
    "SecretMappings": {
      "Providers:OpenAI:ApiKey": "OpenAI-ApiKey",
      "Providers:Anthropic:ApiKey": "Anthropic-ApiKey"
    }
  }
}
```

#### Using Managed Identity (Recommended)

For Azure deployments, use Managed Identity:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-vault.vault.azure.net/",
    "UseManagedIdentity": true
  }
}
```

#### Using Service Principal (Alternative)

For non-Azure deployments or testing:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-vault.vault.azure.net/",
    "UseManagedIdentity": false,
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "use-environment-variable"
  }
}
```

#### Secret Naming Convention

In Key Vault, secrets should follow this naming pattern:
- `OpenAI-ApiKey` (hyphens instead of colons)
- `Anthropic-ApiKey`
- `JWT-Secret-Key`

#### Automatic Secret Refresh

Secrets are automatically refreshed at half the cache expiration interval. This ensures minimal downtime when secrets are rotated.

## Input Validation & Sanitization

### Input Sanitizer Utility

The `InputSanitizer` class provides comprehensive input validation:

#### File Path Sanitization

```csharp
// Prevent directory traversal
var safePath = InputSanitizer.SanitizeFilePath(userInput, baseDirectory);
```

#### XSS Protection

```csharp
// Remove XSS patterns
var clean = InputSanitizer.SanitizeForXss(userInput);

// Check for XSS patterns
if (InputSanitizer.ContainsXssPattern(userInput))
{
    // Reject input
}
```

#### SQL Injection Protection

```csharp
// Check for SQL injection patterns (defense-in-depth)
// Always use parameterized queries as primary defense
if (InputSanitizer.ContainsSqlInjectionPattern(userInput))
{
    // Reject input
}
```

#### Prompt Injection Protection

```csharp
// Sanitize LLM prompts
var safePrompt = InputSanitizer.SanitizePrompt(userInput);
```

#### Log Injection Prevention

```csharp
// Sanitize for logging
var safeLog = InputSanitizer.SanitizeForLogging(userInput);
```

### FluentValidation

All API requests are validated using FluentValidation:

```csharp
public class ScriptRequestValidator : AbstractValidator<ScriptRequest>
{
    public ScriptRequestValidator()
    {
        RuleFor(x => x.Brief)
            .NotEmpty()
            .MaximumLength(10000)
            .Must(brief => !InputSanitizer.ContainsXssPattern(brief))
            .WithMessage("Input contains potentially malicious content");
    }
}
```

## Security Headers

### Implemented Headers

#### Content-Security-Policy
Prevents XSS attacks by controlling resource loading:
```
default-src 'self';
script-src 'self' 'unsafe-inline' 'unsafe-eval';
style-src 'self' 'unsafe-inline';
img-src 'self' data: blob: https:;
connect-src 'self' ws: wss:;
```

#### X-Content-Type-Options
Prevents MIME type sniffing:
```
X-Content-Type-Options: nosniff
```

#### X-Frame-Options
Prevents clickjacking:
```
X-Frame-Options: DENY
```

#### Strict-Transport-Security (HSTS)
Enforces HTTPS:
```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

#### X-XSS-Protection
Enables XSS filter in older browsers:
```
X-XSS-Protection: 1; mode=block
```

#### Referrer-Policy
Controls referrer information:
```
Referrer-Policy: strict-origin-when-cross-origin
```

#### Permissions-Policy
Restricts browser features:
```
Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=()
```

### Configuration

Security headers are enabled by default and can be configured:

```json
{
  "Security": {
    "EnableSecurityHeaders": true
  }
}
```

## CSRF Protection

### Double-Submit Cookie Pattern

CSRF protection uses the double-submit cookie pattern:

1. Server sets `XSRF-TOKEN` cookie
2. Client reads cookie and includes value in `X-XSRF-TOKEN` header
3. Server validates that cookie and header values match

### Configuration

```json
{
  "Security": {
    "EnableCsrfProtection": true
  }
}
```

### Client Integration

#### JavaScript/TypeScript

```typescript
// Read CSRF token from cookie
function getCsrfToken(): string {
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? match[1] : '';
}

// Include in request headers
fetch('/api/endpoint', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-XSRF-TOKEN': getCsrfToken()
  },
  body: JSON.stringify(data)
});
```

#### Axios (Automatic)

```typescript
import axios from 'axios';

// Axios automatically handles XSRF tokens
axios.defaults.xsrfCookieName = 'XSRF-TOKEN';
axios.defaults.xsrfHeaderName = 'X-XSRF-TOKEN';
```

### Exempt Paths

Safe methods (GET, HEAD, OPTIONS, TRACE) and specific paths are exempt:
- `/health*`
- `/healthz`
- `/swagger`

## Rate Limiting

### Configuration

Rate limiting is configured per endpoint:

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "POST:/api/jobs",
        "Period": "1m",
        "Limit": 10
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Rate Limit Headers

Responses include rate limit information:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1234567890
Retry-After: 30
```

### Bypassing Rate Limits

Admins can bypass rate limits using the `RateLimitBypass` policy.

## Audit Logging

### Security Event Logging

All security-relevant events are logged to a dedicated audit log:

#### Logged Events

- Authentication attempts (success/failure)
- Authorization failures
- Sensitive data access
- Configuration changes
- API key usage
- Rate limit violations
- Input validation failures
- Suspicious activity

#### Log Format

```json
{
  "timestamp": "2024-01-01T00:00:00Z",
  "level": "Information",
  "eventType": "Authentication",
  "userId": "user-123",
  "ipAddress": "192.168.1.1",
  "message": "User authenticated successfully",
  "correlationId": "abc-123"
}
```

#### Log Retention

- Audit logs: 90 days
- Error logs: 30 days
- General logs: 30 days

### Using the Audit Logger

```csharp
public class MyController : ControllerBase
{
    private readonly IAuditLogger _auditLogger;

    public MyController(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    [HttpPost("sensitive")]
    public IActionResult SensitiveOperation()
    {
        _auditLogger.LogSensitiveDataAccess(
            UserId,
            "SensitiveResource",
            "Read");
        
        return Ok();
    }
}
```

## HTTPS Enforcement

### Configuration

HTTPS enforcement can be configured:

```json
{
  "Security": {
    "EnforceHttps": true
  }
}
```

### Development Environment

For local development (localhost/127.0.0.1), HTTPS enforcement is automatically disabled to allow HTTP connections.

### Production Environment

In production, all HTTP requests are redirected to HTTPS with a 301 status code.

## Security Scanning

### CI/CD Integration

Security scanning is automatically performed on:
- Every push to main/develop
- Pull requests
- Daily scheduled scans

### Scanning Tools

1. **Dependency Scanning**
   - .NET: `dotnet list package --vulnerable`
   - npm: `npm audit`

2. **SAST Scanning**
   - CodeQL (GitHub)
   - Semgrep

3. **Container Scanning**
   - Trivy (Aqua Security)

4. **Secret Scanning**
   - TruffleHog
   - Gitleaks

5. **License Compliance**
   - Dependency Review Action

### Scan Results

Results are uploaded to:
- GitHub Security tab (SARIF format)
- GitHub Actions artifacts
- Pull request comments

### Failure Thresholds

- Critical vulnerabilities: Build fails
- High vulnerabilities: Warning issued
- Medium/Low vulnerabilities: Informational

## Configuration

### Production Configuration Example

```json
{
  "Authentication": {
    "EnableJwtAuthentication": true,
    "RequireAuthentication": true,
    "JwtSecretKey": ""  // Load from Key Vault
  },
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://prod-vault.vault.azure.net/",
    "UseManagedIdentity": true,
    "AutoReload": true
  },
  "Security": {
    "EnableCsrfProtection": true,
    "EnforceHttps": true,
    "EnableSecurityHeaders": true,
    "EnableAuditLogging": true
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Environment Variables

Sensitive configuration should use environment variables:

```bash
# JWT Secret (if not using Key Vault)
export Authentication__JwtSecretKey="your-secret-key"

# Key Vault Configuration
export KeyVault__Enabled=true
export KeyVault__VaultUri="https://your-vault.vault.azure.net/"

# API Keys (if not using Key Vault)
export Providers__OpenAI__ApiKey="sk-..."
```

## Incident Response

### Security Event Response

1. **Detection**: Security events are logged and monitored
2. **Analysis**: Review audit logs and security alerts
3. **Containment**: Rate limiting, IP blocking, service shutdown
4. **Recovery**: Rotate secrets, patch vulnerabilities
5. **Post-Mortem**: Document incident and improve defenses

### Emergency Procedures

#### Suspected Breach

1. Review audit logs: `logs/audit-*.log`
2. Check failed authentication attempts
3. Look for suspicious IP addresses
4. Review configuration changes

#### Secret Compromise

1. Immediately rotate compromised secrets in Key Vault
2. Review audit logs for unauthorized access
3. Force re-authentication of all users
4. Update API keys for affected providers

#### DDoS Attack

1. Review rate limit logs
2. Identify attacking IP addresses
3. Add to IP blacklist
4. Enable more restrictive rate limits
5. Consider enabling DDoS protection service

### Monitoring Alerts

Set up alerts for:
- Multiple failed authentication attempts
- Unusual API access patterns
- Rate limit violations
- Configuration changes
- High/critical vulnerabilities detected

## Best Practices

### Development

1. **Never commit secrets** to source control
2. **Use Key Vault** for all sensitive configuration
3. **Test security features** locally before deployment
4. **Review security scan results** before merging PRs
5. **Keep dependencies updated** regularly

### Deployment

1. **Enable all security features** in production
2. **Use Managed Identity** for Azure resources
3. **Configure monitoring alerts**
4. **Implement backup and recovery**
5. **Document security incidents**

### Operations

1. **Monitor audit logs** regularly
2. **Review security scan results** daily
3. **Rotate secrets** periodically (90 days)
4. **Update dependencies** monthly
5. **Conduct security reviews** quarterly

## Security Checklist

### Pre-Deployment

- [ ] All secrets moved to Key Vault
- [ ] HTTPS enforcement enabled
- [ ] Security headers configured
- [ ] CSRF protection enabled
- [ ] Rate limiting configured
- [ ] Audit logging enabled
- [ ] Authentication required
- [ ] Authorization policies applied
- [ ] Security scans passing
- [ ] Monitoring alerts configured

### Post-Deployment

- [ ] Verify HTTPS redirect working
- [ ] Test authentication flow
- [ ] Verify rate limiting active
- [ ] Check audit logs generating
- [ ] Review security headers
- [ ] Test CSRF protection
- [ ] Verify secret loading from Key Vault
- [ ] Check monitoring alerts working

## Compliance

### OWASP Top 10 Addressed

1. **A01:2021 - Broken Access Control**: Authorization policies, CSRF protection
2. **A02:2021 - Cryptographic Failures**: HTTPS enforcement, secure secrets storage
3. **A03:2021 - Injection**: Input validation, sanitization
4. **A04:2021 - Insecure Design**: Defense-in-depth architecture
5. **A05:2021 - Security Misconfiguration**: Secure defaults, security headers
6. **A06:2021 - Vulnerable Components**: Dependency scanning
7. **A07:2021 - Authentication Failures**: JWT/API key auth, audit logging
8. **A08:2021 - Software and Data Integrity**: SAST scanning, code signing
9. **A09:2021 - Security Logging Failures**: Comprehensive audit logging
10. **A10:2021 - SSRF**: Input validation, URL validation

### GDPR Compliance

- Audit logging for data access
- Secure data storage (encryption)
- Data retention policies
- Right to be forgotten support

### SOC 2 Controls

- Access controls (authentication/authorization)
- Encryption in transit (HTTPS)
- Encryption at rest (Key Vault)
- Audit logging
- Vulnerability management
- Incident response procedures

## Support

For security issues or questions:
- Create a security advisory on GitHub
- Email: security@aura.studio
- Review: `/docs/security/` directory

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
