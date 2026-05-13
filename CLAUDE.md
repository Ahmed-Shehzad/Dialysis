# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, run, test

- **SDK**: .NET 10.0.100 (`global.json`, `rollForward: latestFeature`). Central package management is on (`Directory.Build.props` + `Directory.Packages.props`); add new dependencies as `<PackageVersion>` in `Directory.Packages.props`, not per-project versions.
- **Solution file** is `Dialysis.slnx` (XML-format solution, not `.sln`). Most tooling commands take it directly: `dotnet build Dialysis.slnx`, `dotnet test Dialysis.slnx`.
- **Run a module host** (each module is a separate ASP.NET host):
  - HIS: `dotnet run --project src/backend/HIS/Dialysis.HIS.Api/Dialysis.HIS.Api.csproj` (dev URL `http://localhost:5288`)
  - EHR: `dotnet run --project src/backend/EHR/Dialysis.EHR.Api/Dialysis.EHR.Api.csproj`
  - PDMS: `dotnet run --project src/backend/PDMS/Dialysis.PDMS.Api/Dialysis.PDMS.Api.csproj`
  - SmartConnect: `dotnet run --project src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/Dialysis.SmartConnect.Api.csproj`
  - Identity BFF: `dotnet run --project src/backend/Identity/Dialysis.Identity.Bff/Dialysis.Identity.Bff.csproj` (dev URL `http://localhost:5275`)
- **Infra for local dev** is in the root `docker-compose.yml`: per-module Postgres containers (`postgres-his` 5440, `postgres-smartconnect` 5441, `postgres-ehr` 5442, `postgres-pdms` 5443, `postgres-identity` 5444), `rabbitmq` (5672 / mgmt 15672), and `keycloak` (8081). Module hosts are *not* in compose — start infra with `docker compose up -d`, then run module APIs with `dotnet run`.
- **Identity dev infra** has its own compose at `src/backend/Identity/docker-compose.yml` (Keycloak + Postgres for the identity realm, port 8080); the realm `dialysis` is auto-imported from `keycloak/dialysis-realm.json`. See `src/backend/Identity/RUNBOOK.md` for the BFF + HIS-behind-JWT smoke flow.
- **Tests**: `dotnet test Dialysis.slnx` runs everything. Single-project examples:
  - `dotnet test src/backend/HIS/Dialysis.HIS.Tests/Dialysis.HIS.Tests.csproj`
  - `dotnet test tests/Dialysis.ArchitectureTests/Dialysis.ArchitectureTests.csproj`
  - Single test filter: `dotnet test ... --filter "FullyQualifiedName~SomeTestName"`
- **HIS outbox golden-path test** is gated by `HIS_CI_OUTBOX_E2E=1` and needs real SQL Server + RabbitMQ (see `.github/workflows/his-ci.yml`). Default `dotnet test` of the HIS test project runs in-memory only — use `--filter "FullyQualifiedName!~HisOutboxRelayGoldenPathTests"` if you have those services running but don't want the golden path.
- **EF migrations** (HIS): `dotnet ef ...` against `HisDbContextDesignTimeFactory`. Connection comes from `--connection`, `HIS_SQL_CONNECTION` env, or `ConnectionStrings:His` / `ConnectionStrings__His`. Each module's `<Module>.Persistence` project owns its own `DbContext` and migrations history table (e.g. PDMS uses `pdms.__ef_migrations`); never share a `DbContext` across modules.
- **SmartConnect PDF SOT**: Python tools under `tools/smartconnect/` (`pip install -r tools/smartconnect/requirements.txt`) regenerate/verify PDF table-of-contents and traceability artifacts. CI (`.github/workflows/smartconnect-pdf-sot.yml`) requires `docs/book/mirth-connect-user-guide.pdf` to be materialized via Git LFS — without it, the workflow fails by design until the file is committed.

## Architecture

This is a **modular monolith**: each bounded context is a folder under `src/backend/<Module>/` (HIS, EHR, PDMS, SmartConnect, Identity), each with its own ASP.NET host and database. Modules communicate via **integration events** over Transponder (RabbitMQ in prod, in-memory in dev), never via direct domain references.

### Module project layout (HIS is the reference; EHR/PDMS follow the same shape)

