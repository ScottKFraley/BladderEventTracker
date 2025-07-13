@description('Specifies the location for all resources.')
param location string = resourceGroup().location

@description('Specifies the name of the container app.')
param containerAppName string = 'bladder-tracker'

@description('Specifies the name of the container app environment.')
param containerAppEnvName string = 'bladder-tracker-env'

@description('Specifies the name of the log analytics workspace.')
param logAnalyticsWorkspaceName string = 'bladder-tracker-logs'

@description('Specifies the name of the container registry.')
param containerRegistryName string = 'bladdertracker'

@description('Specifies the name of the Azure SQL Server.')
param sqlServerName string = 'bladder-tracker-sql-${uniqueString(resourceGroup().id)}'

@description('Specifies the administrator login for the SQL server.')
param sqlAdminLogin string = 'sqladmin'

@description('Specifies the password for the SQL administrator.')
@secure()
param sqlAdminPassword string

@secure()
@description('Azure Container Registry admin password')
param acrPassword string

// Create Log Analytics workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Create Container App Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-10-01' = {
  name: containerAppEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Create Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Create SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2024-05-01-preview' = {
  parent: sqlServer
  name: 'BETrackingDb'
  location: location
  sku: {
    name: 'GP_S_Gen5_2' // The _2 is for 2 vCore capacity
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32GB
    autoPauseDelay: 60
    minCapacity: json('0.5')
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'BillOverUsage'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// Allow Azure services to access the SQL server
resource sqlServerFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Create Container App
resource containerApp 'Microsoft.App/containerApps@2022-10-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      registries: [
        {
          server: 'bladdertracker.azurecr.io'
          username: 'bladdertracker' // Usually same as registry name
          passwordSecretRef: 'acr-password'
        }
      ]
      ingress: {
        external: true
        targetPort: 80 // nginx port
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      secrets: [
        {
          name: 'sql-connection-string'
          value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=BETrackingDb;User Id=${sqlAdminLogin};Password=${sqlAdminPassword};TrustServerCertificate=true;'
        }
        {
          name: 'sql-password'
          value: sqlAdminPassword // This should be the parameter from your Bicep
        }
        {
          name: 'acr-password'
          value: acrPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'nginx'
          image: 'bladdertracker.azurecr.io/bladder-tracker/nginx:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
        {
          name: 'backend'
          image: '${containerRegistryName}.azurecr.io/bladder-tracker/backend:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:5000'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'SQL_PASSWORD'
              secretRef: 'sql-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 5
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
  dependsOn: [
    sqlDatabase
  ]
}

output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
