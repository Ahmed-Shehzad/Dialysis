# Dialysis PDMS – System Architecture

This document describes the system architecture, components, and data flows of the Dialysis Patient Data Management System (PDMS) including the Mirth Connect integration engine.

---

## 1. High-Level Architecture

```mermaid
flowchart TB
 subgraph External["External Systems"]
        Hospital["Hospital ADT / EHR"]
        Lab["Lab System"]
        DialysisMachine["Dialysis Machines"]
        IdP["Identity Provider<br>(OIDC/JWT)"]
        FHIRClient["FHIR Clients"]
  end
 subgraph Mirth["Mirth Connect (Integration Engine)"]
        MLLP["MLLP Listener<br>:2575"]
        SFTP["SFTP / File"]
        MirthTransform["Transform & Route"]
        MirthQueue["Queue & Retry"]
  end
 subgraph PDMS["Dialysis PDMS (.NET)"]
        Gateway["FhirCore.Gateway<br>:5000"]
        HIS["Dialysis.HisIntegration<br>:5001"]
        Device["Dialysis.DeviceIngestion<br>:5002"]
        Alerting["Dialysis.Alerting<br>:5003"]
        Audit["Dialysis.AuditConsent<br>:5004"]
        Identity["Dialysis.IdentityAdmission<br>:5005"]
        Subscriptions["FhirCore.Subscriptions<br>:5006"]
        Prediction["Dialysis.Prediction<br>(Background Worker)"]
  end
 subgraph DataStores["Data Stores"]
        FHIRStore["Azure FHIR Store<br>(or Health Data Services)"]
        Postgres[("PostgreSQL")]
        Redis[("Redis")]
        ServiceBus["Azure Service Bus"]
  end
    Hospital -- HL7 v2 MLLP --> MLLP
    Lab -- HL7 / SFTP --> SFTP
    DialysisMachine -- HL7 ORU / MLLP --> MLLP
    MLLP --> MirthTransform
    SFTP --> MirthTransform
    MirthTransform --> MirthQueue
    MirthQueue -- POST /api/v1/hl7/stream --> HIS
    MirthQueue -- POST /api/v1/adt/ingest --> HIS
    MirthQueue -- POST /api/v1/vitals/ingest --> Device
    MirthQueue -- POST /fhir/* --> Gateway
    IdP -. JWT .-> PDMS
    FHIRClient -- FHIR CRUD --> Gateway
    HIS --> Gateway & FHIRStore
    Identity --> Gateway & Postgres
    Device --> Gateway
    Gateway --> FHIRStore & ServiceBus
    ServiceBus --> Prediction & Alerting & Subscriptions
    Alerting --> Postgres & Redis
    Audit --> Postgres
    Subscriptions --> Postgres
```

---

## 2. Component Inventory

| Component                            | Type                       | Responsibility                                                                                                       |
| ------------------------------------ | -------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Mirth Connect**              | Integration Engine         | MLLP, SFTP, file connectivity; HL7 parsing; transform & route; queue, retry, dead-letter                             |
| **FhirCore.Gateway**           | Reverse Proxy + Validation | FHIR validation (IG profiles), YARP proxy to FHIR store, publishes `ResourceWrittenEvent` + `ObservationCreated` |
| **Dialysis.HisIntegration**    | Web API                    | ADT ingest (custom parser), HL7 streaming (Azure $convert-data), provenance, multi-tenant FHIR                       |
| **Dialysis.DeviceIngestion**   | Web API                    | Vitals ingest → FHIR Observations via Gateway                                                                       |
| **Dialysis.IdentityAdmission** | Web API                    | Patient admission, session scheduling → Patient/Encounter to FHIR                                                   |
| **Dialysis.Alerting**          | Web API + Consumer         | Create/acknowledge alerts; consumes `HypotensionRiskRaised` from Service Bus                                       |
| **Dialysis.AuditConsent**      | Web API                    | Audit event recording (ResourceType, Action, AgentId)                                                                |
| **Dialysis.Prediction**        | Background Worker          | Consumes `ObservationCreated`; risk scoring; publishes `HypotensionRiskRaised`                                   |
| **FhirCore.Subscriptions**     | Web API + Consumer         | CRUD for FHIR subscriptions; consumes `ResourceWrittenEvent`; criteria match → webhook                            |

---

## 3. ADT / HIS Integration Flow

Hospital ADT, lab, or upstream systems send HL7 v2 messages. Mirth receives them, transforms (optional), and forwards to HIS Integration.

