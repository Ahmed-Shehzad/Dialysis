# Dialysis MVP — Client Demo Guide

A step-by-step runbook for demonstrating the Dialysis modular monolith to a client.
Walks through every page in the SPA, what the client should see, what code paths fire, and
what talking points to use at each step.

**Expected demo length:** 25–35 minutes including Q&A.
**Audience:** clinical product owner + IT integration lead + (optionally) compliance/security stakeholder.
**Story arc:** a clinic uses the platform to admit a patient, schedule and run a dialysis session
with real-time vitals, exchange data with partner organisations over FHIR, and oversee everything
through one operations console.

---

## 1. Architecture at a glance (for opening 60 seconds)

```
                                    ┌─────────────────────────────┐
   Browser SPA ◀──────── HTTPS ────▶│   YARP Gateway (port 5000)  │
   React + Tailwind                 │   JWT validation, LB,       │
   TanStack Query, D3               │   rate limit, CORS,         │
   SignalR client                   │   active health checks      │
                                    └──────────────┬──────────────┘
                                                   │
            ┌───────────┬───────────┬──────────────┼────────────┬──────────────┐
            │           │           │              │            │              │
            ▼           ▼           ▼              ▼            ▼              ▼
       Identity BFF   HIS API    EHR API       PDMS API     HIE API    SmartConnect API
       (Keycloak       (5288)    (5111)        (5112)      (5095)        (5113)
        OIDC + cookie)
            │
            └─▶ Keycloak realm (port 8081)

   Cross-module backbone:  RabbitMQ (Transponder)  ·  Valkey (distributed cache + DP keyring)
   Per-module storage:     Postgres-{his,ehr,pdms,hie,smartconnect}
   Compliance baseline:    BSI C5 control map at docs/compliance/C5.md
```

**Key talking points for this slide:**
- *Modular monolith*: each bounded context (HIS, EHR, PDMS, SmartConnect, HIE) is its own
  ASP.NET host with its own database. Modules talk **only** through integration events on
  RabbitMQ — never via direct project references. The architecture-tests enforce this in CI.
- *One identity*: Keycloak issues JWTs; the BFF brokers cookie-auth for the SPA so tokens never
  live in the browser.
- *Horizontal scaling*: every module host is stateless. Valkey holds the distributed cache and
  ASP.NET Data Protection key ring, so any replica can decrypt anti-forgery tokens issued by any
  other replica. Bump `replicas:` in compose to scale.
- *Compliance posture*: see [docs/compliance/C5.md](compliance/C5.md) — the BSI C5 controls
  the code addresses out of the box (IDM, KRY, KOS, OPS, IDC, PI, BEI, INQ).

---

## 2. Prerequisites

- Docker Desktop (or Docker Engine + Compose) running locally.
- ~6 GB free RAM for the full stack (Postgres × 5 + Keycloak + RabbitMQ + Valkey + 5 module hosts + gateway + nginx).
- Ports `8080` (SPA), `5000` (gateway), `8081` (Keycloak admin) free on the host.
- A browser. Chrome / Edge tested.

---

## 3. Pre-demo checklist — start 10 minutes before the client joins

```bash
# 1. From repo root
docker compose -f docker-compose.modules.yml up -d --build

# 2. Wait for Keycloak realm import to finish (~30s)
docker logs -f dialysis-keycloak 2>&1 | grep -m 1 "Realm 'dialysis' imported"

# 3. Smoke-check the gateway
curl -s http://localhost:5000/ | jq .

# 4. Smoke-check the SPA is reachable
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080
```

What "ready" looks like:
- `http://localhost:8080` — SPA loads, redirects to `/login`.
- `http://localhost:5000/` — JSON listing the route prefixes.
- `http://localhost:8081/realms/dialysis/.well-known/openid-configuration` — returns OIDC discovery JSON.

**Demo credentials**: `demo` / `demo` (realm role `his-developer`, configured in
[keycloak/dialysis-realm.json](../keycloak/dialysis-realm.json)).

