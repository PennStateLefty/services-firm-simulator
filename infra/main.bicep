targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of the container registry')
param containerRegistryName string = ''

@description('Name of the Log Analytics workspace')
param logAnalyticsName string = ''

@description('Name of the Application Insights instance')
param applicationInsightsName string = ''

@description('Name of the Container Apps Environment')
param containerAppsEnvironmentName string = ''

@description('Name of the Redis cache instance for Dapr state store')
param redisCacheName string = ''

@description('Enable Dapr telemetry')
param daprTelemetryEnabled bool = true

// Generate unique resource names
var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Container Registry
module containerRegistry './core/host/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    name: !empty(containerRegistryName) ? containerRegistryName : '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: tags
  }
}

// Log Analytics workspace
module logAnalytics './core/monitor/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    name: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    location: location
    tags: tags
  }
}

// Application Insights
module applicationInsights './core/monitor/application-insights.bicep' = {
  name: 'application-insights'
  params: {
    name: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// Redis Cache for Dapr state store and pub/sub
module redisCache './core/cache/redis.bicep' = {
  name: 'redis-cache'
  params: {
    name: !empty(redisCacheName) ? redisCacheName : '${abbrs.cacheRedis}${resourceToken}'
    location: location
    tags: tags
  }
}

// Container Apps Environment with Dapr
module containerAppsEnvironment './core/host/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    name: !empty(containerAppsEnvironmentName) ? containerAppsEnvironmentName : '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    daprEnabled: true
    daprTelemetryEnabled: daprTelemetryEnabled
  }
}

// Dapr Components
module daprStateStore './core/dapr/state-store.bicep' = {
  name: 'dapr-state-store'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    redisHost: redisCache.outputs.hostName
    redisPassword: redisCache.outputs.primaryKey
  }
}

module daprPubSub './core/dapr/pub-sub.bicep' = {
  name: 'dapr-pub-sub'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    redisHost: redisCache.outputs.hostName
    redisPassword: redisCache.outputs.primaryKey
  }
}

// Employee Service
module employeeService './core/host/container-app.bicep' = {
  name: 'employee-service'
  params: {
    name: '${abbrs.appContainerApps}employee-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'employeeservice' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'employeeservice:latest'
    targetPort: 8080
    daprAppId: 'employeeservice'
    daprAppPort: 8080
    env: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

// Onboarding Service
module onboardingService './core/host/container-app.bicep' = {
  name: 'onboarding-service'
  params: {
    name: '${abbrs.appContainerApps}onboarding-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'onboardingservice' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'onboardingservice:latest'
    targetPort: 8080
    daprAppId: 'onboardingservice'
    daprAppPort: 8080
    env: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

// Performance Service
module performanceService './core/host/container-app.bicep' = {
  name: 'performance-service'
  params: {
    name: '${abbrs.appContainerApps}performance-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'performanceservice' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'performanceservice:latest'
    targetPort: 8080
    daprAppId: 'performanceservice'
    daprAppPort: 8080
    env: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

// Merit Service
module meritService './core/host/container-app.bicep' = {
  name: 'merit-service'
  params: {
    name: '${abbrs.appContainerApps}merit-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'meritservice' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'meritservice:latest'
    targetPort: 8080
    daprAppId: 'meritservice'
    daprAppPort: 8080
    env: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

// Offboarding Service
module offboardingService './core/host/container-app.bicep' = {
  name: 'offboarding-service'
  params: {
    name: '${abbrs.appContainerApps}offboarding-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'offboardingservice' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'offboardingservice:latest'
    targetPort: 8080
    daprAppId: 'offboardingservice'
    daprAppPort: 8080
    env: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApplicationInsights__ConnectionString'
        value: applicationInsights.outputs.connectionString
      }
    ]
  }
}

// Frontend Static Web App
module frontend './core/host/static-web-app.bicep' = {
  name: 'frontend'
  params: {
    name: '${abbrs.webStaticSites}${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'frontend' })
    sku: {
      name: 'Standard'
      tier: 'Standard'
    }
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output EMPLOYEE_SERVICE_URL string = employeeService.outputs.uri
output ONBOARDING_SERVICE_URL string = onboardingService.outputs.uri
output PERFORMANCE_SERVICE_URL string = performanceService.outputs.uri
output MERIT_SERVICE_URL string = meritService.outputs.uri
output OFFBOARDING_SERVICE_URL string = offboardingService.outputs.uri
output FRONTEND_URL string = frontend.outputs.uri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.outputs.connectionString
