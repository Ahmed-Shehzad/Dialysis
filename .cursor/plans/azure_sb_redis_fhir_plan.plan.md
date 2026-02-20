---
name: Azure SB, Redis, FHIR Review
overview: Wire Azure Service Bus transport, optional Redis cache, and document FHIR alignment remaining considerations.
todos:
  - id: asb-transport
    content: Add Azure Service Bus as optional transport (config-driven)
    status: completed
  - id: asb-docker
    content: Document ASB usage; add Azurite or emulator note for local dev
    status: completed
  - id: redis-optional
    content: Add Redis to docker-compose; optional AddTransponderRedisCache wiring
    status: completed
  - id: fhir-review
    content: Add Future Alignment section to FHIR Implementation Guide
    status: completed
isProject: true
---

# Azure Service Bus, Redis, FHIR Plan

## 1. Azure Service Bus

### Context
- SignalR transport is **publish-only** (no receive endpoints)
- Transponder supports ASB via `Transponder.Transports.AzureServiceBus`
- InboxStates are used when **receive endpoints** process messages (ASB has receive; SignalR does not)
- Full Inbox usage requires: ASB receive endpoint + handler that checks session.Inbox

### Approach
- Wire ASB as **optional** transport when `AzureServiceBus:ConnectionString` is configured
- Use **dual transport**: SignalR (real-time) + ASB (durable) when both configured
- Document receive-endpoint + Inbox pattern for when cross-service event consumption is added
- Local dev: Use connection string to Azure (trial) or Azure Service Bus Emulator; document

### Files
- Treatment/Alarm Program.cs – add UseAzureServiceBus when config present
- docker-compose – optional ASB profile or document external ASB
- docs – update AZURE-SERVICE-BUS.md or similar

## 2. Redis

### Context
- `Transponder.Persistence.Redis` adds `IDistributedCache` via `AddTransponderRedisCache`
- No Dialysis service uses `IDistributedCache` today
- "If needed" = optional; add Redis to docker-compose and document usage pattern

### Approach
- Add Redis service to docker-compose (optional profile or default)
- Document when to use: read-heavy lookups (Patient by MRN, Prescription by OrderId)
- Optional: wire in one service as example (e.g. Prescription API with cache-aside)

## 3. FHIR Implementation Guide Review

### Context
- ALIGNMENT-REPORT: 54/54 aligned, 0 not aligned
- IMPLEMENTATION_STATUS: Phases 1–6 complete
- Formal Dialysis Machine FHIR IG in development by Dialysis Interoperability Consortium

### Approach
- Add "Future Alignment" section to ALIGNMENT-REPORT or IMPLEMENTATION_STATUS
- Note: When formal IG published, reassess profiles and any new requirements
- No implementation changes needed
