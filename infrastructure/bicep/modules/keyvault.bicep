// Azure Key Vault Module

param location string
param resourcePrefix string
param uniqueSuffix string
param aksIdentityPrincipalId string
param subnetIds array
param tags object

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${resourcePrefix}-kv-${uniqueSuffix}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      virtualNetworkRules: [for subnetId in subnetIds: {
        id: subnetId
      }]
    }
    
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: aksIdentityPrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

output vaultUri string = keyVault.properties.vaultUri
output vaultName string = keyVault.name
