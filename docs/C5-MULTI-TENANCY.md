# C5 Multi-Tenancy – Database Strategy

## Current Implementation

The Dialysis PDMS uses **shared databases with TenantId filtering**:

- One database per service (e.g. `dialysis_patient`, `dialysis_treatment`)
- All tenant data stored in the same tables
- `TenantId` column on all tenant-scoped entities
- Queries always filter by `X-Tenant-Id` (via `TenantResolutionMiddleware` and `TenantContext`)
- Redis cache keys are tenant-scoped: `{tenantId}:prescription:{mrn}` (see [REDIS-CACHE.md](REDIS-CACHE.md))

## C5 Alignment

C5 (BSI Cloud Computing Compliance Criteria Catalogue) states:

> **Multi-Tenancy**: Tenant isolation via `X-Tenant-Id`, per-tenant DBs, tenant-scoped cache keys.

| Approach | Isolation | C5 Fit | Notes |
|----------|-----------|--------|-------|
| **Per-tenant DB** | Strong – physical separation | Preferred | `dialysis_patient_tenant1`, `dialysis_patient_tenant2` |
| **Shared DB + TenantId** | Logical – query filter | Acceptable | Current implementation; suitable for learning platform |

## Rationale for Current Approach

- **Learning platform**: Primary goal is understanding healthcare systems, dialysis, and FHIR. Shared DB simplifies development and deployment.
- **Operational simplicity**: Single connection string per service; no dynamic DB routing.
- **TenantId enforcement**: All reads/writes go through repositories that inject `TenantContext`; EF queries include `Where(x => x.TenantId == tenantId)`.

## Future: Per-Tenant Databases

For production deployments requiring stronger isolation:

1. **Connection string per tenant**: `ConnectionStrings:PatientDb_{TenantId}` or Key Vault per tenant
2. **DbContext factory**: Resolve connection string from `TenantContext` at request start
3. **Migrations**: Run same migrations against each tenant DB (or use a template DB)
4. **Document**: Data residency per tenant in DPA

See [.cursor/rules/multi-tenancy.mdc](../.cursor/rules/multi-tenancy.mdc) for example connection string format.
