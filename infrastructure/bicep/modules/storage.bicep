// Azure Storage Account Module

param location string
param resourcePrefix string
param uniqueSuffix string
param tags object

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: toLower(replace('${resourcePrefix}sa${uniqueSuffix}', '-', ''))
  location: location
  tags: tags
  sku: {
    name: 'Standard_GRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    
    blobServices: {
      containers: [
        {
          name: 'media'
          properties: {
            publicAccess: 'None'
          }
        }
      ]
      
      deleteRetentionPolicy: {
        enabled: true
        days: 30
      }
      
      isVersioningEnabled: true
    }
  }
}

output accountName string = storage.name
output accountId string = storage.id
