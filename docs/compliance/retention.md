# Document Retention Runbook (HIE Documents)

How the DPO adopts and operates the GDPR Art. 5(1)(e) *storage limitation* pipeline for
HIE-held documents. Backed by `DocumentRetentionPolicy`, the `RetentionPoliciesController`
(`/api/v1.0/documents/retention/...`), and the `RetentionPurgerHostedService`.

> **Nothing is purged until the DPO adopts policies _and_ enables the purger.** No windows are
> seeded and `Documents:Retention:AutoPurge` defaults to `false`, so the platform never deletes
> clinical data on its own. This is deliberate — retention is a controller decision, not a
> platform default.

## The two moving parts

1. **Retention policies** — one window per `DocumentReference.Kind`, set by the DPO. A document
   becomes eligible for purge when `CreatedAtUtc < now - RetentionDays`.
2. **The purger** — `RetentionPurgerHostedService` ticks every 24 h. When
   `Documents:Retention:AutoPurge=true`, it walks each policy's expired documents (in `take`
   batches) and purges them; when `false` it logs that it's disabled and returns.

## What "purge" does (tombstone, not data loss)

A purged document is **not** hard-deleted. It transitions to `EnteredInError` with
`StorageRef = purged://…`, and the blob bytes are removed via `IDocumentBlobStore.DeleteAsync`.
The row survives as a tombstone so an audit replay sees a deliberate, attributable purge rather
than a gap. Erasure (Art. 17) is a separate pipeline — see `data-subject-rights-runbook.md`.

## Operator surface

All routes require auth and are audited via `[PhiAccess]`:

| Action | Endpoint | Permission |
|---|---|---|
| List policies | `GET /api/v1.0/documents/retention/policies` | `hie.documents.retention.list` |
| Upsert a window | `PUT /api/v1.0/documents/retention/policies/{kind}` body `{ "retentionDays": N }` | `hie.documents.retention.upsert` |
| Remove a window | `DELETE /api/v1.0/documents/retention/policies/{kind}` | `hie.documents.retention.delete` |

The admin UI exposes the same at `/hie/admin/documents/retention`.

## Suggested starting windows (review before adopting)

`Kind` is free-form; below are conservative defaults aligned with the German legal bases the
controller is most likely operating under. **These are a starting point — the DPO must confirm
them against the clinic's processing register and any sector rules before applying.**

| Kind (example) | Suggested window | Legal basis |
|---|---|---|
| `ClinicalNote`, `DischargeSummary`, `LabReport` | 3650 days (10 yr) | Patientenrechtegesetz / BGB §630f(3) — 10-year clinical record retention |
| `RadiologyImage`, `TransfusionRecord` | 10950 days (30 yr) | Sector retention for imaging / transfusion records |
| `Invoice`, `BillingExport`, `AccountingDoc` | 3650 days (10 yr) | HGB §257 — commercial/accounting documents |
| `Consent`, `ConsentWithdrawal` | 3650 days (10 yr) | Evidence of lawful basis (BDSG §22 special-category processing) |
| `CorrespondenceDraft`, `ScratchExport` | 90 days | No statutory minimum — minimise per Art. 5(1)(e) |

Windows shorter than a statutory minimum are a compliance risk; windows much longer than
necessary defeat storage limitation. When in doubt, keep the longer statutory window.

## Adoption checklist

- [ ] DPO maps each `Kind` the deployment actually uses to a window from the register above.
- [ ] `PUT` each window via the API / admin UI; confirm with `GET .../policies`.
- [ ] Spot-check on a non-prod environment: create a document with a backdated `CreatedAtUtc`
      past its window, run a purger tick, confirm it becomes `EnteredInError` with a
      `purged://` storage ref and the blob is gone.
- [ ] Set `Documents:Retention:AutoPurge=true` on the HIE host only after the windows are
      confirmed and the spot-check passes.
- [ ] Record the adopted windows in the processing register (`docs/compliance/gdpr-controls.md`).

## Config reference

```jsonc
// HIE host appsettings (or env)
"Documents": {
  "Retention": {
    "AutoPurge": false   // flip to true only after policies are adopted + spot-checked
  }
}
```
