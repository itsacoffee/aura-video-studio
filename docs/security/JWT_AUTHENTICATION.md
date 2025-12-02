# JWT Authentication Guide

This document describes how to configure and use JWT (JSON Web Token) authentication in Aura Video Studio.

## Overview

Aura Video Studio supports JWT bearer token authentication for securing API endpoints. When enabled, clients must provide a valid JWT token in the `Authorization` header to access protected endpoints.

## Configuration

### Enable JWT Authentication

Configure JWT authentication in `appsettings.json`:

```json
{
  "Authentication": {
    "EnableJwtAuthentication": true,
    "EnableApiKeyAuthentication": true,
    "JwtSecretKey": "your-secret-key-minimum-32-characters-long",
    "JwtIssuer": "AuraVideoStudio",
    "JwtAudience": "AuraApi",
    "JwtExpirationMinutes": 60,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00",
    "RequireAuthentication": true,
    "AnonymousEndpoints": [
      "/health",
      "/healthz",
      "/api/health",
      "/swagger",
      "/api-docs"
    ]
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableJwtAuthentication` | bool | `false` | Enable JWT bearer token authentication |
| `EnableApiKeyAuthentication` | bool | `true` | Enable API key authentication (fallback) |
| `JwtSecretKey` | string | `""` | Signing key for HS256 algorithm (min 32 chars) |
| `JwtIssuer` | string | `"AuraVideoStudio"` | Expected token issuer |
| `JwtAudience` | string | `"AuraApi"` | Expected token audience |
| `JwtExpirationMinutes` | int | `60` | Token expiration time in minutes |
| `ValidateLifetime` | bool | `true` | Whether to validate token expiration |
| `ClockSkew` | TimeSpan | `00:05:00` | Clock skew tolerance for expiration |
| `RequireAuthentication` | bool | `false` | Require authentication for all endpoints |
| `AnonymousEndpoints` | string[] | (see above) | Endpoints exempt from authentication |

## Security Best Practices

### Signing Key Requirements

- **Minimum Length**: The signing key must be at least 256 bits (32 characters) for HS256 algorithm
- **Entropy**: Use a cryptographically random key, not a simple password
- **Storage**: Store the key securely using environment variables or Azure Key Vault

Generate a secure signing key:

```bash
# Using OpenSSL
openssl rand -base64 32

# Using PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Minimum 0 -Maximum 256) }))
```

### Environment Variable Configuration

For production, set the JWT secret key via environment variable:

```bash
# Linux/macOS
export AUTHENTICATION__JWTSECRETKEY="your-secret-key-here"

# Windows PowerShell
$env:AUTHENTICATION__JWTSECRETKEY = "your-secret-key-here"
```

### Azure Key Vault Integration

For enterprise deployments, configure Azure Key Vault:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-vault.vault.azure.net/",
    "SecretMappings": {
      "Authentication:JwtSecretKey": "JWT-Secret-Key"
    }
  }
}
```

## Token Generation

### Token Structure

Aura Video Studio expects JWT tokens with the following structure:

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id",
    "iss": "AuraVideoStudio",
    "aud": "AuraApi",
    "exp": 1234567890,
    "iat": 1234567890
  }
}
```

### Example: Generate Token with C#

```csharp
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static string GenerateToken(string userId, string secretKey)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: "AuraVideoStudio",
        audience: "AuraApi",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Example: Generate Token with JavaScript

```javascript
const jwt = require('jsonwebtoken');

function generateToken(userId, secretKey) {
  const payload = {
    sub: userId,
    iat: Math.floor(Date.now() / 1000)
  };

  return jwt.sign(payload, secretKey, {
    algorithm: 'HS256',
    expiresIn: '1h',
    issuer: 'AuraVideoStudio',
    audience: 'AuraApi'
  });
}
```

## Using JWT Tokens

### HTTP Request

Include the token in the `Authorization` header:

```http
GET /api/jobs HTTP/1.1
Host: localhost:5005
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### cURL Example

```bash
curl -X GET "http://localhost:5005/api/jobs" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### JavaScript Fetch Example

```javascript
const response = await fetch('http://localhost:5005/api/jobs', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

## Error Handling

### Common Error Responses

| Status Code | Error | Description |
|-------------|-------|-------------|
| 401 | Unauthorized | No token provided or authentication required |
| 401 | Token expired | JWT token has expired |
| 401 | Invalid signature | Token signature does not match |
| 401 | Invalid issuer | Token issuer does not match configuration |
| 401 | Invalid audience | Token audience does not match configuration |

### Error Response Format

```json
{
  "type": "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E401",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required. Provide a valid API key via X-API-Key header.",
  "correlationId": "abc123"
}
```

## Fallback Authentication

When both JWT and API key authentication are enabled, the middleware will:

1. First attempt API key authentication (if `EnableApiKeyAuthentication` is true)
2. Then attempt JWT authentication (if `EnableJwtAuthentication` is true)
3. Reject the request if both fail

This allows for flexible authentication strategies where API keys can be used for service-to-service communication while JWTs are used for user authentication.

## Troubleshooting

### Token Validation Fails

1. Verify the signing key matches between token generation and validation
2. Check that issuer and audience match the configuration
3. Ensure the token has not expired
4. Verify the token format is correct (three base64-encoded sections separated by dots)

### "JWT authentication enabled but no signing key configured"

This warning indicates that JWT authentication is enabled but `JwtSecretKey` is not set. Set the signing key in configuration or environment variables.

### Clock Skew Issues

If tokens are being rejected due to timing issues, adjust the `ClockSkew` value:

```json
{
  "Authentication": {
    "ClockSkew": "00:10:00"
  }
}
```

## See Also

- [Security Hardening Guide](SECURITY_HARDENING_GUIDE.md)
- [Secret Management Guide](SECRET_MANAGEMENT_GUIDE.md)
- [API Authentication Options](../api/authentication.md)
