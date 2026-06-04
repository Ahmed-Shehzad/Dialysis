# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, run, test

- **SDK**: .NET 10.0.100 (`global.json`, `rollForward: latestFeature`). Central package management is on (`Directory.Build.props` + `Directory.Packages.props`); add new dependencies as `<PackageVersion>` in `Directory.Packages.props`, not per-project versions.
- **Solution file** is `Dialysis.slnx` (XML-format solution, not `.sln`). Tooling takes it directly: `dotnet build Dialysis.slnx`, `dotnet test Dialysis.slnx`.
- **Local dev = the Aspire AppHost. This is the single dev entrypoint:**
  - `dotnet run --project src/aspire/Dialysis.AppHost`
  - One process brings up per-module Postgres (HIS/EHR/PDMS/SmartConnect/HIE) + RabbitMQ + Valkey + Keycloak **and** runs all five module APIs + the Identity BFF + the edge Gateway + the Vite SPA. The Aspire dashboard (logs/metrics/traces) opens automatically; the OTLP endpoint is injected into every project via `OTEL_EXPORTER_OTLP_ENDPOINT` (`ModuleTelemetryExtensions` falls back to it when `<Module>:Telemetry:OtlpEndpoint` is unset).
  - **Browser entry point is the Gateway: `http://localhost:9090`** (proxies `/identity/*`→BFF, `/api/*` `/fhir/*` `/hubs/*`→module APIs, catch-all→SPA). The BFF is pinned to `:5275` and the Gateway to `:9090` because the Keycloak `dialysis-bff` client only accepts those `redirect_uri`s — do not change these ports without updating `src/backend/Identity/keycloak/dialysis-realm.json`.
  - Keycloak is deliberately **not** a persistent container: `--import-realm` only imports when the realm is absent, so a long-lived container would make `dialysis-realm.json` edits invisible. Re-running the AppHost re-imports.
  - There is intentionally **no** base infra-only `docker-compose.yml` and no per-module `dotnet run` workflow — Aspire owns the dev inner loop. `src/aspire/Dialysis.ServiceDefaults` (`AddServiceDefaults()` / `MapDefaultEndpoints()`) supplies service discovery, HTTP resilience, and the `/alive` probe to every host.
- **Identity-only smoke flow**: `src/backend/Identity/docker-compose.yml` (Keycloak + Postgres for the identity realm, port 5444; realm `dialysis` auto-imported from `src/backend/Identity/keycloak/`). See `src/backend/Identity/RUNBOOK.md` for the BFF + HIS-behind-JWT smoke test.
- **Tests**: `dotnet test Dialysis.slnx` runs everything and needs **no infra** (in-memory EF + in-memory Transponder). Single-project examples:
  - `dotnet test src/backend/HIS/Dialysis.HIS.Tests/Dialysis.HIS.Tests.csproj`
  - `dotnet test tests/Dialysis.ArchitectureTests/Dialysis.ArchitectureTests.csproj`
  - Single test filter: `dotnet test ... --filter "FullyQualifiedName~SomeTestName"`
- **HIS outbox golden-path test** is gated by `HIS_CI_OUTBOX_E2E=1` and needs real SQL Server + RabbitMQ (see `.github/workflows/his-ci.yml`). Default `dotnet test` of the HIS test project runs in-memory only — use `--filter "FullyQualifiedName!~HisOutboxRelayGoldenPathTests"` if you have those services but don't want the golden path.
- **EF migrations**: each module's `<Module>.Persistence` project owns its own `DbContext`, design-time factory, and migrations history table (e.g. PDMS uses `pdms.__ef_migrations`), with schema-per-slice naming (`his_security`, `hie_outbound`, …). The Transponder outbox/inbox/saga tables live on that same `DbContext` under the `transponder` schema — never share a `DbContext` across modules and never add a second outbox.
- **Frontend** (`src/frontend/dialysis-web`, React 18 + Vite + TypeScript + TanStack Query, npm): `npm run dev` (Vite `:5173`; `predev` auto-runs `npm install`), `npm run build` (`tsc -b && vite build`), `npm run lint` (`eslint . --max-warnings=0`), `npm run typecheck`, `npm run test:e2e` (Playwright). A Husky `pre-commit` hook runs `lint-staged` (eslint + prettier on staged files). The SPA proxies `/api`, `/fhir`, `/hubs`, `/identity`, `/auth` to the Gateway. Under Aspire the SPA is reached via the Gateway origin, not its own port.
- **SmartConnect PDF SOT**: Python tools under `tools/smartconnect/` (`pip install -r tools/smartconnect/requirements.txt`) regenerate/verify the Mirth user-guide table-of-contents and traceability artifacts. CI (`.github/workflows/smartconnect-pdf-sot.yml`) requires `docs/book/mirth-connect-user-guide.pdf` materialized via Git LFS — without it the workflow fails by design.