```mermaid
flowchart LR
    subgraph Source["Source"]
        ADT["Hospital ADT<br/>HL7 v2"]
    end

    subgraph Mirth["Mirth Connect"]
        MLLP1["MLLP Listener"]
        T1["Parse / Filter"]
        D1["HTTP Sender"]
    end

    subgraph HIS["HIS Integration"]
        HL7Stream["/api/v1/hl7/stream"]
        AdtIngest["/api/v1/adt/ingest"]
        Parser["AdtMessageParser"]
        AzureConvert["Azure $convert-data"]
        FhirWriter["FhirAdtWriter"]
        Provenance["ProvenanceRecorder"]
    end

    subgraph Gateway["FhirCore.Gateway"]
        Proxy["YARP Proxy"]
    end

    subgraph Store["FHIR Store"]
        FHIR[(Patient, Encounter)]
    end

    ADT -->|TCP MLLP| MLLP1
    MLLP1 --> T1
    T1 -->|ADT_A01 etc| D1
    D1 -->|POST rawMessage + messageType| HL7Stream
    D1 -->|POST legacy| AdtIngest

    HL7Stream --> AzureConvert
    AzureConvert -->|Bundle| Proxy
    Proxy --> FHIR
    AzureConvert --> Provenance
    Provenance --> FHIR

    AdtIngest --> Parser
    Parser --> FhirWriter
    FhirWriter --> Proxy
```

---

## 4. Vitals / Device Ingestion Flow

Dialysis machines send vitals (HL7 ORU or proprietary). Mirth maps to `IngestVitalsCommand` or forwards FHIR directly to the Gateway.

```mermaid
flowchart LR
    subgraph Source["Source"]
        Device["Dialysis Machine<br/>HL7 ORU / FHIR"]
    end

    subgraph Mirth["Mirth Connect"]
        MLLP2["MLLP / File"]
        T2["Map OBX → JSON"]
        D2["HTTP Sender"]
    end

    subgraph DeviceIngestion["Device Ingestion"]
        VitalsAPI["POST /api/v1/vitals/ingest"]
        Handler["IngestVitalsHandler"]
        ObsWriter["FhirObservationWriter"]
    end

    subgraph Gateway["FhirCore.Gateway"]
        Validate["Profile Validation"]
        Proxy2["Proxy to FHIR"]
        Publish["Publish ObservationCreated<br/>+ ResourceWrittenEvent"]
    end

    subgraph Store["FHIR Store"]
        Obs[(Observation)]
    end

    subgraph Bus["Service Bus"]
        ObsTopic["observation-created"]
        ResTopic["resource-written"]
    end

    Device -->|ORU^R01| MLLP2
    MLLP2 --> T2
    T2 -->|PatientId, EncounterId, Readings| D2
    D2 --> VitalsAPI

    VitalsAPI --> Handler
    Handler --> ObsWriter
    ObsWriter -->|POST /fhir/Observation| Validate
    Validate --> Proxy2
    Proxy2 --> Obs
    Validate --> Publish
    Publish --> ObsTopic
    Publish --> ResTopic
```

---

## 5. Prediction & Alerting Flow

Observations written through the Gateway trigger hypotension risk prediction. High-risk scores produce alerts.

```mermaid
flowchart TB
    subgraph Gateway["FhirCore.Gateway"]
        Write["POST /fhir/Observation"]
        Middleware["FhirValidationMiddleware"]
    end

    subgraph ServiceBus["Azure Service Bus"]
        ObsTopic["observation-created<br/>topic"]
        RiskTopic["hypotension-risk-raised<br/>topic"]
    end

    subgraph Prediction["Dialysis.Prediction"]
        Worker["PredictionWorker"]
        Handler["ObservationCreatedHandler"]
        Cache["VitalHistoryCache"]
        Scorer["EnhancedRiskScorer"]
        Publisher["HypotensionRiskPublisher"]
    end

    subgraph Alerting["Dialysis.Alerting"]
        Consumer["HypotensionRiskConsumer"]
        CreateAlert["CreateAlertHandler"]
        AlertDB[(PostgreSQL<br/>Alerts)]
    end

    Write --> Middleware
    Middleware -->|Publish| ObsTopic
    ObsTopic -->|Consume| Worker
    Worker --> Handler
    Handler --> Cache
    Handler --> Scorer
    Scorer -->|Risk ≥ 0.6?| Publisher
    Publisher --> RiskTopic
    RiskTopic --> Consumer
    Consumer --> CreateAlert
    CreateAlert --> AlertDB
```

---

## 6. FHIR Subscription & Webhook Flow

FHIR resource writes are broadcast. Subscriptions service matches criteria and notifies registered webhooks.

