# Infrastructure as Code Guide

## Overview

This guide covers the Infrastructure as Code (IaC) implementation for Aura, including Terraform and Azure Bicep templates for provisioning and managing cloud infrastructure.

## Table of Contents

- [Getting Started](#getting-started)
- [Terraform Setup](#terraform-setup)
- [Azure Bicep Setup](#azure-bicep-setup)
- [Infrastructure Components](#infrastructure-components)
- [Deployment](#deployment)
- [State Management](#state-management)

## Getting Started

### Prerequisites

**Required Tools**:
```bash
# Terraform
terraform --version  # >= 1.5.0

# Azure CLI
az --version  # >= 2.50.0

# For Azure Bicep
az bicep version  # >= 0.20.0
```

**Required Access**:
- Azure subscription owner or contributor role
- Service principal with appropriate permissions
- Access to Azure Key Vault for secrets

### Directory Structure

```
infrastructure/
├── terraform/
│   ├── main.tf                 # Main configuration
│   ├── variables.tf            # Variable definitions
│   ├── outputs.tf              # Output values
│   ├── providers.tf            # Provider configuration
│   └── modules/
│       ├── aks/               # AKS module
│       ├── networking/        # Network module
│       └── storage/           # Storage module
├── bicep/
│   ├── main.bicep             # Main deployment
│   └── modules/
│       ├── network.bicep      # Network resources
│       ├── aks.bicep          # AKS cluster
│       ├── acr.bicep          # Container registry
│       ├── redis.bicep        # Redis cache
│       ├── storage.bicep      # Storage account
│       ├── keyvault.bicep     # Key Vault
│       └── monitoring.bicep   # Monitoring resources
└── kubernetes/
    ├── namespaces/
    ├── deployments/
    └── services/
```

## Terraform Setup

### Initialize Terraform

```bash
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Create workspace for environment
terraform workspace new production
terraform workspace new staging

# Select workspace
terraform workspace select production
```

### Configure Backend

The Terraform state is stored in Azure Storage:

```hcl
# backend.tf
terraform {
  backend "azurerm" {
    resource_group_name  = "aura-terraform-state"
    storage_account_name = "auraterraformstate"
    container_name       = "tfstate"
    key                  = "production.terraform.tfstate"
  }
}
```

**Setup Backend Storage**:
```bash
# Create resource group
az group create \
  --name aura-terraform-state \
  --location eastus

# Create storage account
az storage account create \
  --name auraterraformstate \
  --resource-group aura-terraform-state \
  --location eastus \
  --sku Standard_LRS \
  --encryption-services blob

# Create container
az storage container create \
  --name tfstate \
  --account-name auraterraformstate
```

### Terraform Variables

Create `terraform.tfvars`:

```hcl
# Environment configuration
environment = "production"
location    = "eastus"

# AKS configuration
aks_node_count = 3
aks_node_size  = "Standard_D4s_v3"

# Feature flags
enable_monitoring = true
enable_backup     = true

# Tags
tags = {
  Environment = "Production"
  Project     = "Aura"
  ManagedBy   = "Terraform"
  CostCenter  = "Engineering"
}
```

### Deploy Infrastructure

```bash
# Plan deployment
terraform plan -out=tfplan

# Review plan
terraform show tfplan

# Apply deployment
terraform apply tfplan

# Get outputs
terraform output -json > infrastructure-outputs.json
```

### Update Infrastructure

```bash
# Check what will change
terraform plan

# Apply changes
terraform apply

# Refresh state
terraform refresh
```

### Destroy Infrastructure

```bash
# Plan destruction
terraform plan -destroy

# Destroy resources (CAUTION!)
terraform destroy
```

## Azure Bicep Setup

### Deploy with Bicep

#### 1. Validate Template

```bash
cd infrastructure/bicep

# Validate main template
az bicep build --file main.bicep

# What-if deployment
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters environment=production \
  --what-if
```

#### 2. Deploy to Subscription

```bash
# Deploy main template
az deployment sub create \
  --name aura-production-deployment \
  --location eastus \
  --template-file main.bicep \
  --parameters environment=production \
               location=eastus \
               aksNodeCount=3 \
               enableMonitoring=true
```

#### 3. Deploy with Parameters File

Create `parameters.production.json`:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environment": {
      "value": "production"
    },
    "location": {
      "value": "eastus"
    },
    "aksNodeCount": {
      "value": 3
    },
    "aksNodeSize": {
      "value": "Standard_D4s_v3"
    },
    "enableMonitoring": {
      "value": true
    }
  }
}
```

Deploy:
```bash
az deployment sub create \
  --name aura-production-deployment \
  --location eastus \
  --template-file main.bicep \
  --parameters @parameters.production.json
```

#### 4. Get Deployment Outputs

```bash
# Get outputs
az deployment sub show \
  --name aura-production-deployment \
  --query properties.outputs

# Save outputs to file
az deployment sub show \
  --name aura-production-deployment \
  --query properties.outputs \
  > infrastructure-outputs.json
```

## Infrastructure Components

### 1. Networking

**Resources**:
- Virtual Network (VNet)
- Subnets (AKS, Services, Application Gateway)
- Network Security Groups (NSGs)
- Private DNS zones

**Configuration**:
```bash
# Terraform
terraform plan -target=module.networking

# Bicep
az deployment group create \
  --resource-group aura-production-rg \
  --template-file modules/network.bicep
```

### 2. Azure Kubernetes Service (AKS)

**Resources**:
- AKS cluster
- Node pools
- System-assigned managed identity
- Azure AD integration

**Access Cluster**:
```bash
# Get credentials
az aks get-credentials \
  --resource-group aura-production-rg \
  --name aura-production-aks

# Verify access
kubectl get nodes
```

**Scale Node Pool**:
```bash
# Manual scaling
az aks nodepool scale \
  --resource-group aura-production-rg \
  --cluster-name aura-production-aks \
  --name default \
  --node-count 5

# Update autoscaling
az aks nodepool update \
  --resource-group aura-production-rg \
  --cluster-name aura-production-aks \
  --name default \
  --enable-cluster-autoscaler \
  --min-count 2 \
  --max-count 10
```

### 3. Container Registry

**Resources**:
- Azure Container Registry (ACR)
- Geo-replication
- Content trust
- Retention policies

**Push Images**:
```bash
# Login to ACR
az acr login --name auraproductionacr

# Tag image
docker tag aura-api:v1.2.3 auraproductionacr.azurecr.io/aura-api:v1.2.3

# Push image
docker push auraproductionacr.azurecr.io/aura-api:v1.2.3
```

### 4. Redis Cache

**Resources**:
- Azure Cache for Redis (Premium)
- Private endpoint
- TLS 1.2 enforcement

**Connection**:
```bash
# Get connection string
az redis show \
  --resource-group aura-production-rg \
  --name aura-production-redis \
  --query "hostName" -o tsv

# Get access key
az redis list-keys \
  --resource-group aura-production-rg \
  --name aura-production-redis \
  --query "primaryKey" -o tsv
```

### 5. Storage Account

**Resources**:
- Storage account (GRS)
- Blob containers
- Versioning enabled
- Soft delete enabled

**Upload Files**:
```bash
# Upload file
az storage blob upload \
  --account-name auraproductionsa \
  --container-name media \
  --name video.mp4 \
  --file ./video.mp4

# Generate SAS token
az storage blob generate-sas \
  --account-name auraproductionsa \
  --container-name media \
  --name video.mp4 \
  --permissions r \
  --expiry 2024-12-31
```

### 6. Key Vault

**Resources**:
- Azure Key Vault
- Secrets
- Access policies
- Network restrictions

**Manage Secrets**:
```bash
# Add secret
az keyvault secret set \
  --vault-name aura-production-kv \
  --name OpenAI-ApiKey \
  --value "sk-..."

# Get secret
az keyvault secret show \
  --vault-name aura-production-kv \
  --name OpenAI-ApiKey \
  --query "value" -o tsv

# List secrets
az keyvault secret list \
  --vault-name aura-production-kv
```

### 7. Monitoring

**Resources**:
- Log Analytics workspace
- Application Insights
- Alerts
- Dashboards

**Query Logs**:
```bash
# Query with Azure CLI
az monitor log-analytics query \
  --workspace aura-production-logs \
  --analytics-query "
    AppTraces
    | where TimeGenerated > ago(1h)
    | where SeverityLevel >= 3
    | project TimeGenerated, Message, SeverityLevel
    | order by TimeGenerated desc
  "
```

## State Management

### Terraform State

**View State**:
```bash
# List resources in state
terraform state list

# Show specific resource
terraform state show azurerm_kubernetes_cluster.main

# Pull current state
terraform state pull > terraform.tfstate.backup
```

**Import Existing Resources**:
```bash
# Import resource
terraform import azurerm_kubernetes_cluster.main \
  /subscriptions/{subscription-id}/resourceGroups/aura-production-rg/providers/Microsoft.ContainerService/managedClusters/aura-production-aks
```

**Manage State**:
```bash
# Move resource to different state file
terraform state mv \
  azurerm_storage_account.main \
  module.storage.azurerm_storage_account.main

# Remove resource from state (keeps in Azure)
terraform state rm azurerm_resource_group.main
```

### Backup and Recovery

**Backup State**:
```bash
# Backup Terraform state
terraform state pull > backups/terraform.tfstate.$(date +%Y%m%d)

# Backup Bicep deployment
az deployment sub show \
  --name aura-production-deployment \
  > backups/bicep-deployment.$(date +%Y%m%d).json
```

**Disaster Recovery**:
```bash
# Restore Terraform state
terraform state push backups/terraform.tfstate.20241110

# Re-import resources if state is lost
./scripts/infrastructure/reimport-resources.sh
```

## Environment Management

### Multiple Environments

#### Terraform Workspaces

```bash
# Create environments
terraform workspace new staging
terraform workspace new production

# Deploy to staging
terraform workspace select staging
terraform apply -var-file=staging.tfvars

# Deploy to production
terraform workspace select production
terraform apply -var-file=production.tfvars
```

#### Bicep Parameters

```bash
# Deploy staging
az deployment sub create \
  --template-file main.bicep \
  --parameters @parameters.staging.json

# Deploy production
az deployment sub create \
  --template-file main.bicep \
  --parameters @parameters.production.json
```

## Cost Management

### Estimate Costs

```bash
# Use Azure Pricing Calculator
# https://azure.microsoft.com/en-us/pricing/calculator/

# Export current costs
az consumption usage list \
  --start-date 2024-11-01 \
  --end-date 2024-11-10 \
  > consumption-report.json
```

### Optimize Costs

- Use Azure Reserved Instances for AKS nodes
- Enable autoscaling for dynamic workloads
- Use Standard storage tier for non-critical data
- Configure retention policies for logs
- Delete unused resources

## Security Best Practices

### Access Control

- Use managed identities instead of service principals
- Implement RBAC at resource level
- Restrict Key Vault access to specific IPs/VNets
- Enable Azure Defender for all services

### Network Security

- Deploy AKS in private VNet
- Use private endpoints for PaaS services
- Enable network security groups
- Implement Azure Firewall for egress traffic

### Secrets Management

- Store all secrets in Key Vault
- Use Key Vault references in configuration
- Rotate secrets regularly
- Enable audit logging for secret access

## Troubleshooting

### Common Issues

#### Terraform State Lock

```bash
# If state is locked
terraform force-unlock <LOCK_ID>

# Alternatively, in Azure Storage
az storage blob lease break \
  --container-name tfstate \
  --blob-name production.terraform.tfstate \
  --account-name auraterraformstate
```

#### Bicep Deployment Failures

```bash
# Get deployment error details
az deployment sub show \
  --name aura-production-deployment \
  --query properties.error

# List all deployments
az deployment sub list --query "[].{name:name,state:properties.provisioningState}"
```

#### AKS Connection Issues

```bash
# Re-authenticate
az aks get-credentials \
  --resource-group aura-production-rg \
  --name aura-production-aks \
  --overwrite-existing

# Check RBAC
az role assignment list \
  --scope /subscriptions/{subscription-id}/resourceGroups/aura-production-rg
```

---

**Last Updated**: 2024-11-10  
**Document Owner**: DevOps Team  
**Review Cycle**: Quarterly
