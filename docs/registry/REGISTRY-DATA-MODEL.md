# Registry Data Model

FHIR resources → registry export fields mapping for ESRD, QIP, and CROWNWeb adapters.

---

## 1. ESRD (End-Stage Renal Disease)

**Adapter:** `Dialysis.Registry.Adapters.EsrdAdapter`  
**Output:** NDJSON (one Encounter per line) or HL7 v2 (ORU^R01)

### FHIR → Registry Field Mapping

| Registry Concept | FHIR Resource | FHIR Path | Export Field |
|------------------|---------------|-----------|--------------|
| Patient ID | Encounter | `subject.reference` (e.g. `Patient/123`) | `PatientId` (from reference) |
| Encounter ID | Encounter | `id` | `EncounterId` |
| Session date | Encounter | `period.start` | `period.start` (ISO 8601) |
| Status | Encounter | `status` | `status` |
| Treatment type | Encounter | `class.code` | (HL7 v2: OBR-4) |

### FHIR Search Used

```
GET [base]/Encounter?date=ge{from}&date=le{to}&_elements=id,subject,period,status&_count=500
```

### NDJSON Output

Each line is a JSON-serialized `Encounter` resource.

### HL7 v2 Output (`format=hl7v2`)

One ORU^R01 message per Encounter:

- **MSH** – sending app, timestamp, message type
- **PID** – patient ID from `subject.reference`
- **OBR** – encounter ID, period start, "Dialysis Session"
- **OBX** – period.start as ST

---

## 2. QIP (Quality Incentive Program)

**Adapter:** `Dialysis.Registry.Adapters.QipAdapter`  
**Output:** CSV summary

### FHIR → Registry Field Mapping

| Registry Concept | FHIR Resource | FHIR Path | Export Field |
|------------------|---------------|-----------|--------------|
| Period start | (query param) | `date=ge{from}` | `From` |
| Period end | (query param) | `date=le{to}` | `To` |
| Session count | Bundle | `total` from Encounter search | `SessionCount` |

### FHIR Search Used

```
GET [base]/Encounter?date=ge{from}&date=le{to}&_summary=count
```

### CSV Output

```csv
Period,From,To,SessionCount
QIP,2025-01-01,2025-01-31,42
```

---

## 3. CROWNWeb (CMS-2728)

**Adapter:** `Dialysis.Registry.Adapters.CrownWebAdapter`  
**Output:** CSV (simplified CMS-2728 export)

### FHIR → Registry Field Mapping

| Registry Concept | FHIR Resource | FHIR Path | Export Field |
|------------------|---------------|-----------|--------------|
| Patient ID | Encounter | `subject.reference` | `PatientId` |
| Encounter ID | Encounter | `id` | `EncounterId` |
| Session date | Encounter | `period.start` | `SessionDate` |
| Status | Encounter | `status` | `Status` |
| Treatment type | Encounter | `class.code` | `TreatmentType` |
| Export date | (request) | N/A | `ExportDate` |

### FHIR Search Used

```
GET [base]/Encounter?date=ge{from}&date=le{to}&_elements=id,subject,period,status,class&_count=500
```

### CSV Output

```csv
CROWNWeb_CMS2728_Export
PatientId,EncounterId,SessionDate,Status,TreatmentType,ExportDate
123,enc-1,2025-01-15T08:00:00,finished,AMB,2025-01-31
```

### CMS-2728 Alignment

CMS-2728 (Medical Evidence Report) includes patient demographics, ESRD onset date, vascular access, and treatment history. This adapter exports a **session-level summary** suitable for batch submission. Full CMS-2728 field mapping would require Patient, Condition, and Procedure data—see Phase 8 (Vascular Access adapter) for Procedure integration.

---

## 4. Common Patterns

### Date Range Filter

All adapters accept `from` and `to` (DateOnly). Queries use:

- `date=ge{from}` 
- `date=le{to}`

### Tenant Isolation

When `X-Tenant-Id` is present, exports are scoped to that tenant. FHIR Gateway enforces tenant context.

### Pagination

Adapters use `_count=500` and follow `next` links until no further pages.

---

## 5. NHSN (CDC Dialysis Event)

**Adapter:** `Dialysis.Registry.Adapters.NhsnAdapter`  
**Output:** CSV (infection events, vascular access procedures)

### FHIR → Registry Field Mapping

| Registry Concept | FHIR Resource | FHIR Path | Export Field |
|------------------|---------------|-----------|--------------|
| Facility ID | (config) | N/A | `FacilityId` (tenant) |
| Patient ID | Condition, Procedure | `subject.reference` | `PatientId` |
| Event date | Condition | `onsetDateTime`, `onsetPeriod.start` | `EventDate` |
| Event date | Procedure | `performedDateTime`, `performedPeriod.start` | `EventDate` |
| Event type | Condition | `code.coding` | BSI, VascularAccessInfection, IVSiteInfection |
| Vascular access type | Procedure | `code.coding` | Fistula, Graft, TunneledCatheter, Catheter |
| Encounter ID | Condition, Procedure | `encounter.reference` | `EncounterId` |

### FHIR Searches Used

```
GET [base]/Condition?onset-date=ge{from}&onset-date=le{to}&_elements=id,code,subject,onsetDateTime,onsetPeriod,encounter&_count=500
GET [base]/Procedure?date=ge{from}&date=le{to}&_elements=id,code,subject,performedDateTime,encounter&_count=500
```

### CSV Output

See [NHSN-FIELD-MAPPING.md](NHSN-FIELD-MAPPING.md).

---

## 6. Vascular Access

**Adapter:** `Dialysis.Registry.Adapters.VascularAccessAdapter`  
**Output:** CSV (fistula, graft, catheter procedures)

### FHIR → Registry Field Mapping

| Registry Concept | FHIR Resource | FHIR Path | Export Field |
|------------------|---------------|-----------|--------------|
| Patient ID | Procedure | `subject.reference` | `PatientId` |
| Procedure ID | Procedure | `id` | `ProcedureId` |
| Procedure date | Procedure | `performedDateTime`, `performedPeriod.start` | `ProcedureDate` |
| Procedure type | Procedure | `code.coding` | Fistula, Graft, TunneledCatheter, Catheter |
| Laterality | Procedure | `bodySite.coding` | Left, Right, Bilateral |
| Status | Procedure | `status` | `Status` |
| Encounter ID | Procedure | `encounter.reference` | `EncounterId` |

### FHIR Search Used

```
GET [base]/Procedure?date=ge{from}&date=le{to}&_elements=id,code,subject,performedDateTime,bodySite,encounter,status&_count=500
```

### CSV Output

See [VASCULAR-ACCESS-FIELD-MAPPING.md](VASCULAR-ACCESS-FIELD-MAPPING.md).

---

## 7. References

- [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](../PUBLIC-HEALTH-RESEARCH-REGISTRIES.md)
- [FHIR Encounter](https://www.hl7.org/fhir/encounter.html)
- [CMS-2728](https://www.cms.gov/medicare/end-stage-renal-disease/esrd-medical-evidence-report-mandatory-filing)
- [EQRS Data Management Guidelines](https://mycrownweb.org/)
