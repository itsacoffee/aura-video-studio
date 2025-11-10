// Azure Container Registry Module

param location string
param resourcePrefix string
param uniqueSuffix string
param tags object

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: toLower(replace('${resourcePrefix}acr${uniqueSuffix}', '-', ''))
  location: location
  tags: tags
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Enabled'
    
    policies: {
      retentionPolicy: {
        days: 30
        status: 'enabled'
      }
      trustPolicy: {
        status: 'enabled'
        type: 'Notary'
      }
    }
    
    encryption: {
      status: 'disabled'
    }
  }
}

output acrId string = acr.id
output loginServer string = acr.properties.loginServer
