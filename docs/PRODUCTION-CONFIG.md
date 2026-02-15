# Production Configuration Guide

This guide explains how to configure Dialysis PDMS for production deployment with a real OIDC Identity Provider (IdP) and Azure Service Bus.

## Identity Provider (OIDC/JWT)

Configure `Auth:Authority` and `Auth:Audience` for your chosen IdP.

### Azure AD (Entra ID)

1. Register an app registration in Azure AD.
2. Expose an API and add scope `dialysis.read`, `dialysis.write`, `dialysis.admin`.
3. Configure API permissions for your client apps.

| Setting | Value | Example |
|---------|-------|---------|
| `Auth:Authority` | Azure AD OIDC discovery URL | `https://login.microsoftonline.com/{tenant-id}/v2.0` |
| `Auth:Audience` | Application ID URI or API identifier | `api://your-app-id` or `https://your-domain.com/api` |

**appsettings.Production.json:**
```json
{
  "Auth": {
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0",
    "Audience": "api://YOUR_APP_ID",
    "RequireHttpsMetadata": true
  }
}
```

### IdP Multi-Factor Authentication (MFA)

MFA is enforced at the Identity Provider layer. Configure your IdP (Azure AD, Keycloak, Auth0) to require MFA for users accessing Dialysis PDMS:

| IdP | MFA Configuration |
|-----|--------------------|
| **Azure AD** | Conditional Access → Require MFA for the app; or Per-user MFA in Azure AD → Security |
| **Keycloak** | Realm → Authentication → Required Actions → Configure OTP; bind to clients |
| **Auth0** | Dashboard → Security → Multi-Factor Auth → Enable (e.g. One-time Password) |

No code changes required in Dialysis PDMS; JWT validation uses the IdP’s token.

---

### Keycloak

1. Create a realm (e.g. `dialysis`) and a client for the API.
2. Create a client scope with roles: `dialysis.read`, `dialysis.write`, `dialysis.admin`.
3. Assign the scope to clients.

| Setting | Value | Example |
|---------|-------|---------|
| `Auth:Authority` | Keycloak realm OIDC URL | `https://your-keycloak.example.com/realms/dialysis` |
| `Auth:Audience` | Client ID or resource | `dialysis-api` |

**appsettings.Production.json:**
```json
{
  "Auth": {
    "Authority": "https://your-keycloak.example.com/realms/dialysis",
    "Audience": "dialysis-api",
    "RequireHttpsMetadata": true
  }
}
```

### Auth0

1. Create an API in Auth0 dashboard.
2. Create permissions (scopes) and assign to the API.

| Setting | Value | Example |
|---------|-------|---------|
| `Auth:Authority` | Auth0 tenant URL | `https://your-tenant.auth0.com/` |
| `Auth:Audience` | API Identifier | `https://your-domain.com/api` |

**appsettings.Production.json:**
```json
{
  "Auth": {
    "Authority": "https://YOUR_TENANT.auth0.com/",
    "Audience": "https://dialysis.your-domain.com/api",
    "RequireHttpsMetadata": true
  }
}
```

---

## Azure Service Bus

### Provisioning Topics and Subscriptions

Create the following entities in your Azure Service Bus namespace:

| Topic | Subscriptions |
|-------|---------------|
| `observation-created` | `prediction-subscription` |
| `hypotension-risk-raised` | `alerting-subscription` |
| `resource-written` | `subscriptions-subscription` |

**Preferred:** Deploy topics and subscriptions via Bicep (see `deploy/azure/main.bicep` and [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md)). Or use Azure Portal or CLI. Example CLI:

```bash
# Replace my-rg with your resource group name (e.g. rg-dialysis-pdms)
# Create topics
az servicebus topic create --resource-group my-rg --namespace-name dialysis-ns --name observation-created
az servicebus topic create --resource-group my-rg --namespace-name dialysis-ns --name hypotension-risk-raised
az servicebus topic create --resource-group my-rg --namespace-name dialysis-ns --name resource-written

# Create subscriptions
az servicebus topic subscription create --resource-group my-rg --namespace-name dialysis-ns --topic-name observation-created --name prediction-subscription
az servicebus topic subscription create --resource-group my-rg --namespace-name dialysis-ns --topic-name hypotension-risk-raised --name alerting-subscription
az servicebus topic subscription create --resource-group my-rg --namespace-name dialysis-ns --topic-name resource-written --name subscriptions-subscription
```

### Connection String

Set the connection string for each service that uses Service Bus:

- **FhirCore.Gateway** – publishes `ObservationCreated`, `ResourceWrittenEvent`
- **Dialysis.Prediction** – consumes `ObservationCreated`, publishes `HypotensionRiskRaised`
- **Dialysis.Alerting** – consumes `HypotensionRiskRaised`
- **FhirCore.Subscriptions** – consumes `ResourceWrittenEvent`

| Setting | Description |
|---------|-------------|
| `ServiceBus:ConnectionString` | Azure Service Bus connection string (primary or secondary) |
| `ConnectionStrings:ServiceBus` | Alternative key (some services read this) |

