---
name: Next Steps – Indexes and Integration Tests
overview: Add read-model indexes for query performance; add integration tests for FHIR export, CDS, and Reports.
todos:
  - id: idx-patient
    content: Add TenantId index to PatientDbContext for list/search
    status: completed
  - id: idx-treatment
    content: Add (TenantId, StartedAt) index to TreatmentDbContext for list/search
    status: completed
  - id: int-fhir
    content: Add integration test for FhirBulkExportService
    status: completed
  - id: int-cds-reports
    content: Add integration tests for CDS and Reports flows
    status: completed
---

## Context

- ReadModels query by TenantId, MRN, SessionId, etc.; some lack supporting indexes.
- Patient/Treatment write DbContexts own schema; Read DbContexts map same tables.

## Index Additions

### Patient
- `IX_Patients_TenantId` – for GetAllForTenant and Search (TenantId always first)

### Treatment
- `IX_TreatmentSessions_TenantId_StartedAt` – for GetAllForTenant (OrderBy StartedAt DESC) and Search (date range)

## Integration Tests

### FhirBulkExportService
- Mock HttpClient to return valid JSON Bundles from backends; assert ExportAsync merges correctly.

### CDS / Reports
- Unit-style tests for PrescriptionComplianceService and ReportsAggregationService with mocked HttpClient.
