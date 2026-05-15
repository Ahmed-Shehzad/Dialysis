# HIS — Architecture (low-level)

Companion to [README.md](README.md) and [his_ddd_modular_plan.md](his_ddd_modular_plan.md). This file documents the **internal** anatomy of the Hospital Information System module: how the slices wire together, how a request flows through the host, and the state shape of the long-running aggregates.

> All diagrams are [Mermaid](https://mermaid.js.org/). GitHub, GitLab, JetBrains, and most modern Markdown previews render them inline; if a diagram does not render in your viewer, paste the fenced block into <https://mermaid.live>.

---

## 1. System architecture (component view)

The HIS host is a single ASP.NET process that composes one vertical slice per RA sub-context. Each slice owns its domain, its handlers, and its ports; persistence is unified in `HisDbContext` with **schema-per-context** table naming. Cross-slice references are forbidden — every cross-slice need traverses `Dialysis.HIS.Contracts`.

```mermaid
flowchart LR
    subgraph "Edge / Ingress"
        Browser["SPA / Patient Portal"]
        Mobile["Mobile / Device clients"]
        Partner["Lab + Pharmacy partners (HTTP)"]
    end

    subgraph "Dialysis.HIS.Api (ASP.NET host, port 5288)"
        APIv1["api/v1.0/* controllers<br/>HATEOAS ResourceEnvelope (T)"]
        OpenAPI["/openapi/v1.json<br/>Asp.Versioning + ApiExplorer"]
        Health["/health, /health/ready"]
        Auth["JwtBearer + ICurrentUser<br/>(His:Authentication:*)"]
        APIv1 --> Pipeline
        Auth --> Pipeline
        subgraph "Request pipeline (CQRS + Verifier + Authorization)"
            Verifier["Verifier (validation)"]
            AuthZ["AuthorizationPipelineBehavior<br/>(HisPermissions)"]
            Handler["Slice command/query handler"]
            Verifier --> AuthZ --> Handler
        end
    end

    subgraph "Bounded sub-contexts (project per slice)"
        Security["HIS.Security<br/>users, roles, audit"]
        PatientFlow["HIS.PatientFlow<br/>Patient, ADT, Referral"]
        Scheduling["HIS.Scheduling<br/>Appointment, Resource"]
        Medication["HIS.Medication<br/>Order, Administration"]
        Operations["HIS.Operations<br/>Staff, Inventory, BillingExportJob"]
        DataSvc["HIS.DataServices<br/>Import, Search, Dashboard"]
        Portal["HIS.PatientAccess<br/>Portal reads + consent gate"]
        Integration["HIS.Integration<br/>Lab/Pharmacy gateways, Device ingest"]
        RaCaps["HIS.RaCapabilities<br/>RA reference reads/writes (schema his_ra)"]
        Handler --> Security
        Handler --> PatientFlow
        Handler --> Scheduling
        Handler --> Medication
        Handler --> Operations
        Handler --> DataSvc
        Handler --> Portal
        Handler --> Integration
        Handler --> RaCaps
    end

    subgraph "HIS.Persistence (HisDbContext, schema-per-slice)"
        Schemas[("his_security, his_patientflow,<br/>his_scheduling, his_medication,<br/>his_operations, his_data, his_integration,<br/>his_patient_access, his_ra,<br/>transponder (outbox/inbox)")]
        Audit[("AuditTrail")]
    end

    subgraph "Transponder (in-proc bus or RabbitMQ)"
        Outbox["Transactional outbox<br/>(schema transponder, on HisDbContext)"]
        Relay["OutboxRelay HostedService<br/>His:Transponder:EnableOutboxRelay"]
        Bus["ITransponderBus"]
        Consumers["AddHisIntegrationConsumers<br/>(EHR/PDMS/Pharmacy stubs)"]
        Outbox --> Relay --> Bus --> Consumers
    end

    subgraph "External systems"
        Keycloak["Keycloak (OIDC)<br/>via Identity.Bff"]
        Lab["ILaboratoryGateway<br/>(stub or HTTP)"]
        Pharmacy["IPharmacyGateway<br/>(stub or HTTP)"]
        EHRsvc["EHR module"]
        PDMSsvc["PDMS module"]
    end

    Browser --> APIv1
    Mobile --> APIv1
    Partner --> APIv1
    Auth -. validates JWT .-> Keycloak

    Handler --> Schemas
    Handler -- enqueue events --> Outbox
    Integration --> Lab
    Integration --> Pharmacy
    Bus -. integration events .-> EHRsvc
    Bus -. integration events .-> PDMSsvc
    EHRsvc -. consumed via ACL .-> PatientFlow
    PDMSsvc -. consumed via ACL .-> Handler
```

**Key invariants**

- The only assembly other modules may reference from HIS is `Dialysis.HIS.Contracts` — enforced by [ModuleBoundaryTests](../../tests/Dialysis.ArchitectureTests/ModuleBoundaryTests.cs).
- Cross-slice domain references are forbidden inside HIS — enforced by `BoundedContextReferenceTests`.
- Aggregate roots have **no public setters** — enforced by `AggregateRootEncapsulationTests`.
- Integration events all declare `int SchemaVersion` — enforced by event-versioning gate.

---

## 2. Workflow — Book Appointment (representative command path)

This is the canonical write workflow. It exercises auth, validation, aggregate invariants, outbox enqueue, and downstream event delivery.

```mermaid
sequenceDiagram
    autonumber
    participant Client as SPA / Caller
    participant API as HIS.Api Controller
    participant Auth as JwtBearer + ICurrentUser
    participant Med as Intercessor
    participant Val as Verifier (validator)
    participant AZ as AuthorizationBehavior
    participant H as BookAppointmentHandler
    participant Repo as IAppointmentRepository
    participant Ctx as HisDbContext
    participant OBX as Transponder Outbox
    participant Relay as Outbox Relay
    participant Bus as ITransponderBus
    participant Down as EHR / PDMS consumers

    Client->>API: POST /api/v1.0/scheduling/appointments
    API->>Auth: validate Bearer JWT
    Auth-->>API: ClaimsPrincipal + HisPermissions
    API->>Med: Send(BookAppointmentCommand)
    Med->>Val: Validate(command)
    alt invalid
        Val-->>Med: ValidationProblem
        Med-->>API: 400 ProblemDetails (HATEOAS-wrapped error)
    else valid
        Val-->>Med: ok
        Med->>AZ: require his.scheduling.write
        alt missing permission
            AZ-->>Med: 403
            Med-->>API: 403 ProblemDetails
        else permitted
            AZ-->>Med: ok
            Med->>H: Handle
            H->>Repo: load resource + overlap check
            Repo->>Ctx: query his_scheduling
            Ctx-->>Repo: candidate appointments
            H->>H: enforce SingleResourceOverlap rule
            H->>Repo: Add(Appointment)
            H->>Ctx: enqueue AppointmentBookedIntegrationEvent<br/>(transponder.outbox)
            H->>Ctx: SaveChangesAsync (single UoW)
            Ctx-->>H: committed
            H-->>Med: AppointmentDto
            Med-->>API: result
            API-->>Client: 201 { data, links } (HATEOAS)
        end
    end

    Note over Relay,Bus: out-of-band, async
    Relay->>OBX: poll unpublished rows
    OBX-->>Relay: AppointmentBookedIntegrationEvent
    Relay->>Bus: Publish
    Bus-->>Down: deliver to EHR.Scheduling / PDMS consumers
    Down-->>OBX: ack → mark row published
```

**Why this shape**

- The outbox row and the aggregate change commit in the **same** EF transaction, so a successful 201 implies the event will eventually publish.
- The relay runs as an `IHostedService` only when `His:Transponder:EnableOutboxRelay = true` — dev hosts can drop the relay and still serve writes.
- Consumers across the bus boundary never load HIS aggregates; they apply ACL translators (see `Dialysis.HIS.Integration`).

---

## 3. Activity — Patient Flow lifecycle (Register → Admit → Discharge)

```mermaid
stateDiagram-v2
    [*] --> Registered: RegisterPatient (his.patientflow.write)
    Registered --> Admitted: AdmitPatient<br/>publishes PatientAdmittedIntegrationEvent
    Admitted --> UnderTreatment: scheduling + medication slices act
    UnderTreatment --> Admitted: continue stay
    Admitted --> Discharged: DischargePatient<br/>publishes PatientDischargedIntegrationEvent
    Discharged --> [*]

    Registered --> PortalConsentBootstrapped: RuleBasedPatientConsentGate seeds PortalConsentPreference
    PortalConsentBootstrapped --> Registered
    Registered --> ReferralOpen: CreateReferral<br/>publishes ReferralCreatedIntegrationEvent
    ReferralOpen --> Registered: lab ACK / completion

    state UnderTreatment {
        [*] --> Scheduled: BookAppointment
        Scheduled --> Ordered: PlaceMedicationOrder<br/>(IMedicationOrderSafetyPolicy)
        Ordered --> Administered: RecordAdministration
        Administered --> Ordered: next dose
        Ordered --> Discontinued: DiscontinueOrder<br/>publishes MedicationOrderDiscontinuedIntegrationEvent
        Discontinued --> [*]
    }
```

**Notes**

- `RegisterPatient` bootstraps a `PortalConsentPreference` row when `His:PatientAccess:RequireExplicitConsentRowForPortal` is true; otherwise the rule-based gate evaluates implicit consent.
- The medication sub-machine is guarded by `IMedicationOrderSafetyPolicy` (formulary check) at the `Ordered` transition.
- Every transition writes an `IAuditTrail` entry (separate `SaveChanges` per the README's compromise — promote to single UoW when warranted).

---

## 4. Composition root (how slices register)

```mermaid
flowchart TB
    Program["Program.cs (Dialysis.HIS.Api)"]
    Program --> AddModuleHost["AddModuleHost of HisPermissionCatalog<br/>(ModuleSlug = 'his')"]
    Program --> AddHIS["AddHospitalInformationSystem(configuration, …)"]
    AddHIS --> Persistence["AddDbContext of HisDbContext<br/>EF in-memory by default<br/>or SqlServer when configurePersistence"]
    AddHIS --> Bus["AddTransponder + module consumers"]
    AddHIS --> Slices["per-slice AddX() extensions<br/>(handlers, ports, validators)"]
    AddHIS --> Auth["AddAuthentication + AddAuthorization<br/>RolePermissionMap"]
    AddHIS --> Hosted["IHostedService: HisDatabaseInitializer<br/>+ optional outbox relay"]
    AddHIS --> RA["AddRaCapabilities (schema his_ra)"]
```

**Configuration touch-points** are listed in [README.md §Configuration keys](README.md#composition-host-registration). The single rule: every flag is `His:*` scoped and bound by `IOptions<T>` inside the slice that owns it.

---

## 5. Data layout

```mermaid
erDiagram
    HisDbContext ||--o{ his_security : "Users, Roles, Audit"
    HisDbContext ||--o{ his_patientflow : "Patient, Referral, Admission, Discharge"
    HisDbContext ||--o{ his_scheduling : "Appointment, SchedulingResource, Waitlist"
    HisDbContext ||--o{ his_medication : "MedicationOrder, Administration"
    HisDbContext ||--o{ his_operations : "StaffMember, InventoryItem, BillingExportJob"
    HisDbContext ||--o{ his_data : "DataImportJob, Manager dashboard views"
    HisDbContext ||--o{ his_integration : "DeviceReadingRecord, idempotency"
    HisDbContext ||--o{ his_patient_access : "PortalConsentPreference"
    HisDbContext ||--o{ his_ra : "Ra* reference rows (Tummers 2021)"
    HisDbContext ||--o{ transponder : "Outbox, Inbox, Sagas"
```

Migrations history: `his_migrations` table (separate from `transponder.__ef_migrations`). The Transponder outbox/inbox lives on the **same** `DbContext` — there are no duplicate HIS-side outbox tables.

---

## 6. Where to look next

- Slice handlers → `Dialysis.HIS.<Slice>/Features/**`.
- Aggregate roots → `Dialysis.HIS.<Slice>/Domain/**` (no public setters; mutation via behaviour methods).
- Integration event contracts → `Dialysis.HIS.Contracts/IntegrationEvents/**`.
- RA capability traceability → [his_ra_submodules.md](his_ra_submodules.md).
- End-to-end outbox proof → [`.github/workflows/his-ci.yml`](../../../.github/workflows/his-ci.yml) + [his_transponder_e2e_runbook.md](his_transponder_e2e_runbook.md).
