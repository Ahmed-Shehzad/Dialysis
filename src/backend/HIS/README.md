# Hospital Information System (HIS)

Modular monolith under `src/backend/HIS`, aligned with **Tummers et al. (2021)** — *Designing a reference architecture for health information systems* ([PDF](../../../docs/book/s12911-021-01570-2.pdf), [DOI](https://doi.org/10.1186/s12911-021-01570-2)). Bounded contexts, vertical slices, CQRS (Intercessor + Verifier), DDD primitives, and [Transponder](../BuildingBlocks/Transponder/README.md) for async integration including the **shared** transactional outbox/inbox on `HisDbContext` (no duplicate HIS outbox tables).

## Documentation in this folder

| Document | Purpose |
|----------|---------|
| [his_ddd_modular_plan.md](./his_ddd_modular_plan.md) | Full architecture plan: RA mapping, bounded contexts, physical layout, phased feature checklist, definition of done |
| [his_ra_submodules.md](./his_ra_submodules.md) | **Fig. 6 (34 sub-modules)** traceability table (labels, Stub/Partial status, code and API links) |
| [his_production_security_backlog.md](./his_production_security_backlog.md) | Production security backlog (IdP, TLS, secrets, audit/PHI) — aligns with B2b/B3 |
| [his_integration_backlog.md](./his_integration_backlog.md) | Real integrations backlog (broker, HL7/FHIR, LIS/pharmacy, data pipelines) |
| [his_transponder_e2e_runbook.md](./his_transponder_e2e_runbook.md) | Broker + outbox relay: config, RabbitMQ example, observability/idempotency notes |
| [his_api_threat_model_notes.md](./his_api_threat_model_notes.md) | Lightweight API / trust-boundary checklist (threat modeling aid) |
| [Transponder README](../BuildingBlocks/Transponder/README.md) | Messaging: `ITransponderBus`, transports (RabbitMQ, NATS), `AddTransponder`, consumers, correlation |

## Build

HIS projects are included in the repo root solution **`Dialysis.slnx`**.

```bash
dotnet build Dialysis.slnx
dotnet test src/backend/HIS/Dialysis.HIS.Tests/Dialysis.HIS.Tests.csproj
```

Run the HTTP host (`http://localhost:5288` by default). Versioned routes use **`api/v1.0/...`** (see `Dialysis.HIS.Api/Controllers`). Discover modules: **`GET /api/v1.0/reference-architecture/catalog`**; discover capability entrypoints: **`GET /api/v1.0/reference-architecture/capabilities`**; RA **Help** slice + doc paths: **`GET /api/v1.0/help`**. Responses include **`links`** (HATEOAS) except **`204 No Content`**.

OpenAPI (Microsoft.AspNetCore.OpenApi + Asp.Versioning.Mvc.ApiExplorer): one JSON document per ApiExplorer group name (matches `[ApiVersion]` → e.g. **`GET http://localhost:5288/openapi/v1.json`** for API **1.0**). Neutral endpoints such as **`GET /health`** appear in every versioned document.

Neutral **`/health`** HATEOAS links use **`IOptions<ApiVersioningOptions>.Value.DefaultApiVersion`** (same default as `Program.cs`), not a hard-coded URL segment. Optional config **`His:InMemoryDatabaseName`** overrides the default EF in-memory store name (`DialysisHIS`) — used by **`Dialysis.HIS.Tests`** so parallel **`WebApplicationFactory`** runs do not share one in-memory database.

```bash
dotnet run --project src/backend/HIS/Dialysis.HIS.Api/Dialysis.HIS.Api.csproj
```

## Composition (host registration)

Reference assembly **`Dialysis.HIS.Composition`** (or run **`Dialysis.HIS.Api`**) and call:

```csharp
services.AddHospitalInformationSystem(
    configuration,                           // IConfiguration — binds His:Authentication, His:Transponder, …
    configurePersistence: null,              // default: EF in-memory; pass o => o.UseSqlServer(...) for SQL
    enableOutboxDispatcher: false,           // or read His:Transponder:EnableOutboxRelay (see appsettings)
    configureTransponderTransport: null);    // optional: e.g. s => s.AddHisTransponderRabbitMqIfConfigured(uri, queue, exchange)
```

Use a **generic host** so `IHostedService` runs (database initializer; optional Transponder outbox relay).

**Configuration keys** (see `Dialysis.HIS.Api/appsettings.json`):

- **`His:Authentication:Authority`** — when set, registers JWT Bearer; permission claims use type **`His:Authentication:PermissionClaimType`** (default `his_permission`) with values from `HisPermissions`. When **empty**, `ICurrentUser` keeps all dev permissions (same as the former `DevelopmentCurrentUser`).
- **`His:Authentication:RoleClaimType`** + **`His:Authentication:RolePermissionMap`** — map IdP role/group names to HIS permission strings (merged with direct `his_permission` claims). **`His:Authentication:PatientPortalPatientIdClaimType`** — optional explicit patient id claim for portal scoping (default `his_patient_id`; `sub` also accepted when it equals route `patientId`).
- **`His:PatientAccess:RequireExplicitConsentRowForPortal`** — when `true`, portal consent gate requires a persisted `PortalConsentPreference` row (no legacy implicit allow-all).
- **`His:RequireHttpsRedirection`** / **`His:UseHsts`** — when `true` and not Development, enables HTTPS redirection and HSTS (B3; TLS still expected at ingress).
- **`His:UseForwardedHeaders`** — when `true`, enables `X-Forwarded-Proto` / `X-Forwarded-For` for correct scheme/client IP behind a reverse proxy.
- **`His:Laboratory:BaseUri`** — when set, **`ILaboratoryGateway`** uses HTTP to this base instead of the in-process stub (integration spine demo).
- **`His:Transponder:EnableOutboxRelay`** — when `true`, publishes outbox rows to **`ITransponderBus`**.
- **`His:Transponder:RabbitMq:ConnectionUri`** — when set, **`Dialysis.HIS.Api`** replaces the in-memory bus with RabbitMQ (same subscriptions as `AddHisIntegrationConsumers`).

---

## Living checklist (first slice vs next)

Legend: **[x]** first vertical slice in code · **[ ]** still to do or harden for production

### Phase A — Platform and composition

- [x] **A1**: Projects per bounded context + `Dialysis.HIS.Contracts` + `Dialysis.HIS.Persistence` + `Dialysis.HIS.Composition`
- [x] **A2**: **`Dialysis.HIS.Api`** — ASP.NET host calling `AddHospitalInformationSystem`; **versioned MVC** under `api/v1.0/...` (`Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer`, URL segment reader) plus neutral `/health`; **OpenAPI** per ApiExplorer group (`Microsoft.AspNetCore.OpenApi`, e.g. `/openapi/v1.json`). Successful JSON bodies use **HATEOAS** shape `{"data":...,"links":[{"rel","href","method"}]}` via `ResourceEnvelope<T>` (`Dialysis.HIS.Api/Hateoas`). RA extension reads: **`GET /api/v1.0/reference-architecture/capabilities`** (index) and sub-routes (schema **`his_ra`**, CQRS in **`Dialysis.HIS.RaCapabilities`**).
- [x] **A3**: Single `HisDbContext`, **schema-per-context** table naming (`his_security`, `his_patientflow`, …)
- [x] **A3b**: **EF migrations** — `20260504000348_InitialHis`, `20260504001308_SchedulingResourcesPortalConsentDeviceIdempotency`, `20260504003229_RaCapabilitiesHisRaSchema` + `HisDbContextDesignTimeFactory` (connection: `dotnet ef ... -- --connection "..."`, env `HIS_SQL_CONNECTION`, or `ConnectionStrings:His` / `ConnectionStrings__His` from `Dialysis.HIS.Api` appsettings + env — same as `Dialysis.HIS.Api` at runtime); SQL Server uses `MigrateAsync` in `HisDatabaseInitializer`
- [x] **A4**: **Transponder** transactional outbox/inbox on `HisDbContext` (`transponder` schema); optional `enableOutboxDispatcher` → `AddTransponderOutboxRelay<HisDbContext>()` — wire **broker transport** per [Transponder README](../BuildingBlocks/Transponder/README.md) for production

### Phase B — Security

- [x] **B1**: Local user model + registration (stub password handling)
- [x] **B2**: Permissions + `AuthorizationPipelineBehavior` on permissioned commands/queries; `DevelopmentCurrentUser` grants all for dev
- [x] **B2b**: **Real identity** (claims / IdP) and least-privilege roles — **partial**: optional JWT + `his_permission` claims when `His:Authentication:Authority` is set (`HttpContextCurrentUser`); **IdP role → `HisPermissions`** via **`His:Authentication:RolePermissionMap`**; portal **patient scope** filter when Authority is set
- [x] **B3**: **Partial** — optional **`His:RequireHttpsRedirection`**, **`His:UseHsts`**, **`His:UseForwardedHeaders`** (ingress/TLS posture; secrets/password policy still follow backlog)
- [x] **B4**: `IAuditTrail` + EF-backed audit rows (separate save from domain transaction — refine if you need single UoW)

### Phase C — Patient flow

- [x] **C1**: MRN on `Patient` (EHR link / ACL when EHR owns identity)
- [x] **C2**: Register patient, admit, discharge
- [x] **C3**: Referral create (narrow type)
- [x] **C4**: Integration events + outbox enqueue before `SaveChanges`
- [x] **C4b**: **Transponder `IConsumer<>`** stubs for EHR/PDMS/pharmacy/scheduling (`AddHisIntegrationConsumers` on the Transponder builder in composition)

### Phase D — Scheduling

- [x] **D1**: `Appointment` + book command
- [x] **D2**: Single resource overlap rule
- [x] **D3**: **`SchedulingResource`** registry + **`ListSchedulingResources`** query + **`BookAppointment`** requires **`ResourceKindCode`** (directory + overlap); demo seed + **`GET /api/scheduling/resources`**; **waitlist enqueue** via **`POST …/capabilities/planning-and-scheduling/waitlists`** (`his.ra.commands.write`); advanced rules still open

### Phase E — Medication

- [x] **E1** / **E2**: Place order + record administration; **discontinue** command + domain rules + `MedicationOrderDiscontinuedIntegrationEvent`
- [x] **E3**: **`IMedicationOrderSafetyPolicy`** + **`FormularyMedicationOrderSafetyPolicy`** (demo blacklist); **Pharmacy** `IConsumer<>` stubs + **`IPharmacyGateway`** stub **or** **`HttpPharmacyGateway`** when **`His:Pharmacy:BaseUri`** is set (mirror lab); full formulary still open

### Phase F — Operations (generic MIS)

- [x] **F1**–**F3**: Staff role, inventory movement, billing export job **stubs**
- [x] **F***: **Partial** — RA **coordination** write: `POST …/capabilities/generic-mis/organizational-communications` (`his.ra.commands.write`); billing job **`StatusCode`** / **`PayerCode`** + **`GET …/operations/billing/export-jobs/{id}`**; RA **EHR document register**, **quality task status**, **security assessment** POSTs under capabilities

### Phase G — Data services

- [x] **G1**–**G3**: Import job command, search query, dashboard query (stubs / thin read models)
- [x] **G***: **Partial** — import job **validation** + **`GET …/data-management/import-jobs/{id}`**; full-text list **optional `q` filter**; **request analytics export** command + RA rows; **manager-dashboard** optional **`reportFocus`** + queued billing / open quality counts; **`GET …/data-management/integration/outbox-metadata`** (outbox metadata index, `his.data.share.read`)

### Phase H — Integration

- [x] **H1**: `IntegrationEventCatalog` + contract records in `Dialysis.HIS.Contracts`
- [x] **H2**: **`ILaboratoryGateway`** stub + **`LaboratoryReferralFromHisStubConsumer`** (ACL-shaped path)
- [x] **H3**: Device ingest + **rate limiter**; optional **`ExternalMessageId`** + filtered unique index for **idempotent** replays
- [x] **H***: **Partial** — optional HTTP **`ILaboratoryGateway`** / **`IPharmacyGateway`** when lab/pharmacy **BaseUri** is set; **`docker-compose.integration.yml`** + runbook for Rabbit; broker/outbox under **`his_transponder_e2e_runbook.md`**; full-scale idempotency/DLQ still open

### Phase I — Patient portal

- [x] **I1** / **I2**: Portal summary + appointment request; **`PortalConsentPreference`** + **`RuleBasedPatientConsentGate`** (summary vs appointment-request flags); bootstrap on **`RegisterPatient`**
- [x] **I***: **Partial** — **`His:PatientAccess:RequireExplicitConsentRowForPortal`** for explicit consent rows; patient-scoped portal auth when JWT Authority is set (`PatientPortalPatientScopeFilter`)

### Quality

- [x] **Tests**: **xUnit** (`Dialysis.HIS.Tests`) — medication discontinue, book-appointment validator, formulary safety policy, portal consent gate; **`WebApplicationFactory`** integration tests for HATEOAS envelope + RA capabilities + health default-version links
- [x] **Architecture tests** (optional): **`BoundedContextReferenceTests`** — key assemblies must not reference foreign bounded-context **domain** packages (scheduling/medication vs patient flow; data services vs patient flow; integration vs RA capabilities)

---

## Projects (bounded contexts)

| Project | Role |
|---------|------|
| `Dialysis.HIS.Contracts` | Integration events, `HisTransponderOutboxEnvelope`, `HisPermissions`, `IPermissionedCommand` |
| `Dialysis.HIS.Security` | Users, roles, audit port, authorization pipeline behavior |
| `Dialysis.HIS.PatientFlow` | Patient, referral, ADT commands |
| `Dialysis.HIS.Scheduling` | Appointments |
| `Dialysis.HIS.Medication` | Orders, administration |
| `Dialysis.HIS.Operations` | Staff, inventory, billing export stub |
| `Dialysis.HIS.DataServices` | Import, search, dashboard queries |
| `Dialysis.HIS.PatientAccess` | Portal reads + patient-initiated stub |
| `Dialysis.HIS.Integration` | External gateway stubs, device ingest, rate limiting |
| `Dialysis.HIS.RaCapabilities` | RA-aligned extended reads (Tummers et al. 2021); queries + ports; persisted in schema **`his_ra`** |
| `Dialysis.HIS.Persistence` | `HisDbContext` (extends Transponder persistence base), repositories, read models, audit implementation |
| `Dialysis.HIS.Composition` | `AddHospitalInformationSystem` |
| `Dialysis.HIS.Api` | ASP.NET minimal API host (dev entry point) |
| `Dialysis.HIS.Tests` | Unit tests (xUnit) for domain / Verifier slices |

---

## Related code (outside HIS)

- [EHR](../EHR), [PDMS](../PDMS) — integrate via **integration events** and ACLs, not direct domain references to those assemblies from HIS domain types.
