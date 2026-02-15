# Vascular Access – FHIR Field Mapping

Vascular access procedures: fistula, graft, and catheter placement for dialysis.

---

## FHIR → Export Field Mapping

| Export Field | FHIR Resource | FHIR Path | Notes |
|--------------|---------------|----------|-------|
| PatientId | Procedure | `subject.reference` | Extract from Patient/123 |
| ProcedureId | Procedure | `id` | FHIR Procedure ID |
| ProcedureDate | Procedure | `performedDateTime` or `performedPeriod.start` | |
| ProcedureType | Procedure | `code.coding` | Fistula, Graft, Catheter |
| Laterality | Procedure | `bodySite.coding` | Left, Right |
| EncounterId | Procedure | `encounter.reference` | |
| Status | Procedure | `status` | completed, in-progress, etc. |
| Reason | Procedure | `reasonCode` | Indication (optional) |

---

## Procedure Type Codes

| Type | CPT | SNOMED | Description |
|------|-----|--------|------------|
| **AV Fistula** | 36821, 36818, 36819, 36820 | 232985003, 392019007 | Creation of arteriovenous fistula |
| **AV Graft** | 36831, 36832, 36833 | 232982001 | Creation of arteriovenous graft |
| **Tunneled Catheter** | 36558, 36561 | 384748005 | Tunneled dialysis catheter |
| **Non-tunneled Catheter** | 36555, 36556 | 25133009 | Temporary dialysis catheter |
| **Catheter Removal** | 36589 | - | Catheter removal |

---

## Laterality (bodySite)

| Value | SNOMED | Description |
|-------|--------|-------------|
| Left | 7771000 | Left |
| Right | 24028007 | Right |
| Bilateral | 51440002 | Bilateral |

---

## FHIR Search Used

```
GET [base]/Procedure?date=ge{from}&date=le{to}&_elements=id,code,subject,performedDateTime,bodySite,encounter,status&_count=500
```

Optional: filter by procedure code value set for vascular access.

---

## CSV Output Format

```csv
VascularAccess_Export
PatientId,ProcedureId,ProcedureDate,ProcedureType,Laterality,Status,EncounterId,ExportDate
```

---

## References

- [REGISTRY-DATA-MODEL.md](REGISTRY-DATA-MODEL.md)
- [US Core Procedure Code ValueSet](https://hl7.org/fhir/us/core/ValueSet-us-core-procedure-code.html)
