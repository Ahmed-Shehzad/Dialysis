# HIE — Architecture (low-level)

The **Health Information Exchange** module is the platform's outward-facing FHIR / IHE gateway. It owns three responsibilities:

1. **Outbound dispatch** — translate internal integration events into FHIR R4 resources and POST them to partner endpoints with retry, signing, and audit.
2. **Inbound ingestion** — accept FHIR pushes from partners, validate, and route to the owning module via integration events.
3. **Consent gating** — be the source of truth for cross-organization release-of-information decisions; other modules query HIE via `IsResourceAccessPermittedQuery` for the cross-cutting FHIR read facade.

Sub-modules: `Outbound`, `Inbound`, `Consent`, `OpenEhr`, `Xds` (IHE XDS Registry+Repository actors).

> Mermaid renders inline on GitHub/GitLab/JetBrains/VS Code; paste into <https://mermaid.live> if your viewer does not.

---

## 1. System architecture (component view)

```mermaid
flowchart LR
    subgraph "Internal modules (event sources/sinks)"
        EHRsvc["EHR"]
        PDMSsvc["PDMS"]
        HISsvc["HIS"]
        SCsvc["SmartConnect"]
    end

    subgraph "Dialysis.HIE.Api (ASP.NET host)"
        FhirCtl["FhirController<br/>POST /fhir/{Type} (ingest)"]
        XdsCtl["IHE XDS SOAP endpoints<br/>(ITI-18/41/43)"]
        Auth["JwtBearer + mTLS + TEFCA IAS"]
        Pipeline["Verifier + AuthorizationBehavior"]
        FhirCtl --> Pipeline
        XdsCtl --> Pipeline
        Auth --> Pipeline
    end

    subgraph "HIE sub-contexts"
        Out["HIE.Outbound<br/>Mappers, Dispatch, Partners"]
        In["HIE.Inbound<br/>InboundIngestionService"]
        Consent["HIE.Consent<br/>Policy aggregates + IsResourceAccessPermittedQuery"]
        OEHR["HIE.OpenEhr<br/>Composition mappers"]
        XDS["HIE.Xds<br/>Registry + Repository actors"]
    end

    subgraph "Cross-cutting building blocks"
        FhirCore["BuildingBlocks.Fhir.Core<br/>IFhirResourceMapper (TEvent, TResource)"]
        Audit["BuildingBlocks.Fhir.Audit<br/>(FHIR AuditEvent)"]
        Tefca["BuildingBlocks.Fhir.Tefca<br/>(IAS JWT, trust anchors)"]
        Validation["BuildingBlocks.Fhir.Validation<br/>(US Core)"]
    end

    subgraph "HIE.Persistence (HieDbContext)"
        Schemas[("hie_outbound (OutboundBundle, DispatchAttempt),<br/>hie_inbound (IngestionRecord),<br/>hie_consent (ConsentPolicy),<br/>hie_xds_registry (DocumentEntry),<br/>transponder (outbox/inbox)")]
    end

    subgraph "Transponder"
        Inbox["transponder.inbox<br/>(EHR/PDMS/HIS/SC event subscriptions)"]
        Outbox["transponder.outbox<br/>(FhirResourceDelivered/Failed)"]
    end

    subgraph "Partner endpoints"
        FHIRpart["External FHIR servers<br/>(payer / HIE / QHIN)"]
        XDSpart["IHE XDS Affinity Domains"]
    end

    EHRsvc -. PrescriptionOrdered / EncounterClosed .-> Inbox
    PDMSsvc -. IntradialyticAdverseEvent .-> Inbox
    HISsvc -. AppointmentBooked .-> Inbox
    SCsvc -. Hl7V2MessageTransformedToFhir .-> Inbox

    Inbox --> Out
    Out --> FhirCore
    Out --> Validation
    Out --> Tefca
    Out --> Audit
    Out --> Schemas
    Out -- HTTP POST FHIR --> FHIRpart
    Out --> Outbox
    Outbox -. FhirResourceDelivered/Failed .-> EHRsvc

    FHIRpart -- inbound POST /fhir/{Type} --> FhirCtl
    FhirCtl --> In
    In --> Validation
    In --> Consent
    In --> Schemas
    In --> Outbox

    Consent -. answers IsResourceAccessPermittedQuery .-> EHRsvc
    Consent -. answers IsResourceAccessPermittedQuery .-> HISsvc

    XDSpart <--> XdsCtl
    XdsCtl --> XDS
    XDS --> Schemas
```

**Invariants**

