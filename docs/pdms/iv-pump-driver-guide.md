# IV pump driver guide

Four vendor wire formats converge on one `IvPumpReading`. This guide covers what each
driver expects on the wire and how to wire a new pump (vendor-specific or standards-
conformant) into the platform.

## Driver matrix

| Vendor code     | Wire format                                       | Driver                       |
| --------------- | ------------------------------------------------- | ---------------------------- |
| `bd-alaris`     | BD Alaris CareFusion Connectivity Interface JSON  | `BdAlarisCqiDriver`          |
| `baxter-sigma`  | Baxter SIGMA Spectrum drug-library JSON           | `BaxterSigmaDriver`          |
| `plum-360`      | Hospira / ICU Medical Plum 360 snake-case JSON    | `HospiraPlum360Driver`       |
| `pcd04`         | HL7 v2 IHE PCD-04 (standards-conformant fallback) | `Pcd04NormalisedDriver`      |

All four drivers implement `IIvPumpDriver`:

```csharp
public interface IIvPumpDriver
{
    string VendorCode { get; }
    Task<IvPumpReading> ParseAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
```

## HTTP ingress

Vendor edge agents POST raw payloads at:

```
POST /api/v1.0/iv-pumps/telemetry?vendor=<code>&sessionId=<guid>&chairId=<guid>
Content-Type: application/json | application/octet-stream | text/plain

<vendor-shaped body>
```

`IvPumpsController` dispatches to the driver registered for `vendor` and applies the
parsed reading to the matching `IvPumpInfusion` aggregate (create on `Start`, update on
`Progress` / `Pause` / `Resume` / `Alarm` / `Complete`).

## PCD-04 — the standards-conformant fallback

Any pump that exposes IHE PCD-04 (Point-of-Care Device data) feeds through
`Pcd04NormalisedDriver` regardless of brand. The driver reads:

- **MSH-7** — message timestamp (`yyyyMMddHHmmss`)
- **MSH-10** — message control id (used as the reading's sequence number)
- **PRT-10** — device identifier (the pump's serial)
- **OBX-3** observation identifier (LOINC) + **OBX-5** observation value, for these LOINC codes:
  - `69869-3` — IV infusion rate (mL/h, actual)
  - `69870-1` — IV infused volume (mL)
  - `69871-9` — IV programmed rate (mL/h)
  - `69872-7` — IV programmed volume (mL)
- **RXR-1** — drug RxNorm code (when the vendor emits it)

The driver throws `FormatException` when PRT-10 is missing — the controller surfaces
this as a 400 to the agent so the operator can investigate.

## Adding a new vendor driver

1. Implement `IIvPumpDriver` in `Dialysis.PDMS.Medications.IvPumps`.
2. `VendorCode` must be stable; vendor edge agents key on it.
3. Map the vendor wire shape to one `IvPumpReadingKind` per emission:
   `Start` → `Progress` repeatedly → `Complete` (normal) or `Alarm` (operator
   intervention). `Pause` / `Resume` for clinical holds.
4. Register the driver in `PdmsCompositionExtensions`:
   ```csharp
   services.AddSingleton<IIvPumpDriver, MyVendorDriver>();
   ```
5. Add a parse-correctness test in `Dialysis.PDMS.Tests/Medications/IvPumpDriverTests.cs`
   following the existing pattern (input → assert on the recovered `IvPumpReading`).

## Alarm handling

When a driver emits a reading with `Kind = Alarm`:

1. The infusion aggregate's `MarkAlarm(...)` raises
   `IvPumpAlarmRaisedIntegrationEvent`.
2. The PDMS On-Call slice's `OnIvPumpAlarmRaisedConsumer` looks up the active
   `OnCallRotation` for the chair, picks the primary chain link, and dispatches via
   `IClinicianNotificationDispatcher`.
3. Bodies are PHI-minimised: the SMS reads
   `"Chair {chairLabel} alarm: {alarmCode}. Acknowledge in the app."` — no patient
   name, no MRN.
4. Acknowledge in the app via `POST /api/v1.0/oncall/{chairId}/acknowledge`.

The full delivery audit (which clinician received which alarm, on which channel, when,
and the response latency) surfaces in `/admin/oncall/audit`.

## Compliance gates

Per-event:

- **Lawful basis** — `health.treatment.emergency` for alarms; `health.treatment` for
  routine telemetry (GDPR Art. 6(1)(b) + Art. 9(2)(h)).
- **PHI minimisation** — alarm notifications follow GDPR Art. 5(1)(c).
- **Audit** — every reading writes a `IvPumpTelemetryReceived` audit row;
  every alarm dispatch writes a per-attempt delivery row.

## See also

- `docs/pdms/medications-and-on-call.md` — the on-call rotation + escalation policy.
- `docs/compliance/gdpr-controls.md` — Art. 5 / Art. 6 / Art. 9 mapping.
