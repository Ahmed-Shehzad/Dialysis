# Immediate / High Priority – Deep Dive Plan

This document provides a focused plan for the three immediate/high-priority workstreams required for C5 compliance and Prescription Phase 2 completion.

---

## 1. Authentication (C5) – JWT for All Business Endpoints

### Current State

- **No JWT authentication** – All API projects (Patient, Treatment, Alarm, Prescription) have no `AddAuthentication`, `AddJwtBearer`, or `[Authorize]` attributes.
- **Endpoints exposed without auth** – `PrescriptionController`, `Hl7Controller`, Patient, Treatment, and Alarm controllers are all anonymous.
- **C5 rule**: "All APIs require JWT; scope policies (Read/Write/Admin). No anonymous business endpoints except health."

### What’s Needed

| Step | Action | Details |
|------|--------|---------|
| 1 | Add JWT packages | `Microsoft.AspNetCore.Authentication.JwtBearer` (Central Package Management in `Directory.Packages.props`) |
| 2 | Configuration | Add `Authentication:JwtBearer` section (authority, audience, issuer); externalize to config/Key Vault per C5 |
| 3 | Wire auth in each API | `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` in Patient, Treatment, Alarm, Prescription `Program.cs` |
| 4 | Require auth globally | `app.UseAuthentication(); app.UseAuthorization();` and a global policy or `[Authorize]` on controllers |
| 5 | Exempt health/OpenAPI | Health checks (`/health`), OpenAPI (`/openapi/*`) remain anonymous via `[AllowAnonymous]` or endpoint metadata |
| 6 | Scope policies | Define policies (e.g. `Prescription:Read`, `Prescription:Write`, `Hl7:Ingest`) and apply via `[Authorize(Policy = "Prescription:Read")]` on actions |

### Implementation Checklist

- [x] Add `Microsoft.AspNetCore.Authentication.JwtBearer` to `Directory.Packages.props`
- [x] Add `Authentication:JwtBearer` configuration schema (Authority, Audience, RequireHttpsMetadata)
- [x] Wire auth in each API (`AddAuthentication().AddJwtBearer()` in Patient, Treatment, Alarm, Prescription `Program.cs`)
- [x] Add `[Authorize]` to all controllers except health/OpenAPI endpoints
- [x] Define scope policies (Read/Write/Admin) via `ScopeOrBypassRequirement` and apply per action
- [x] Add `Authentication:JwtBearer:DevelopmentBypass` for local testing when `IsDevelopment`
- [ ] Document JWT claims and how Mirth/integration clients obtain tokens
- [x] Add `X-Tenant-Id` handling for multi-tenancy (C5)
- [x] Add `IAuditRecorder` (C5 audit) to all controllers
- [x] Add `TenantResolutionMiddleware` and tenant-scoped Prescription persistence

### Files to Touch

- `Directory.Packages.props`
- `Services/Dialysis.Patient/Dialysis.Patient.Api/Program.cs`
- `Services/Dialysis.Treatment/Dialysis.Treatment.Api/Program.cs`
- `Services/Dialysis.Alarm/Dialysis.Alarm.Api/Program.cs`
- `Services/Dialysis.Prescription/Dialysis.Prescription.Api/Program.cs`
- All controller files (add `[Authorize]` and policies)
- `appsettings.json` / `appsettings.Development.json` for auth config
- `docs/SYSTEM-ARCHITECTURE.md` (auth flow)
- `.cursor/rules/mirth-integration.mdc` (already references JWT)

---

## 2. Prescription Profile Engine (P1) – QBP^D01 / RSP^K22 Full Flow

### Current State

| Component | Status | Notes |
|-----------|--------|-------|
| **QBP^D01 builder** | Done | `QbpD01Builder.Build(mrn, sendingApp, queryTag)` – builds outbound query |
| **RSP^K22 parser** | Done | Ingest: parses MSH, MSA, QAK, QPD, ORC, OBX; supports constant + profiled (LINEAR, EXPONENTIAL, STEP, CONSTANT, VENDOR) |
| **RspK22Validator** | Done | MSA-2, QPD-2 (QueryTag), QPD-1 (QueryName), MSA-1 (AA/AE/AR) |
| **ProfileCalculator** | Done | All 5 profile types with formulas |
| **PrescriptionSettingResolver** | Done | Resolves values at t=0 for API (BloodFlow, UF rate, UF target) |
| **RSP^K22 response builder** | Done | `RspK22Builder` serializes `Prescription` → HL7 RSP^K22 (ORC + OBX hierarchy) |
| **QBP^D01 parser** | Done | `QbpD01Parser` extracts MRN, MSH-10, QPD-2, QPD-1 |
| **Prescription-by-MRN API** | Done | `GET /api/prescriptions/{mrn}` returns JSON |
| **HL7 endpoint for QBP^D01** | Done | `POST /api/hl7/qbp-d01` receives QBP^D01, returns RSP^K22 (`application/x-hl7-v2+er7`) |

