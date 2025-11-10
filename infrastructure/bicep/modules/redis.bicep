// Azure Redis Cache Module

param location string
param resourcePrefix string
param tags object

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: '${resourcePrefix}-redis'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Premium'
      family: 'P'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
    redisVersion: '6'
  }
}

output hostname string = redis.properties.hostName
output sslPort int = redis.properties.sslPort
output resourceId string = redis.id
