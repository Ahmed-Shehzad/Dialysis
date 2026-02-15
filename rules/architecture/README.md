# Architecture Rules

Architecture rules apply to design and implementation across the Dialysis PDMS.

| Rule | Description |
|------|-------------|
| [api-versioning.mdc](api-versioning.mdc) | URL path versioning `api/v1/...` |
| [c5-compliance.mdc](c5-compliance.mdc) | **C5** (BSI Cloud Computing Compliance) – mandatory for all changes |
| [mirth-integration.mdc](mirth-integration.mdc) | Mirth Connect = integration engine; PDMS = domain + FHIR |
| [data-persistence.mdc](data-persistence.mdc) | EF Core, per-tenant DBs, migrations |
| [intercessor.mdc](intercessor.mdc) | CQRS, commands, queries, events |
| [multi-tenancy.mdc](multi-tenancy.mdc) | X-Tenant-Id, per-tenant isolation |
| [verifier.mdc](verifier.mdc) | FluentValidation for commands/queries |
