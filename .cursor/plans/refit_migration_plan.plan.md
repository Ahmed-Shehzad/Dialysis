---
name: Refit Migration Plan
overview: Replace HttpClient with Refit for type-safe REST client calls across internal service-to-service communication.
todos:
  - id: add-refit-packages
    content: "Add Refit and Refit.HttpClientFactory to Directory.Packages.props"
    status: completed
  - id: migrate-device-registration
    content: "Migrate DeviceRegistrationClient (Treatment, Alarm) to Refit"
    status: completed
  - id: migrate-reports
    content: "Migrate ReportsAggregationService to Refit"
    status: completed
  - id: migrate-fhir-subscription-notify
    content: "Migrate FhirSubscriptionNotifyClient to Refit"
    status: completed
  - id: migrate-fhir-bulk-export
    content: "Migrate FhirBulkExportService to Refit"
    status: completed
  - id: migrate-cds-controllers
    content: "Migrate CDS controllers (HypotensionRisk, etc.) to Refit"
    status: completed
  - id: migrate-seeder
    content: "Migrate Seeder to Refit"
    status: completed
isProject: false
---

# Refit Migration Plan

## Scope

| Component | Action | Notes |
|-----------|--------|-------|
| DeviceRegistrationClient | Refit | IDeviceApi with POST api/devices |
| ReportsAggregationService | Refit | Gateway client with multiple GET endpoints |
| FhirSubscriptionNotifyClient | Refit | Single POST to notify URL |
| FhirBulkExportService | Refit | Multiple GET to Patient/Device/Prescription/Treatment/Alarm |
| CDS controllers | Refit | GET Treatment/Prescription via gateway |
| Seeder | Refit | POST Prescription, Treatment, Alarm HL7 |
| SubscriptionDispatcher | Keep HttpClient | Dynamic resourceUrl and subscriber endpoints |
| Transponder Webhooks | Keep HttpClient | Arbitrary webhook URLs |

## Shared Handlers

- `X-Tenant-Id` and `Authorization` via DelegatingHandler or Refit `[Header]` + `ITenantContext` / `IHttpContextAccessor`
- Per-request headers: use `[HeaderCollection]` or custom handler

## Out of Scope

- Transponder webhook delivery (fully dynamic URLs)
- SubscriptionDispatcher rest-hook POST (subscriber endpoints are user-configured)
