# TEFCA QHIN sandbox runbook

The QHIN onboarding admin surface (`/hie/admin/tefca/partners`) shipped in PR #135
with the full lifecycle: revise endpoints, attach trust anchors, rotate mTLS,
issue IAS JWTs, transition `Onboarding → Active → Suspended`. This runbook walks
those operations against a synthetic "Acme Sandbox QHIN" partner that the dev
stack seeds on startup when `Hie:Demo:Enabled=true` — so a real activation
sequence can be exercised without a real partner identified.

## Why a sandbox partner

Production TEFCA QHIN onboarding waits on a real partner being identified — the
trust-anchor chain has to come from their CA, the mTLS material has to be
signed by their issuing authority, and the IAS scope set is negotiated per
agreement. None of that is meaningful with placeholder credentials.

But the admin UI + the activation invariants (≥ 1 trust anchor, mTLS material
on file) shouldn't sit untested until the first real partner shows up. The
sandbox partner exercises the *workflow* — what an operator clicks through —
against a row that's clearly synthetic (RFC 2606 `.example` URLs, the
`demo-seeder` `UpdatedBy` field, a deterministic UUID v7 with a recognizable
prefix).

## Activate the sandbox

Start the Aspire stack with the demo flag on:

```bash
Hie__Demo__Enabled=true dotnet run --project src/aspire/Dialysis.AppHost
```

The `HieTefcaSandboxSeeder` hosted service materializes one partner row:

| Field | Value |
|---|---|
| Id | `0190a000-0000-7777-8000-000000000001` |
| Name | `Acme Sandbox QHIN` |
| FhirBaseUrl | `https://sandbox-qhin.example/fhir` |
| IasEndpoint | `https://sandbox-qhin.example/ias` |
| Status | `Onboarding` |
| TrustAnchors | (empty) |
| MtlsCertThumbprint | `null` |

It's idempotent — re-running the AppHost doesn't duplicate the row.

## Walkthrough — onboarding to Active

Open `http://localhost:9090/hie/admin/tefca/partners`. The Acme Sandbox QHIN
row should appear with status `Onboarding`. The activation invariant requires
**one trust anchor + mTLS material on file** before a transition to `Active` is
accepted.

### Step 1 — Generate a self-signed trust anchor (operator workstation)

```bash
openssl req -x509 -newkey rsa:2048 -nodes \
  -keyout sandbox-anchor.key \
  -out sandbox-anchor.crt \
  -days 365 \
  -subj "/CN=Acme Sandbox QHIN Root/O=Acme Health/C=US"
cat sandbox-anchor.crt
```

The on-screen `BEGIN CERTIFICATE` / `END CERTIFICATE` block is what gets pasted
into the admin UI.

### Step 2 — Attach via the admin UI

1. Click **Manage** on the Acme Sandbox QHIN row → opens the trust-anchor + mTLS
   drawer.
2. Paste the certificate PEM into the **Attach trust anchor** field → click
   **Attach**.
3. The row's `TrustAnchorCount` ticks from `0` to `1`. The `TrustAnchorParser`
   (using `X509Certificate2.CreateFromPem`) populates Subject, Thumbprint,
   NotBefore/NotAfter automatically.

### Step 3 — Generate + upload an mTLS PFX

```bash
openssl pkcs12 -export \
  -out sandbox-mtls.pfx \
  -inkey sandbox-anchor.key \
  -in sandbox-anchor.crt \
  -passout pass:sandbox-mtls-passw0rd
base64 -i sandbox-mtls.pfx > sandbox-mtls.pfx.b64
```

Paste the base64 PFX + the password into the **Rotate mTLS certificate** form
and submit. The `RotateMtlsCertificateCommand` uses
`X509CertificateLoader.LoadPkcs12` to extract the thumbprint, stores the PFX
via `IDocumentBlobStore`, and writes the thumbprint back onto the partner row.
The drawer now shows a populated `MtlsCertThumbprint` field.

### Step 4 — Transition to Active

The status-transition button now lights up because the invariant holds. Clicking
**Activate** fires `TransitionQhinPartnerStatusCommand` with `next = Active`. The
domain rule (`QhinPartner.TransitionStatus`) sanity-checks the invariant again
server-side; if either piece is missing it throws `InvalidOperationException`
and the controller returns 409.

After the transition, the row shows status `Active` and you can:

- Issue an IAS JWT (the partner's outbound auth header): hit
  `POST /api/v1.0/tefca/partners/{id}/ias-jwt` with `{ subjectPatientId, scope, lifetimeSeconds }`.
  Returns the signed token; verify it via `jwt.io` using the HMAC secret from
  `Tefca:IasJwtIssuer:SigningKey`.
- Test outbound dispatch — for the sandbox, point your test FHIR client at
  `https://sandbox-qhin.example/fhir` (it won't resolve; that's the point —
  the test exercises the request shape, not the partner).

### Step 5 — Suspend / re-activate

The Suspend button is always available on Active partners. Clicking it transitions
to `Suspended` (no invariant); re-clicking Activate transitions back to `Active`
(invariant re-checked).

## What the sandbox does NOT cover

| Concern | Why not |
|---|---|
| TEFCA Common Agreement / RCE-issued cert chain | Self-signed certs satisfy the schema, not the contract — real activation requires a TEFCA-recognized CA chain |
| Real partner's IAS scope vocabulary | Each QHIN publishes its own scope set; the sandbox demonstrates the API surface only |
| Partner-side webhook subscriptions | The sandbox URLs don't resolve; outbound dispatch will 503 / DNS-fail in the field |
| Multi-anchor rollover | Attach a second anchor manually if you want to exercise the rotation flow |

## Cleanup

Disable the demo flag (`Hie__Demo__Enabled=false`) and restart. The sandbox row
remains in the DB; delete it manually if you want a clean slate:

```sql
-- inside hie_tefca schema on the HIE Postgres
DELETE FROM "QhinTrustAnchors" WHERE "PartnerId" = '0190a000-0000-7777-8000-000000000001';
DELETE FROM "QhinPartners" WHERE "Id" = '0190a000-0000-7777-8000-000000000001';
```

(Or just `kubectl exec` into the CNPG primary and run the same SQL.)