**Simulators that are running automatically** (all gated by `<Module>:Demo:*` env vars in
[docker-compose.modules.yml](../docker-compose.modules.yml)):
- **EHR registration simulator** — every 20 s adds a new patient → emits `PatientRegisteredIntegrationEvent`.
- **PDMS vitals ticker** — every 5 s writes a synthetic intradialytic reading on each in-progress session.
- **PDMS machine telemetry simulator** — every 9 s publishes a `DialysisMachineTreatmentSnapshotIntegrationEvent`; every 6th tick adds an alarm.
- **SmartConnect HL7 v2 simulator** — every 7 s alternates ADT^A01 / ORU^R01 messages into two seeded demo flows.

These are **only** activated in `Development` / demo configurations; they cannot run when
`<Module>:Demo:Enabled=false` (the production default).

---

## 4. The demo script

### Step 1 — Sign in (2 min)

1. Open `http://localhost:8080` in an incognito window.
2. The SPA loads, calls `/identity/user`, gets `401`, and renders the **Login** page.
3. Click **Sign in**.

What happens under the hood:
- SPA sends `GET /identity/login?returnUrl=http://localhost:8080/` to the BFF.
- BFF issues an OIDC challenge (`authorization_code` + PKCE) to Keycloak.
- Keycloak prompts for credentials. Use `demo` / `demo`.
- Keycloak redirects back to the BFF callback; BFF sets an HTTP-only session cookie scoped to the gateway.
- BFF redirects to the allowlisted SPA origin.
- SPA re-calls `/identity/user`, now gets `200` with `{ name, email, roles }`, and renders the
  authenticated shell.

**Talking points:**
- Cookies, not tokens, live in the browser — this is the standard *BFF pattern* recommended by
  the IETF OAuth-Browser BCP. Even if XSS happens, the attacker can't extract a usable token.
- The `returnUrl` is **allowlisted** in [BffSpaOptions.cs](../src/backend/Identity/Dialysis.Identity.Bff/Configuration/BffSpaOptions.cs);
  arbitrary URLs are rejected. Open-redirect protection.

### Step 2 — Dashboard (5 min)

The dashboard shows three things stacked vertically:

1. **Operations snapshot** (top) — three stat cards reading
   `GET /api/his/api/v1.0/data-management/manager-dashboard`. No PHI.
2. **Active dialysis sessions** — cards rendered from `GET /api/pdms/api/v1.0/sessions?activeOnly=true`.
3. **Cross-module integration events** — table reading `GET /api/his/api/v1.0/data-management/integration/outbox-metadata`,
   refreshing every 15 s.

Have the client watch the events table for ~30 seconds. They will see new rows arrive as
the simulators emit registrations, snapshots, and HL7 messages.

**Talking points:**
- This is the *real* cross-module event substrate. Every clinically-significant action
  produces an integration event in a Postgres outbox table on the originating module's
  schema, gets relayed by Transponder to RabbitMQ, and is consumed by other modules.
- The same outbox metadata feed will be useful for auditors — the `correlationId` column
  ties end-to-end across the entire request chain.

### Step 3 — Live session vitals (5 min)

1. Click any active-session card.
2. The **Live Vitals** page opens; it immediately:
   - Loads historical readings via REST.
   - Opens a SignalR connection to `/hubs/vitals` over the gateway.
   - Joins the `session:{id}` group.
3. Within 5 s, the **status badge** shows `connected` and a new dot appears on the chart.
   Every subsequent 5 s another point arrives — the chart slides forward in real time.

The **session lifecycle controls** bar lets the demoer:
- **Complete** the session — enter UF volume, click. Status flips to `Completed`, the chart stops
  updating (the ticker only runs against in-progress sessions). Watch the dashboard tile change.
- **Abort** the session — pick a reason code, click. Status flips to `Aborted`.

