# FHIR-to-PDF and eHealth Integration

Design for PDF generation, template filling, FHIR Document handling, and eHealth platform integration in Dialysis PDMS. **All capabilities below are in scope.**

---

## 1. Generate PDF from FHIR Data

**Capability:** Create PDF documents from FHIR resources (Patient, Encounter, Observation, Condition, etc.).

### Approach

| Technique | Use Case | Library |
|-----------|----------|---------|
| **HTML → PDF** | Summary reports, session summaries | Puppeteer, wkhtmltopdf, SelectPdf, DinkToPdf |
| **Direct PDF** | Structured forms, tables | QuestPDF, iText, PDFSharp |
| **Report templates** | Consistent layout | RDLC, Crystal Reports (export PDF) |

### Dialysis PDMS Use Cases

- **Session summary PDF** – Patient + Encounter + Observations (vitals, lab) for a dialysis session
- **Patient summary** – Demographics, conditions, recent encounters, vascular access
- **Measure report PDF** – Public health MeasureReport rendered as readable PDF

### API Design

```
POST /api/v1/documents/generate-pdf
  Body: { resourceType, resourceId?, bundle?, template?: "session-summary" | "patient-summary" | "measure-report" }
  Response: application/pdf
```

### Implementation Notes

- Use FHIR narrative (`Resource.text`) when present; otherwise derive from structured data
- Support HTML templates with placeholders (e.g. Mustache, Razor) → render → convert to PDF
- Consider QuestPDF (MIT) or iText (AGPL/commercial) for .NET

---

## 2. Fill Existing PDF Template Using FHIR

**Capability:** Populate a pre-designed PDF form (AcroForm) with data from FHIR resources.

### Approach

| Technique | Use Case |
|-----------|----------|
| **AcroForm fields** | Fill named form fields (e.g. `PatientName`, `SessionDate`) |
| **XFA** (deprecated) | Legacy XML-based forms |
| **PDF libraries** | iText (set form field values), PDFSharp (limited) |

### Dialysis PDMS Use Cases

- **Dialysis prescription form** – Pre-printed clinic form filled from PlanDefinition/CarePlan
- **Discharge summary** – Standard hospital form filled from Encounter + Observations
- **Consent form** – Patient demographics, procedure from FHIR

### API Design

```
POST /api/v1/documents/fill-template
  Body: { templateId, patientId, encounterId?, mappings: { "fieldName": "fhirPath" } }
  Response: application/pdf
```

### Implementation Notes

- Store template PDFs in blob storage or config path
- Map FHIR paths (e.g. `Patient.name[0].given[0]`) to form field names
- Flatten choice elements, handle missing values (blank or default)

---

## 3. JavaScript/Macros Inside PDF

**Capability:** Embed JavaScript or calculated fields in PDF for dynamic behavior (e.g. totals, validation).

### Approach

| Technique | Use Case | Caveats |
|-----------|----------|---------|
| **AcroForm JavaScript** | Calculate totals, format on blur | Reader support varies (Adobe full; others limited) |
| **Calculated fields** | Sum, product of form fields | PDF form field `calculate` action |
| **OpenAction** | Run script on open | Often disabled for security |

### Dialysis PDMS Use Cases

- **Adequacy calculator** – Kt/V, URR from input fields; validate ranges
- **Medication dosing** – Weight-based calc displayed in PDF
- **Form validation** – Require fields before submit

### Implementation Notes

- **Security:** Many PDF viewers block embedded JavaScript; document for target viewers
- **iText:** `PdfDocument.AddJavaScript()` for doc-level scripts
- **Alternative:** Pre-calculate in backend, fill as static values (more portable)

### API Design

```
POST /api/v1/documents/fill-template
  ?includeScripts=true   # Inject calculator/validation scripts
  Body: { templateId, ... }
```

---

## 4. Convert FHIR Document Bundle to PDF

**Capability:** Take a FHIR DocumentReference or Composition/Document Bundle (e.g. CDA-derived) and render as PDF.

