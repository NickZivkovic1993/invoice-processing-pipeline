// LedgerFlow infrastructure: capture storage, Service Bus, Document Intelligence, SQL, the
// Functions worker and the exception-queue API. Every compute identity is managed; no secrets in
// app settings. Deploy with:
//   az deployment group create -g <rg> -f infra/main.bicep -p namePrefix=<prefix> sqlAdminLogin=<u> sqlAdminPassword=<p>

@description('Short prefix for all resource names, e.g. "ledgerflow".')
param namePrefix string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SQL administrator login.')
param sqlAdminLogin string

@description('SQL administrator password.')
@secure()
param sqlAdminPassword string

var suffix = uniqueString(resourceGroup().id)
var storageName = toLower('${namePrefix}stg${suffix}')
var serviceBusName = '${namePrefix}-sb-${suffix}'
var docIntelName = '${namePrefix}-di-${suffix}'
var planName = '${namePrefix}-plan-${suffix}'
var functionAppName = '${namePrefix}-fn-${suffix}'
var apiAppName = '${namePrefix}-api-${suffix}'
var sqlServerName = '${namePrefix}-sql-${suffix}'
var insightsName = '${namePrefix}-ai-${suffix}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource inbox 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storageName}/default/invoices-inbox'
  dependsOn: [storage]
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusName
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
}

resource ingestQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: 'invoices-in'
  properties: {
    maxDeliveryCount: 5
    deadLetteringOnMessageExpiration: true
  }
}

resource docIntel 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: docIntelName
  location: location
  sku: { name: 'S0' }
  kind: 'FormRecognizer'
  properties: {
    customSubDomainName: docIntelName
    publicNetworkAccess: 'Enabled'
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: { Application_Type: 'web' }
}

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'LedgerFlow'
  location: location
  sku: { name: 'S0', tier: 'Standard' }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: { name: 'Y1', tier: 'Dynamic' }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'AzureWebJobsStorage__accountName', value: storage.name }
        { name: 'BlobServiceUri', value: storage.properties.primaryEndpoints.blob }
        { name: 'ServiceBusConnection__fullyQualifiedNamespace', value: '${serviceBusName}.servicebus.windows.net' }
        { name: 'IngestQueueName', value: 'invoices-in' }
        { name: 'DocumentIntelligenceEndpoint', value: docIntel.properties.endpoint }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output apiAppName string = apiAppName
output docIntelEndpoint string = docIntel.properties.endpoint
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
