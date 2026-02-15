# DDD Architecture with BuildingBlocks and Transponder

This document describes how to shape Dialysis microservices using **Domain Driven Design** principles, with **BuildingBlocks** for domain abstractions and **Transponder** for distributed messaging.

---

## 1. Overview

| Layer | Responsibility | Libraries |
|-------|----------------|-----------|
| **Domain** | Aggregates, entities, value objects, domain events | BuildingBlocks |
| **Application** | Use cases, integration events, handlers | Intercessor (MediatR), BuildingBlocks |
| **Infrastructure** | Messaging, persistence, external services | Transponder, EF Core |
| **Presentation** | Controllers, middleware | ASP.NET Core |

---

## 2. Bounded Contexts

| Bounded Context | Services | Domain Focus |
|-----------------|----------|--------------|
| **FHIR Gateway** | FhirCore.Gateway | Validation, proxying, publishing integration events |
| **Prediction** | Dialysis.Prediction | Risk scoring, vital history, hypotension alerts |
| **Alerting** | Dialysis.Alerting | Alert lifecycle, notifications |
| **Subscriptions** | FhirCore.Subscriptions | FHIR subscription management, webhooks |
| **HIS Integration** | Dialysis.HisIntegration | ADT/HL7 ingest, FHIR mapping |
| **Device Ingestion** | Dialysis.DeviceIngestion | Vitals ingest, observations |
| **Identity & Admission** | Dialysis.IdentityAdmission | Patient admission, sessions |
| **Audit & Consent** | Dialysis.AuditConsent | Audit events, consent |

---

## 3. BuildingBlocks Usage

### 3.1 Domain Events vs Integration Events

- **IDomainEvent** – Events within a bounded context, handled in-process via MediatR.
- **IIntegrationEvent** – Events across bounded contexts, published via Transponder.

```csharp
// Domain event (same bounded context)
public sealed record PatientAdmitted(Ulid CorrelationId, string PatientId) : DomainEvent;

// Integration event (cross-bounded context)
public sealed record ObservationCreated(
    Ulid CorrelationId, string? TenantId, string ObservationId, ...)
    : IntegrationEvent(CorrelationId), INotification;
```

### 3.2 Aggregate Roots

Use `AggregateRoot` from BuildingBlocks when your bounded context has rich domain logic:

```csharp
public sealed class Alert : AggregateRoot
{
    public string PatientId { get; private set; }
    public string Severity { get; private set; }
    public AlertStatus Status { get; private set; }

    public static Alert Create(string patientId, string severity, string message)
    {
        var alert = new Alert { PatientId = patientId, Severity = severity, ... };
        alert.ApplyEvent(new AlertCreated(...));  // Domain event
        alert.ApplyEvent(new AlertCreatedIntegrationEvent(...));  // Integration event if needed
        return alert;
    }
}
```

### 3.3 Value Objects and Enumerations

Use BuildingBlocks `Enumeration<T>` for domain enumerations:

```csharp
public sealed class AlertStatus : Enumeration<AlertStatus>
{
    public static readonly AlertStatus Pending = new(1, "Pending");
    public static readonly AlertStatus Acknowledged = new(2, "Acknowledged");
}
```

---

## 4. Transponder for Distributed Messaging

### 4.1 Event Contract Requirements

Integration events **must** extend `IntegrationEvent` and implement `IIntegrationEvent` (which inherits `ICorrelatedMessage` from Transponder):

- `CorrelationId` – Chains related messages for tracing
- `EventId` – Unique per event
- `OccurredOn` – Timestamp

### 4.2 Publishing (Producer)

```csharp
// Inject IPublishEndpoint
public class MyHandler
{
    private readonly IPublishEndpoint _publish;

    public async Task HandleAsync(...)
    {
        var evt = new ObservationCreated(
            Ulid.NewUlid(),
            tenantId, observationId, patientId, encounterId, code, value, effective, deviceId);
        await _publish.PublishAsync(evt, cancellationToken);
    }
}
```

### 4.3 Consuming (Consumer)

Configure receive endpoints that subscribe to topics. Each consumer registers a handler that processes deserialized messages.

### 4.4 Transport Configuration

Use `Transponder.Transports.AzureServiceBus` for Azure Service Bus. Topic names default to message type name (e.g. `ObservationCreated`). A custom `IAzureServiceBusTopology` can map to existing kebab-case topics (`observation-created`).

---

## 5. Service Structure (Vertical Slice)

Each microservice should follow a vertical-slice structure:

```
Dialysis.Alerting/
├── Domain/           # Aggregates, value objects, domain events (if any)
├── Application/
│   ├── Features/     # Use cases (Commands, Queries, Handlers)
│   └── Messaging/    # Integration event handlers (Transponder consumers)
├── Infrastructure/   # DbContext, repositories, external clients
├── Controllers/
└── Program.cs
```

### 5.1 Application Layer

- **Commands** – `IRequest<T>`, handled by `IRequestHandler<TRequest, TResponse>`
- **Queries** – Same pattern as commands
- **Notifications** – `INotification`, handled by `INotificationHandler<T>`
- **Integration event handlers** – Consume from Transponder receive endpoints

### 5.2 Dependency Flow

```
Controllers → Application (Handlers) → Domain
                    ↓
            Infrastructure (Transponder, EF)
```

---

## 6. Correlation and Tracing

1. **Gateway** creates integration events with `CorrelationId = Ulid.NewUlid()` (or from request headers if available).
2. **Downstream handlers** preserve `CorrelationId` when publishing chained events (e.g. `HypotensionRiskRaised` uses `ObservationCreated.CorrelationId`).
3. Transponder and OpenTelemetry propagate correlation for distributed tracing.

---

## 7. Migration Path

| Phase | Action |
|-------|--------|
| **Done** | Events extend `IntegrationEvent`; `CorrelationId` preserved across chain |
| **Done** | FhirCore.Gateway uses Transponder for publishing `ObservationCreated` and `ResourceWrittenEvent` |
| **Next** | Replace raw Azure Service Bus with Transponder in Prediction, Alerting, Subscriptions (consumers) |
| **Later** | Introduce aggregates where domain logic warrants (e.g. Alert in Alerting) |
| **Later** | Optionally add Outbox pattern via Transponder for at-least-once delivery |

## 8. Dialysis.Messaging

The `Dialysis.Messaging` project provides:

- **AddDialysisTransponder()** – Registers Transponder with Azure Service Bus using `MappingAzureServiceBusTopology` (from Transponder) to map message types to kebab-case topics (`observation-created`, `hypotension-risk-raised`, `resource-written`, etc.); uses no-op when connection string is empty (local dev)
- **TransponderHostedService** – Starts/stops the bus with the application lifecycle

---

## Related

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) – System overview
- [DEPLOYMENT.md](DEPLOYMENT.md) – Configuration and deployment