### What’s Needed

#### 2.1 QBP^D01 Parser

Parse incoming QBP^D01 to build `RspK22ValidationContext` for response validation:

- MSH-10 → Message Control ID
- QPD-2 → Query Tag
- QPD-3 → MRN (extract from `@PID.3^{MRN}^^^^MR`)
- QPD-1 → Query name (must be `MDC_HDIALY_RX_QUERY`)

**Output**: `QbpD01ParseResult(Mrn, MessageControlId, QueryTag, QueryName)`.

#### 2.2 RSP^K22 Response Builder

Serialize stored `Prescription` aggregate to HL7 RSP^K22:

- MSH (with MSH-10 = QBP request MSH-10)
- MSA (AA, MSH-10)
- QAK (QPD-1, QPD-2, status)
- QPD (echo QPD from request)
- ORC (Order Control, Placer Order Number, Ordering Provider, Callback Phone)
- PID (MRN)
- OBX segments: constant settings as `OBX|N|NM|{code}^MDC||{value}|{units}||||||||||{provenance}`
- OBX segments: profiled settings as facet objects:
  - `MDC_HDIALY_PROFILE_TYPE`
  - `MDC_HDIALY_PROFILE_VALUE` (tilde-separated)
  - `MDC_HDIALY_PROFILE_TIME` (if present)
  - `MDC_HDIALY_PROFILE_EXP_HALF_TIME` (if present)
  - `MDC_HDIALY_PROFILE_NAME` (for VENDOR)

**Reference**: `RspK22Parser` for inverse logic; Implementation Plan §3.2.2 (ORC structure), §3.2.5 (Profile Facet Objects).

#### 2.3 HL7 Endpoint for QBP^D01

- **Route**: e.g. `POST /api/hl7/qbp-d01` (raw HL7 body) or content-type `x-application/hl7-v2+er7`
- **Flow**:
  1. Parse QBP^D01 → `QbpD01ParseResult`
  2. Validate query name = `MDC_HDIALY_RX_QUERY`
  3. Look up prescription by MRN → `Prescription` or null
  4. If not found → build RSP^K22 with MSA|NF|… (no data found) or QAK status
  5. If found → build RSP^K22 from Prescription, validate MSA-2/QAK-1/QPD-3
  6. Return HL7 message as text (content-type `x-application/hl7-v2+er7` or `text/plain`)

#### 2.4 Profile Engine Gaps (from Implementation Plan)

| Gap | Status | Action |
|-----|--------|--------|
| OBX sub-ID (dotted notation) | Parser stores `SubId`; Builder must emit correct hierarchy | Map settings to IEEE 11073 containment (e.g. 1.1.9.x for UF) |
| Pumpable profile mapping | Parser uses generic `MDC_HDIALY_PROFILE` for profiled params | Confirm if we need pump-specific codes (UF, Dialysate, RF, etc.) per §3.2.6 |
| Rx Use column mapping | Not implemented | Phase 2 task: map Table 2 "Use" (M/C/O) to prescription-eligible params |
| Prescription conflict options | Not implemented | "Handle prescription conflict options (discard, callback, partial accept)" – lower priority |

### Implementation Checklist

- [x] Add `IQbpD01Parser` + `QbpD01Parser` (parse QBP^D01 → `QbpD01ParseResult`)
- [x] Add `IRspK22Builder` + `RspK22Builder` (Prescription + validation context → HL7 string)
- [x] Add `ProcessQbpD01QueryCommand` – parses query, loads prescription by MRN, builds RSP^K22
- [x] Add `POST /api/hl7/qbp-d01` to `Hl7Controller` – accepts raw HL7, returns RSP^K22
- [x] Unit tests: `QbpD01Parser`, `RspK22Builder`
- [ ] Integration test: QBP^D01 → RSP^K22 round-trip
- [x] Update docs: `SYSTEM-ARCHITECTURE.md` prescription flow

### Files to Create/Modify

- `Dialysis.Prescription.Application/Abstractions/IQbpD01Parser.cs`
- `Dialysis.Prescription.Application/Abstractions/QbpD01ParseResult.cs`
- `Dialysis.Prescription.Infrastructure/Hl7/QbpD01Parser.cs`
- `Dialysis.Prescription.Application/Abstractions/IRspK22Builder.cs`
- `Dialysis.Prescription.Infrastructure/Hl7/RspK22Builder.cs`
- `Dialysis.Prescription.Api/Controllers/Hl7Controller.cs` (add QBP^D01 endpoint)
- `Dialysis.Prescription.Api/Contracts/` (request/response if needed)
- Tests

---

## 3. Prescription EF Migrations ✓

### Current State

