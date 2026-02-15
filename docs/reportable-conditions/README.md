# Reportable Conditions Registry

Configurable registry mapping condition codes (ICD-10, SNOMED, LOINC) to public health reporting requirements by jurisdiction.

## Structure

- **REPORTABLE-CONDITIONS-REGISTRY.json** – Sample registry with dialysis-relevant conditions
- Jurisdiction codes (e.g. `DE` for Germany)
- Condition codes with `reportable: true` and applicable jurisdictions

## Dialysis-Specific Conditions

| Code System | Code | Display | Notes |
|-------------|------|---------|-------|
| SNOMED | 403191000 | Infection of vascular access device | Vascular access infection |
| SNOMED | 267063004 | Bloodstream infection | Bacteremia |
| ICD-10 | B99.0 | Unspecified bacterial infection | General bacterial |

## Future Integration

- **Dialysis.PublicHealth** service will load this config
- On `Condition` or `Observation` create/update, match against registry
- If reportable: generate HL7 v2 or FHIR report and queue for delivery

See [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](../PUBLIC-HEALTH-RESEARCH-REGISTRIES.md).
