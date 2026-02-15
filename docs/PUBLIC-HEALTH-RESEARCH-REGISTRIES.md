# Public Health, Research & Registries

Design and roadmap for reportable conditions, dialysis registries, research cohorts, real-world data, and data-sharing governance in Dialysis PDMS.

---

## 1. Reportable Conditions & Public Health Interfaces

### 1.1 Scope

Reportable conditions (notifiable diseases) and public health reporting interfaces for dialysis care contexts.

| Interface Type | Description | Dialysis PDMS Relevance |
|----------------|-------------|--------------------------|
| **ELR (Electronic Lab Reporting)** | Lab results to PHD | Dialysis lab values (e.g. potassium, phosphate) |
| **Syndromic surveillance** | Symptom/encounter aggregates | Session-related events, hypotension episodes |
| **Case reporting** | Individual case notifications | Infection, vascular access events |
| **Immunization (IIS)** | Vaccine records | Flu, COVID vaccination status |

### 1.2 Standards & Protocols

| Standard | Use |
|----------|-----|
| **HL7 v2** | Legacy public health message exchange (ORU, MDM, ADT) |
| **FHIR Public Health (PH)** | FHIR R4 profiles for reportable conditions, MeasureReport |
| **HL7 CDA** | Structured documents for some jurisdictions |

### 1.3 Implementation Approach

| Component | Description |
|-----------|-------------|
| **Reportable Condition Registry** | Mapping: condition code (ICD-10, SNOMED) → reportability; jurisdiction rules |
| **Report Generator** | Trigger on Condition/Observation/Procedure; map to PH message format |
| **Delivery** | Push (HL7 v2 to PH endpoint), Pull (PH queries FHIR), or hybrid |

### 1.4 Dialysis-Specific Reportable Events

| Event | Data Source | Typical Format |
|-------|-------------|----------------|
| Vascular access infection | Condition (SNOMED), Procedure | HL7 v2 MDM/ORU or FHIR |
| Dialysis-related bacteremia | Observation (culture), Condition | ELR/ case report |
| Hypotension episodes (aggregate) | Dialysis.Alerting | Syndromic / aggregate only |
| Flu/COVID vaccination | Immunization resource | IIS feed |

### 1.5 Architecture Integration

```
FHIR Store (Condition, Observation, Procedure, Immunization)
     |
     v
Dialysis.PublicHealth (new service)
  - ReportableConditionMatcher
  - ReportGenerator (HL7 v2 / FHIR)
  - DeliveryQueue → PH endpoint
```

---

## 2. Registries (Dialysis-Specific)

### 2.1 Registry Types

| Registry | Purpose | Dialysis PDMS Data |
|----------|---------|--------------------|
| **ESRD (End-Stage Renal Disease)** | CMS/regional kidney disease registry | Patient, Encounter, Procedure, Observation |
| **Vascular Access** | Fistula/graft/catheter tracking | Procedure, Device, Observation |
| **Dialysis Quality (QIP)** | Quality metrics (UFR, Kt/V) | Observation, Encounter |
| **Infection** | NHSN dialysis events | Condition, Observation, Procedure |

### 2.2 FHIR Alignment

- **Patient** – demographics, identifiers (MPI alignment for registry linkage)
- **Encounter** – dialysis sessions (class, period, serviceType)
- **Procedure** – vascular access creation, catheter placement
- **Condition** – ESRD, comorbidities, infections
- **Observation** – vitals, lab, adequacy (Kt/V, URR)
- **Device** – vascular access device

### 2.3 Registry Submission Patterns

| Pattern | Description |
|---------|-------------|
| **Batch export** | Periodic extract (e.g. monthly) → registry format → secure transfer |
| **API push** | Registry provides API; PDMS pushes on event or schedule |
| **API pull** | Registry queries FHIR/API with auth; PDMS exposes read-only subset |

### 2.4 Implementation Roadmap

| Phase | Deliverable |
|-------|-------------|
| 1 | Registry data model documentation (FHIR resources → registry fields) |
| 2 | Batch export job (FHIR → CSV/JSON/NDJSON) with date range, tenant filter |
| 3 | `Dialysis.Registry` service: export API + optional push connector |
| 4 | Per-registry adapters (ESRD format, QIP measures, etc.) |

---

## 3. Research Cohorts & De-Identification Best Practices

### 3.1 Research Cohorts

- Extends [ANALYTICS-DECISION-SUPPORT.md](ANALYTICS-DECISION-SUPPORT.md) cohort building.
- **Research cohorts** require explicit consent, IRB approval, and de-identification before use.

### 3.2 De-Identification Methods

| Method | Description | When to Use |
|--------|-------------|-------------|
| **Suppression** | Remove identifiers (name, MRN, DOB exact) | Baseline for all research exports |
| **Generalization** | Age bands, date ranges (e.g. year only) | Reduce re-identification risk |
| **Pseudonymization** | Replace identifiers with stable pseudonyms | Linkage across datasets |
| **Synthetic data** | Generate synthetic records from real distributions | Training, demos |
| **k-anonymity** | Ensure ≥k individuals match each quasi-identifier combination | Formal privacy guarantee |

### 3.3 FHIR De-Identification