### FHIR Document Model

| Resource | Role |
|----------|------|
| **Composition** | Document metadata, sections, author, date |
| **DocumentReference** | Points to Binary or contained resources |
| **Bundle** (document) | `type: document`; first resource is Composition |
| **Binary** | Raw content (e.g. PDF, CDA XML) |

### Approach

| Source | Conversion |
|--------|------------|
| **Composition + section references** | Resolve referenced resources; render sections to HTML/PDF |
| **Binary (contentType=application/pdf)** | Return as-is |
| **Binary (application/xml CDA)** | Parse CDA; transform to HTML/PDF (e.g. XSLT) |
| **FHIR R4 narrative** | Use `Composition.section.text` → HTML → PDF |

### Dialysis PDMS Use Cases

- **Discharge document** – CDA-based document from external system → PDF for patient portal
- **Clinical note** – Composition with sections (History, Plan) → PDF

### API Design

```
POST /api/v1/documents/bundle-to-pdf
  Body: FHIR Bundle (document) or { documentReferenceId }
  Response: application/pdf
```

---

## 5. Embed PDF Inside FHIR

**Capability:** Store generated or received PDF as FHIR Binary and reference via DocumentReference.

### Approach

| Resource | Use |
|----------|-----|
| **Binary** | `contentType: application/pdf`; `data` base64 or use `url` for large files |
| **DocumentReference** | `content.attachment` points to Binary or external URL |
| **DiagnosticReport** | `presentedForm` – PDF report attachment |

### Dialysis PDMS Use Cases

- **Session summary PDF** – Generate PDF → POST Binary → create DocumentReference
- **Scanned consent** – Upload PDF → Binary → DocumentReference for consent
- **External report** – Reference external PDF URL in DocumentReference

### API Design

```
POST /api/v1/documents
  Body: multipart (file=PDF) or { base64Data, patientId, type }
  Creates: Binary + DocumentReference
  Response: { documentReferenceId, binaryId }
```

### Implementation Notes

- Use FHIR Gateway/Binary endpoint or custom storage with Binary.url
- DocumentReference: `docStatus=final`, `type` LOINC for document type, `content.attachment.contentType=application/pdf`

---

## 6. eHealth Integration (eHIR Platform)

**Capability:** Integrate with national eHealth platforms (e.g. German ePA, French DMP, gematik TI) for document exchange and patient access.

### eHealth Platforms (Examples)

| Platform | Region | Protocol |
|----------|--------|----------|
| **gematik TI (Germany)** | DE | OIDC, FHIR (ePA), CDA, XCA |
| **ePA (Elektronische Patientenakte)** | DE | KIM, FdV, FHIR R4 |
| **DMP (Dossier Médical Partagé)** | FR | FHIR, national API |
| **NHS login / Spine** | UK | FHIR, OAuth |

### Integration Patterns

| Pattern | Description |
|---------|-------------|
| **Document upload** | Push PDF/CDA to eHealth platform on behalf of patient |
| **Document query** | Query patient documents from eHealth (XCA, FHIR) |
| **Patient consent** | Check/record patient consent for ePA access |
| **Identity linkage** | Map PDMS patient to eHealth identity (KVNR, INS) |

### Dialysis PDMS – eHIR Alignment

- **Documents:** Session summaries, discharge notes as PDF/CDA → upload to ePA
- **Consent:** Dialysis.AuditConsent records consent events; sync with ePA consent
- **Identity:** Patient.identifier with KVNR (DE) or INS (FR) for linkage

### API Design (Stub)

```
POST /api/v1/ehealth/upload
  Body: { documentReferenceId, platform: "epa" | "dmp", patientIdentifier }
  Pushes document to eHealth platform

GET /api/v1/ehealth/documents
  ?patientId=&platform=epa
  Lists documents available from eHealth for patient
```

### Implementation Notes

