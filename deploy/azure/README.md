# Dialysis PDMS – Azure Bicep Deployment

C5-compliant infrastructure in EU/Germany regions. See [OPERATIONAL-CHECKLIST.md](../../docs/OPERATIONAL-CHECKLIST.md).

## Prerequisites

- Azure CLI (`az`)
- Subscription with permissions to create resources

## Deploy

```bash
# Create resource group
az group create --name rg-dialysis-pdms --location westeurope

# Deploy (Key Vault + Service Bus)
az deployment group create \
  --resource-group rg-dialysis-pdms \
  --template-file main.bicep \
  --parameters parameters.json

# Or with overrides
az deployment group create \
  --resource-group rg-dialysis-pdms \
  --template-file main.bicep \
  --parameters location=germanywestcentral environment=prod
```

## Outputs

| Output | Description |
|--------|-------------|
| keyVaultUri | Key Vault URI for `KeyVault__VaultUri` |
| keyVaultName | Key Vault name for adding secrets |
| serviceBusConnectionString | Connection string – **store in Key Vault**; avoid logging or committing (visible in deployment history) |
| serviceBusNamespaceName | Service Bus namespace |
| location | Deployed region |

## C5 Regions

| Location | Use |
|----------|-----|
| `westeurope` | EU data residency |
| `germanywestcentral` | Germany |
| `germanycentral` | Azure Germany (sovereign) |

## Next Steps

1. Add secrets to Key Vault (see OPERATIONAL-CHECKLIST.md)
2. Grant app Managed Identity access to Key Vault
3. Deploy PostgreSQL, Redis, App Service / AKS
4. Configure IdP (Azure AD, Keycloak, Auth0)
