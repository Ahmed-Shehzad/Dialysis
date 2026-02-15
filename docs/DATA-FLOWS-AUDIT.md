# Data Flows – Auditor / C5 Reference

This document summarizes system description, data flows, and jurisdiction for C5 auditors and customer due diligence. Detailed architecture: [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md).

---

## System Description

**Dialysis PDMS** is a microservices-based Patient Data Management System for dialysis care. It provides:

- FHIR-compliant API (gateway to Azure Health Data Services FHIR store)
- HL7 v2 ADT/vitals ingest (via Mirth Connect)
- Hypotension early-warning (risk scoring, alerts)
- Multi-tenant storage (PostgreSQL per tenant)
- Audit logging and FHIR Provenance

---

## Data Flow Summary

| Flow | Source | Destination | Data |
|------|--------|-------------|------|
| ADT/Patient | Hospital EHR (HL7) | FHIR Store (Patient, Encounter) | Demographics, encounter data |
| Vitals | Dialysis devices (HL7/FHIR) | FHIR Store (Observation) | Blood pressure, HR, SpO2 |
| Risk prediction | ObservationCreated (Service Bus) | HypotensionRiskRaised (Service Bus) | Risk scores |
| Alerts | HypotensionRiskRaised | Alerting DB, cache | Patient/encounter + score |
| Subscriptions | ResourceWrittenEvent | Webhooks (external) | FHIR resource references |
| Audit | API requests | Audit DB | Action, agent, resource type |
| Public health / Registry | FHIR, Dialysis.PublicHealth (future) | PH agency, registry | Reportable conditions, registry submissions |
| Research export | Cohort + de-id (future) | Research partner | De-identified RWD per DSA |

---

## Jurisdiction & Data Location

- **Target regions**: EU (e.g. West Europe, Germany West Central) or Azure Germany for DigiG §393 SGB V.
- **Data residency**: FHIR data, PostgreSQL, Redis, Service Bus should be deployed in the selected jurisdiction.
- **Azure in-scope**: App Service, Health Data Services, Service Bus, Azure Database for PostgreSQL, Azure Cache for Redis, Key Vault.

See [C5-COMPLIANCE.md](C5-COMPLIANCE.md) for controls and attestation references.

---

## Public Health, Research & Registries

For reportable conditions, dialysis registries, research cohorts, and data sharing governance, see [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](PUBLIC-HEALTH-RESEARCH-REGISTRIES.md). Data sharing agreement template: [DATA-SHARING-AGREEMENT-TEMPLATE.md](DATA-SHARING-AGREEMENT-TEMPLATE.md).
