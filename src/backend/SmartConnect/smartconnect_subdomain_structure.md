# SmartConnect — Subdomain structure (Large-Scale Structure)

This document records SmartConnect's **Large-Scale Structure pattern** per Eric Evans, *Domain-Driven Design* (2003), pp. 307–337. SmartConnect uses a **Pluggable Component Framework** (Evans p. 334).

## Pluggable pipeline

```
   ┌──────────┐    ┌──────────────┐    ┌──────────┐    ┌──────────────┐
   │  SOURCE  │ →  │ TRANSFORMER  │ →  │  FILTER  │ →  │ DESTINATION  │
   └──────────┘    └──────────────┘    └──────────┘    └──────────────┘
       │                  │                 │                 │
       └────── pluggable connectors implementing a runtime contract ──┘
```

Each stage is a **pluggable component** that conforms to a runtime interface declared in `Dialysis.SmartConnect.Core`. New protocols (vendor X HL7 dialect, a new lab gateway, a new SFTP destination) plug in by implementing the relevant connector interface — the channel core does not change.

## Aggregate concept

There are **no clinical/operational aggregate roots** in SmartConnect. The runtime owns `IntegrationMessage`/`AlertEvent` etc. as transient orchestration state. Per Evans (p. 281), Generic Subdomains do not warrant deep modeling.

## Concrete connector types (today)

| Stage | Connector | Implementation |
|---|---|---|
| Source | TCP / MLLP | `Dialysis.SmartConnect.Core.Sources.*` |
| Source | HTTP | `HttpSource` |
| Source | File / FTP | `FileSource` |
| Source | SMTP poll | `SmtpSource` |
| Transformer | HL7v2 parser | `Hl7Transformer` |
| Transformer | FHIR adapter | `FhirTransformer` |
| Transformer | MDC normalizer | `MdcNormalizer` |
| Filter | JavaScript rule | `JavaScriptFilter` |
| Destination | RabbitMQ outbox publisher | `OutboxDestination` |
| Destination | HTTP webhook | `HttpOutboundAdapter` |
| Destination | File / SFTP write | `FileOutboundAdapter` |

## Why Pluggable Component Framework and not Open Host Service alone?

SmartConnect is *both* an Open Host Service to downstream modules (publishing versioned events) AND internally a pluggable runtime. The OHS shape governs the cross-context edge (`Dialysis.SmartConnect.Contracts.Integration`); the Pluggable Component Framework governs the internal channel runtime. They compose without conflict.

## Anti-pattern reminder

Per the [README](README.md), do not let clinical or billing rules leak into a connector. Connectors transform and route; downstream modules interpret.
