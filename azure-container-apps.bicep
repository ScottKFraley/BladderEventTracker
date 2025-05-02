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

@description('Specifies the name of the storage account for PostgreSQL data.')
param storageAccountName string = 'bladdertrackerstorage'

@description('Specifies the password for the PostgreSQL database.')
@secure()
param dbPassword string

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

// Create Storage Account for PostgreSQL data
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// Create File Share for PostgreSQL data
resource fileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2022-09-01' = {
  name: '${storageAccount.name}/default/postgres-data'
  properties: {
    accessTier: 'Hot'
    shareQuota: 100
  }
}

// Create Container App
resource containerApp 'Microsoft.App/containerApps@2022-10-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 4200
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
          name: 'postgres-password'
          value: dbPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: '${containerRegistryName}.azurecr.io/bladder-tracker/frontend:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'BACKEND_API_URL'
              value: 'http://backend'
            }
          ]
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
              name: 'PG_DATABASE'
              value: 'BETrackingDb'
            }
            {
              name: 'PG_USER'
              value: 'postgres'
            }
            {
              name: 'PG_HOST'
              value: 'database'
            }
            {
              name: 'PG_PORT'
              value: '5432'
            }
            {
              name: 'PG_PASSWORD'
              secretRef: 'postgres-password'
            }
          ]
        }
        {
          name: 'database'
          image: 'postgres:15'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'POSTGRES_USER'
              value: 'postgres'
            }
            {
              name: 'POSTGRES_PASSWORD'
              secretRef: 'postgres-password'
            }
            {
              name: 'POSTGRES_DB'
              value: 'BETrackingDb'
            }
          ]
          volumeMounts: [
            {
              name: 'postgres-data'
              mountPath: '/var/lib/postgresql/data'
            }
          ]
        }
      ]
      volumes: [
        {
          name: 'postgres-data'
          storageType: 'AzureFile'
          storageName: 'postgres-storage'
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
}

output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn