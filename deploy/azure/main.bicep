// Dialysis PDMS - C5-compliant Azure infrastructure
// Use location=westeurope or germanywestcentral for EU/Germany data residency
targetScope = 'resourceGroup'

@description('Azure region for C5 compliance. Use westeurope or germanywestcentral.')
@allowed(['westeurope', 'germanywestcentral', 'germanycentral'])
param location string = 'westeurope'

@description('Environment suffix (e.g. prod, staging)')
param environment string = 'prod'

@description('Deploy Key Vault for secrets')
param deployKeyVault bool = true

@description('Deploy Service Bus namespace')
param deployServiceBus bool = true

var baseName = 'dialysis-${environment}'

// Key Vault - C5 requirement for secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = if (deployKeyVault) {
  name: 'kv-${baseName}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Service Bus - messaging pipeline
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = if (deployServiceBus) {
  name: 'sb-${baseName}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    zoneRedundant: false
    minimumTlsVersion: '1.2'
  }
}

resource topicObservation 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (deployServiceBus) {
  parent: serviceBusNamespace
  name: 'observation-created'
}

resource topicHypotension 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (deployServiceBus) {
  parent: serviceBusNamespace
  name: 'hypotension-risk-raised'
}

resource topicResourceWritten 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (deployServiceBus) {
  parent: serviceBusNamespace
  name: 'resource-written'
}

resource subPrediction 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (deployServiceBus) {
  parent: topicObservation
  name: 'prediction-subscription'
}

resource subAlerting 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (deployServiceBus) {
  parent: topicHypotension
  name: 'alerting-subscription'
}

resource subSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (deployServiceBus) {
  parent: topicResourceWritten
  name: 'subscriptions-subscription'
}

resource topicHl7Ingest 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = if (deployServiceBus) {
  parent: serviceBusNamespace
  name: 'hl7-ingest'
}

resource subHis 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = if (deployServiceBus) {
  parent: topicHl7Ingest
  name: 'his-subscription'
}

output keyVaultUri string = deployKeyVault ? keyVault.properties.vaultUri : ''
output keyVaultName string = deployKeyVault ? keyVault.name : ''
output serviceBusConnectionString string = deployServiceBus ? listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', '2022-10-01-preview').primaryConnectionString : ''
output serviceBusNamespaceName string = deployServiceBus ? serviceBusNamespace.name : ''
output location string = location
