# Improvement areas — full-codebase survey

**Date:** 2026-06-10
**Scope:** three parallel surveys over the whole repo — backend (`src/backend/`, ~2,300 `.cs` files), frontend (the seven SPAs under `src/frontend/`), and CI / deployment / docs (`.github/workflows/`, `build/`, `deploy/`, `docs/`, `tools/`)
**Method:** evidence-backed findings only — every item below names the file(s) it was observed in. Contested findings (Art. 15 export, health checks) were re-verified by hand against the source before inclusion.

## Headline

The foundations are unusually healthy — build system, deploy drift gate, workflow hygiene, and
frontend dependency alignment all checked out clean (see "Explicitly healthy" at the end). The
real gaps cluster in four places: **production Kubernetes hardening**, **compliance surface
completeness** (Art. 15 export is wired but hollow), **dependency-aware health/resilience**, and
**frontend failure containment** (no error boundaries, no a11y linting). Nothing found contradicts
the architecture; everything below is finishable within the existing patterns.

## P1 — High impact

### 1. Helm charts ship no `resources`, no PodDisruptionBudgets, no NetworkPolicies

Every pod spec under `deploy/charts/dialysis-*/` (e.g. `ehr-bff/deployment.yaml`) has an empty
`resources:` block, and `grep -r PodDisruptionBudget\|NetworkPolicy deploy/` returns zero hits.
Consequences: OOMKill / CFS-throttle risk with no guaranteed CPU/memory for clinical services; a
node drain can evict every replica of a component at once; pod-to-pod traffic is unrestricted —
a HIPAA defense-in-depth gap. Because the charts are **generated from the AppHost** (and
drift-gated by `deploy-artifacts.yml`), the fix belongs in the AppHost publisher path
(`src/aspire/Dialysis.AppHost/ComposePublishExtensions.cs` and the k8s publisher), never in
hand-edits to `deploy/`. Related: the HPA/operator manifests in `deploy/k8s/operators/` are a
separate `render.sh | kubectl apply` step — a fresh `helm install` has no HPAs.

### 2. GDPR Art. 15/20 export is hollow; erasure coverage has holes

`IModuleDataExtractor` exists
(`src/backend/BuildingBlocks/DataProtection/.../DataSubjectRights/IDataSubjectRightsService.cs`)
and `DefaultDataSubjectRightsService` walks every registered extractor — but **zero modules
implement it**. Art. 17 erasure has four erasers (HIS, EHR, PDMS, HIE-Documents); Art. 15/20
export has none, so a data-subject access request returns an empty export today. Two erasure
holes alongside it: HIE's eraser covers only the Documents slice (consent / XDS registry / TEFCA
rows are untouched), and Lab — which owns patient-linked `LabOrder` aggregates — ships no eraser
at all.

### 3. `/health/ready` doesn't actually check any dependency

`Shared/Dialysis.Module.Hosting/Health/ModuleHealthExtensions.cs` maps `/health/live` and
`/health/ready` for every module, but no module registers a Postgres or RabbitMQ check —
`AspNetCore.HealthChecks.NpgSql` and `AspNetCore.HealthChecks.RabbitMQ` (9.0.0) are pinned in
`Directory.Packages.props` and referenced by **nothing**. The only concrete check in the backend
is `GlideValkeyHealthCheck`. Net effect: Kubernetes readiness probes pass while the module's
database is down. Wiring the pinned packages into `AddModuleHost` (connection-string-conditional,
like the Hangfire wiring) closes this centrally.

### 4. No React error boundary in any of the seven SPAs

Every app's `main.tsx` / `src/app/AppProviders.tsx` wraps only `StrictMode` → providers → router;
there is no `ErrorBoundary` anywhere in `src/frontend/`. One unhandled render error blanks the
entire screen for a clinical user mid-shift. A top-level boundary (friendly fallback +
`humanizeError`-style message + reload affordance), duplicated byte-for-byte per repo convention,
is a small, high-leverage fix.

### 5. The resilience helper exists but almost nothing uses it

`AddResilientModuleHttpClient` (`Shared/Dialysis.Module.Hosting/Resilience/`) has ~3 call sites in
the whole backend. Outbound HTTP that goes through bare `AddHttpClient` with no retry/timeout
policy includes the SmartConnect vendor adapters (`Adapters.Cerner`, `Adapters.Allscripts`) and
the Identity BFF's `"keycloak"` client — the single client every login in the system depends on.
HIE's partner clients already use Polly; the rest should converge on the existing helper.

## P2 — Medium

### 6. No coverage measurement, and test distribution is lopsided

`build-test.yml` uploads test results but enforces no coverage threshold. Backend test-file
counts: SmartConnect 146, EHR 55, HIE 43, PDMS 37, HIS 25 — versus **Lab 3, Identity 5,
PatientPortal 0**. Frontend: smartconnect-web and patient-portal-web have 2 unit-test files each
(the shared `PermissionGate` / `humanizeError` pair) while ehr-web has 6 + 2 e2e specs. Adding
coverage collection to the NUKE `Test` target and a modest ratchet would stop the gap widening.

