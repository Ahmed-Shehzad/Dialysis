# Medical coding systems — where each lives in the platform

The four primary US coding systems work together to translate every patient diagnosis,
procedure, and service into standardized codes for records and billing. This page maps each to
its home in the codebase, and records the gaps deliberately left for future slices.

| System | Role | Canonical FHIR system URI | Where it lives |
|---|---|---|---|
| **ICD-10-CM** | Diagnoses — "what's wrong" | `http://hl7.org/fhir/sid/icd-10-cm` | `Diagnosis.Icd10Code` (EHR ClinicalNotes), 837P claim diagnosis segments (`Edi837PClaimWriter`), `EhrCodeSystems.Icd10Cm` |
| **ICD-10-PCS** | Inpatient/institutional procedures | `http://www.cms.gov/Medicare/Coding/ICD10` | Declared (`EhrCodeSystems.Icd10Pcs`) for the 837I/UB-04 institutional path — see "gaps" below |
| **CPT** (HCPCS Level I) | Professional procedures & services | `http://www.ama-assn.org/go/cpt` | `Charge.CptCode`, `CptFeeScheduleEntry`, `PerformedProcedure` (+ modifiers), `EvaluationManagementCoder`, 837P SV1 |
| **HCPCS Level II** | Supplies, drugs (J-codes), DME | `https://www.cms.gov/Medicare/Coding/HCPCSReleaseCodeSets` | Rides the same `CptCode` fields — CPT and HCPCS Level II share the X12 `HC` qualifier on SV1, so J-codes (e.g. ESRD drugs like epoetin) flow through charge capture → claim unchanged |

Sibling clinical systems already wired: LOINC (lab orders/results), SNOMED CT, RxNorm + NDC
(medications), CVX (immunizations) — all in `EhrCodeSystems`
(`src/backend/EHR/Dialysis.EHR.Contracts/CodeSets/EhrCodeSystems.cs`).

## Notes and known gaps

- **`EhrCodeSystems.Icd10Pcs` URI was corrected** (2026-06): it previously pointed at
  `http://hl7.org/fhir/sid/icd-10` — the WHO ICD-10 *diagnosis* system — which would have
  mislabeled any future PCS-coded FHIR payload. No call sites existed, so the fix is
  non-breaking.
- **No code-system discriminator on `Charge`/`PerformedProcedure`**: both CPT and HCPCS Level II
  codes are accepted in the `CptCode` fields. This is wire-correct for 837P (shared `HC`
  qualifier) and deliberate — adding a `CodeSystem` column is schema churn with no consumer
  today. Revisit if FHIR `Coding.system` precision on charges becomes an exchange requirement
  (the code shape distinguishes them mechanically: CPT is 5 digits; Level II is a letter + 4
  digits).
- **ICD-10-PCS has no emitting surface yet**: the platform ships the 837P (professional) claim
  writer; the institutional path (837I/UB-04 — how freestanding ESRD facilities commonly bill,
  with revenue codes and PCS procedures) is declared in `EhrClaimFormats` but not implemented.
  That is the future slice where `Icd10Pcs` becomes load-bearing.