**Environment variable:**
```
ServiceBus__ConnectionString=Endpoint=sb://dialysis-ns.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...
```

When the connection string is empty, services use a no-op publish endpoint and consumers idle (suitable for local dev).

---

## Azure Key Vault (Optional)

For production, store secrets in Azure Key Vault. Enable by setting the vault URI:

| Setting | Description |
|---------|-------------|
| `KeyVault:VaultUri` | Key Vault URI, e.g. `https://your-vault.vault.azure.net/` |
| `KeyVault__VaultUri` | Environment variable (double underscore) |

Uses `DefaultAzureCredential` (Managed Identity, Azure CLI, env vars). Grant the app's Managed Identity **Get** and **List** on the vault's Secrets.

Secret names map to config keys: use `--` for hierarchy (e.g. `ServiceBus--ConnectionString` → `ServiceBus:ConnectionString`).

Add to `Program.cs` before `var app = builder.Build();`:
```csharp
builder.Configuration.AddKeyVaultIfConfigured();
```

Requires `Dialysis.Configuration` package and `using Dialysis.Configuration;`.

---

## Local Development with Service Bus Emulator

For local integration testing, use the [Azure Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator) (Docker). The emulator requires SQL Server/Azure SQL Edge as its internal backend—this is a Microsoft requirement. Dialysis PDMS uses **PostgreSQL** for all application data (alerting, subscriptions, audit); SQL is only used by the Service Bus Emulator itself.

```bash
docker run -d -p 5672:5672 -p 5673:5673 -p 15672:15672 mcr.microsoft.com/azure-messaging/azure-service-bus-emulator:latest
```

Connection string for emulator: `Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...` (see emulator docs for default keys).

---

## Research & Analytics

### dialysis.research Scope

Add the `dialysis.research` scope to your IdP for research export access:

| IdP | Configuration |
|-----|---------------|
| **Azure AD** | Expose API → Add scope `dialysis.research` |
| **Keycloak** | Client scope with scope `dialysis.research` |
| **Auth0** | API permissions → Add `dialysis.research` |

### Saved Cohorts (PostgreSQL)

When using PostgreSQL for saved cohorts:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:Analytics` | PostgreSQL connection string |

### Research Export De-identification

| Setting | Description |
|---------|-------------|
| `Analytics:PublicHealthBaseUrl` | PublicHealth service URL for de-identification (Basic, SafeHarbor, ExpertDetermination) |

---

## Public Health Report Delivery

When pushing reports to a public health endpoint, configure the PH service URL:

| Setting | Description |
|---------|-------------|
| `PublicHealth:ReportDeliveryEndpoint` | Full URL of the PH endpoint that receives reports (HTTP POST) |
| `PublicHealth:ReportDeliveryFormat` | Format sent: `fhir` (application/json) or `hl7v2` (default: fhir) |

**Environment variable:** `PublicHealth__ReportDeliveryEndpoint`

When not configured, `POST /api/v1/reports/deliver` generates the report but uses a no-op delivery (no actual push). Use this for local dev when no PH endpoint is available.

**Example production config:**
```json
{
  "PublicHealth": {
    "FhirBaseUrl": "https://gateway.your-domain.com/fhir",
    "ReportDeliveryEndpoint": "https://ph-surveillance.example.gov/api/receive",
    "ReportDeliveryFormat": "fhir"
  }
}
```

---

## eHealth Gateway (Certification Prep)

eHealth platforms (gematik ePA, DMP, NHS Spine) require certification. Use stub adapter for development.

### Development (Stub)

| Setting | Description |
|---------|-------------|
| `EHealth:Platform` | epa, dmp, spine |
| `EHealth:Jurisdiction` | DE, FR, UK |
| `EHealth:DocumentsBaseUrl` | Documents service URL (for documentReferenceId resolution) |
| `EHealth:FhirBaseUrl` | FHIR Gateway URL (alternative for documentReferenceId) |
| `EHealth:AuditConsentBaseUrl` | Consent check API |

### Certified Integration (DE/FR/UK)

When certified, configure jurisdiction-specific options. See [ehealth/CERTIFICATION-CHECKLIST.md](ehealth/CERTIFICATION-CHECKLIST.md).

**Germany (ePA):**
```json
{
  "EHealth": {
    "Platform": "epa",
    "Jurisdiction": "DE",
    "De": {
      "KonnektorUrl": "https://konnektor.example.de",
      "FdVBaseUrl": "https://fdv.example.de"
    }
  }
}
```

**Secrets** (Key Vault): `EHealth__De__ClientCertificate`, `EHealth__De__ClientCertificatePassword`

---

## OpenTelemetry

Configure tracing and metrics for production observability.

| Setting | Description | Example |
|---------|-------------|---------|
| `OpenTelemetry:OtlpEndpoint` | OTLP/gRPC collector endpoint | `http://localhost:4317` |

**Environment variable:** `OTEL_EXPORTER_OTLP_ENDPOINT`

When not configured, OpenTelemetry is disabled (no-op). Use with Jaeger, Grafana Tempo, or any OTLP-compatible backend.