- **Germany:** gematik Konnektor, FdV (Fachdienst Verzeichnis), CDA CH:EMED
- **Certification:** eHealth platforms require conformance/certification
- **Middleware:** Consider dedicated adapter service (e.g. Dialysis.EHealthGateway)

---

## Architecture Overview

```
                    ┌─────────────────────────────────────────┐
                    │         Dialysis PDMS                    │
┌──────────┐        │  ┌─────────────────────────────────┐   │
│ FHIR     │        │  │ Dialysis.Documents (new)        │   │
│ Store    │◄───────┤  │ - Generate PDF from FHIR        │   │
└──────────┘        │  │ - Fill PDF template             │   │
                    │  │ - Bundle → PDF                  │   │
                    │  │ - Embed PDF (Binary + DocRef)    │   │
                    │  └──────────────┬──────────────────┘   │
                    │                 │                     │
                    │  ┌──────────────▼──────────────────┐   │
                    │  │ Dialysis.EHealthGateway (new)   │   │
                    │  │ - ePA / DMP / national adapters │   │
                    │  └─────────────────────────────────┘   │
                    └─────────────────────────────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │ eHealth Platform (ePA, DMP, …)   │
                    └─────────────────────────────────┘
```

---

## Roadmap (Future Phases)

| Phase | Deliverable | Priority |
|-------|-------------|----------|
| **Phase 12 (PDF core)** | Generate PDF from FHIR; fill template; Bundle→PDF | High |
| **Phase 13 (Documents)** | Embed PDF in FHIR; DocumentReference CRUD; Binary storage | High |
| **Phase 14 (PDF advanced)** | JavaScript/macros in filled PDFs; calculator templates | Medium |
| **Phase 15 (eHealth)** | eHealth gateway; ePA/DMP adapter (jurisdiction config) | Medium (jurisdiction-specific) |

---

## Microsoft Data Integration Toolkit (FHIR–Dataverse Sync)

For organizations using **Microsoft for Healthcare** and Power Platform, the [Data Integration Toolkit](https://learn.microsoft.com/en-us/industry/healthcare/business-applications/data-integration-toolkit-manage-fhir-data) syncs PHI between EHR systems and Dataverse. Care teams and patients get fast, secure access to data in the Microsoft for Healthcare environment.

### Key Features

| Feature | Description |
|---------|-------------|
| **Entity maps** | FHIR resources ↔ Dataverse entities (e.g. Patient → Contact, Encounter → msemr_encounter) |
| **Attribute maps** | FHIR elements ↔ Dataverse columns; JSONPath, data type mapping |
| **Expansion maps** | Complex JSON → relational Dataverse (e.g. patient identifiers, links) |
| **Logs** | Transaction logs for troubleshooting; no EMR data stored |
| **Consent flow** | Per-patient sync enablement; consent before data flows |

### Dialysis PDMS Relevance

- **Dual architecture:** Dialysis PDMS FHIR store remains the source; Dataverse can sync for Power Apps, care coordination, patient portals.
- **Entity alignment:** Toolkit maps Patient, Encounter, Observation, Condition, Procedure, etc. — aligns with dialysis session data.
- **Writeback:** Changes in Dataverse can post back to FHIR endpoint (with consent).
- **Alternative to custom sync:** Use toolkit instead of building FHIR–CRM sync from scratch.

**Reference:** [Manage FHIR data using data integration toolkit](https://learn.microsoft.com/en-us/industry/healthcare/business-applications/data-integration-toolkit-manage-fhir-data)

---

## References

- [Manage FHIR data using data integration toolkit](https://learn.microsoft.com/en-us/industry/healthcare/business-applications/data-integration-toolkit-manage-fhir-data)
- [FHIR R4 DocumentReference](https://hl7.org/fhir/documentreference.html)
- [FHIR R4 Composition](https://hl7.org/fhir/composition.html)
- [FHIR R4 Binary](https://hl7.org/fhir/binary.html)
- [gematik ePA](https://fachportal.gematik.de/)
- [QuestPDF](https://www.questpdf.com/)
- [iText 7](https://itextpdf.com/)
