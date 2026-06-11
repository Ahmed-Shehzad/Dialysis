# Dialysis Platform

> One coherent software platform for dialysis clinics and renal-care networks — it runs the front desk, the clinical chart, the dialysis machine room, the lab-order loop, the patient portal, and the cross-organization data exchange that modern healthcare regulation expects, as a single system with one shared language.

This README has two parts. **Part 1** is a plain-language explanation. **Part 2** is the engineering architecture, and stitches together the per-module deep-dives:

- [HIS — Hospital Information System](src/backend/HIS/ARCHITECTURE.md)
- [EHR — Electronic Health Record](src/backend/EHR/ARCHITECTURE.md)
- [PDMS — Patient Data Management System](src/backend/PDMS/ARCHITECTURE.md)
- [HIE — Health Information Exchange](src/backend/HIE/ARCHITECTURE.md)
- [SmartConnect — Legacy-protocol integration engine](src/backend/SmartConnect/ARCHITECTURE.md)
- [Lab — Laboratory orders (headless)](src/backend/Lab/ARCHITECTURE.md)
- [Patient Portal — aggregation BFF](src/backend/PatientPortal/ARCHITECTURE.md)
- [Identity & Auth](src/backend/Identity/ARCHITECTURE.md)

Related references: build/run/test/deploy conventions in [CLAUDE.md](CLAUDE.md); the deployment-unit decision record in [docs/architecture/adr-0001-bounded-context-deployment-units.md](docs/architecture/adr-0001-bounded-context-deployment-units.md); image publishing in [docs/operations/container-registry.md](docs/operations/container-registry.md); device/coding interop in [docs/interoperability/](docs/interoperability/); vulnerability reporting in [SECURITY.md](SECURITY.md).

---

# Part 1 — In plain language

A dialysis clinic today juggles four kinds of software that rarely talk to each other: front-office/hospital systems, the clinical record, the machine-room systems, and external data exchange with payers, hospitals, registries and patient apps. The result is duplicate data entry, missed billing, slow audits, and a brittle ability to participate in modern care networks.

This platform is **one system, six connected modules, one shared language:**

| Module | Mental model |
|---|---|
| **HIS** | The *operations* brain — front desk, the patient queue, scheduling, staff & inventory, device registry, billing-export. |
| **EHR** | The *patient story* brain — demographics, encounters, orders, prescriptions, notes, billing claims (professional **and** institutional), the patient portal. The source of truth for who the patient is. |
| **PDMS** | The *machine room* brain — watches each dialysis session live, records every vital sign and alarm, drives IV pumps, runs on-call escalation, renders session documents. |
| **HIE** | The *outside world* brain — speaks FHIR / IHE / TEFCA to share records with insurers, hospitals, registries and patient apps; owns consent and document retention. |
| **SmartConnect** | The *translator* — talks to legacy machines and older hospital systems (HL7 v2, files, DICOM) and converts everything to the modern vocabulary, including the IEEE 11073 dialysis-machine prescription wire (HD / HF / HDF therapy with UF profiles). |
| **Lab** | The *lab loop* — places LOINC-coded lab orders, tracks them to resulted, headless (no UI of its own; its data surfaces in the EHR chart). |

Plus an **Identity** layer for sign-in and a **web app per context** (front desk, chart, chairside, exchange, feeds, admin, patient portal) that staff and patients actually use.

The modules never reach into each other's database. They cooperate by publishing **events** ("patient admitted", "session completed", "claim submitted") that the others react to — so each module can evolve, scale and fail independently.

---

# Part 2 — Engineering architecture

## 2.1 System context

Who touches the system, and through what door:

```mermaid
flowchart TB
    classDef actor fill:#dbeafe,stroke:#2563eb
    classDef ext fill:#fef3c7,stroke:#d97706

    Clin(["Clinicians<br/>front desk · nurses · physicians"]):::actor
    Pat(["Patients"]):::actor
    Adm(["Admins · DPO · integration operators"]):::actor
    IdP(["Upstream IdPs<br/>Okta · Auth0 · Entra"]):::ext
    Partner(["Partner HIEs / QHINs<br/>hospitals · registries · payers"]):::ext
    LIS(["Laboratory information system"]):::ext
    Mach(["Dialysis machines · IV pumps<br/>imaging modalities · RPM devices"]):::ext

    Clin -->|"/his /ehr /pdms /smartconnect /hie"| GW
    Pat -->|"/portal"| GW
    Adm -->|"/admin (console) · /hie/admin · admin APIs"| GW

    GW["Edge Gateway :9090<br/>YARP — the single browser origin"]
    KC["Keycloak<br/>realm dialysis"]
    GW -->|"/auth/*"| KC
    IdP -. "brokered OIDC (kc_idp_hint)" .-> KC

    subgraph SYS["Dialysis platform — modular monolith, one DB per module"]
        HIS["HIS<br/>clinic operations"]
        EHR["EHR<br/>patient record · billing"]
        PDMS["PDMS<br/>live treatment telemetry"]
        SC["SmartConnect<br/>protocol translator"]
        HIE["HIE<br/>FHIR / IHE exchange"]
        LAB["Lab<br/>order lifecycle (headless)"]
    end

    GW -->|"per-context BFFs (§2.6)"| SYS
    HIE <-->|"FHIR R4 · IHE XCA/XDS · TEFCA IAS JWT"| Partner
    EHR -->|"EDI 837P/837I claims · 277CA/999 acks"| Partner
    SC <-->|"HL7v2 MLLP · DICOM DIMSE + DICOMweb · files<br/>IEEE 11073 prescription download (HD/HF/HDF, UF profiles)"| Mach
    SC <-->|"HL7v2 orders & results"| LIS
```

## 2.2 Bounded-context map

It is a **modular monolith**: eight backend bounded contexts, each with its own ASP.NET host(s) and its own database. Two rules hold everything together, and both are build-failures if broken (`tests/Dialysis.ArchitectureTests`):