- **`MigrateAsync()`** in Prescription `Program.cs` when `IsDevelopment`
- **No migrations** – Prescription service has no EF migrations (unlike Transponder’s PostgreSql project)
- **Schema**: `Prescriptions` table with `Id`, `OrderId`, `PatientMrn`, `Modality`, `OrderingProvider`, `CallbackPhone`, `ReceivedAt`, `SettingsJson` (jsonb)

### Why Migrations Matter

- Versioned schema changes (add column, index, new table) without data loss
- Production-safe deployment (apply migrations on startup or via CI)
- Aligns with other services (Treatment, Alarm, Patient likely use migrations or similar)

### What’s Needed

| Step | Action |
|------|--------|
| 1 | Add `Microsoft.EntityFrameworkCore.Design` to Prescription Infrastructure (if not present) |
| 2 | Remove `EnsureCreatedAsync()` from Prescription `Program.cs` |
| 3 | Create initial migration: `dotnet ef migrations add InitialPrescriptionSchema --project Dialysis.Prescription.Infrastructure --startup-project Dialysis.Prescription.Api` |
| 4 | Apply migrations on startup: `await db.Database.MigrateAsync()` (or use a hosted service) |
| 5 | Document migration workflow in README/WIKI |

### Implementation Checklist

- [x] Resolve EF design-time issue (map `SettingsJson` as string; ignore `Settings` property)
- [x] Create `Dialysis.Prescription.Infrastructure/Migrations/` with initial migration
- [x] Replace `EnsureCreatedAsync()` with `MigrateAsync()` in `Program.cs`
- [ ] Verify migration produces schema equivalent to current EnsureCreated
- [ ] Add migration workflow to docs (how to add new migrations)
- [ ] Consider: run migrations in CI or at deploy time vs. app startup

### Caveats

- **Current blocker**: EF Core design-time fails with "No suitable constructor was found for ProfileDescriptor" when building the model for migrations. The `SettingsForPersistence` converter uses `List<ProfileSetting>`, and EF analyzes the element type including `ProfileDescriptor`. Workaround: keep `EnsureCreatedAsync()` until the model is refactored (e.g. map a string column only, or add parameterless constructor to value objects).
- If schema differs from current DB, may need a data migration or one-time script
- In dev, `MigrateAsync` will create/update; ensure connection string points to correct DB

---

## Summary: Execution Order

1. **Authentication** – Highest impact for C5; unblocks production readiness.
2. **Prescription Profile Engine** – Completes Phase 2 HL7 flow (QBP^D01 in, RSP^K22 out).
3. **EF Migrations** – Optional but recommended for schema versioning; can run in parallel with (1) or (2).

---

---

## 4. HL7 Implementation Guide Alignment Matrix

Cross-reference of the Dialysis Machine HL7 Implementation Guide (Rev 4.0) with the current implementation.

| Guide Requirement | HL7 Transaction | Status | Gap |
|---|---|---|---|
| Patient Demographics (PDQ) | QBP^Q22 / RSP^K22 | Done | — |
| Prescription Transfer | QBP^D01 / RSP^K22 | Done | Minor: OBX sub-ID, Rx Use column |
| Treatment Reporting (PCD-01) | ORU^R01 / ACK^R01 | Done | — |
| Alarm Reporting (PCD-04) | ORU^R40 / ORA^R41 | Done | — |
| HL7-to-FHIR Mapping | N/A | Done | — |
| C5 Auth / Audit / Tenant | N/A | Done | — |
| Tests | N/A | Done | Additional coverage possible |
| HL7 Batch Protocol | FHS/BHS/BTS/FTS | Not started | Lower priority |

### Remaining Gaps (Lower Priority)

| Gap | Category | Priority |
|---|---|---|
| OBX sub-ID dotted notation for IEEE 11073 containment | Prescription | P5 |
| Rx Use column (M/C/O from Table 2) | Prescription | P5 |
| Prescription conflict handling (discard/callback/partial) | Prescription | P5 |
| HL7 Batch Protocol (FHS/BHS/BTS/FTS) | Treatment | P5 |
| Integration test: QBP^D01 → RSP^K22 round-trip | Prescription | P3 |
| Document JWT claims and Mirth token workflow | Auth | P3 |

---

## References

- `.cursor/rules/c5-compliance.mdc` – Access control, audit, encryption, multi-tenancy
- `docs/Dialysis_Machine_HL7_Implementation_Guide/IMPLEMENTATION_PLAN.md` – Phase 2 prescription tasks
- `docs/ARCHITECTURE-CONSTRAINTS.md` – C5, technology stack
- `Services/Dialysis.Prescription/` – Current RSP^K22 parser, validator, ProfileCalculator, domain model
- `docs/PROCESS-DIAGRAMS.md` – All process diagrams for supervisor reporting
