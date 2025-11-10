# Secret Management Guide

## Overview

This guide explains how to securely manage secrets in Aura Video Studio using Azure Key Vault and best practices for secret handling.

## Table of Contents

- [Why Secret Management Matters](#why-secret-management-matters)
- [Azure Key Vault Setup](#azure-key-vault-setup)
- [Configuration](#configuration)
- [Secret Rotation](#secret-rotation)
- [Local Development](#local-development)
- [Production Deployment](#production-deployment)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why Secret Management Matters

### Security Risks Without Proper Secret Management

1. **Source Control Exposure**: Secrets committed to git are permanently in history
2. **Configuration File Leaks**: Config files accidentally shared or deployed
3. **Environment Variable Exposure**: Environment variables visible in process lists
4. **Log File Contamination**: Secrets accidentally logged
5. **Unauthorized Access**: Secrets without proper access controls

### Benefits of Azure Key Vault

1. **Centralized Management**: Single source of truth for all secrets
2. **Access Control**: Fine-grained RBAC and access policies
3. **Audit Trail**: Complete logging of secret access
4. **Automatic Rotation**: Built-in support for secret rotation
5. **Encryption**: Secrets encrypted at rest and in transit
6. **High Availability**: Enterprise-grade reliability

## Azure Key Vault Setup

### Prerequisites

- Azure subscription
- Azure CLI or Azure Portal access
- Appropriate permissions to create Key Vault

### Step 1: Create Key Vault

#### Using Azure CLI

```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create --name aura-production --location eastus

# Create Key Vault
az keyvault create \
  --name aura-prod-vault \
  --resource-group aura-production \
  --location eastus \
  --enable-rbac-authorization true
```

#### Using Azure Portal

1. Navigate to Azure Portal
2. Click "Create a resource"
3. Search for "Key Vault"
4. Fill in details:
   - Name: `aura-prod-vault`
   - Region: Choose appropriate region
   - Pricing tier: Standard
5. Click "Review + Create"

### Step 2: Configure Access

#### Enable Managed Identity (Recommended)

For Azure App Service or Azure Container Instances:

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name aura-api \
  --resource-group aura-production

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name aura-api \
  --resource-group aura-production \
  --query principalId -o tsv)

# Grant access to Key Vault
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $PRINCIPAL_ID \
  --scope /subscriptions/{subscription-id}/resourceGroups/aura-production/providers/Microsoft.KeyVault/vaults/aura-prod-vault
```

#### Using Service Principal (Alternative)

For non-Azure deployments:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name aura-keyvault-sp \
  --role "Key Vault Secrets User" \
  --scopes /subscriptions/{subscription-id}/resourceGroups/aura-production/providers/Microsoft.KeyVault/vaults/aura-prod-vault

# Output will include:
# - appId (ClientId)
# - password (ClientSecret)
# - tenant (TenantId)
```

### Step 3: Add Secrets

#### Using Azure CLI

```bash
# Add OpenAI API key
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --value "sk-..."

# Add Anthropic API key
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "Anthropic-ApiKey" \
  --value "sk-ant-..."

# Add JWT secret
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "JWT-Secret-Key" \
  --value "your-secret-key"
```

#### Using Azure Portal

1. Navigate to your Key Vault
2. Click "Secrets" in the left menu
3. Click "+ Generate/Import"
4. Enter:
   - Name: `OpenAI-ApiKey` (use hyphens, not colons)
   - Value: Your API key
5. Click "Create"

#### Secret Naming Convention

In Key Vault, use hyphens instead of colons:
- Configuration: `Providers:OpenAI:ApiKey`
- Key Vault: `OpenAI-ApiKey`

The application automatically maps these using the `SecretMappings` configuration.

## Configuration

### Application Configuration

#### appsettings.json

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://aura-prod-vault.vault.azure.net/",
    "UseManagedIdentity": true,
    "CacheExpirationMinutes": 60,
    "AutoReload": true,
    "SecretMappings": {
      "Providers:OpenAI:ApiKey": "OpenAI-ApiKey",
      "Providers:Anthropic:ApiKey": "Anthropic-ApiKey",
      "Providers:ElevenLabs:ApiKey": "ElevenLabs-ApiKey",
      "Providers:Stability:ApiKey": "Stability-ApiKey",
      "Authentication:JwtSecretKey": "JWT-Secret-Key"
    }
  }
}
```

#### Environment Variables (Optional Override)

```bash
# Enable Key Vault
export KeyVault__Enabled=true

# Key Vault URI
export KeyVault__VaultUri="https://aura-prod-vault.vault.azure.net/"

# Use Managed Identity (Azure deployments)
export KeyVault__UseManagedIdentity=true

# Service Principal (non-Azure deployments)
export KeyVault__UseManagedIdentity=false
export KeyVault__TenantId="your-tenant-id"
export KeyVault__ClientId="your-client-id"
export KeyVault__ClientSecret="your-client-secret"
```

### Adding New Secrets

1. Add secret to Key Vault:
```bash
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "New-Secret-Name" \
  --value "secret-value"
```

2. Update `SecretMappings` in configuration:
```json
{
  "KeyVault": {
    "SecretMappings": {
      "Your:Config:Path": "New-Secret-Name"
    }
  }
}
```

3. Access in code:
```csharp
var value = Configuration["Your:Config:Path"];
```

## Secret Rotation

### Manual Rotation

#### Step 1: Create New Secret Version

```bash
# Add new version of secret
az keyvault secret set \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --value "sk-new-key-..."
```

#### Step 2: Application Auto-Refresh

The application automatically refreshes secrets at half the cache expiration interval (default: 30 minutes).

No application restart is required!

#### Step 3: Verify

```bash
# Check secret version
az keyvault secret show \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey"

# Check application logs
tail -f logs/aura-api-*.log | grep "Secret refresh"
```

### Automatic Rotation

#### Using Azure Key Vault Rotation

```bash
# Enable rotation policy (90 days)
az keyvault secret set-attributes \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --expires "2024-12-31T23:59:59Z"

# Configure rotation notification
az keyvault secret rotation-policy update \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --rotation-policy '{
    "lifetimeActions": [{
      "trigger": {
        "timeBeforeExpiry": "P30D"
      },
      "action": {
        "type": "Notify"
      }
    }]
  }'
```

### Rotation Best Practices

1. **Frequency**: Rotate secrets every 90 days
2. **Overlap**: Keep old secret valid for 24 hours after rotation
3. **Testing**: Test new secrets in staging before production
4. **Monitoring**: Set up alerts for rotation failures
5. **Documentation**: Document rotation procedures

## Local Development

### Option 1: User Secrets (Recommended)

```bash
cd Aura.Api

# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "Providers:OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "Providers:Anthropic:ApiKey" "sk-ant-..."

# List secrets
dotnet user-secrets list
```

User secrets are stored at:
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- Linux/macOS: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

### Option 2: Environment Variables

```bash
# Windows (PowerShell)
$env:Providers__OpenAI__ApiKey="sk-..."
$env:Providers__Anthropic__ApiKey="sk-ant-..."

# Linux/macOS (Bash)
export Providers__OpenAI__ApiKey="sk-..."
export Providers__Anthropic__ApiKey="sk-ant-..."
```

### Option 3: Local Key Vault

For testing Key Vault integration locally:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://aura-dev-vault.vault.azure.net/",
    "UseManagedIdentity": false,
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "from-environment-variable"
  }
}
```

**Never commit Key Vault credentials to source control!**

## Production Deployment

### Azure App Service

1. Enable managed identity:
```bash
az webapp identity assign \
  --name aura-api \
  --resource-group aura-production
```

2. Grant Key Vault access (see setup section)

3. Set application settings:
```bash
az webapp config appsettings set \
  --name aura-api \
  --resource-group aura-production \
  --settings \
    KeyVault__Enabled=true \
    KeyVault__VaultUri="https://aura-prod-vault.vault.azure.net/" \
    KeyVault__UseManagedIdentity=true
```

### Docker/Kubernetes

#### Using Managed Identity (AKS)

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: aura-api
spec:
  containers:
  - name: aura-api
    image: aura-api:latest
    env:
    - name: KeyVault__Enabled
      value: "true"
    - name: KeyVault__VaultUri
      value: "https://aura-prod-vault.vault.azure.net/"
    - name: KeyVault__UseManagedIdentity
      value: "true"
  serviceAccountName: aura-api-sa
```

#### Using Service Principal

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: keyvault-credentials
type: Opaque
data:
  tenant-id: <base64-encoded>
  client-id: <base64-encoded>
  client-secret: <base64-encoded>
---
apiVersion: v1
kind: Pod
metadata:
  name: aura-api
spec:
  containers:
  - name: aura-api
    image: aura-api:latest
    env:
    - name: KeyVault__Enabled
      value: "true"
    - name: KeyVault__VaultUri
      value: "https://aura-prod-vault.vault.azure.net/"
    - name: KeyVault__UseManagedIdentity
      value: "false"
    - name: KeyVault__TenantId
      valueFrom:
        secretKeyRef:
          name: keyvault-credentials
          key: tenant-id
    - name: KeyVault__ClientId
      valueFrom:
        secretKeyRef:
          name: keyvault-credentials
          key: client-id
    - name: KeyVault__ClientSecret
      valueFrom:
        secretKeyRef:
          name: keyvault-credentials
          key: client-secret
```

## Best Practices

### Do's ✅

1. **Use Key Vault in production** - Never use local configuration for secrets
2. **Use Managed Identity** - Avoid service principal credentials when possible
3. **Enable auto-reload** - Allow secrets to be rotated without restart
4. **Set up monitoring** - Alert on secret access failures
5. **Rotate regularly** - Change secrets every 90 days
6. **Use least privilege** - Grant minimal required access
7. **Enable audit logging** - Track all secret access
8. **Use separate vaults** - Different vaults for dev/staging/prod

### Don'ts ❌

1. **Never commit secrets** - Use .gitignore for local config files
2. **Never log secrets** - Sanitize logs to prevent exposure
3. **Never share secrets** - Each service should have its own
4. **Never hardcode secrets** - Always use configuration
5. **Never use weak secrets** - Generate strong, random values
6. **Never ignore rotation** - Old secrets are security risks
7. **Never skip access reviews** - Regularly audit who has access
8. **Never use production secrets in dev** - Use separate environments

## Troubleshooting

### Key Vault Not Accessible

**Symptom**: Application logs show Key Vault connection errors

**Solutions**:

1. Check managed identity is enabled:
```bash
az webapp identity show \
  --name aura-api \
  --resource-group aura-production
```

2. Verify RBAC assignment:
```bash
az role assignment list \
  --assignee <principal-id> \
  --scope /subscriptions/{subscription-id}/resourceGroups/aura-production/providers/Microsoft.KeyVault/vaults/aura-prod-vault
```

3. Check network access:
```bash
az keyvault network-rule list \
  --name aura-prod-vault \
  --resource-group aura-production
```

### Secret Not Found

**Symptom**: Application logs show secret not found errors

**Solutions**:

1. Verify secret exists:
```bash
az keyvault secret show \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey"
```

2. Check secret name mapping:
```json
{
  "KeyVault": {
    "SecretMappings": {
      "Providers:OpenAI:ApiKey": "OpenAI-ApiKey"
    }
  }
}
```

3. Verify secret hasn't expired:
```bash
az keyvault secret show \
  --vault-name aura-prod-vault \
  --name "OpenAI-ApiKey" \
  --query "attributes.expires"
```

### Authentication Failures

**Symptom**: 401/403 errors when accessing Key Vault

**Solutions**:

1. For Managed Identity:
```bash
# Ensure identity has correct role
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <principal-id> \
  --scope /subscriptions/{subscription-id}/resourceGroups/aura-production/providers/Microsoft.KeyVault/vaults/aura-prod-vault
```

2. For Service Principal:
```bash
# Verify credentials are correct
az login --service-principal \
  --username <client-id> \
  --password <client-secret> \
  --tenant <tenant-id>
```

### Slow Secret Retrieval

**Symptom**: Application startup is slow

**Solutions**:

1. Increase cache expiration:
```json
{
  "KeyVault": {
    "CacheExpirationMinutes": 120
  }
}
```

2. Enable connection pooling
3. Check network latency to Key Vault
4. Consider using separate Key Vault in same region

## Security Checklist

### Development
- [ ] User secrets configured for local development
- [ ] .gitignore includes secrets files
- [ ] No secrets in source control
- [ ] Separate dev/prod Key Vaults

### Production
- [ ] Key Vault created and configured
- [ ] Managed Identity enabled
- [ ] RBAC permissions granted
- [ ] All secrets migrated to Key Vault
- [ ] Auto-reload enabled
- [ ] Audit logging enabled
- [ ] Rotation policy configured
- [ ] Monitoring alerts set up

### Operations
- [ ] Secret rotation schedule defined
- [ ] Incident response plan documented
- [ ] Regular access reviews scheduled
- [ ] Backup/recovery procedures tested

## Additional Resources

- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity Overview](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Secret Management Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