## Deployment (containerized stack)

The deployment topology is **generated from the Aspire AppHost** — the AppHost is the single source of truth. There is **no** hand-curated overlay: every concern the Aspire publishers don't model out-of-the-box (build stanzas, host ports, ASP.NET production hardening, the OTLP collector, replica counts, healthcheck cadence) lives in `src/aspire/Dialysis.AppHost/ComposePublishExtensions.cs` and is shaped per environment by `DIALYSIS_DEPLOY_ENV`.

**Two publishers, three environments.**

```bash
# Docker Compose — one folder per environment
./build.sh PublishCompose      --environment prod   # → deploy/compose/prod/
./build.sh PublishAllCompose                        # fan out across dev / staging / prod

# Kubernetes / Helm — one chart per environment
./build.sh PublishKubernetes   --environment prod   # → deploy/charts/dialysis-prod/
./build.sh PublishAllKubernetes                     # fan out across dev / staging / prod
```

Each NUKE target wraps `dotnet run --project src/aspire/Dialysis.AppHost --publisher {compose|k8s} --output-path … --deploy false`. Always pass `--deploy false` — without it Aspire tries to talk to a Docker daemon during publish. After **any** AppHost change, re-run the appropriate `PublishAll*` and commit the regenerated artifacts alongside the AppHost edit so the three env shapes stay in lockstep.

The per-env shapes share ~90% of the topology; the diff is a small set of strings (`ASPNETCORE_ENVIRONMENT`, HSTS / `ForwardedHeaders`, `Authentication__RequireAuthorityWhenNotDevelopment`, OTLP wiring, replica counts, healthcheck cadence, host port mappings). See `deploy/compose/README.md` for the per-env matrix.

Run a compose env locally:

```bash
cd deploy/compose/prod
docker compose up -d --build
```

This builds every host image from the repo using `Dockerfile.module` (parameterised by `MODULE_PROJECT`/`MODULE_DLL`), `Dockerfile.gateway`, and `src/frontend/dialysis-web/Dockerfile`. Hosts: HIS `:5288`, EHR `:5289`, PDMS `:5290`, SmartConnect `:5291`, HIE `:5292`, BFF `:5275`, gateway `:9090`, web `:8080`. Module hosts are stateless and scale horizontally (`docker compose up -d --scale his-api=3`); Valkey backs the distributed cache **and** the ASP.NET Data Protection key ring, so multi-replica is safe. CI (`.github/workflows/*`) uses plain `dotnet`; it does **not** build these images.

For Kubernetes, the Helm chart at `deploy/charts/dialysis-<env>/` is renderable on any cluster — `helm install dialysis ./deploy/charts/dialysis-prod`. The gateway endpoint is marked external so the chart emits an `Ingress` for it.

## Architecture

A **modular monolith**: each bounded context under `src/backend/<Module>/` (HIS, EHR, PDMS, SmartConnect, HIE, Identity) has its own ASP.NET host and its own database. Modules communicate **only** via integration events over Transponder (RabbitMQ in the deployment stack, in-memory in dev/tests) — never via direct domain references.

### Event storming, not event sourcing

We model with **event storming** (Brandolini): commands, aggregates, policies, domain events, integration events, read models. We **do not** implement event sourcing — aggregates persist current state via EF, never as a replayable log. The split in code:

