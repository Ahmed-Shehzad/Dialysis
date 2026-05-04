# Real integrations backlog (HIS)

Goal: move from **stub gateways and in-process demos** to **broker-backed, protocol-realistic** interoperability, consistent with Tummers et al. (2021) discussion on **HL7 FHIR** and standards, and with this repo’s **Transponder** + **SmartConnect** direction.

Current posture: **stubs** with **incremental paths** — optional **HTTP `ILaboratoryGateway`** when `His:Laboratory:BaseUri` is set; optional **HTTP `IPharmacyGateway`** when **`His:Pharmacy:BaseUri`** is set (same pattern as lab); EHR/PDMS/pharmacy **Transponder consumers** remain stubs; **outbox/inbox** on `HisDbContext` with **`His:Transponder:EnableOutboxRelay`** + RabbitMQ per [his_transponder_e2e_runbook.md](./his_transponder_e2e_runbook.md). A **metadata-only data-share** read exists at **`GET …/data-management/integration/outbox-metadata`** (`his.data.share.read`) for interoperability index rows without payload bodies.

## 1. Messaging infrastructure

| Item | Description |
|------|-------------|
| Broker transport | Enable RabbitMQ or NATS (per [Transponder README](../BuildingBlocks/Transponder/README.md)) in production |
| Outbox relay | Run `AddTransponderOutboxRelay<HisDbContext>()` with durable publishing |
| Consumer scale-out | Multiple workers, dead-letter handling, observability (correlation ids) |
| Schema versioning | `IntegrationEventCatalog` + versioned event names when payloads break |

## 2. Clinical and operational protocols

| Area | Today | Target integration patterns |
|------|--------|-----------------------------|
| Laboratory | **Partial:** in-process stub **or** `HttpLaboratoryGateway` when `His:Laboratory:BaseUri` is set; `LaboratoryReferralFromHisStubConsumer` | HL7 v2 ORM/ORU, FHIR ServiceRequest/Observation, or LIS vendor API; align with [SmartConnect](../SmartConnect/) inbound if used |
| Pharmacy | `IPharmacyGateway` stub **or** **`HttpPharmacyGateway`** when **`His:Pharmacy:BaseUri`** is set | NCPDP / vendor API / FHIR MedicationRequest; outbox events already emitted for medication lifecycle |
| Orders & results | Device path only (`IngestDeviceReadingCommand`) | Expand to lab/imaging workflow IDs, placer/filler order numbers |
| EHR / PDMS | Stub consumers | FHIR subscription / messaging, or proprietary ACL; never direct domain references across bounded contexts |
| Patient portal / external apps | REST only | OAuth2 for third-party; consider FHIR Patient-facing apps where applicable |

## 3. Device and streaming data

| Item | Description |
|------|-------------|
| Device ingest | Existing rate limiter + optional `ExternalMessageId` idempotency | Scale-out: partition key, dedup store, back-pressure |
| Sensor fusion (RA Data management) | Single ingest command | Normalization layer, device identity registry, time-series or message bus fan-out |

## 4. Data management integration

| Item | Description |
|------|-------------|
| Import pipelines | **Partial:** `SubmitDataImportJobCommand` + validation + **`GET …/data-management/import-jobs/{id}`** | Real ETL: staging tables, validation, reconciliation |
| Full-text / indexing | **Partial:** `ListFullTextSearchEntriesQuery` with optional **`q`** filter (SQL `Contains`) | OpenSearch/Elastic or SQL full-text, sync from domain events |
| Analytics exports | **Partial:** `RequestAnalyticsExportJobCommand` + list query | Scheduled jobs, PHI-safe export to warehouse |

## 5. Definition of done (integration slice)

- At least one **external system** path runs end-to-end: **HIS → outbox → broker → adapter → external test harness** (or sandbox).
- **Progress in this repo:** outbox relay + Rabbit can be exercised with SQL + `docker-compose.integration.yml` (see runbook); **second adapter** paths include **`HttpLaboratoryGateway`** / **`HttpPharmacyGateway`** when base URIs are set, plus **outbox metadata** listing for operator/CI visibility.
- Idempotency and replay documented per message type.
- Runbooks for broker credentials, queue monitoring, and failed message replay.

## References

- [his_ra_submodules.md](./his_ra_submodules.md) — rows 3, 7–11, 22–23, etc.
- [Dialysis.HIS.Integration](./Dialysis.HIS.Integration/) — gateways, device ingest, consumers
- [Dialysis.HIS.Contracts](./Dialysis.HIS.Contracts/) — integration events catalog