**Talking points:**
- The vitals chart is **D3** (`Dialysis.PDMS.Api/Realtime/SignalRVitalsBroadcaster.cs`
  pushes the snapshot through a SignalR group; the React hook
  [useVitalsStream.ts](../src/frontend/dialysis-web/src/features/vitals/hooks/useVitalsStream.ts)
  consumes it, maintains a 500-point ring buffer, and feeds the chart in
  [VitalsChart.tsx](../src/frontend/dialysis-web/src/features/vitals/components/VitalsChart.tsx)).
- Connection-quality is honest: pull the network or stop the PDMS container and the badge
  flips through `reconnecting` → `disconnected`. The hook re-joins automatically when the
  server returns.
- The buttons exercise the *real* CQRS pipeline including the authorization behaviour —
  a user without `pdms.session.complete` would get a `403`.

### Step 4 — Patients (3 min)

1. Click **Patients** in the top nav.
2. Type "Khan" in the search field. The query hits `GET /api/ehr/api/v1.0/patients?q=Khan`
   which runs the case-insensitive Postgres ILIKE search on family/given/MRN.
3. Click any patient row → **Patient chart** page.

The chart page shows five sections (Problems, Allergies, Medications, Vital signs,
Immunizations) read from a single
[GetPatientChartQuery](../src/backend/EHR/Dialysis.EHR.PatientChart/Features/GetPatientChart/GetPatientChartQuery.cs)
handler. Below it: the patient's **HIE consent grants** from
`GET /api/hie/api/v1.0/hie/consents/patient/{id}`, demonstrating *cross-module composition* —
two different modules (EHR + HIE) contributing to one screen without a shared database.

**Talking points:**
- The architecture tests in [ModuleBoundaryTests.cs](../tests/Dialysis.ArchitectureTests/ModuleBoundaryTests.cs)
  enforce that EHR can only see HIE through `Dialysis.HIE.Contracts`. Reach for that file if a
  client asks "how do you stop one module's code from leaking into another".

### Step 5 — HIS workflows (4 min)

Navigate to **HIS** in the top nav. Three forms:

| Card | Endpoint | What to enter |
|---|---|---|
| Admit patient | `POST /api/his/api/v1.0/patient-flow/admissions` | Paste a patient id from the previous page; pick a ward |
| Book appointment | `POST /api/his/api/v1.0/scheduling/appointments` | Patient + provider Guid; pick tomorrow at 10:00 |
| Place medication order | `POST /api/his/api/v1.0/medication/orders` | Patient + RxNorm code (e.g. `29046` Lisinopril) + dosage |

After each successful submission the green chip shows the new entity id. Switch back to the
**Dashboard** — the operations stat cards and the integration events feed both reflect the
new activity within a few seconds.

**Talking points:**
- Each form is a one-shot CQRS command. The handler validates inputs, runs the aggregate,
  saves, and (where the slice publishes events) drops an outbox row.
- The HIS API wraps successful bodies in a HATEOAS envelope `{ data, links }` — the SPA
  unwraps this in [hisApi.ts](../src/frontend/dialysis-web/src/features/his/api/hisApi.ts).
  Clients that want HATEOAS get it; the SPA doesn't pretend the wrapper isn't there.

### Step 6 — EHR workflows (4 min)

Navigate to **EHR**. Four forms:

| Card | Endpoint | Notes |
|---|---|---|
| Register patient | `POST /api/ehr/api/v1.0/clinical/patients` | MRN must be new; family + given + DOB required |
| Start encounter | `POST /api/ehr/api/v1.0/clinical/encounters` | Class code `AMB` / `IMP` / `EMER` |
| Sign clinical note | `POST /api/ehr/api/v1.0/clinical/notes/{id}/sign` | Demonstrates the *signed* event Hie consumes |
| Order lab test | `POST /api/ehr/api/v1.0/clinical/lab-orders` | Comma-separated LOINC panel codes |

After registering a patient here, switch to the **Patients** page in another tab — the new
record appears within a refresh. This is the same internal API the registration simulator uses,
so the "audit feed never sleeps" effect is genuine, not staged.