### 7. The duplicated frontend files have already drifted

Checksumming the "duplicated byte-for-byte" set across the seven apps shows real divergence:
`patientLoader.ts` and `usePatientName.ts` differ in hie-web / patient-portal-web (different
import paths), and `AuthProvider.tsx` / `authApi.ts` / `useDurableCommand.ts` are unique per app
**only because of hardcoded `/{ctx}/` path strings** (the basename is already available via
`import.meta.env.BASE_URL`). Parameterizing the context collapses seven copies to one identical
copy; a CI checksum drift-gate over the duplicated set (same philosophy as the
`deploy-artifacts.yml` drift gate) prevents recurrence. Verified identical today: `lazyPage.ts`,
`toastBus.ts`, `ToastHost.tsx`, `ThemeProvider.tsx`, `eslint.config.js`, `tsconfig.json`.

### 8. No accessibility tooling anywhere in the SPA tier

No `eslint-plugin-jsx-a11y`, no axe/axe-core, in any of the seven apps — for patient- and
clinician-facing clinical software. Lint already runs `--max-warnings=0` in `frontend.yml`, so
adding the plugin to the (single, shared) `eslint.config.js` gives immediate CI enforcement.

### 9. No automated dependency updates; two prerelease packages in prod paths

There is no Dependabot or Renovate config; `security.yml` scans weekly but nothing proposes
updates. `Directory.Packages.props` carries two prereleases on production code paths:
`OpenTelemetry.Instrumentation.EntityFrameworkCore 1.14.0-beta.1` and `Aspire.Hosting.Kubernetes
13.4.0-preview.1` (the latter consciously accepted for the publisher; the former is in every
module's telemetry).

### 10. Observability dashboards cover one subsystem

`deploy/k8s/observability/` contains exactly one dashboard + one alert file, both for the
durable-command bus. There are no per-module request-rate/latency/error dashboards and no
Transponder outbox-lag dashboard, despite the outbox being the backbone of cross-module flow.

### 11. Dev-secret discipline in base config files

`*-bff-dev-secret-change-me` client secrets sit in **base** `appsettings.json` (HIS, HIE, and
Identity BFFs) rather than `appsettings.Development.json`, and
`src/backend/Identity/keycloak/dialysis-realm.json` carries ~30 placeholder secrets. All dev-only
by design, but base-file placement risks leaking into prod config-as-code, and nothing fails fast
if a `change-me` secret is observed outside Development (the gateway already has exactly this
fail-fast pattern for CORS origins — copy it).

## P3 — Lower

12. **God files**: `SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs` (755 lines),
    `SmartConnect/Management/.../ManagementEndpointExtensions.cs` (681),
    `PDMS/.../Controllers/V1/OnCallController.cs` (661 — also one of three PDMS controllers with
    swallowed exceptions), `BuildingBlocks/Verifier/Verifier.Core/RuleBuilder.cs` (634),
    `HIE/Dialysis.HIE.Tefca/Features/QhinPartnerCommands.cs` (558).
13. **Docs/governance**: no `SECURITY.md`, no `.github/CODEOWNERS`, no PR template; only
    SmartConnect has a module README; no ADRs. (CLAUDE.md itself spot-checked accurate.)
14. **Console diagnostics in prod bundles**: seven `console.info/warn` calls per app in
    `AuthProvider.tsx` / `apiClient.ts` — intentional, but should be wrapped in
    `if (import.meta.env.DEV)`.
15. **SmartConnect modality gap**: HF/HDF dialysate wire formats and ultrafiltration profiles are
    TODO-stubbed (`Prescription/PrescriptionDocument.cs`, `Prescription/Hl7V2RxResponseBuilder.cs`)
    — the only TODO cluster in ~2,300 backend files.

## Explicitly healthy (no action recommended)

- **Build system**: NUKE + GitVersion (pinned twice, in lockstep) + central package management.
- **Deploy drift gate**: compose/Helm regenerated from the AppHost and CI-enforced.
- **Workflows**: actions pinned, concurrency cancellation present, caching correct; security.yml
  layers dotnet/npm audit + Trivy + ZAP + GitGuardian.
- **Secrets in compose**: all via `${VAR}` substitution, none hardcoded.
- **Frontend dependency alignment**: all seven apps on identical versions of react/vite/ts/query.
- **Gateway CORS**: fails fast on missing allowed origins outside Development.

## Suggested first five fixes

1. Register NpgSql/RabbitMQ health checks in `AddModuleHost` (packages already pinned). *(small)*
2. Add a top-level `ErrorBoundary` to all seven SPAs. *(small)*
3. Route the Keycloak client and SmartConnect vendor adapters through
   `AddResilientModuleHttpClient`. *(small)*
4. Emit `resources`, PDBs, and default-deny NetworkPolicies from the AppHost k8s publisher and
   regenerate the charts. *(medium)*
5. Implement `IModuleDataExtractor` for HIS/EHR/PDMS/HIE (mirroring the existing erasers) and add
   the Lab eraser + HIE non-Documents erasure. *(medium)*
