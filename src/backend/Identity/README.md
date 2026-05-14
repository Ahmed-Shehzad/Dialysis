# Identity — OIDC / Keycloak BFF module

Identity is the platform's standards-based identity broker. It wraps Keycloak (the dev IdP) and exposes a Backend-for-Frontend for browser flows plus a Provisioning slice that materializes Identity events on the message bus.

Hosts as a separate ASP.NET app (`Dialysis.Identity.Api` and `Dialysis.Identity.Bff`).

## Slices

| Slice | Responsibility |
|---|---|
| [`Dialysis.Identity.Contracts`](Dialysis.Identity.Contracts) | Cross-context integration-event contracts (`UserRegistered`, `UserDeactivated`, `RoleAssigned`, `RoleRevoked`). |
| [`Dialysis.Identity.Provisioning`](Dialysis.Identity.Provisioning) | Local provisioning operations (`ProvisionUser`, `AssignRoleToUser`, `RevokeRoleFromUser`, `DeactivateUser`) — issue integration events. |
| [`Dialysis.Identity.Persistence`](Dialysis.Identity.Persistence) | `IdentityDbContext`, schema-per-slice tables. |
| [`Dialysis.Identity.Composition`](Dialysis.Identity.Composition) | `AddIdentityModule(...)` registration. |
| [`Dialysis.Identity.Api`](Dialysis.Identity.Api) | Provisioning ASP.NET host. |
| [`Dialysis.Identity.Bff`](Dialysis.Identity.Bff) | OIDC BFF host for browser auth flows. See [`RUNBOOK.md`](RUNBOOK.md). |
| [`Dialysis.Identity.Tests`](Dialysis.Identity.Tests) | Integration tests. |

See [`identity_subdomain_structure.md`](identity_subdomain_structure.md) for the large-scale-structure note.

---

## DDD Alignment

**Subdomain classification** (Evans, p. 281): **Generic Subdomain**. OIDC, JWT issuance, role/group mapping is standardized commodity infrastructure. The Identity module is replaceable by Okta, Auth0, Azure Entra, etc. with appropriate config; no clinical or billing logic is allowed to creep in.

**Domain vision statement**: *"Standard OIDC. Replaceable by Okta/Auth0/Entra. No clinical or billing logic."*

**Bounded Context**: `Dialysis.Identity.*` is a single Bounded Context with one slice (`Provisioning`). All cross-context references go through `Dialysis.Identity.Contracts`.

**Aggregate roots**: minimal. Local provisioning aggregates around `User`/`Role` are deliberately thin because the authoritative store is the IdP (Keycloak), not this module. The integration events are the durable interface.

**Context-map role** (Evans pp. 250–264):
- **Conformist source** (Evans p. 255): Identity supplies the OIDC contract; every downstream module Conforms to JWT claims and role names (`<Module>:Authentication:RolePermissionMap`). There is no per-downstream customization.
- **Open Host Service + Published Language** for `UserRegistered`/`UserDeactivated`/`RoleAssigned`/`RoleRevoked` events.

**Large-scale structure**: none beyond the standard OIDC + Provisioning split. This is intentional — generic subdomains do not warrant deep modeling effort (Evans p. 281).

**Module-specific anti-patterns to watch**:
- Building clinical-role logic (e.g. "renal-nurse only") inside Identity. Role names are opaque strings here; each downstream module maps them to its own permission catalog (`HisPermissions`, `EhrPermissions`, `PdmsPermissions`).
- Storing per-module business permissions in `IdentityDbContext`. The IdP owns role *names*; each module owns role *meaning*.
- Treating the BFF as an application-layer entry point. It is a pure auth gateway; business endpoints live in their owning module's API.

**Integration-event versioning**: see [`Dialysis.Identity.Contracts/Integration/`](Dialysis.Identity.Contracts/Integration) and the policy in [`Versioning.md`](../DomainDrivenDesign/Dialysis.Domain.Driven.Design.Core.Abstraction/IntegrationEvents/Versioning.md).