- `Dialysis.HIE.Outbound` is the only place that POSTs FHIR to partner endpoints — other modules never speak HTTP-FHIR directly.
- Mappers implement `IFhirResourceMapper<TEvent, TResource>` from `BuildingBlocks.Fhir.Core`; they are protocol-pure (no HTTP, no persistence).
- All partner traffic is recorded as a FHIR `AuditEvent` via `BuildingBlocks.Fhir.Audit` — pervasive, not opt-in.
- TEFCA partners get an IAS JWT in `Authorization: Bearer …` (or `X-User-Authorization-JWT` per QHIN profile, configurable) built by `ITefcaIdentityAssertionBuilder`.

---

## 2. Workflow — Outbound dispatch (event → FHIR Bundle → partner)

```mermaid
sequenceDiagram
    autonumber
    participant Src as EHR / PDMS / HIS / SC
    participant Bus as ITransponderBus
    participant IBX as HIE transponder.inbox
    participant Cons as Outbound Consumer
    participant Map as IFhirResourceMapper (TEvent, TResource)
    participant Val as ProfileEnforcement (US Core)
    participant Bnd as OutboundBundle aggregate
    participant Disp as PartnerDispatcher
    participant Tef as ITefcaIdentityAssertionBuilder
    participant Aud as IAuditEventEmitter
    participant Part as Partner FHIR endpoint

    Src->>Bus: Publish(PrescriptionOrderedIntegrationEvent v1)
    Bus->>IBX: persist (idempotent by msg id)
    IBX->>Cons: deliver
    Cons->>Map: Map(event) → MedicationRequest
    Map-->>Cons: Hl7.Fhir.Model.MedicationRequest
    Cons->>Val: Validate against US Core profile
    alt invalid (Strict)
        Val-->>Cons: OperationOutcome errors
        Cons->>Aud: AuditEvent (outcome=4 minor failure)
        Cons->>Bus: Publish(FhirResourceDeliveryFailedIntegrationEvent + OperationOutcome)
    else valid
        Cons->>Bnd: append resource to OutboundBundle
        Cons->>Disp: DispatchAsync(bundle, partnerPolicy)
        opt TEFCA partner
            Disp->>Tef: Build IAS JWT (purpose, treating org, …)
            Tef-->>Disp: signed JWT
        end
        Disp->>Part: POST /fhir Bundle (+ Authorization header)
        alt 2xx
            Part-->>Disp: 200 Bundle response
            Disp->>Aud: AuditEvent (outcome=0 success)
            Disp->>Bus: Publish(FhirResourceDeliveredIntegrationEvent)
        else transient (5xx, 429)
            Part-->>Disp: error
            Disp->>Disp: Polly exponential backoff (retry N)
        else permanent (4xx other)
            Part-->>Disp: error
            Disp->>Aud: AuditEvent (outcome=8 serious failure)
            Disp->>Bus: Publish(FhirResourceDeliveryFailedIntegrationEvent)
        end
    end
```

---

## 3. Workflow — Inbound ingestion (partner POST → routed to owning module)

```mermaid
sequenceDiagram
    autonumber
    participant Part as Partner FHIR client
    participant API as HIE.Api FhirController
    participant Tef as TefcaInboundMiddleware
    participant In as InboundIngestionService
    participant Val as ProfileEnforcement
    participant Cons as Consent gate (HIE.Consent)
    participant Aud as IAuditEventEmitter
    participant Ctx as HieDbContext
    participant Bus as ITransponderBus
    participant Mod as Owning module (EHR / HIS / PDMS)

    Part->>API: POST /fhir/{Type}<br/>+ Bearer IAS JWT (TEFCA) or SMART
    API->>Tef: validate IAS + trust anchor (X.509 chain)
    Tef-->>API: ok (or 401 OperationOutcome)
    API->>In: Ingest(resource, principal, purpose)
    In->>Val: validate against profile
    Val-->>In: outcome
    In->>Cons: IsResourceAccessPermitted(patient, type, purpose)
    Cons-->>In: permit / deny
    alt denied
        In->>Aud: AuditEvent (outcome=4)
        In-->>API: 403 OperationOutcome[forbidden]
    else permitted
        In->>Ctx: persist IngestionRecord + enqueue Hl7FhirResourceReceivedIntegrationEvent v1
        Ctx-->>In: committed
        In->>Aud: AuditEvent (success)
        In-->>API: 201 Created + Location
        Bus-->>Mod: deliver typed event (EHR ClinicalNotes ACL, …)
    end
```

---

## 4. Activity — OutboundBundle lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending: append mapped resource(s)
    Pending --> Dispatching: PartnerDispatcher picks up
    Dispatching --> Delivered: 2xx from partner<br/>FhirResourceDeliveredIntegrationEvent
    Dispatching --> Retrying: transient (5xx / 429)
    Retrying --> Dispatching: Polly backoff window elapsed
    Retrying --> DeadLettered: max attempts exceeded<br/>FhirResourceDeliveryFailedIntegrationEvent
    Dispatching --> Rejected: permanent 4xx (other than 429)<br/>FhirResourceDeliveryFailedIntegrationEvent
    Delivered --> [*]
    Rejected --> [*]
    DeadLettered --> [*]: ops-driven replay possible
