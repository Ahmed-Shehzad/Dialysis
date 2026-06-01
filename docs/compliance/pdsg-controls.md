# PDSG (Patientendaten-Schutz-Gesetz) controls — Dialysis platform mapping

The German Patient Data Protection Act (in force since 2020) governs the **elektronische Patientenakte (ePA)** — the German electronic health record — and the **Telematikinfrastruktur (TI)**, the closed network operated by gematik that connects practices, hospitals, pharmacies, and patients to the ePA. Pair with [`gdpr-controls.md`](./gdpr-controls.md) and [`bdsg-controls.md`](./bdsg-controls.md).

## SMC-B authentication (institutional identity)

Every practice that connects to the TI must authenticate using a **SMC-B (Security Module Card type B)** — a smart card issued by gematik to the practice as an institution. Crypto operations (signing the authentication challenge during the IDP handshake; signing ePA-uploaded documents to prove authorship; unwrapping the per-document encryption keys) run **on the card**; the application only holds opaque handles.

The platform exposes the card via `ISmcBCardReader`. The default `StubSmcBCardReader` reports `IsPresent = false` and throws on any call — so any path that tries to talk to gematik without a card fails loudly. Production deployments register a real PC/SC-backed reader.

## Granular consent per ePA document

PDSG mandates **per-document, per-practitioner** consent on the ePA: the patient decides which document each practitioner can read. The platform's `IPatientConsentGateway` consults the new `EpaConsentSet` aggregate (separate from HIE's general clinical consent) before every ePA read/write. The consent UI (`/admin/data-protection/consents`) lets the patient toggle per-document permissions; the platform refuses ePA writes lacking explicit consent.

## TI environments (RU / TU / PU)

| Environment | Code | Use |
|---|---|---|
| Referenzumgebung | RU | Conformance testing only; no real patients |
| Testumgebung | TU | Integration testing with synthetic patients |
| Produktivumgebung | PU | Real patients; requires SMC-B + production-mode opt-in |

The platform's `GematikEndpointCatalog` ships URLs for all three. Operators select the environment per deployment; **PU requires an explicit `DataProtection:TiProductionMode = true` toggle**, surfaced on the operator dashboard as a yellow badge until a real SMC-B is presented.

## BSI TR-03161 cryptographic conformance

gematik mandates the Bundesamt für Sicherheit in der Informationstechnik (BSI) cryptographic catalog **TR-03161** for TI-side operations. The platform's TI client refuses to start if a weaker cipher set is configured. The conformance pack is verified in the CI suite gated behind `GEMATIK_CONFORMANCE_VECTORS=1`.

## Access logging on every ePA call

PDSG requires the patient to see who-accessed-what on their ePA. The platform's `IDataProtectionAuditEmitter` writes a `DataProtectionAuditEvent` carrying `EpaContext { DocumentId, FolderPath, EpaEnvironment }` for every ePA call. The patient-portal page `/portal/data-access-log` surfaces these rows.

## Discharge-letter ePA upload — operator walkthrough

1. Operator generates a discharge letter (PDMS Reporting slice).
2. Operator clicks "Publish to ePA" on the SessionLivePage Reports tab.
3. The platform consults `IPatientConsentGateway.AuthoriseAsync(... scope: EpaDocument ...)`.
4. If consent is missing, the UI prompts: "Patient consent required — request via portal? [Yes / Cancel]".
5. If consent is granted, the platform signs the document with the SMC-B's signing key, encrypts with the patient's ePA key (unwrapped via SMC-B's key-agreement), uploads via `IEpaUploadService.UploadAsync(...)`, and records the returned `EpaDocumentId` on the local `SessionReport`.
6. Every step writes an audit row carrying `EpaContext` + the consent record id.