For module `X`:
- `Dialysis.X.Contracts` — integration events, permission catalog (`XPermissions`), `IPermissionedCommand`, public DTOs. **The only assembly other modules may reference.**
- `Dialysis.X.<Slice>` — vertical slices (e.g. `PatientFlow`, `Scheduling`, `Medication`, `Registration`, `PatientChart`) holding commands/queries/handlers/domain types for one bounded slice.
- `Dialysis.X.Persistence` — single `XDbContext`, schema-per-slice table naming (`his_security`, `his_patientflow`, …), repositories, read models, audit implementation. Extends Transponder's persistence base so the outbox/inbox live on the same `DbContext` (no duplicate outbox tables).
- `Dialysis.X.Composition` — single `AddX(...)` registration extension consumed by the API host.
- `Dialysis.X.Api` — ASP.NET host. Versioned MVC under `api/v1.0/...` via `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer` (URL segment reader). OpenAPI is per ApiExplorer group, e.g. `GET /openapi/v1.json`. Successful JSON bodies use the HATEOAS envelope `{"data":...,"links":[{"rel","href","method"}]}` (`ResourceEnvelope<T>`). Neutral endpoints like `/health` appear in every versioned document.
- `Dialysis.X.Tests` — xUnit + `WebApplicationFactory` integration tests.

### Cross-cutting building blocks (`src/backend/BuildingBlocks/`)

- **Intercessor** — in-process mediator used for command/query dispatch.
- **Verifier** — request/command validation pipeline.
- **Transponder** — distributed messaging (`ITransponderBus`), transports (RabbitMQ, NATS, Azure Service Bus, SQS, gRPC, SignalR, SSE), EF Core outbox/inbox/saga persistence (`SqlServer`, `Postgresql`, `Shared`), and schedulers (Hangfire / Quartz / TickerQ — pick exactly one per host). The transactional outbox lives on each module's own `DbContext` under the `transponder` schema; `enableOutboxRelay` (or `<Module>:Transponder:EnableOutboxRelay` config) opts the host into background publishing. See `src/backend/BuildingBlocks/Transponder/README.md` for the full transport/persistence/saga API.

`DomainDrivenDesign/` holds DDD primitives (entities, value objects, aggregate root markers, persistence base classes). `CQRS/` holds CQRS contracts (commands, queries, handlers, pipeline behaviors). `Shared/Dialysis.Module.Contracts` + `Shared/Dialysis.Module.Hosting` provide the common host scaffolding consumed by every module's `Program.cs` via `builder.AddModuleHost<XPermissionCatalog>(new ModuleHostingOptions { ModuleSlug = "x", HandlerAssemblies = [...] })`.

### Module boundary invariant (enforced)

`tests/Dialysis.ArchitectureTests/ModuleBoundaryTests.cs` codifies the long-term rule via assembly-reference inspection and `NetArchTest`:

> A project under module `X` (e.g. `Dialysis.EHR.*`) may only reference its own siblings, the shared layers (`Dialysis.DomainDrivenDesign`, `Dialysis.BuildingBlocks`, `Dialysis.CQRS`, `Dialysis.Module.Contracts`, `Dialysis.Module.Hosting`), and the **`Dialysis.Y.Contracts`** assembly of any other module. Referencing any other module's internals is a build-test failure.

When adding cross-module functionality, route it through an integration event in `<Module>.Contracts` and an `IConsumer<>` on the receiving side, not a direct project reference.

### Reference architecture alignment (HIS-specific)

HIS is mapped to Tummers et al. (2021) — see `src/backend/HIS/README.md` and `src/backend/HIS/his_ddd_modular_plan.md`. Discovery endpoints:
- `GET /api/v1.0/reference-architecture/catalog` — module catalog
- `GET /api/v1.0/reference-architecture/capabilities` — capability entrypoints (CQRS in `Dialysis.HIS.RaCapabilities`, persisted in schema `his_ra`)
- `GET /api/v1.0/help` — RA Help slice with doc paths

### Identity / auth

JWT Bearer is registered only when `<Module>:Authentication:Authority` is set; in Development with no Authority, `ICurrentUser` exposes all permissions for local work. IdP role/group names map to module permission strings via `<Module>:Authentication:RolePermissionMap`. HIS portal endpoints additionally filter by patient claim (`his_patient_id` or `sub` matching route `patientId`). The Identity BFF + Keycloak realm are the canonical IdP — see `src/backend/Identity/RUNBOOK.md`.
