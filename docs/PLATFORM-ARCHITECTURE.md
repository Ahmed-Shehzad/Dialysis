# Dialysis Platform — Concrete System Architecture Plan

A concrete system architecture plan for a dialysis-focused platform in .NET. Assumes an **internal platform** (not replacing an EHR), with FHIR integration.

---

## 1. Logical Architecture (High Level)

### Channels

| Channel | Purpose |
|---------|---------|
| **Web UI** | Nurses, clinicians — session documentation, vitals review, alerts |
| **Admin UI** | Facility ops — config, user management, reports |
| **Integration endpoints** | FHIR + HL7 v2 adapter — inbound/outbound interoperability |
| **Device ingestion** | Dialysis machine data — HL7 ORU, vitals, alarms |

### Core Services

| Service | Responsibility |
|---------|----------------|
| **1. Dialysis Workflow Service** | Session lifecycle, orders, documentation, events |
| **2. Clinical Data Service** | Observations, meds, problems — internal canonical model |
| **3. FHIR Gateway** | Outbound: publish to EHR/partner FHIR server; Inbound: consume Patient/Allergies/Meds/etc. |
| **4. Integration Service** | HL7 v2 adapters, mapping, queues, retries, auditing |
| **5. Identity & Consent** | User authN/authZ, patient matching, consent rules |
| **6. Reporting / Analytics** | Quality measures, dashboards |

### Data

| Layer | Technology |
|-------|------------|
| **OLTP** | PostgreSQL / SQL Server — app data |
| **Message broker** | Azure Service Bus / RabbitMQ / Kafka — async integration |
| **Audit log** | Immutable-ish store — compliance, audit trail |

---

## 2. Minimum Viable Technical Stack (.NET)

| Component | Choice |
|-----------|--------|
| **Web API** | ASP.NET Core (controllers / minimal APIs) |
| **FHIR** | Firely .NET SDK — resources, parsing, validation |
| **Auth** | OAuth/OIDC; SMART patterns if embedding in EHR |
| **Persistence** | EF Core |
| **Async processing** | Background workers (Hosted Services) for queue consumers & scheduled tasks |
| **Observability** | OpenTelemetry for tracing + structured logs — healthcare integrations need observability |
| **Sagas** | **Transponder Sagas with orchestration** — long-running workflows, compensation |

---

## 3. Integration Patterns

### Canonical model inside, adapters at the edges

> Avoid letting one external FHIR server's quirks dictate your internal domain.

- Internal domain: `Session`, `Observation`, `Patient`, `EpisodeOfCare`, etc.
- FHIR adapters: map domain → FHIR resources at outbound boundaries
- HL7 adapters: map segments → domain at inbound boundaries

### Event-driven outbox pattern

When a dialysis session completes:

1. Write to DB (transactional)
2. Publish integration event reliably (e.g. via outbox or broker)

Ensures at-least-once delivery without losing events on failure.

### Idempotency + replay

Healthcare integrations fail often; safe retries are essential.

- **Idempotency keys** — e.g. MSH-10 for HL7; correlation IDs for FHIR
- **Retry queues** — failed messages → retry with backoff
- **DLQ** — dead-letter for manual inspection

**Dialysis implementation:** `ProcessedHl7MessageStore`, `FailedHl7MessageStore`, MSH-10 idempotency in `ProcessHl7StreamHandler`.

---

## 4. Transponder Sagas (Orchestration)

**Decision:** Use **Transponder Sagas with orchestration** for long-running, multi-step workflows.

### When to use

- **Session completion workflow** — Complete session → persist → publish Procedure → notify EHR → audit
- **Patient sync** — ADT received → match/create patient → update encounters → propagate
- **Order fulfillment** — Order placed → validate → route → document → charge

### Orchestration vs choreography

| Style | Use when |
|-------|----------|
| **Orchestration** | Central coordinator; explicit steps and compensation; easier to reason about |
| **Choreography** | Decoupled; each participant reacts to events; more distributed |

**Platform choice:** Orchestration — `TransponderTransportRegistrationOptions.UseSagaOrchestration(...)`.

### Transponder saga components

- `ISagaState` — persistent state per saga instance
- `ISagaMessageHandler<TState, TMessage>` — handler for saga messages
- `ISagaStepProvider` — steps + compensation
- `ISagaRepository<TState>` — EF Core or InMemory
- `SagaEndpointBuilder` — `StartWith<TMessage>`, `Handle<TMessage>`

### Example (conceptual)

```csharp
// Session completion saga: CompleteSessionCommand → persist → publish Procedure → audit
opt.TransportBuilder.UseSagaOrchestration(b => b.AddSaga<SessionCompletionSaga, SessionCompletionState>(e =>
{
    e.StartWith<CompleteSessionCommand>(new Uri("queue:session-complete"));
    e.Handle<ProcedurePublished>(new Uri("queue:procedure-published"));
}));
```

---

## 5. Mapping to Current Dialysis PDMS

| Architecture component | Current implementation |
|------------------------|-------------------------|
| Dialysis Workflow Service | `Session` aggregate, `SessionsController`, `StartSession` / `CompleteSession` |
| Clinical Data Service | `Observation`, `Patient`, `EpisodeOfCare`, `Condition`, `VascularAccess` |
| FHIR Gateway | `FhirPatientController`, `FhirObservationController`, `FhirProcedureController`, `PushToEhrCommandHandler` |
| Integration Service | `ProcessHl7StreamHandler`, HL7/stream endpoint, Mirth config |
| Identity & Consent | `IPatientIdentifierResolver`, `IIdMappingRepository`, `TenantContext`, audit APIs |
| Reporting / Analytics | `QualityBundleService`, cohort queries, adequacy |
| Message broker | Transponder + Azure Service Bus (event export) |
| Sagas | Transponder available; session completion uses event choreography (SessionCompleted → procedure push, audit) |

---

## 6. References

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) — Diagrams, implementation status
- [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md) — Phased build plan
- [LEARNING-PATH.md](LEARNING-PATH.md) — Onboarding path
- Transponder: `UseSagaOrchestration`, `SagaEndpointBuilder`, `ISagaMessageHandler`
