param name string
param location string = resourceGroup().location
param tags object = {}
param logAnalyticsWorkspaceId string
param applicationInsightsConnectionString string

@description('Enable Dapr in the Container Apps Environment')
param daprEnabled bool = true

@description('Enable Dapr telemetry')
param daprTelemetryEnabled bool = true

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2022-10-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2022-10-01').primarySharedKey
      }
    }
    daprAIConnectionString: daprTelemetryEnabled ? applicationInsightsConnectionString : null
    zoneRedundant: false
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