- **Domain events** (`Dialysis.DomainDrivenDesign.DomainEvents.IDomainEventHandler<T>`) — in-process, raised by aggregates, dispatched within the same `SaveChanges` transaction. Use for *within-context* coordination.
- **Integration events** (`Dialysis.DomainDrivenDesign.IntegrationEvents.IIntegrationEvent`) — persisted to the Transponder outbox in the same transaction as the state change, then relayed asynchronously over RabbitMQ. Use for *cross-context* signals. Schema-versioned (`int SchemaVersion`); a bump goes through `IntegrationEventVersioningTests`.
- **Read models** — denormalized tables or in-memory projections built from current aggregate state (e.g. `EfManagerDashboardReadModel`, `EfPatientSearchReadModel`). Never rebuilt from an event log.

Do not introduce `IEventStore`, aggregate-from-event-log, or replay loops. If a feature feels like it wants event sourcing, model it as an aggregate + projection instead.

### Module project layout (HIS is the reference shape)

For module `X`:
- `Dialysis.X.Contracts` — integration events, permission catalog (`XPermissions`), `IPermissionedCommand`, public DTOs. **The only assembly other modules may reference.**
- `Dialysis.X.<Slice>` — vertical slices (e.g. `PatientFlow`, `Scheduling`, `Medication`, `Registration`, `PatientChart`, `TreatmentSessions`) holding commands/queries/handlers/domain types for one bounded slice. Slices also carry a `Fhir/` folder with their FHIR resource mappers.
- `Dialysis.X.Persistence` — single `XDbContext`, schema-per-slice naming, repositories, read models, audit. Extends Transponder's EF persistence base so outbox/inbox/saga live on the same `DbContext`.
- `Dialysis.X.Composition` — single `AddX(...)` registration extension consumed by the host.
- `Dialysis.X.Api` — ASP.NET host. Versioned MVC under `api/v1.0/...` via `Asp.Versioning.Mvc` (+ ApiExplorer, URL-segment reader). OpenAPI per group (`GET /openapi/v1.json`). Successful JSON bodies use the HATEOAS envelope `{"data":...,"links":[...]}` (`ResourceEnvelope<T>`). Health: `/health/live`, `/health/ready`.
- `Dialysis.X.Tests` — xUnit + `WebApplicationFactory` integration tests.

**Known, intentional divergences from the reference shape:**
- **Per-module persistence-provider split**: EHR, PDMS, and SmartConnect have a `Persistence/` sibling folder with `Core.Persistence.{Abstractions,InMemory,Postgresql}` projects (provider chosen at composition time). HIS keeps a single `Persistence` project.
- **PDMS is single-slice** (`Dialysis.PDMS.TreatmentSessions`, `DialysisSession`/`TreatmentAlarm` aggregates) — it uses Evans' *System Metaphor* ("a treatment-machine cycle observed through telemetry") rather than HIS's many Responsibility-Layer slices. This is deliberate; do not "normalize" PDMS to the HIS shape.

### HIE module (FHIR R4 / IHE health-information exchange)