- **FHIR R4**: [De-identification](https://www.hl7.org/fhir/bulk-data/de-identification.html) guidance.
- **Tools**: e.g. FHIR Shorthand (FSH) de-id rules, Microsoft FHIR Tools for anonymization.
- **Elements to remove/transform**: Patient.name, identifier, Telecom, Address; Encounter.location detail; free text in `text` divs.

### 3.4 Best Practices

| Practice | Implementation |
|----------|-----------------|
| **Minimum necessary** | Export only fields required for research question |
| **Date shifting** | Consistent per-patient shift; preserve relative timing |
| **Safe harbor (HIPAA)** | 18 identifiers removal/modification |
| **Expert determination** | Document risk assessment; re-id risk acceptable |
| **Audit** | Log all research exports (who, when, cohort, de-id level) |

### 3.5 Dialysis PDMS Integration

- **Dialysis.Analytics** or **Dialysis.Research** service:
  - Cohort definition (reuse analytics criteria)
  - De-identification pipeline (configurable rules)
  - Consent check (Consent resource or external consent store)
  - Export (NDJSON, CSV) with audit trail

---

## 4. RWD vs RCTs: Complementary Roles

### 4.1 Definitions

| Type | Description |
|------|-------------|
| **RCT (Randomized Controlled Trial)** | Gold standard; randomization, blinding; causal inference |
| **RWD (Real-World Data)** | Observational; EHR, registries, claims; effectiveness in practice |

### 4.2 Complementary Roles

| Use Case | Preferred Source | Notes |
|----------|------------------|-------|
| Drug efficacy, safety (pre-market) | RCT | Regulatory evidence |
| Post-market surveillance | RWD | Larger, diverse populations |
| Pragmatic trials | RWD + embedded trial | Hybrid designs |
| Quality improvement | RWD | Local benchmarking |
| Hypothesis generation | RWD | Informs future RCTs |

### 4.3 Dialysis PDMS as RWD Source

- **Strengths**: Structured FHIR data, vitals, sessions, alerts, provenance.
- **Limitations**: Single specialty; linkage to claims, lab, pharmacy often external.
- **Use**: Feasibility studies, quality metrics, registry contributions, pragmatic trial recruitment.

### 4.4 Governance

- Document when PDMS data is used as RWD.
- Align with [Data Sharing Agreements & Governance](#5-data-sharing-agreements--governance).

---

## 5. Data Sharing Agreements & Governance

### 5.1 Principles

| Principle | Implementation |
|-----------|----------------|
| **Purpose specification** | Define use (research, PH, QI, registry) in agreement |
| **Lawful basis** | Consent, legitimate interest, legal obligation per jurisdiction |
| **Data minimization** | Share only what is necessary |
| **Transparency** | Document flows; update DATA-FLOWS-AUDIT.md |
| **Accountability** | DPA, processor terms; audit trail |

### 5.2 Data Sharing Agreement (DSA) Elements

| Element | Description |
|---------|-------------|
| **Parties** | Data controller, processor, recipient |
| **Purpose** | Specific use case(s) |
| **Data categories** | e.g. demographic, clinical, identifiers |
| **Retention** | How long recipient may hold data |
| **Security** | Encryption, access control |
| **Sub-processors** | Any onward sharing |
| **Breach notification** | Timelines, responsibilities |
| **Audit rights** | Controller access to verify compliance |
| **Termination** | Return/destruction of data |

### 5.3 Governance in Dialysis PDMS

| Component | Role |
|-----------|------|
| **Consent** | Dialysis.AuditConsent records consent events; link to research/registry |
| **Tenant isolation** | Per-tenant DSAs; no cross-tenant sharing without explicit design |
| **Audit** | All exports logged (Dialysis.AuditConsent or dedicated export audit) |
| **Access control** | Scope `dialysis.admin` for export; `dialysis.research` (future) for research cohort access |
| **Data flow documentation** | [DATA-FLOWS-AUDIT.md](DATA-FLOWS-AUDIT.md) extended for PH, registry, research flows |

### 5.4 C5 / GDPR Alignment

- **C5**: Transparency, audit, access control – already aligned.
- **GDPR**: Lawful basis, purpose limitation, data minimization – document in DSA.
- **DigiG / German healthcare**: Additional consent and transparency requirements for secondary use.

---

## 6. Implementation Roadmap

| Phase | Deliverable | Effort |
|-------|-------------|--------|
| **1. Documentation** | DSA template; data flow extensions for PH/registry/research | ✅ Done |
| **2. Reportable conditions** | Reportable condition registry (config); ReportGenerator design | ✅ Config: `docs/reportable-conditions/` |
| **3. Registry export** | Batch export API (FHIR → NDJSON/CSV) with tenant/date filter | ✅ In Dialysis.Analytics |
| **4. De-identification** | De-id rules config; pipeline for research export | Medium |
| **5. Public health service** | Dialysis.PublicHealth: matching, report gen, delivery | Large |
| **6. Registry service** | Dialysis.Registry: adapters for ESRD, QIP, etc. | Large |

---

## 7. References

- [FHIR Public Health](https://www.hl7.org/fhir/publichealth.html)
- [HIPAA Safe Harbor De-Identification](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)
- [GDPR Article 5](https://gdpr-info.eu/art-5-gdpr/) – Principles
- [DATA-FLOWS-AUDIT.md](DATA-FLOWS-AUDIT.md)
- [C5-COMPLIANCE.md](C5-COMPLIANCE.md)
- [ANALYTICS-DECISION-SUPPORT.md](ANALYTICS-DECISION-SUPPORT.md)