```mermaid
flowchart LR
    subgraph Gateway["FhirCore.Gateway"]
        AnyWrite["POST /fhir/*"]
        Middleware2["FhirValidationMiddleware"]
    end

    subgraph ServiceBus["Azure Service Bus"]
        ResTopic["resource-written<br/>topic"]
    end

    subgraph Subscriptions["FhirCore.Subscriptions"]
        Consumer2["ResourceWrittenConsumer"]
        Store[(Subscription Store<br/>PostgreSQL)]
        Matcher["CriteriaMatcher"]
        Notifier["WebhookNotifier"]
    end

    subgraph Webhooks["External"]
        Webhook1["Webhook Endpoint 1"]
        Webhook2["Webhook Endpoint 2"]
    end

    AnyWrite --> Middleware2
    Middleware2 -->|Publish ResourceWrittenEvent| ResTopic
    ResTopic -->|Consume| Consumer2
    Consumer2 --> Store
    Consumer2 --> Matcher
    Matcher -->|Match?| Notifier
    Notifier -->|HTTP POST| Webhook1
    Notifier -->|HTTP POST| Webhook2
```

---

## 7. HL7 Streaming via Service Bus

HL7 messages can be sent directly to Service Bus (bypassing Mirth) for async processing.

```mermaid
flowchart LR
    subgraph Producer["Producer"]
        Ext["External System / Mirth"]
    end

    subgraph ServiceBus["Azure Service Bus"]
        HL7Topic["hl7-ingest<br/>topic"]
        Sub["his-subscription"]
    end

    subgraph HIS["HIS Integration"]
        Hl7Consumer["Hl7StreamConsumer"]
        Handler2["Hl7StreamIngestHandler"]
        Writer2["AzureHl7StreamingWriter"]
    end

    subgraph Azure["Azure Healthcare API"]
        Convert["$convert-data"]
    end

    subgraph FHIRStore["FHIR Store"]
        Resources[(Patient, Encounter, etc.)]
    end

    Ext -->|JSON: rawMessage, messageType, tenantId| HL7Topic
    HL7Topic --> Sub
    Sub --> Hl7Consumer
    Hl7Consumer --> Handler2
    Handler2 --> Writer2
    Writer2 --> Convert
    Convert --> Resources
```

---

## 8. Identity & Admission Flow

Client applications admit patients and schedule sessions. Identity Admission creates Patient and Encounter resources in FHIR.

```mermaid
flowchart LR
    subgraph Client["Client"]
        App["Frontend / API Client"]
    end

    subgraph Identity["Dialysis.IdentityAdmission"]
        AdmitAPI["POST /api/v1/patients/admit"]
        SessionAPI["POST /api/v1/sessions"]
        AdmitHandler["AdmitPatientHandler"]
        SessionHandler["CreateSessionHandler"]
        FhirWriter["IFhirIdentityWriter"]
    end

    subgraph Gateway["FhirCore.Gateway"]
        Proxy3["YARP Proxy"]
    end

    subgraph FHIRStore["FHIR Store"]
        Patient[(Patient)]
        Encounter[(Encounter)]
    end

    App -->|AdmitPatientCommand| AdmitAPI
    App -->|CreateSessionCommand| SessionAPI
    AdmitAPI --> AdmitHandler
    SessionAPI --> SessionHandler
    AdmitHandler --> FhirWriter
    SessionHandler --> FhirWriter
    FhirWriter --> Proxy3
    Proxy3 --> Patient
    Proxy3 --> Encounter
```

---

## 9. Service Bus Topics Overview

```mermaid
flowchart TB
    subgraph Publishers["Publishers"]
        GW["FhirCore.Gateway"]
        Pred["Dialysis.Prediction"]
    end

    subgraph Topics["Azure Service Bus Topics"]
        T1["observation-created"]
        T2["hypotension-risk-raised"]
        T3["resource-written"]
        T4["hl7-ingest"]
    end

    subgraph Consumers["Consumers"]
        C1["Dialysis.Prediction<br/>prediction-subscription"]
        C2["Dialysis.Alerting<br/>alerting-subscription"]
        C3["FhirCore.Subscriptions<br/>subscriptions-subscription"]
        C4["Dialysis.HisIntegration<br/>his-subscription"]
    end

    GW --> T1
    GW --> T3
    Pred --> T2

    T1 --> C1
    T2 --> C2
    T3 --> C3
    T4 --> C4
```

---

## 10. Data Stores by Service

| Service                    | Database                                    | Purpose                    |
| -------------------------- | ------------------------------------------- | -------------------------- |
| Dialysis.Alerting          | PostgreSQL (`dialysis_alerting_{tenant}`) | Alerts                     |
| Dialysis.AuditConsent      | PostgreSQL (`dialysis_audit_{tenant}`)    | Audit events               |
| Dialysis.IdentityAdmission | —                                          | Uses FHIR only             |
| FhirCore.Subscriptions     | PostgreSQL (`fhir_subscriptions`)         | Subscription CRUD          |
| Mirth Connect              | PostgreSQL (`mirthdb`)                    | Channels, messages, config |

---

## Related Documentation

- [DEPLOYMENT.md](DEPLOYMENT.md) – Configuration, Docker Compose, security
- [MIRTH-INTEGRATION.md](MIRTH-INTEGRATION.md) – Mirth channel setup, PDMS endpoints