**Talking points:**
- Every command is permissioned — see
  [EhrPermissions.cs](../src/backend/EHR/Dialysis.EHR.Contracts/Security/EhrPermissions.cs).
  The CQRS gateway runs the `AuthorizationPipelineBehavior` before the handler executes.
- The signed-note flow demonstrates *why* events matter: a clinician signing a note in the
  EHR triggers HIE downstream to consider partner organisations for outbound dispatch — the
  modules don't have to know about each other directly.

### Step 7 — Integrations (4 min)

Navigate to **Integrations**. Two sections:

1. **Flows** — two demo flows seeded at startup ("Demo ADT^A01 inbound" and "Demo ORU^R01 inbound").
   Click **Pause** / **Stop** / **Start** on either; the message ledger reacts within ~7 s
   because the HL7 v2 simulator continues to push messages and only the *Started* flow accepts them.
2. **Message ledger** — latest 25 inbound messages. Each row shows flow id, status,
   correlation id, and a **Reprocess** button.

Click **Reprocess** on a delivered message. The handler re-runs the inbound dispatch path.
Watch a new row appear with a new id, same correlation id.

**Talking points:**
- This is SmartConnect's *operator console* — the equivalent surface that legacy integration
  engines (Mirth Connect, Rhapsody, BizTalk) provide. The flow runtime, ledger, alert rules,
  attachments are all addressable via the same REST API the SPA uses.
- The HL7 v2 messages are processed in-process via `IInboundTransport`, *exactly* the same way
  the public `POST /smartconnect/v1/flows/{flowId}/messages` endpoint would dispatch them.
  Pause the flow → messages stop flowing through. Stop a flow and look at the OperationOutcome
  the simulator gets back.

### Step 8 — FHIR exchange (4 min)

Navigate to **FHIR Exchange**. Two panels:

1. **Submit FHIR Bundle** — pre-populated with a sample Patient resource. Click **Submit**.
   The textarea is just plain FHIR JSON; the SPA `POST`s as `application/fhir+json` with a
   `X-HIE-Partner` header. The response (OperationOutcome) is rendered as JSON.
2. **Patient $match** — fill in MRN/family/given/birthdate. Click **Match**. The HIE patient
   index returns a `Bundle.type=searchset` of matching `Patient` resources.

**Talking points:**
- HIE accepts *any* FHIR resource type, not just Bundles — see
  [FhirController.cs](../src/backend/HIE/Dialysis.HIE.Inbound/Controllers/FhirController.cs).
  We use Bundle in the demo because it's the most common HIE use case (composition of multiple resources).
- The `$match` operation follows the HL7 FHIR Patient Match IG. We exposed only the simple
  parameter form here; the SPA can be extended to send a full Parameters resource for advanced
  matching (Soundex, weighted scoring).
- Cross-organisation exchange is gated by consent — go back to the **Patient chart** and the
  patient's HIE consent grants are right there alongside the chart. Revoking a consent here
  immediately blocks the partner from $match results (consent gate runs in the read pipeline).

### Step 9 — Wrap-up (2 min)

Go back to the **Dashboard**. The integration events feed has new rows from everything the
client just did — admit, book, prescribe, register, sign, order, ingest, match. End by
opening one of the live sessions you didn't complete: vitals still streaming, chart still
moving. The system was running the whole time.

---

## 5. Architecture deep-dive answers (for likely questions)

### "How do you scale this?"
Each module host is stateless. `docker compose ... up -d --scale his-api=3` spins three HIS
replicas behind the gateway; round-robin load balancing, active health checks, session
affinity (not needed but available) all in [appsettings.json](../src/backend/Shared/Dialysis.Module.Gateway/appsettings.json).
Distributed state lives in Valkey (Redis-protocol fork) — both `IDistributedCache` and
the ASP.NET Data Protection key ring — see
[ValkeyDistributedCacheServiceCollectionExtensions.cs](../src/backend/BuildingBlocks/DistributedCache/Dialysis.BuildingBlocks.DistributedCache.Valkey/ValkeyDistributedCacheServiceCollectionExtensions.cs).

