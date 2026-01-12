param containerAppsEnvironmentName string
param redisHost string
@secure()
param redisPassword string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource daprComponent 'Microsoft.App/managedEnvironments/daprComponents@2023-05-01' = {
  name: 'statestore'
  parent: containerAppsEnvironment
  properties: {
    componentType: 'state.redis'
    version: 'v1'
    metadata: [
      {
        name: 'redisHost'
        value: '${redisHost}:6380'
      }
      {
        name: 'redisPassword'
        secretRef: 'redis-password'
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
    ]
    secrets: [
      {
        name: 'redis-password'
        value: redisPassword
      }
    ]
    scopes: [
      'employeeservice'
      'onboardingservice'
      'performanceservice'
      'meritservice'
      'offboardingservice'
    ]
  }
}

output name string = daprComponent.name