`src/backend/HIE/` is the cross-organization gateway. It still obeys the module pattern (owns `HieDbContext`, schemas `hie_outbound`/`hie_inbound`/`hie_consent`/`hie_xds_registry`/`hie_documents`/`hie_tefca`/`transponder`; references only other modules' `*.Contracts`). Slices:
- **Outbound** — consumes upstream integration events, maps them to FHIR resources (Patient, Encounter, LabOrder/Result, AdverseEvent, DialysisSession, ClinicalNote), validates against US Core, dispatches to partner endpoints with Polly retry, TEFCA IAS JWT auth, FHIR `AuditEvent` trail, and Pending→Dispatching→Delivered/DeadLettered state.
- **Inbound** — partners `POST /fhir/{Type}`; TEFCA middleware validates the IAS JWT/trust anchor, the ingestion service validates profile + consent, then enqueues `Hl7FhirResourceReceivedIntegrationEvent` for the owning module to consume.
- **Consent** — `ConsentPolicy` aggregates + `IsResourceAccessPermittedQuery`; EHR/HIS call this as a cross-module query to gate resource visibility.
- **Documents** — `DocumentReference` aggregate + `IDocumentBlobStore`-backed binary content; PAdES signing, PDF viewer, audit trail. Owns the GDPR Art. 5(1)(e) retention pipeline: `DocumentRetentionPolicy` per `DocumentReference.Kind`, `RetentionPurgerHostedService` (24-hour tick, opt-in via `Documents:Retention:AutoPurge`), and the Art. 17 `HieDocumentsPatientEraser` that walks every `Current` document for the patient.
- **Tefca** (US TEFCA QHIN onboarding — distinct from the German gematik TI building block at `BuildingBlocks/Tefca/Dialysis.BuildingBlocks.Tefca.Ti`, naming overlap is acronym-only) — `QhinPartner` aggregate (`Onboarding` → `Active` → `Suspended`, activation requires ≥1 trust anchor **and** mTLS material), `TrustAnchorParser` (PEM via `X509Certificate2.CreateFromPem`), `HmacIasJwtIssuer` for IAS JWT minting, admin surface at `/hie/admin/tefca/partners`.
- **Xds** (IHE XDS Registry/Repository, ITI-18/41/42/43) and **OpenEhr** (openEHR Composition ↔ FHIR — projections are now **declarative**: JSON archetype definitions in `Dialysis.HIE.OpenEhr/Archetypes/Definitions/` are evaluated by a `ResourcePath` walker; add a new archetype by dropping a JSON file, no code change required).

### Cross-cutting building blocks (`src/backend/BuildingBlocks/`)

- **Intercessor** — in-process mediator for command/query dispatch.
- **Verifier** — request/command validation pipeline.
- **Transponder** — distributed messaging (`ITransponderBus`); transports (RabbitMQ, NATS, Azure Service Bus, SQS, gRPC, SignalR, SSE); EF outbox/inbox/saga persistence (`Postgresql`, `Shared`); schedulers (Hangfire / Quartz / TickerQ — exactly one per host). `enableOutboxRelay` (or `<Module>:Transponder:EnableOutboxRelay`) opts a host into background publishing. See `src/backend/BuildingBlocks/Transponder/README.md`.
- **Fhir** — ~18 projects implementing the FHIR stack: `Core` (`IFhirResourceMapper<TEvent,TResource>`), `Validation` (US Core), `Smart` (SMART-on-FHIR), `Subscriptions`/`BulkData`/`Audit` (each with an `*.EntityFrameworkCore` persistence project), `Tefca` (IAS JWT + trust anchors), `Terminology`, `DeIdentification`, `OpenEhr`, `CdaBridge`, `AspNetCore` (middleware), `Testing`. Per-module mappers live in each slice's `Fhir/` folder; SmartConnect's `Hl7V2ToFhirPipeline` routes HL7v2 by MSH-9 trigger to per-trigger mappers.
- **DataProtection** — GDPR/BDSG compliance surface. Mounts `/api/v1.0/data-subject-rights/...` via `MapEuDataProtectionRoutes()`. Two per-module participation hooks: `IModuleDataExtractor` (Art. 15 export) and `IPatientEraser` (Art. 17 erasure). The `DefaultDataSubjectRightsService` orchestrates request → approval → execution and writes the audit row through `IErasureRequestStore` (HIE registers a Scoped EF impl; non-HIE hosts fall back to the `InMemoryErasureRequestStore` registered via `TryAddSingleton`). Sibling concerns: consent, lawful bases, RoPA, retention, breach notification, encryption, audit.
- **Direct** (Direct secure messaging), **PlatformGateway**, **DistributedCache.Valkey**.

`DomainDrivenDesign/` holds DDD primitives + persistence base classes; `CQRS/` holds CQRS contracts and pipeline behaviors. `Shared/Dialysis.Module.{Contracts,Hosting,Hosting.Testing}` provide host scaffolding consumed by every `Program.cs` via `builder.AddModuleHost<XPermissionCatalog>(new ModuleHostingOptions { ModuleSlug = "x", HandlerAssemblies = [...] })`; `Shared/Dialysis.Module.Gateway` is the YARP edge gateway.

### Compliance surfaces (GDPR / BDSG)

Two distinct mechanisms; do not conflate them.

- **Storage limitation (Art. 5(1)(e)) — scheduled purge.** The HIE Documents slice owns the only retention pipeline today: `DocumentRetentionPolicy` per `DocumentReference.Kind`, set via the admin UI at `/hie/admin/documents/retention`. The `RetentionPurgerHostedService` ticks every 24 h and is opt-in via `Documents:Retention:AutoPurge` (default `false`); no windows are seeded, so the purger is a no-op until the DPO adopts policies. Purged documents transition to `EnteredInError` with `StorageRef = purged://…` (tombstone, so audit replay sees deliberate purge, not data loss) and the blob is deleted via `IDocumentBlobStore.DeleteAsync`.
- **Right to erasure (Art. 17) — approve-and-execute pipeline.** Patient files a request → DPO reviews → DPO approves via `/admin/data-protection/data-subject-rights` → `DefaultDataSubjectRightsService.ApproveErasureAsync` walks every registered `IPatientEraser` and persists the per-module breakdown to `IErasureRequestStore`. To participate, a module implements `IPatientEraser` (the contract mirrors `IModuleDataExtractor` for Art. 15 export) and registers it in its composition extension. Today four modules are wired: `HieDocumentsPatientEraser` (HIE Documents — tombstone + blob purge), `HisPatientEraser` (HIS — soft-delete the three Audit-tracked aggregates, hard-delete RA-capability + device-reading rows), `EhrPatientEraser` (EHR — soft-delete the Patient root + 16 chart/scheduling/portal/notes/billing aggregates), and `PdmsPatientEraser` (PDMS — soft-delete sessions and their per-session children by `SessionId`, plus direct patient-linked rows). SmartConnect deliberately doesn't ship one: it routes messages mentioning patients but doesn't own patient PII. Erasers use EF Core 7+ `ExecuteUpdateAsync`/`ExecuteDeleteAsync` so a long-tenured patient with thousands of telemetry rows clears in one round-trip per type.

### Durable command bus (opt-in write durability)

`Dialysis.BuildingBlocks.DurableCommandBus` (+ `.AspNetCore`) moves the durability boundary from "the row is in Postgres" to "the command is in a durable RMQ queue with publisher confirms acknowledged." An opted-in endpoint calls `IDurableCommandBus.EnqueueAsync` instead of `ICqrsGateway.SendCommandAsync`, returns `202 Accepted` with a status-endpoint URL, and `DurableCommandConsumer<TDbContext>` (an `IConsumer<DurableCommandEnvelope>`) dequeues, opens an explicit EF transaction, claims via the `command_ledger` row (idempotent on `CommandId`), dispatches into the existing `ICommandHandler` via the registered closure on `IDurableCommandCatalog`, marks the ledger row Applied, and commits. Handler change + ledger row commit in one transaction; if anything throws, the tx rolls back and the broker redelivers. CLAUDE.md's "no event sourcing" rule holds — the queue is an in-flight buffer, never a permanent log; aggregates still persist current state via EF.

Today one slice opts in: **PDMS `RecordReading`** (flag `Pdms:DurableCommands:RecordReading:Enabled`, default off). The reference deterministically derives the reading's id from the `CommandId` so a redelivery yields the same row. Status surface: `GET /api/v1.0/command-status/{correlationId}` returns the ledger row; authorized against the row's stored `requestedBySubject` so a leaked correlation id can't probe across permission boundaries. Full design + runbook in `docs/architecture/durable-writes.md`.

**Both tiers of the durability story go HA via Kubernetes operators** (`deploy/k8s/operators/`). CloudNativePG `Cluster` CRs render per module Postgres with sync replication on the clinical tier (HIS/EHR/PDMS) and async on the integration tier (SmartConnect/HIE), plus WAL archiving to S3/MinIO for PITR. A RabbitMQ Cluster Operator `RabbitmqCluster` runs the broker as a 3-replica quorum-queue cluster; `TransponderRabbitMqOptions.QueueType = Quorum` declares the durable-command queue Raft-replicated. PgBouncer sidecars front each module's Postgres in transaction-pooling mode. Per-env knobs live in `deploy/k8s/operators/values/{dev,staging,prod}.env`; apply with `./deploy/k8s/operators/render.sh <env> | kubectl apply -n dialysis-<env> -f -`. Aspire's k8s publisher emits the application-layer chart; the operator CRs are hand-maintained because Aspire 13.4's `KubernetesEnvironment` doesn't model arbitrary CRDs.

### Module boundary invariant (enforced)

`tests/Dialysis.ArchitectureTests` codifies, via assembly-reference inspection + `NetArchTest`:

> A project under module `X` may only reference its own siblings, the shared layers (`Dialysis.DomainDrivenDesign`, `Dialysis.BuildingBlocks`, `Dialysis.CQRS`, `Dialysis.Module.Contracts`, `Dialysis.Module.Hosting`), and the **`Dialysis.Y.Contracts`** assembly of any other module. Anything else is a build-test failure.

It also enforces aggregate-root encapsulation and integration-event versioning. Cross-module functionality goes through an integration event in `<Module>.Contracts` + an `IConsumer<>` on the receiving side (or, for synchronous reads, a cross-module query like HIE's consent check) — never a direct project reference.

### Reference-architecture alignment (HIS-specific)

HIS is mapped to Tummers et al. (2021) — see `src/backend/HIS/README.md` and `his_ddd_modular_plan.md`. Discovery: `GET /api/v1.0/reference-architecture/catalog`, `.../capabilities` (CQRS in `Dialysis.HIS.RaCapabilities`, schema `his_ra`), `GET /api/v1.0/help`.

### Identity / auth

JWT Bearer is registered only when `<Module>:Authentication:Authority` is set; in Development with no Authority, `ICurrentUser` exposes all permissions for local work. IdP role/group names map to module permission strings via `<Module>:Authentication:RolePermissionMap`. HIS portal endpoints additionally filter by patient claim (`his_patient_id` or `sub` matching route `patientId`). The Identity BFF + Keycloak realm `dialysis` are the canonical IdP — see `src/backend/Identity/RUNBOOK.md` and `ARCHITECTURE.md`.

**Multi-IdP federation (Okta / Auth0 / Entra) goes through Keycloak brokering, not BFF-side IdP swapping.** Keycloak stays the only direct OIDC client; upstream IdPs are added as realm `identityProviders[]` entries (placeholders for `okta`/`auth0`/`entra` ship disabled in `dialysis-realm.json`) and surfaced to the SPA through `IIdentityProviderCatalog` + `GET /identity/providers`. The BFF forwards `kc_idp_hint=<alias>` on the OIDC auth request when the caller hits `/identity/login?provider=<alias>`, gated by an allowlist so unknown aliases can't probe Keycloak. See RUNBOOK §8.

**Session continuity is handled BFF-side, not by re-login.** `ITokenRefreshService` is wired to the cookie handler's `OnValidatePrincipal` event: when the saved `expires_at` token falls within 60 s of now, the BFF calls Keycloak's `/token` endpoint with the stashed `refresh_token`, rewrites `access_token`/`refresh_token`/`id_token`/`expires_at` on the auth ticket, and sets `ShouldRenew = true`. Failures (no refresh token, Keycloak rejects the grant) call `RejectPrincipal()` so the SPA bounces back through login instead of sitting on a stale session. The OIDC handler requests `offline_access` scope on auth to make sure Keycloak issues a refresh token in the first place.

**Permission claims reach the SPA via `dialysis_permission`.** The Keycloak realm mapper emits a JSON-typed `dialysis_permission` claim through the userinfo endpoint; the BFF's `/identity/user` response parses it into a top-level `permissions: string[]` array. The SPA's `PermissionGate` does a simple `permissions.includes(required)` check — call sites pass typed permission strings from the module's `<Module>Permissions` catalog, no claim plumbing required.

### Frontend module-shell (`src/frontend/dialysis-web`)

The SPA mirrors the backend boundaries: one folder per module under `src/modules/<slug>/`, all composed into a shared chrome.

- **`src/shell/`** is the kernel:
  - `registry.ts` exports `MODULE_MANIFESTS` and `enabledModules()`. The router and the top-nav module switcher both read from this list — adding a module to the UI is a one-line change here.
  - `types.ts` defines `ModuleManifest` (slug, displayName, tagline, optional `requires` permission, `enabled` flag, optional `home` route, `renderRoutes()`).
  - `PatientContextProvider` + `usePatientContext` carry the selected patient (id, displayName, mrn) across modules — HIS check-in / EHR chart load / PDMS chairside all read and write the same context so the patient follows the user.
  - `PatientContextBar` surfaces the selected patient under the app header.
  - `PermissionGate` wraps content that requires authentication / a permission (today gates on auth only; permission claims wire through when the BFF surfaces them).
- **`src/modules/<slug>/manifest.tsx`** per module (`his`, `ehr`, `pdms`, `smartconnect`, `hie`, `identity`). Pages are loaded through **`src/shared/lazyPage.ts`** which wraps `React.lazy` with a typed adapter for named exports; the router wraps the outlet in `<Suspense>`. Result: each module ships in its own chunk and only downloads on first visit. Initial bundle is ~89 kB gz; echarts (`BokehChart`) is its own ~380 kB gz chunk loaded only when a chart mounts.
- **`humanizeError`** (`src/lib/api/humanizeError.ts`) maps ProblemDetails / network errors to user-readable sentences. Never expose raw status codes or stack traces to clinical users.
- **TanStack Query mutation pattern** — every queue/chart/note mutation uses optimistic-update + rollback on error + invalidate-on-settle. `useQueueMutation` in `modules/his/today/queueApi.ts` is the canonical helper; copy its shape for new mutations.

## Conventions & tooling

- **Warnings are errors.** `Directory.Build.props` sets `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` (XML-doc warnings CS1591/1573/1574/1734/1735 are suppressed; missing docs on genuinely public API still surface as other diagnostics). Microsoft VS Threading analyzers are referenced solution-wide; **`VSTHRD200` (async methods must end with `Async`) is pinned to error** in `.editorconfig` — keep new async methods suffixed.
- **Style** (`.editorconfig`, mostly warning-severity so they fail the build): file-scoped namespaces; no `this.`/`Me.` qualification; predefined type keywords; accessibility modifiers required; `readonly` where possible. SonarAnalyzer.CSharp runs in-build. Noisy rules are tuned down (`CA2007`, `CA1303` off; `CA1062`, `CA1848` suggestion).
- **SonarQube MCP** (`.mcp.json` + `.github/instructions/sonarqube_mcp.instructions.md`): when modifying code, disable automatic analysis at task start and re-enable it (and run `analyze_file_list` on changed files) at the very end if those tools exist. Look up project keys via `search_my_sonarqube_projects` (don't guess); don't re-query issues immediately after a fix (the server lags).
- **CI**: one workflow per module (`his/ehr/pdms/hie/smartconnect/identity-ci.yml`), `buildingblocks-transponder-ci.yml`, `buildingblocks-fhir-ci.yml`, `frontend-ci.yml` (eslint/prettier/tsc/Playwright), `solution-ci.yml` (full build + tests + architecture tests), `smartconnect-pdf-sot.yml`. Path-filtered to the touched area. Static analysis is owned by the Aspire-hosted SonarQube server (`tools/sonarqube/`) — CodeQL was removed once SonarQube took over the security + quality role.

### Port reference (Aspire dev / deployment-compose)

| Component | Aspire dev | compose | DB port |
|---|---|---|---|
| Gateway (browser entry) | 9090 | 5000 | — |
| Identity BFF | 5275 | — | — |
| HIS / EHR / PDMS / SmartConnect / HIE | dynamic (via Gateway) | 5288 / 5289 / 5290 / 5291 / 5292 | 5440 / 5442 / 5443 / 5441 / 5445 |
| Web SPA | behind Gateway | 8080 | — |
| Keycloak | 8081 | 8081 | — |
| RabbitMQ | 5672 / mgmt 15672 | same | — |
| Valkey | 6379 | same | — |
| OTLP collector | injected | 4317 (gRPC) / 4318 (HTTP) | — |
| SonarQube (auto-start, dev only) | 9000 | — | — |