### "What happens if RabbitMQ goes down?"
Cross-module events are written to a transactional outbox **on the originating module's own
DbContext** in the same transaction as the aggregate change (`transponder` schema, per-module
DB). A background relay publishes them; if RabbitMQ is unreachable, the rows sit in the outbox
until the broker returns. Consumers track processed message ids in an inbox table so re-delivery
is idempotent.

### "What about compliance — German hospitals need BSI C5, hospitals everywhere need HIPAA"
See [docs/compliance/C5.md](compliance/C5.md) for the BSI C5 control map by domain. The code
addresses IDM (auth), KRY (TLS + key ring), KOS (security headers + segmentation), OPS
(structured audit + OTel), IDC (OAuth2 / OIDC / SMART), PI (FHIR R4 + Bulk Data), BEI (secure SDLC),
INQ (audit-event store). The operational pieces — physical security, personnel checks, vulnerability
scans — are the auditor deliverables listed at the bottom of that document.

### "How do I onboard a new EHR / HIS vendor?"
Two paths: (1) build an HL7 v2 / FHIR feed into SmartConnect — see the seeded demo flows for
the pattern; (2) implement an outbound adapter in `Dialysis.SmartConnect.Adapters.*` — Epic,
Cerner, Meditech, OpenEMR, Allscripts already have skeleton adapters with full vendor-specific
OAuth2 wiring in place.

### "Can the SPA work against a real hospital's data, not the simulators?"
Yes — turn off the `<Module>:Demo:*` flags in compose (or just deploy without them in
production). The endpoints are identical; the simulators just exercise them. The SPA doesn't
have any demo-only branching.

---

## 6. Troubleshooting cheatsheet

| Symptom | Likely cause | Fix |
|---|---|---|
| SPA shows "Authenticating…" indefinitely after sign-in | BFF cookie didn't make it back through gateway. | Check browser dev-tools → Network → `/identity/user` response headers. Ensure `Set-Cookie: SameSite=Lax`. |
| Login button redirects to a JSON page (BFF root) | `returnUrl` was not allowlisted. | Verify your origin is in `Identity:Spa:AllowedReturnUrlPrefixes` in [appsettings.json](../src/backend/Identity/Dialysis.Identity.Bff/appsettings.json). |
| Live vitals chart is stuck on "Awaiting readings…" | Vitals ticker not running. | `docker logs dialysis-modules-pdms-api-1 \| grep -i ticker` should show "Vitals ticker started". If not, ensure `Pdms__Demo__VitalsTicker=true` in compose. |
| Integrations page is empty | SmartConnect demo seeder didn't run. | `docker logs dialysis-modules-smartconnect-api-1 \| grep -i seeder` — should show "ensured demo ADT + ORU flows". |
| HIE FHIR submit returns 401 | The JWT lost its audience in transit. | Confirm `Gateway__Audience=account` in compose for the gateway service. |
| `/fhir/Patient/$match` returns empty bundle | The patient index hasn't been built yet. | The EHR registration simulator seeds new patients but the HIE index updates on event consumption — wait ~20 s after a registration. |

---

## 7. What this MVP does NOT yet show (be honest with the client)

These are deferred items the team will deliver against the production-ready roadmap:

- **FHIR `AuditEvent` emission on every PHI read** — the audit-event building block is wired and persisted, but the REST controllers do not yet call it on every endpoint. Required for HIPAA + C5 OPS-08.
- **TLS termination in front of the gateway** — the demo stack is plaintext HTTP for simplicity; production deploys behind an L7 LB (ALB / Traefik / Envoy) terminating TLS.
- **Secrets out of source control** — the demo realm secret (`bff-dev-secret-change-me`) ships in the repo for the demo only. Production uses Azure Key Vault / AWS Secrets Manager.
- **Real OIDC role → module permission mapping** — Keycloak roles map only to HIS for the demo user. EHR / PDMS / HIE / SmartConnect permission strings need their own role mappings per [RolePermissionMap convention](../src/backend/Shared/Dialysis.Module.Hosting/).
- **Playwright end-to-end tests** — backend has 338 passing tests; the SPA has no automated test coverage yet.
- **OTel collector wired into compose** — module hosts emit OTel traces and meters, but the demo stack doesn't run a collector + Jaeger/Grafana. Pre-prod deployments do.

---

## 8. Demo reset

Between demos:

```bash
# Full reset — drops all volumes (Postgres data, Valkey persistence, RabbitMQ state)
docker compose -f docker-compose.modules.yml down -v
docker compose -f docker-compose.modules.yml up -d
```

Soft reset (just bounce the API hosts, keep Keycloak/Postgres warm):

```bash
docker compose -f docker-compose.modules.yml restart \
  his-api ehr-api pdms-api smartconnect-api hie-api gateway web
```

The seeders are idempotent, so on a soft reset the demo patients / flows persist.

---

## 9. Appendix — endpoint catalogue used in the demo

| Module | Method | Path | Page |
|---|---|---|---|
| Identity BFF | GET | `/identity/login?returnUrl=…` | Login |
| Identity BFF | GET | `/identity/user` | All (auth probe) |
| HIS | GET | `/api/his/api/v1.0/data-management/manager-dashboard` | Dashboard |
| HIS | GET | `/api/his/api/v1.0/data-management/integration/outbox-metadata` | Dashboard |
| HIS | POST | `/api/his/api/v1.0/patient-flow/admissions` | HIS Workflows |
| HIS | POST | `/api/his/api/v1.0/scheduling/appointments` | HIS Workflows |
| HIS | POST | `/api/his/api/v1.0/medication/orders` | HIS Workflows |
| EHR | GET | `/api/ehr/api/v1.0/patients?q=…` | Patients |
| EHR | GET | `/api/ehr/api/v1.0/patients/{id}/chart` | Patient Chart |
| EHR | POST | `/api/ehr/api/v1.0/clinical/patients` | EHR Workflows |
| EHR | POST | `/api/ehr/api/v1.0/clinical/encounters` | EHR Workflows |
| EHR | POST | `/api/ehr/api/v1.0/clinical/notes/{id}/sign` | EHR Workflows |
| EHR | POST | `/api/ehr/api/v1.0/clinical/lab-orders` | EHR Workflows |
| PDMS | GET | `/api/pdms/api/v1.0/sessions?activeOnly=true` | Dashboard |
| PDMS | GET | `/api/pdms/api/v1.0/sessions/{id}/readings` | Live Vitals |
| PDMS | POST | `/api/pdms/api/v1.0/sessions/{id}/start` | Live Vitals |
| PDMS | POST | `/api/pdms/api/v1.0/sessions/{id}/complete` | Live Vitals |
| PDMS | POST | `/api/pdms/api/v1.0/sessions/{id}/abort` | Live Vitals |
| PDMS | WS | `/hubs/vitals` (`JoinSessionAsync` → `reading` events) | Live Vitals |
| SmartConnect | GET | `/smartconnect/api/v1/admin/flows` | Integrations |
| SmartConnect | GET | `/smartconnect/api/v1/admin/messages` | Integrations |
| SmartConnect | POST | `/smartconnect/api/v1/admin/flows/{id}/{start|pause|stop}` | Integrations |
| SmartConnect | POST | `/smartconnect/api/v1/admin/messages/{id}/reprocess` | Integrations |
| HIE | GET | `/api/hie/api/v1.0/hie/consents/patient/{id}` | Patient Chart |
| HIE | POST | `/api/hie/api/v1.0/fhir/Bundle` | FHIR Exchange |
| HIE | GET | `/api/hie/api/v1.0/fhir/Patient/$match` | FHIR Exchange |
