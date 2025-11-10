// Aura Production Infrastructure - Azure Bicep
// Main deployment template for Aura resources

targetScope = 'subscription'

@description('Environment name')
@allowed([
  'staging'
  'production'
])
param environment string = 'production'

@description('Azure region for resources')
param location string = 'eastus'

@description('Number of AKS nodes')
@minValue(2)
@maxValue(20)
param aksNodeCount int = 3

@description('AKS node size')
param aksNodeSize string = 'Standard_D4s_v3'

@description('Enable monitoring and diagnostics')
param enableMonitoring bool = true

@description('Resource tags')
param tags object = {
  Environment: environment
  Project: 'Aura'
  ManagedBy: 'Bicep'
}

// Variables
var resourcePrefix = 'aura-${environment}'
var uniqueSuffix = uniqueString(subscription().subscriptionId, environment)

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: '${resourcePrefix}-rg'
  location: location
  tags: tags
}

// Virtual Network Module
module vnet './modules/network.bicep' = {
  scope: rg
  name: 'vnet-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// AKS Cluster Module
module aks './modules/aks.bicep' = {
  scope: rg
  name: 'aks-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    nodeCount: aksNodeCount
    nodeSize: aksNodeSize
    subnetId: vnet.outputs.aksSubnetId
    enableMonitoring: enableMonitoring
    logAnalyticsWorkspaceId: enableMonitoring ? monitoring.outputs.workspaceId : ''
    tags: tags
  }
}

// Container Registry Module
module acr './modules/acr.bicep' = {
  scope: rg
  name: 'acr-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

// Redis Cache Module
module redis './modules/redis.bicep' = {
  scope: rg
  name: 'redis-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// Storage Account Module
module storage './modules/storage.bicep' = {
  scope: rg
  name: 'storage-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    uniqueSuffix: uniqueSuffix
    tags: tags
  }
}

// Key Vault Module
module keyVault './modules/keyvault.bicep' = {
  scope: rg
  name: 'keyvault-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    uniqueSuffix: uniqueSuffix
    aksIdentityPrincipalId: aks.outputs.kubeletIdentityObjectId
    subnetIds: [
      vnet.outputs.aksSubnetId
      vnet.outputs.servicesSubnetId
    ]
    tags: tags
  }
}

// Monitoring Module
module monitoring './modules/monitoring.bicep' = if (enableMonitoring) {
  scope: rg
  name: 'monitoring-deployment'
  params: {
    location: location
    resourcePrefix: resourcePrefix
    tags: tags
  }
}

// Role Assignments
module acrRoleAssignment './modules/role-assignment.bicep' = {
  scope: rg
  name: 'acr-role-assignment'
  params: {
    principalId: aks.outputs.kubeletIdentityObjectId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    scope: acr.outputs.acrId
  }
}

// Outputs
output resourceGroupName string = rg.name
output aksClusterName string = aks.outputs.clusterName
output acrLoginServer string = acr.outputs.loginServer
output redisHostname string = redis.outputs.hostname
output storageAccountName string = storage.outputs.accountName
output keyVaultUri string = keyVault.outputs.vaultUri
output appInsightsInstrumentationKey string = enableMonitoring ? monitoring.outputs.instrumentationKey : ''
