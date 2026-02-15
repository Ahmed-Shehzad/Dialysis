# PDF Template Mapping (Dialysis.Documents)

FHIR path mappings for PDF template filling and generation.

## Template storage

Set `Documents__TemplatePath` to a directory containing AcroForm PDF templates (e.g. `/app/templates` in Docker, or a local path for development).

## AcroForm field names

When creating PDF templates for fill-template, use these standard field names for automatic FHIR population:

| Field name       | FHIR source            | Example value       |
|------------------|------------------------|---------------------|
| PatientId        | Patient.id             | `pat-123`           |
| PatientName      | Patient.name           | `Doe, John`         |
| PatientFirstName | Patient.name.given     | `John`              |
| PatientLastName  | Patient.name.family    | `Doe`               |
| PatientDOB       | Patient.birthDate      | `1980-01-15`        |
| PatientGender    | Patient.gender         | `male`              |
| PatientIdentifier| Patient.identifier     | `MRN: 12345`        |
| EncounterId      | Encounter.id           | `enc-456`           |
| EncounterStart   | Encounter.period.start | `2024-02-13T08:00:00` |
| EncounterEnd     | Encounter.period.end   | `2024-02-13T12:00:00` |
| SessionDate      | Encounter.period.start  | `2024-02-13`        |
| EncounterStatus  | Encounter.status       | `finished`          |

## Custom mappings

Pass explicit `mappings` in the fill-template request body to override or add fields:

```json
{
  "templateId": "consent-form",
  "patientId": "pat-123",
  "mappings": {
    "ProcedureDate": "2024-02-13",
    "ClinicianName": "Dr. Smith"
  }
}
```

## Generate-PDF templates

| Template         | Description                          | Required params      |
|------------------|--------------------------------------|----------------------|
| session-summary  | Dialysis session (Patient + Encounter)| patientId, encounterId? |
| patient-summary  | Patient demographics                 | patientId            |
| measure-report   | Public health MeasureReport          | resourceId (MeasureReport ID) |

## Calculator templates (Phase 14)

When `includeScripts=true` and the template ID is in `Documents__CalculatorTemplateIds`, the following are pre-calculated:

| Input fields | Output fields |
|--------------|---------------|
| K, t, V | KtV = (K × t) / V |
| PreUrea, PostUrea | URR = (PreUrea − PostUrea) / PreUrea × 100 |

Example: `Documents__CalculatorTemplateIds=adequacy,dialysis-adequacy`

**Note:** Backend pre-calculation is used (no embedded PDF JavaScript) for maximum portability across viewers.

## References

- [FHIR-PDF-EHEALTH-INTEGRATION.md](../FHIR-PDF-EHEALTH-INTEGRATION.md)
- [ROADMAP-TIERS.md](../ROADMAP-TIERS.md) – Tiers 5–8
