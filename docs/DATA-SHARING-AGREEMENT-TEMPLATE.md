# Data Sharing Agreement – Template

Use this template when sharing Dialysis PDMS data with public health authorities, registries, or research partners. Customize per jurisdiction and use case.

---

## 1. Parties

| Role | Name | Contact |
|------|------|---------|
| **Data Controller** | [Organization] | |
| **Data Processor** (if applicable) | [Processor name] | |
| **Data Recipient** | [PH agency / Registry / Research institution] | |

---

## 2. Purpose

| Item | Description |
|------|-------------|
| **Purpose** | [e.g. Reportable condition notification / ESRD registry submission / Research cohort study] |
| **Lawful basis** | [Consent / Legal obligation / Legitimate interest – specify] |
| **Scope** | [Demographics only / Clinical + vitals / Full FHIR subset] |

---

## 3. Data Categories

| Category | Examples | Shared? (Y/N) |
|----------|----------|---------------|
| Demographics | Name, DOB, gender, address | |
| Identifiers | MRN, national ID | |
| Clinical | Condition, Procedure, Observation | |
| Encounters | Session dates, facility | |
| Free text | Notes, descriptions | |

---

## 4. Technical & Security

| Item | Specification |
|------|----------------|
| **Format** | FHIR NDJSON / HL7 v2 / CSV / other |
| **Transfer** | TLS 1.2+; API (OAuth2) / SFTP / secure email |
| **Encryption at rest** | [Recipient responsibility] |
| **Access control** | Role-based; need-to-know |

---

## 5. Retention & Deletion

| Item | Duration |
|------|----------|
| **Retention** | [e.g. 7 years per [regulation]] |
| **Deletion** | Upon termination: [return / destroy within X days] |

---

## 6. Sub-processors & Onward Sharing

| Item | Description |
|------|-------------|
| **Sub-processors** | [List or "none"] |
| **Onward sharing** | [Prohibited / Allowed with prior written consent] |

---

## 7. Breach & Incidents

| Item | Requirement |
|------|-------------|
| **Notification** | [Controller notified within X hours] |
| **Remediation** | [Recipient responsibilities] |

---

## 8. Audit & Compliance

| Item | Description |
|------|-------------|
| **Audit rights** | Controller may audit Recipient's compliance |
| **Certifications** | [C5, ISO 27001, etc. if required] |

---

## 9. Termination

| Item | Description |
|------|-------------|
| **Notice** | [X days written notice] |
| **Effect** | Cessation of sharing; return/destruction per Section 5 |

---

## 10. Signatures

| Party | Signature | Date |
|-------|------------|------|
| Controller | | |
| Recipient | | |

---

**See:** [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](PUBLIC-HEALTH-RESEARCH-REGISTRIES.md) for governance context.
