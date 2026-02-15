# NHSN Dialysis Event – FHIR Field Mapping

CDC NHSN Dialysis Event module: infection events and vascular access for hemodialysis surveillance.

---

## Event Types (NHSN)

| Event Type | Description | FHIR Source |
|------------|-------------|-------------|
| **BSI** | Positive blood culture (bloodstream infection) | Observation (culture result), Condition |
| **IV antimicrobial start** | IV antibiotic started | Observation, MedicationRequest |
| **Pus/redness/swelling** | Vascular access site infection | Condition |
| **Vascular access infection** | Localized infection at access site | Condition, Procedure |

---

## FHIR → Export Field Mapping

| Export Field | FHIR Resource | FHIR Path | Notes |
|--------------|---------------|----------|-------|
| FacilityId | (config) | N/A | From tenant/config |
| PatientId | Condition/Observation/Procedure | `subject.reference` | Extract from Patient/123 |
| EventDate | Condition | `onsetDateTime` or `onsetPeriod.start` | |
| EventDate | Observation | `effectiveDateTime` or `effectivePeriod.start` | |
| EventDate | Procedure | `performedDateTime` or `performedPeriod.start` | |
| EventType | Condition | `code.coding` | Map: BSI, IVSiteInfection, VascularAccessInfection |
| EventType | Observation | `code.coding` | Blood culture → BSI |
| VascularAccessType | Procedure | `code.coding` | Fistula, Graft, TunneledCatheter, NonTunneledCatheter |
| EncounterId | Condition/Observation/Procedure | `encounter.reference` | |
| ResourceId | Any | `id` | FHIR resource ID |
| Code | Any | `code.coding[].code` | ICD-10, SNOMED, LOINC |

---

## Infection Code References (SNOMED / ICD-10)

| Event Type | Code System | Example Codes |
|------------|-------------|---------------|
| BSI / Bacteremia | SNOMED | 68402000 (Bacterial septicemia), 41652007 (Sepsis) |
| BSI / Bacteremia | ICD-10 | A41.x (Sepsis), R78.81 (Bacteremia) |
| Vascular access infection | SNOMED | 40733004 (Infection of vascular access site) |
| IV site infection | SNOMED | 40388003 (Infection of intravenous site) |

---

## Vascular Access Type Codes (Procedure)

| Type | CPT | SNOMED |
|------|-----|--------|
| AV Fistula | 36821, 36818, 36819, 36820 | 232985003 (Creation of AV fistula) |
| AV Graft | 36831, 36832, 36833 | 232982001 (Creation of AV graft) |
| Tunneled catheter | 36558, 36561 | 384748005 (Insertion of tunneled catheter) |
| Non-tunneled catheter | 36555, 36556 | Central venous catheter codes |

---

## FHIR Searches Used

```
GET [base]/Condition?onset-date=ge{from}&onset-date=le{to}&_elements=id,code,subject,onsetDateTime,encounter&_count=500
GET [base]/Observation?code=...&date=ge{from}&date=le{to}&_elements=id,code,subject,effective,encounter&_count=500
GET [base]/Procedure?date=ge{from}&date=le{to}&_elements=id,code,subject,performedDateTime,encounter&_count=500
```

---

## CSV Output Format

```csv
NHSN_DialysisEvent_Export
FacilityId,PatientId,EventDate,EventType,VascularAccessType,ResourceType,ResourceId,Code,EncounterId,ExportDate
```

---

## References

- [CDC NHSN Dialysis Event](https://www.cdc.gov/nhsn/dialysis/event/index.html)
- [Dialysis Event Manual (PDF)](https://www.cdc.gov/nhsn/PDFs/pscManual/8pscDialysisEventcurrent.pdf)
- [REGISTRY-DATA-MODEL.md](REGISTRY-DATA-MODEL.md)
