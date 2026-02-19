---
name: Code Quality, CDS, HL7/FHIR, UI, Observability
overview: Refactor Reports (S3776), extract HypotensionController, add venous pressure and blood leak CDS, PDQ date range, FHIR search, OpenTelemetry, React SPA, Grafana reference.
todos:
  - id: reports-refactor
    content: Refactor ParseDurationByPatient/ParseObservationsByCode to reduce cognitive complexity
    status: completed
  - id: hypotension-controller
    content: Extract HypotensionRiskController from PrescriptionComplianceController
    status: completed
  - id: venous-pressure-cds
    content: Add CDS rule for venous pressure limits (high > 200 mmHg)
    status: completed
  - id: blood-leak-cds
    content: Add CDS rule for blood leak alert
    status: completed
  - id: pdq-date-range
    content: Add PDQ date range query format (QPD-3 @PID.7 or similar)
    status: completed
  - id: fhir-search-params
    content: Document/add FHIR search parameters; IG tracking doc
    status: completed
  - id: otel-traces
    content: Add OpenTelemetry tracing across APIs
    status: completed
  - id: structured-logging
    content: Add structured logging (Serilog or Microsoft.Extensions.Logging)
    status: completed
  - id: react-spa
    content: Create React TypeScript SPA with dashboards (reports, FHIR)
    status: completed
  - id: grafana-docs
    content: Add Grafana dashboard reference/example for Prometheus metrics
    status: completed
isProject: true
---

# Code Quality, CDS, HL7/FHIR, UI, Observability Plan

## 1. Code Quality

- **ReportsAggregationService**: Extract `FhirBundleParser` static class with `ParseProceduresForDurationByPatient`, `ParseObservationsByCode` to reduce cognitive complexity (S3776).
- **CDS**: Extract `HypotensionRiskController` (route `api/cds`, separate controller).

## 2. New CDS Rules

| Rule | Code | Threshold | Service |
|------|------|-----------|---------|
| Venous pressure high | MDC_PRESS_BLD_VEN | > 200 mmHg | VenousPressureCdsService |
| Blood leak | MDC_ALARM_BLOOD_LEAK or equivalent | Any positive | BloodLeakCdsService |

## 3. HL7/FHIR

- **PDQ date range**: QPD-3 supports @PID.7 (birthdate); extend parser for date range if Guide allows.
- **FHIR search**: Document existing params; add IG version tracking doc.

## 4. Observability

- **OpenTelemetry**: Add `OpenTelemetry.Instrumentation.AspNetCore` to Gateway + APIs; export to Jaeger/OTLP or console.
- **Structured logging**: Ensure Activity/Telemetry correlation; Serilog JSON if not present.

## 5. UI

- **React + TypeScript SPA**: Vite, fetch /api/reports/*, FHIR $export; dashboards for sessions-summary, alarms-by-severity, prescription-compliance.

## 6. Grafana

- **docs/GRAFANA-DASHBOARD.md**: Example Prometheus queries and dashboard JSON for Gateway metrics.