```

**Note**: every state transition writes a `DispatchAttempt` row (latency, status code, partner, correlation id) and emits a FHIR `AuditEvent` — these are the substrate for the platform observability dashboards.

---

## 5. Activity — Consent decision (cross-module query)

```mermaid
flowchart TB
    Q["IsResourceAccessPermittedQuery<br/>(patient, type, requestor, purpose)"]
    Q --> Find["Load ConsentPolicy aggregates<br/>for patient + purpose"]
    Find --> Has{"Any active policy?"}
    Has -- "no" --> Default["Apply HIE default<br/>(deny unless purpose ∈ allowlist)"]
    Default --> Decide
    Has -- "yes" --> Scope{"Policy scope covers<br/>resourceType + requestor?"}
    Scope -- "no" --> Default
    Scope -- "yes" --> Window{"Within validity window<br/>and not revoked?"}
    Window -- "no" --> Deny
    Window -- "yes" --> Permit
    Decide --> Out["FhirConsentDecision { permit, reason }"]
    Deny["FhirConsentDecision.Deny(code, reason)"] --> Out
    Permit["FhirConsentDecision.Permit(obligations…)"] --> Out
    Out --> Aud["AuditEvent (consent decision)"]
```

---

## 6. Composition root

```mermaid
flowchart TB
    Program["Program.cs (Dialysis.HIE.Api)"]
    Program --> AddModuleHost["AddModuleHost of HiePermissionCatalog<br/>(ModuleSlug = 'hie')"]
    Program --> AddHIE["AddHealthInformationExchange(configuration, …)"]
    AddHIE --> Persistence["AddDbContext of HieDbContext"]
    AddHIE --> Bus["AddTransponder + AddHieIntegrationConsumers"]
    AddHIE --> OutSlice["AddOutbound()<br/>mappers, PartnerDispatcher, FhirHttpPartnerEndpoint"]
    AddHIE --> InSlice["AddInbound()<br/>FhirController, InboundIngestionService"]
    AddHIE --> ConsentSlice["AddConsent()<br/>policy aggregate + query handler"]
    AddHIE --> OEHRSlice["AddOpenEhr() (optional)"]
    AddHIE --> XDSSlice["AddXds() (optional)"]
    AddHIE --> FhirBB["AddFhir(…)<br/>Validation + Tefca + Audit"]
```

---

## 7. Data layout

```mermaid
erDiagram
    HieDbContext ||--o{ hie_outbound : "OutboundBundle, DispatchAttempt, PartnerEndpoint"
    HieDbContext ||--o{ hie_inbound : "IngestionRecord"
    HieDbContext ||--o{ hie_consent : "ConsentPolicy, ConsentObligation"
    HieDbContext ||--o{ hie_xds_registry : "DocumentEntry, SubmissionSet"
    HieDbContext ||--o{ hie_xds_repository : "BinaryStored (or storage URI)"
    HieDbContext ||--o{ transponder : "Outbox, Inbox, Sagas"
```

---

## 8. Cross-context contracts (DDD context map)

| Counterparty | Role | Vehicle |
|---|---|---|
| EHR / PDMS / HIS / SmartConnect | **Customer** of upstream modules' integration events; **Supplier** of `FhirResourceDelivered/Failed` and `Hl7FhirResourceReceived`. | `Dialysis.<Module>.Contracts` ↔ `Dialysis.HIE.Contracts` |
| External FHIR partners | **Open Host Service** (read-time + ingest); **Published Language** is FHIR R4 + US Core (CH Core planned). | HTTP FHIR + IHE XDS SOAP |
| TEFCA / QHIN partners | **Open Host Service** with IAS-assertion-validated identity. | TEFCA IAS JWT, mutual trust bundle |
| Identity | **Conformist**. | JWT + per-partner client credentials |

---

## 9. Where to look next

- Outbound mappers → `Dialysis.HIE.Outbound/Mappers/{Patient,Encounter,LabOrder,LabResult,AdverseEvent,DialysisSession,ClinicalNote}Mapper.cs`.
- Partner dispatch → `Dialysis.HIE.Outbound/Dispatch/` and `Dialysis.HIE.Outbound/Partners/Http/FhirHttpPartnerEndpoint.cs`.
- Inbound ingest → `Dialysis.HIE.Inbound/Ingestion/InboundIngestionService.cs` + `Controllers/FhirController.cs`.
- Consent → `Dialysis.HIE.Consent/` (aggregate + query handler).
- XDS actors → `Dialysis.HIE.Xds/` (Registry ITI-18/42, Repository ITI-41/43).
- openEHR bridge → `Dialysis.HIE.OpenEhr/` (archetype ↔ FHIR mappers).
