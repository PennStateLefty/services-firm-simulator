param name string
param location string = resourceGroup().location
param tags object = {}

@description('SKU name for Redis cache')
@allowed(['Basic', 'Standard', 'Premium'])
param sku string = 'Basic'

@description('SKU family for Redis cache')
@allowed(['C', 'P'])
param skuFamily string = 'C'

@description('SKU capacity')
param capacity int = 0

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
      family: skuFamily
      capacity: capacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

output id string = redisCache.id
output name string = redisCache.name
output hostName string = redisCache.properties.hostName
output sslPort int = redisCache.properties.sslPort
output primaryKey string = redisCache.listKeys().primaryKey
