# Aura Production Infrastructure - Main Configuration
# Terraform configuration for deploying Aura to cloud environments

terraform {
  required_version = ">= 1.5.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.23"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "aura-terraform-state"
    storage_account_name = "auraterraformstate"
    container_name       = "tfstate"
    key                  = "production.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

# Variables
variable "environment" {
  description = "Environment name (staging, production)"
  type        = string
  default     = "production"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

variable "aks_node_count" {
  description = "Number of AKS nodes"
  type        = number
  default     = 3
}

variable "aks_node_size" {
  description = "Size of AKS nodes"
  type        = string
  default     = "Standard_D4s_v3"
}

variable "enable_monitoring" {
  description = "Enable Azure Monitor"
  type        = bool
  default     = true
}

# Locals
locals {
  common_tags = {
    Environment = var.environment
    Project     = "Aura"
    ManagedBy   = "Terraform"
    CostCenter  = "Engineering"
  }
  
  resource_prefix = "aura-${var.environment}"
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_prefix}-rg"
  location = var.location
  tags     = local.common_tags
}

# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "${local.resource_prefix}-vnet"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  address_space       = ["10.0.0.0/16"]
  tags                = local.common_tags
}

# Subnets
resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.1.0/24"]
}

resource "azurerm_subnet" "services" {
  name                 = "services-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = ["10.0.2.0/24"]
}

# Azure Kubernetes Service (AKS)
resource "azurerm_kubernetes_cluster" "main" {
  name                = "${local.resource_prefix}-aks"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "${local.resource_prefix}-aks"
  
  default_node_pool {
    name                = "default"
    node_count          = var.aks_node_count
    vm_size             = var.aks_node_size
    vnet_subnet_id      = azurerm_subnet.aks.id
    enable_auto_scaling = true
    min_count           = 2
    max_count           = 10
    
    tags = local.common_tags
  }
  
  identity {
    type = "SystemAssigned"
  }
  
  network_profile {
    network_plugin    = "azure"
    load_balancer_sku = "standard"
    service_cidr      = "10.1.0.0/16"
    dns_service_ip    = "10.1.0.10"
  }
  
  azure_active_directory_role_based_access_control {
    managed = true
  }
  
  oms_agent {
    log_analytics_workspace_id = var.enable_monitoring ? azurerm_log_analytics_workspace.main[0].id : null
  }
  
  tags = local.common_tags
}

# Azure Container Registry
resource "azurerm_container_registry" "main" {
  name                = replace("${local.resource_prefix}acr", "-", "")
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Premium"
  admin_enabled       = false
  
  georeplications {
    location                = "westus"
    zone_redundancy_enabled = true
    tags                    = local.common_tags
  }
  
  tags = local.common_tags
}

# Role assignment for AKS to pull from ACR
resource "azurerm_role_assignment" "aks_acr" {
  principal_id                     = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.main.id
  skip_service_principal_aad_check = true
}

# Azure Cache for Redis
resource "azurerm_redis_cache" "main" {
  name                = "${local.resource_prefix}-redis"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  capacity            = 2
  family              = "P"
  sku_name            = "Premium"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  
  redis_configuration {
    enable_authentication = true
  }
  
  tags = local.common_tags
}

# Azure Storage Account (for media files)
resource "azurerm_storage_account" "main" {
  name                     = replace("${local.resource_prefix}sa", "-", "")
  location                 = azurerm_resource_group.main.location
  resource_group_name      = azurerm_resource_group.main.name
  account_tier             = "Standard"
  account_replication_type = "GRS"
  
  blob_properties {
    versioning_enabled = true
    
    delete_retention_policy {
      days = 30
    }
  }
  
  tags = local.common_tags
}

resource "azurerm_storage_container" "media" {
  name                  = "media"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# Azure Key Vault (for secrets)
resource "azurerm_key_vault" "main" {
  name                       = "${local.resource_prefix}-kv"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 90
  purge_protection_enabled   = true
  
  network_acls {
    bypass         = "AzureServices"
    default_action = "Deny"
    
    virtual_network_subnet_ids = [
      azurerm_subnet.aks.id,
      azurerm_subnet.services.id
    ]
  }
  
  tags = local.common_tags
}

# Key Vault access policy for AKS
resource "azurerm_key_vault_access_policy" "aks" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  
  secret_permissions = [
    "Get",
    "List"
  ]
}

# Azure Application Insights
resource "azurerm_application_insights" "main" {
  count               = var.enable_monitoring ? 1 : 0
  name                = "${local.resource_prefix}-appinsights"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.main[0].id
  
  tags = local.common_tags
}

# Log Analytics Workspace
resource "azurerm_log_analytics_workspace" "main" {
  count               = var.enable_monitoring ? 1 : 0
  name                = "${local.resource_prefix}-logs"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  
  tags = local.common_tags
}

# Data source
data "azurerm_client_config" "current" {}

# Outputs
output "aks_cluster_name" {
  value       = azurerm_kubernetes_cluster.main.name
  description = "AKS cluster name"
}

output "acr_login_server" {
  value       = azurerm_container_registry.main.login_server
  description = "ACR login server"
}

output "redis_hostname" {
  value       = azurerm_redis_cache.main.hostname
  description = "Redis hostname"
  sensitive   = true
}

output "storage_account_name" {
  value       = azurerm_storage_account.main.name
  description = "Storage account name"
}

output "key_vault_uri" {
  value       = azurerm_key_vault.main.vault_uri
  description = "Key Vault URI"
}

output "app_insights_instrumentation_key" {
  value       = var.enable_monitoring ? azurerm_application_insights.main[0].instrumentation_key : null
  description = "Application Insights instrumentation key"
  sensitive   = true
}
