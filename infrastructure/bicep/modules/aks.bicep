// AKS Cluster Module

param location string
param resourcePrefix string
param nodeCount int
param nodeSize string
param subnetId string
param enableMonitoring bool
param logAnalyticsWorkspaceId string
param tags object

resource aks 'Microsoft.ContainerService/managedClusters@2023-08-01' = {
  name: '${resourcePrefix}-aks'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: '${resourcePrefix}-aks'
    
    agentPoolProfiles: [
      {
        name: 'default'
        count: nodeCount
        vmSize: nodeSize
        vnetSubnetID: subnetId
        enableAutoScaling: true
        minCount: 2
        maxCount: 10
        osType: 'Linux'
        mode: 'System'
        type: 'VirtualMachineScaleSets'
      }
    ]
    
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
      loadBalancerSku: 'standard'
      serviceCidr: '10.1.0.0/16'
      dnsServiceIP: '10.1.0.10'
    }
    
    aadProfile: {
      managed: true
      enableAzureRBAC: true
    }
    
    addonProfiles: enableMonitoring ? {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    } : {}
    
    autoUpgradeProfile: {
      upgradeChannel: 'stable'
    }
    
    securityProfile: {
      defender: {
        logAnalyticsWorkspaceResourceId: enableMonitoring ? logAnalyticsWorkspaceId : null
        securityMonitoring: {
          enabled: enableMonitoring
        }
      }
    }
  }
}

output clusterName string = aks.name
output kubeletIdentityObjectId string = aks.properties.identityProfile.kubeletidentity.objectId
output fqdn string = aks.properties.fqdn