1. **Contracts-only references** — a project under module X may reference its own siblings, the shared layers, and another module's `*.Contracts` assembly. Nothing else.
2. **Cross-context flow rides integration events** over the Transponder bus (RabbitMQ deployed, in-memory in dev/tests) — never a direct domain call. The one sanctioned synchronous exception is a cross-module *query* (e.g. HIE's consent check).

```mermaid
flowchart LR
    classDef rule fill:#fee2e2,stroke:#dc2626
    classDef bb fill:#ecfdf5,stroke:#059669

    subgraph SPAS["7 SPAs — React + Vite, one per user-facing context"]
        direction TB
        W1["his-web"]
        W2["ehr-web"]
        W3["pdms-web"]
        W4["smartconnect-web"]
        W5["hie-web"]
        W6["identity-web (admin)"]
        W7["patient-portal-web"]
    end

    subgraph BFFS["BFF tier — OIDC cookie in, bearer JWT out"]
        direction TB
        B1["his-bff"]
        B2["ehr-bff"]
        B3["pdms-bff"]
        B4["smartconnect-bff"]
        B5["hie-bff"]
        B6["admin-bff"]
        B7["portal-bff (PatientPortal context — BFF-only)"]
        B8["identity-bff (OIDC handshake)"]
    end

    subgraph CTX["8 backend bounded contexts"]
        direction TB
        HIS["HIS — PatientFlow · Scheduling · Medication · Security ·<br/>Operations · Integration · DataServices · PatientAccess · RaCapabilities"]
        EHR["EHR — Registration · PatientChart · Scheduling · Integration ·<br/>Billing (837P/837I, UB-04 fields) · ClinicalNotes · PatientPortal slice"]
        PDMS["PDMS — TreatmentSessions · Medications (MAR + IV pumps) ·<br/>OnCall · Reporting — TimescaleDB"]
        SC["SmartConnect — Inbound · Adapters · Dicom ·<br/>AvScanning · Management (channel/flow engine)"]
        HIE["HIE — Outbound · Inbound · Query · Consent ·<br/>Documents · Tefca · Xds · OpenEhr"]
        LAB["Lab — Orders (headless: no BFF, no SPA)"]
        IDY["Identity — Keycloak realm + provisioning API<br/>(users/roles) + identity/admin BFFs"]
        PP["PatientPortal — aggregation/event-push BFF only;<br/>portal domain lives in the EHR PatientPortal slice"]
    end

    subgraph BB["Shared building blocks (src/backend/BuildingBlocks)"]
        direction TB
        BB1["Intercessor (mediator) · Verifier (validation) · CQRS"]:::bb
        BB2["Transponder (bus + outbox/inbox/saga) · DurableCommandBus"]:::bb
        BB3["Fhir stack (~18 projects) · DataProtection (GDPR) · Hipaa"]:::bb
        BB4["ClinicianNotification (SMS/APNs/FCM) · Documents · Direct ·<br/>DistributedCache.Valkey · Module.Hosting/Bff/Bff.Events/Gateway"]:::bb
    end

    MQ{{"RabbitMQ — Transponder integration events<br/>(schema-versioned, outbox → inbox)"}}

    SPAS --> BFFS
    B1 --> HIS
    B2 --> EHR
    B3 --> PDMS
    B4 --> SC
    B5 --> HIE
    B6 --> IDY
    B7 --> PP
    PP -.->|"aggregates over EHR/PDMS/HIS/HIE APIs"| EHR

    HIS <--> MQ
    EHR <--> MQ
    PDMS <--> MQ
    SC <--> MQ
    HIE <--> MQ
    LAB <--> MQ
    IDY --> MQ

    CTX --- BB

    R1["Rule: cross-context = integration events only<br/>(architecture-test enforced)"]:::rule
    R2["Rule: other modules may reference only<br/>Dialysis.X.Contracts"]:::rule
    MQ --- R1
    CTX --- R2
```

Module-internal layout follows a reference shape (HIS is canonical): `Contracts` (events + permission catalog — the only externally referenceable assembly), one project per vertical slice (each slice carries its own `Fhir/` mappers), `Persistence` (one `DbContext` per module, schema-per-slice, Transponder outbox/inbox/saga on the same context), `Composition` (single `AddX(...)`), `Api` (versioned MVC under `api/v1.0`, HATEOAS envelope, `/health/live|ready`), `Bff`, `Tests`. Intentional divergences (provider-split persistence in EHR/PDMS/SmartConnect/HIE, headless Lab, BFF-only PatientPortal, channel-model SmartConnect) are catalogued in [CLAUDE.md](CLAUDE.md).

## 2.3 Event storming, not event sourcing

The system is modelled with **event storming** (Brandolini): commands, aggregates, policies, domain events, integration events, read models. It does **not** use event sourcing — aggregates persist current state via EF Core, never as a replayable log.

- **Domain events** (`IDomainEventHandler<T>`) — in-process, dispatched within the same `SaveChanges` transaction; for *within-context* coordination.
- **Integration events** (`IIntegrationEvent`) — written to a Transponder **outbox** in the same transaction as the state change, then relayed asynchronously over RabbitMQ; for *cross-context* signals. Schema-versioned (`int SchemaVersion`, guarded by `IntegrationEventVersioningTests`).
- **Read models** — denormalized projections built from current state, never rebuilt from a log.

## 2.4 Runtime topology (full stack)

What actually runs, with the pinned ports (pinned because the Keycloak clients only accept these `redirect_uri`s):

```mermaid
flowchart TB
    classDef infra fill:#f3e8ff,stroke:#7c3aed
    classDef db fill:#e0f2fe,stroke:#0284c7

    Browser(["Browser — everything via one origin"])
    Browser --> GW

    GW["Gateway :9090 (YARP)<br/>/{ctx}/api · /{ctx}/hubs · /{ctx}/events · /{ctx}/identity → context BFF<br/>/{ctx}/* (catch-all) → context SPA<br/>/identity/* → Identity BFF · /auth/* → Keycloak"]

    subgraph BFFT["Context BFFs (pinned ports)"]
        direction LR
        HB["his-bff :5301"]
        EB["ehr-bff :5302"]
        PB["pdms-bff :5303"]
        SB["smartconnect-bff :5304"]
        XB["hie-bff :5305"]
        AB["admin-bff :5306"]
        OB["portal-bff :5307"]
    end
    IB["identity-bff :5275 (OIDC handshake)"]

    subgraph SPATIER["SPAs (Vite dev :5331–5337, always reached through the gateway)"]
        direction LR
        SPA1["his-web"]
        SPA2["ehr-web"]
        SPA3["pdms-web"]
        SPA4["smartconnect-web"]
        SPA5["hie-web"]
        SPA6["identity-web"]
        SPA7["patient-portal-web"]
    end

    GW --> BFFT
    GW --> IB
    GW --> SPATIER

    subgraph APIS["Module APIs (compose host ports; dynamic under Aspire)"]
        direction LR
        HA["HIS API :5288"]
        EA["EHR API :5289"]
        PA["PDMS API :5290<br/>+ VitalsHub (SignalR)"]
        SA["SmartConnect API :5291"]
        XA["HIE API :5292"]
        LA["Lab API :5293 (headless)"]
    end

    HB -->|"bearer JWT"| HA
    EB -->|"bearer JWT"| EA
    PB -->|"bearer JWT"| PA
    SB -->|"bearer JWT"| SA
    XB -->|"bearer JWT"| XA
    OB -->|"aggregates"| APIS

    subgraph DBS["One PostgreSQL per module"]
        direction LR
        D1[("postgres-his")]:::db
        D2[("postgres-ehr")]:::db
        D3[("postgres-pdms<br/>TimescaleDB: vitals hypertable,<br/>compression + 365d retention")]:::db
        D4[("postgres-smartconnect")]:::db
        D5[("postgres-hie")]:::db
        D6[("postgres-lab")]:::db
    end

    HA --> D1
    EA --> D2
    PA --> D3
    SA --> D4
    XA --> D5
    LA --> D6

    MQ{{"RabbitMQ :5672<br/>integration events + durable commands<br/>(quorum queues in k8s)"}}:::infra
    VK[("Valkey :6379<br/>distributed cache · BFF session tickets ·<br/>Data Protection key ring · SignalR backplane")]:::infra
    KC["Keycloak :8081<br/>realm dialysis (re-imported each AppHost run)"]:::infra
    OT[["OTel collector :4317/:4318<br/>traces · metrics · logs → Aspire dashboard / Grafana"]]:::infra

    APIS <--> MQ
    BFFT -->|"consume-only bff-(slug) queues → /{ctx}/events SignalR"| MQ
    APIS --> VK
    BFFT --> VK
    BFFT --> KC
    IB --> KC
    APIS --> OT
    BFFT --> OT
    GW --> OT
```

Notes that matter for scale-out: module hosts are stateless; Valkey carries every bit of shared state (cache, BFF cookie tickets, Data Protection keys) **and** the SignalR backplane for the BFF event hubs and the PDMS vitals hub, so any tier can run multiple replicas. Background scheduling is PostgreSQL-backed Hangfire (distributed locks). The full port matrix lives in [CLAUDE.md](CLAUDE.md).

## 2.5 Cross-context event flow — one patient journey

One end-to-end journey (the same one `tools/Dialysis.DataSimulator` drives and the e2e suites assert against), with the delivery machinery shown once in detail. Every hop uses the same pattern: **outbox row in the writer's transaction → relay (advisory-lock gated) → RabbitMQ → consumer inbox dedup**.

```mermaid
sequenceDiagram
    autonumber
    participant U as Front desk / clinician
    participant EHR as EHR API
    participant EDB as EHR Postgres<br/>(aggregates + transponder outbox)
    participant REL as Outbox relay<br/>(hosted service, every replica)
    participant MQ as RabbitMQ
    participant HIS as HIS API (+ inbox)
    participant PDMS as PDMS API (+ TimescaleDB)
    participant HIE as HIE API
    participant EXT as Partner HIE endpoint

    U->>EHR: Register patient
    EHR->>EDB: ONE tx: Patient aggregate row + PatientRegistered outbox row
    note over REL,EDB: Each polling tick: SELECT pg_try_advisory_lock(...)<br/>one replica wins, others skip — lock explicitly released<br/>(crash → server releases on disconnect)
    REL->>EDB: claim pending outbox rows
    REL->>MQ: publish PatientRegisteredIntegrationEvent
    REL->>EDB: mark dispatched (+ Dialysis.Transponder.Outbox metrics)
    MQ->>HIS: deliver
    HIS->>HIS: inbox dedup (unique DeduplicationKey) → mirror patient into ops queue

    U->>HIS: Check in, assign chair
    HIS-->>MQ: PatientCheckedIn · PatientPlacedInChair (same outbox path)
    MQ->>PDMS: deliver → PDMS prepares the session
    MQ->>EHR: deliver → chart reflects flow state

    PDMS->>PDMS: start session · stream vitals to hypertable · alarms · MAR
    PDMS-->>MQ: DialysisSessionCompleted · DialysisSessionChargeReady
    MQ->>EHR: deliver
    EHR->>EHR: capture charge → claim (837P professional / 837I institutional)
    EHR-->>MQ: ClinicalNoteSigned · EncounterClosed · ClaimSubmitted

    MQ->>HIE: deliver
    HIE->>HIE: map to FHIR R4 · US Core validation · consent check
    HIE->>EXT: dispatch w/ Polly retry + TEFCA IAS JWT + AuditEvent trail
    EXT-->>HIE: ack → Delivered (else DeadLettered)
```

The relay's advisory lock is what makes **replicas > 1 correct** (one replica relays per module database per tick; see [ADR-0001](docs/architecture/adr-0001-bounded-context-deployment-units.md)). Outbox health is observable: the `Dialysis.Transponder.Outbox` meter feeds the module-overview Grafana dashboard and alert rules under `deploy/k8s/observability/`.

## 2.6 Identity & auth

A single browser origin (the Gateway) fronts one BFF per context, each running OIDC + a **path-scoped cookie** against Keycloak (realm `dialysis`), so contexts never collide on the shared origin. Multi-IdP federation (Okta/Auth0/Entra) is Keycloak **brokering** — the BFFs never talk to an upstream IdP directly.

```mermaid
sequenceDiagram
    autonumber
    participant B as Browser (his-web)
    participant GW as Gateway :9090
    participant BFF as his-bff
    participant KC as Keycloak (realm dialysis)
    participant VK as Valkey
    participant API as HIS API

    B->>GW: GET /his/identity/login (optionally ?provider=okta)
    GW->>BFF: forward
    BFF->>B: 302 → /auth/... (OIDC code flow, scope incl. offline_access,<br/>kc_idp_hint when provider allow-listed)
    B->>KC: authenticate (or brokered upstream IdP)
    KC->>B: redirect with auth code
    B->>GW: callback /his/...
    GW->>BFF: forward
    BFF->>KC: exchange code → access + refresh + id tokens
    BFF->>VK: store session ticket (tokens live server-side)
    BFF->>B: Set-Cookie (Path=/his — path-scoped, never leaves the BFF prefix)

    B->>GW: GET /his/api/v1.0/... (cookie only, no token in browser)
    GW->>BFF: forward
    BFF->>VK: load ticket (OnValidatePrincipal)
    alt access token expires within 60 s
        BFF->>KC: POST /token (refresh_token grant)
        KC->>BFF: new access/refresh/id tokens
        BFF->>VK: rewrite ticket (ShouldRenew)
    else refresh fails / no refresh token
        BFF->>B: RejectPrincipal → SPA bounces through login
    end
    BFF->>API: YARP bearer-forward proxy (Authorization: Bearer access_token)
    API->>API: validate JWT against Keycloak authority,<br/>map roles → module permissions (RolePermissionMap)
    API->>BFF: ResourceEnvelope response
    BFF->>B: response — SPA gates UI via dialysis_permission claim → PermissionGate
```

Module APIs register JWT Bearer only when an Authority is configured; in Development with no Authority, `ICurrentUser` grants all permissions for local work. Patient-portal endpoints additionally filter by patient claim. Full detail in [Identity & Auth](src/backend/Identity/ARCHITECTURE.md).

## 2.7 Write paths — normal CQRS vs the durable command bus

Most writes are synchronous CQRS. A few telemetry-shaped writes opt into the **durable command bus**, which moves the durability boundary from "the row is in Postgres" to "the command is in a durable, publisher-confirmed RabbitMQ queue". The queue is an in-flight buffer, never a permanent log — the no-event-sourcing rule holds.

```mermaid
sequenceDiagram
    autonumber
    participant SPA as SPA (useDurableCommand hook)
    participant API as Module API
    participant DB as Module Postgres<br/>(aggregate · outbox · command_ledger)
    participant MQ as RabbitMQ (durable quorum queue)
    participant CON as DurableCommandConsumer

    rect rgb(240, 248, 255)
        note over SPA,DB: Normal CQRS write (the default)
        SPA->>API: POST command
        API->>DB: ONE tx: handler mutates aggregate + outbox row
        API->>SPA: 200/201 + ResourceEnvelope
    end

    rect rgb(255, 250, 240)
        note over SPA,CON: Durable write (opt-in: PDMS RecordReading, HIS IngestDeviceReading)
        SPA->>API: POST command
        API->>MQ: IDurableCommandBus.EnqueueAsync<br/>(publisher confirms awaited)
        API->>SPA: 202 Accepted + command-status URL<br/>(row id known up front: id = CommandId)
        MQ->>CON: deliver DurableCommandEnvelope
        CON->>DB: BEGIN tx → claim command_ledger row (idempotent on CommandId)
        CON->>DB: dispatch into the existing ICommandHandler<br/>(aggregate write + outbox row, same tx)
        CON->>DB: mark ledger Applied → COMMIT
        note over CON,DB: any throw → rollback → broker redelivers —<br/>ledger claim makes redelivery a no-op once Applied
        SPA->>API: GET /api/v1.0/command-status/{correlationId}<br/>(authorized against requestedBySubject)
        API->>SPA: ledger state → toast + inline progress chip
    end
```

Observability: the `Dialysis.DurableCommandBus` meter (enqueued/applied/failed counters, latency histograms) has its own Grafana dashboard + alert rules under `deploy/k8s/observability/`.

## 2.8 Cross-cutting building blocks

Under `src/backend/BuildingBlocks/`: **Intercessor** (in-process mediator), **Verifier** (validation pipeline), **Transponder** (messaging + EF outbox/inbox/saga, multiple transports, Hangfire/Quartz/TickerQ schedulers), the **Fhir** stack (~18 projects: core mappers, US-Core validation, SMART-on-FHIR, Subscriptions, BulkData, Audit, TEFCA, Terminology, De-identification, CDA bridge, openEHR), **DataProtection** (the GDPR/BDSG surface incl. the Art. 15 export and Art. 17 erasure hooks), **Hipaa** (PHI column encryption, `[PhiAccess]` audit pipeline emitting FHIR `AuditEvent`s, live safeguard registry), **ClinicianNotification** (multi-channel paging: Twilio SMS / APNs / FCM, used by PDMS on-call escalation), **DurableCommandBus** (§2.7), **Documents**, **Direct**, **DistributedCache.Valkey**, **PlatformGateway**, the gematik-TI **Tefca** block, and the shared **Module.{Hosting,Bff,Bff.Events,Gateway,Contracts}** host scaffolding.

## 2.9 Compliance surfaces (GDPR / BDSG / HIPAA)

Three distinct mechanisms, not to be conflated:

- **Storage limitation (Art. 5(1)(e))** — scheduled purge. HIE's Documents slice owns the retention pipeline (`DocumentRetentionPolicy` per document kind, a 24-hour purger, tombstones so audit replay sees deliberate purge); SmartConnect bounds its message ledger via retention pruning; PDMS bounds telemetry via the TimescaleDB retention policy.
- **Art. 15 access / Art. 17 erasure** — a request → DPO approval → execute pipeline behind the Admin console. **Five modules participate** with both an `IModuleDataExtractor` (Art. 15 export) and an `IPatientEraser` (Art. 17): HIS, EHR, PDMS, HIE (plus a dedicated HIE-Documents eraser), and Lab. `DefaultDataSubjectRightsService` walks every registered eraser and records the per-module breakdown to the EF-backed `IErasureRequestStore`. SmartConnect ships neither by design — it routes messages but owns no patient master record. Erasers use `ExecuteUpdate/DeleteAsync` so even thousands of telemetry rows clear in one round-trip per type.
- **HIPAA Security Rule** — `AddHipaaCompliance(moduleSlug)` wires PHI column encryption, the `[PhiAccess]` audit pipeline, and a live safeguard registry (`GET /admin/hipaa/safeguards`) in PDMS, EHR, HIS, HIE and SmartConnect.

## 2.10 Frontend

The UI is **seven independent per-context React apps** (one per bounded context), not a single shell:

| App | Context base | Dev port | Backing BFF | Real-time push |
|---|---|---|---|---|
| [his-web](src/frontend/his-web/README.md) | `/his` | 5331 | his-bff (5301) | yes |
| [ehr-web](src/frontend/ehr-web/README.md) | `/ehr` | 5332 | ehr-bff (5302) | yes |
| [pdms-web](src/frontend/pdms-web/README.md) | `/pdms` | 5333 | pdms-bff (5303) | yes |
| [smartconnect-web](src/frontend/smartconnect-web/README.md) | `/smartconnect` | 5334 | smartconnect-bff (5304) | no |
| [hie-web](src/frontend/hie-web/README.md) | `/hie` | 5335 | hie-bff (5305) | no |
| [identity-web](src/frontend/identity-web/README.md) (Admin) | `/admin` | 5336 | admin-bff (5306) | no |
| [patient-portal-web](src/frontend/patient-portal-web/README.md) | `/portal` | 5337 | portal-bff (5307) | yes |

**Toolchain** (identical across all seven apps): React 18 + Vite 8 + TypeScript 6 + Tailwind 4 + TanStack Query 5 + React Router 7, SignalR client, ECharts/D3 charts, react-pdf 10 / pdfjs viewers, XYFlow diagrams; **npm**, no monorepo workspace. Lint is strict: flat ESLint 10 config with TS + React + Hooks + Refresh + **jsx-a11y** (accessibility) at `--max-warnings=0`, Prettier, Husky `pre-commit` + `lint-staged`. Tests: Vitest 4 unit tests + a Playwright `e2e/` suite per app.

Cross-cutting code is **duplicated byte-for-byte**, not shared via a package — the auth shell, `useDurableCommand` client (202 → poll → toast), toast/theme shells, `lazyPage` (route-level code-splitting), `ErrorBoundary`, eslint/tsconfig — and `tools/frontend/check-duplicate-sync.sh` fails CI on any drift between copies. Other conventions: `humanizeError` maps ProblemDetails to readable sentences (clinical users never see raw status codes); mutations follow the optimistic-update + rollback + invalidate-on-settle pattern; `enforceGatewayOrigin()` keeps every app on the Gateway origin so the path-scoped BFF cookie survives; in-app links stay unprefixed (router basename adds `/{ctx}`) and cross-context navigation is a full-page hop.

## 2.11 Deployment — full stack and bounded-context units

All deployment artifacts are **generated from the Aspire AppHost** (the single source of truth; a CI drift gate regenerates and fails on diff). Two Kubernetes shapes exist:

- `deploy/compose/<env>/` + `deploy/charts/dialysis-<env>/` — the classic **full-stack** shape, three environments (dev/staging/prod).
- `deploy/charts/units/prod/` — **nine independently deployable Helm releases**, one per bounded context, for a microservices-style ops model while the codebase stays a monorepo ([ADR-0001](docs/architecture/adr-0001-bounded-context-deployment-units.md), [units README](deploy/charts/units/README.md)). Units are generated for prod only by design.

```mermaid
flowchart TB
    classDef plat fill:#f3e8ff,stroke:#7c3aed
    classDef idu fill:#fef9c3,stroke:#ca8a04
    classDef ctx fill:#e0f2fe,stroke:#0284c7

    subgraph NS["Kubernetes namespace dialysis-prod — all nine releases share it;<br/>cross-unit contract = stable Service DNS ((resource)-service, no release prefix)"]
        subgraph UPLAT["release dialysis-platform (install 1st)"]
            GW2["gateway + Ingress<br/>+ namespace-wide NetworkPolicies"]:::plat
            RMQ2["RabbitMQ (quorum)"]:::plat
            VK2["Valkey"]:::plat
        end
        subgraph UID["release dialysis-identity (install 2nd)"]
            KC2["Keycloak"]:::idu
            IBFF2["identity-bff + admin-bff"]:::idu
            IWEB2["identity-web"]:::idu
        end
        subgraph UHIS["dialysis-his"]
            H2["his-api · his-bff · his-web · postgres-his"]:::ctx
        end
        subgraph UEHR["dialysis-ehr"]
            E2["ehr-api · ehr-bff · ehr-web · postgres-ehr"]:::ctx
        end
        subgraph UPDMS["dialysis-pdms"]
            P2["pdms-api · pdms-bff · pdms-web ·<br/>postgres-pdms (TimescaleDB)"]:::ctx
        end
        subgraph USC["dialysis-smartconnect"]
            S2["smartconnect-api · -bff · -web · postgres"]:::ctx
        end
        subgraph UHIE["dialysis-hie"]
            X2["hie-api · hie-bff · hie-web · postgres-hie"]:::ctx
        end
        subgraph ULAB["dialysis-lab"]
            L2["lab-api · postgres-lab (headless)"]:::ctx
        end
        subgraph UPORT["dialysis-portal"]
            O2["portal-bff · patient-portal-web<br/>(portal domain lives in EHR/HIS)"]:::ctx
        end
    end

    UHIS -.->|"bus/cache via platform DNS"| UPLAT
    UEHR -.-> UPLAT
    UPDMS -.-> UPLAT
    USC -.-> UPLAT
    UHIE -.-> UPLAT
    ULAB -.-> UPLAT
    UPORT -.-> UPLAT
    UHIS -.->|"OIDC authority"| UID
    UEHR -.-> UID
    UPDMS -.-> UID
    UPORT -.-> UID
    UID -.-> UPLAT
```

Cross-unit dependencies are **configuration, not model references**: inside a unit chart, RabbitMQ/Valkey/Keycloak appear as values defaulting to the platform/identity units' in-cluster DNS names, overridable at `helm install` time. The gateway stays the single browser origin — adding a unit means updating its ReverseProxy cluster values on the platform unit. Horizontal scaling is safe by construction: Valkey session/key-ring state, the SignalR backplane, Hangfire distributed locks, inbox dedup + ledger claims, quorum queues, and the advisory-locked outbox relay (§2.5). HA infra (CloudNativePG clusters with sync replication on the clinical tier, RabbitMQ operator, PgBouncer) lives under `deploy/k8s/operators/`.

## 2.12 CI/CD and releases

```mermaid
flowchart LR
    classDef gate fill:#fee2e2,stroke:#dc2626
    classDef rel fill:#dcfce7,stroke:#16a34a

    PR["PR / push"] --> WF

    subgraph WF["Seven GitHub workflows"]
        direction TB
        BT["build-test.yml — NUKE: ShowVersion → Test →<br/>CoverageReport → Pack (Release)<br/>+ dispatch-only HIS outbox e2e (SQL+RMQ)"]
        DA["deploy-artifacts.yml — regenerate compose/Helm/units<br/>from the AppHost, fail on git diff"]:::gate
        FE["frontend.yml — per-app eslint/prettier/tsc/vitest/build matrix<br/>+ duplicate-sync gate"]:::gate
        SEC["security.yml — dotnet + npm dependency audit ·<br/>Trivy FS · OWASP ZAP baseline"]
        DOC["docs-regulatory.yml — SmartConnect PDF source-of-truth ·<br/>daily HIPAA regulatory-feed drift check"]
        LT["load-test.yml — scheduled/dispatch k6 scenarios"]
        SIM["simulator-smoke.yml — daily DataSimulator journey smoke"]
    end

    COV["Coverage ratchet: --min-coverage 70<br/>(measured 75% — raise, never lower)"]:::gate
    BT --- COV
    DEP["Dependabot — nuget · npm · actions"] --> PR

    GV["GitVersion 6 (GitVersion.yml)<br/>main → 1.4.1-ci.N · branch → 1.4.1-branch.N ·<br/>tag vX.Y.Z → clean X.Y.Z"]
    BT --- GV

    TAG["git tag vX.Y.Z on main"]:::rel --> GV
    TAG --> PI["./build.sh PushImages --registry host/repo<br/>22 images: 6 module APIs · 8 BFFs · gateway · 7 SPAs<br/>tagged with the GitVersion SemVer"]:::rel
    PI --> REG[("OCI registry<br/>(JFrog Artifactory)")]
    PI --> VAL["artifacts/images/values-images-env.yaml"]
    REG --> HELM["helm install -f values-images:<br/>platform → identity → context units"]:::rel
    VAL --> HELM
```

Static analysis is owned by the Aspire-hosted SonarQube (`tools/sonarqube/`); SonarAnalyzer + VS Threading analyzers run on every compile with warnings-as-errors. Code ownership and review routing are in [.github/CODEOWNERS](.github/CODEOWNERS); the disclosure policy is [SECURITY.md](SECURITY.md).

## 2.13 Running it

Local dev is the **Aspire AppHost** — the single entrypoint that brings up per-module Postgres, RabbitMQ, Valkey and Keycloak, runs all six module APIs + the eight BFFs + the Gateway + the seven Vite apps, and opens the Aspire dashboard:

```bash
dotnet run --project src/aspire/Dialysis.AppHost
# then browse http://localhost:9090
```

Tests run with `dotnet test Dialysis.slnx`. Persistence is **PostgreSQL everywhere** — integration and repository tests spin up PostgreSQL **Testcontainers**, so a running Docker daemon is required; the Transponder bus stays in-memory. A demo walkthrough lives in `e2e-demo/` (a Playwright film of one scripted patient journey against the live stack), and `tools/Dialysis.DataSimulator` keeps the seven SPAs populated with one coherent cross-module patient journey.

See [CLAUDE.md](CLAUDE.md) for the full build/run/test/deploy reference, the port matrix, and versioning/release mechanics.
