# Operator guide — medication inventory

Stock receive / deduct / adjust against the `MedicationInventoryItem` aggregate via the
**Admin → Inventory** SPA page or directly via the HTTP API.

## The aggregate

One row per `(MedicationCoding, LotNumber)`. Fields:

- `MedicationCoding` — RxNorm / NDC / ATC code + display name.
- `LotNumber` — vendor-supplied batch identifier.
- `ExpiryUtc` — vendor-supplied expiration date (UTC midnight).
- `OnHandUnits` — current stock count.
- `Threshold` — operator-configured low-stock level.

Operations are method-driven; HTTP controllers (Inventory) bind directly to them:

| Method | When |
| ------ | ---- |
| `Receive(units, reason)` | Pharmacy delivery; operator increments stock |
| `Deduct(units, reason)` | Auto-fired by `OnMedicationAdministered` consumer on every MAR write |
| `Adjust(newOnHandUnits, reason)` | Physical-count reconciliation; operator corrects to match reality |

`Deduct` raises `MedicationInventoryLowIntegrationEvent` when `OnHandUnits <= Threshold`
— surfaces as an alert badge on the Admin → Inventory page and a notification to the
pharmacy on-call.

## Receiving stock

1. Open **Admin → Inventory**.
2. Find the row by medication name or scan the lot barcode (handheld → SPA via
   keyboard wedge input on the search bar).
3. Click the row → drawer opens.
4. Click **Receive**. Enter quantity + reason (e.g. "Vendor delivery 2026-06-02 PO-12345").
5. Save.

Behind the scenes:
```
POST /api/v1.0/inventory/{id}/receive
{ "units": 100, "reason": "Vendor delivery 2026-06-02 PO-12345" }
```

## Deduct (automatic)

Stock deduction is automatic: every administration on the MAR fires
`MedicationAdministeredIntegrationEvent`, the inventory consumer matches by RxNorm
code, and `Deduct(1, "session:{id}")` runs against the aggregate.

If the matching inventory row doesn't exist (medication administered but not stocked),
the consumer writes an `InventoryDeductFailed` audit row. The MAR write is NOT
blocked — administration is the source of truth; inventory mismatch surfaces as an
alert for pharmacy reconciliation.

## Adjusting after a physical count

When the operator counts physical stock and the system disagrees:

1. Inventory page → row → drawer → **Adjust**.
2. Enter the new on-hand count + reason (e.g. "Quarterly physical count 2026-06-30 —
   3 vials damaged, 1 vial missing").
3. Save.

`Adjust(newOnHandUnits, reason)` resets the aggregate to the supplied count. No
`Receive` / `Deduct` traffic — direct write. The audit trail captures who, when,
old value, new value, and the reason.

## Low-stock alerts

When `Deduct` drops `OnHandUnits` to ≤ `Threshold`:

1. `MedicationInventoryLowIntegrationEvent` raised.
2. Pharmacy on-call rotation paged via the same dispatcher the IV-pump alarms use
   (`IClinicianNotificationDispatcher`).
3. The Inventory page shows a yellow "Low stock" badge on the row until receive
   brings `OnHandUnits` back above `Threshold`.
4. The integration event is idempotent on `(InventoryItemId, OnHandUnits)` so
   repeated `Deduct` calls below threshold don't spam the pager.

## Expiry management

`ExpiryUtc` is captured on Receive and never modified. The retention pruner does NOT
touch inventory rows past expiry — operator policy is to keep them visible (with an
"Expired" badge) for 6 years (HGB §257). Operator adjusts to zero when discarding
expired stock with a reason citing the disposal log entry.

## Compliance gates

- **Lawful basis** — `medication.dispensing` (GDPR Art. 6(1)(c) Apothekengesetz +
  Art. 9(2)(h) healthcare provision).
- **Audit** — every Receive / Deduct / Adjust writes an `InventoryTransaction` audit
  row with the operator sub and the reason.
- **Retention** — 6 years (HGB §257, AO §147).

## See also

- `docs/pdms/medication-administration-tutorial.md` — the MAR side of the loop.
- `docs/compliance/gdpr-controls.md` — Art. 5 / Art. 6 mapping.
