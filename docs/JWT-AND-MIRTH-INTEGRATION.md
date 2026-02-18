# JWT and Mirth Integration

This document describes JWT authentication for the Dialysis PDMS APIs and how Mirth Connect (or similar integration engines) obtain and use tokens for machine-to-machine (M2M) communication.

---

## 1. JWT Claims Expected by PDMS APIs

The PDMS validates JWTs issued by an OAuth 2.0 / OpenID Connect authority (e.g., Azure AD). The following claims are used:

| Claim | Description | Used By |
|-------|-------------|---------|
| `sub` | Subject – unique identifier of the client/principal | Audit, logging |
| `aud` | Audience – must match the API identifier (e.g., `api://dialysis-pdms`) | JwtBearer middleware validates that the token was issued for this API |
| `scope` or `scp` | Space-separated list of scopes | `ScopeOrBypassHandler` – authorization policies check that the caller has at least one of the required scopes (e.g., `Prescription:Read`) |
| `iss` | Issuer – validated against the configured Authority | JwtBearer middleware |
| `exp` | Expiration – token validity | JwtBearer middleware |

---

## 2. Configuration

Each API (Patient, Prescription, Treatment, Alarm) reads JWT settings from configuration:

```json
{
  "Authentication": {
    "JwtBearer": {
      "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
      "Audience": "api://dialysis-pdms",
      "RequireHttpsMetadata": true,
      "DevelopmentBypass": false
    }
  }
}
```

| Setting | Description |
|---------|-------------|
| `Authority` | OAuth 2.0 metadata endpoint; e.g., Azure AD v2.0 (`https://login.microsoftonline.com/{tenant-id}/v2.0`) or `common` for multi-tenant |
| `Audience` | Expected `aud` claim in the token; typically the API Application ID URI (e.g., `api://dialysis-pdms`) |
| `RequireHttpsMetadata` | Whether to require HTTPS for metadata discovery; set `true` in production |
| `DevelopmentBypass` | When `true` and environment is Development, all requests pass authorization without a token (local testing only) |

---

## 3. Scope Policies

Endpoints are protected by scope-based policies. A caller must have at least one of the allowed scopes.

| Service | Policy | Allowed Scopes |
|---------|--------|----------------|
| Prescription | `PrescriptionRead` | `Prescription:Read`, `Prescription:Admin` |
| Prescription | `PrescriptionWrite` | `Prescription:Write`, `Prescription:Admin` |
| Patient | `PatientRead` | `Patient:Read`, `Patient:Admin` |
| Patient | `PatientWrite` | `Patient:Write`, `Patient:Admin` |
| Treatment | `TreatmentRead` | `Treatment:Read`, `Treatment:Admin` |
| Treatment | `TreatmentWrite` | `Treatment:Write`, `Treatment:Admin` |
| Alarm | `AlarmRead` | `Alarm:Read`, `Alarm:Admin` |
| Alarm | `AlarmWrite` | `Alarm:Write`, `Alarm:Admin` |

### 3.1 API Endpoints and Required Scopes

| API | Method | Route | Policy | Scope for Token |
|-----|--------|-------|--------|------------------|
| Prescription | POST | `/api/hl7/qbp-d01` | PrescriptionRead | `Prescription:Read` or `Prescription:Admin` |
| Prescription | POST | `/api/hl7/rsp-k22` | PrescriptionRead | `Prescription:Read` or `Prescription:Admin` |
| Prescription | GET | `/api/prescriptions/{mrn}` | PrescriptionRead | `Prescription:Read` or `Prescription:Admin` |
| Prescription | GET | `/api/prescriptions/{mrn}/fhir` | PrescriptionRead | `Prescription:Read` or `Prescription:Admin` |
| Prescription | GET | `/api/audit-events` | PrescriptionRead | `Prescription:Read` or `Prescription:Admin` |
| Patient | POST | `/api/hl7/qbp-q22` | PatientRead | `Patient:Read` or `Patient:Admin` |
| Patient | GET | `/api/patients/mrn/{mrn}` | PatientRead | `Patient:Read` or `Patient:Admin` |
| Patient | GET | `/api/patients/mrn/{mrn}/fhir` | PatientRead | `Patient:Read` or `Patient:Admin` |
| Patient | GET | `/api/patients/search` | PatientRead | `Patient:Read` or `Patient:Admin` |
| Patient | POST | `/api/patients` | PatientWrite | `Patient:Write` or `Patient:Admin` |
| Treatment | POST | `/api/hl7/oru` | TreatmentWrite | `Treatment:Write` or `Treatment:Admin` |
| Treatment | POST | `/api/hl7/oru/batch` | TreatmentWrite | `Treatment:Write` or `Treatment:Admin` |
| Treatment | GET | `/api/treatment-sessions/{sessionId}` | TreatmentRead | `Treatment:Read` or `Treatment:Admin` |
| Treatment | GET | `/api/treatment-sessions/{sessionId}/fhir` | TreatmentRead | `Treatment:Read` or `Treatment:Admin` |
| Treatment | WebSocket | `/transponder/transport` (Transponder SignalR hub) | TreatmentRead | `Treatment:Read` or `Treatment:Admin` (token via `access_token` query param) |
| Alarm | POST | `/api/hl7/alarm` | AlarmWrite | `Alarm:Write` or `Alarm:Admin` |

---

## 4. How Mirth Obtains Tokens (Client Credentials)

Mirth Connect acts as a **server-side integration engine** and typically uses the **OAuth 2.0 Client Credentials** flow to obtain an access token for the PDMS APIs.

### 4.1 Azure AD App Registration

1. Create an **Application Registration** (or Service Principal) in Azure AD.
2. Create a **client secret** for the application.
3. In the API App Registration (the PDMS API), expose an **Application ID URI** (e.g., `api://dialysis-pdms`) and define **Application Roles** or **Scopes** (e.g., `Prescription.Read`, `Prescription.Write`).
4. Grant the Mirth application the required scopes/roles on the API app.

### 4.2 Mirth Configuration

Mirth can obtain tokens via:

**Option A: HTTP Sender with OAuth 2.0 preprocessor**

- Configure the OAuth 2.0 token endpoint: `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token`
- Grant type: `client_credentials`
- Client ID: Mirth application’s Application (client) ID
- Client Secret: from Azure AD app registration
- Scope: `api://dialysis-pdms/Prescription.Read` (or the appropriate scope for the target endpoint)

**Option B: JavaScript / custom channel step**

```javascript
// Example: POST to token endpoint
var tokenUrl = "https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token";
var clientId = "YOUR_MIRTH_APP_CLIENT_ID";
var clientSecret = "YOUR_MIRTH_APP_CLIENT_SECRET";
var scope = "api://dialysis-pdms/Prescription.Read";  // or Prescription.Write, etc.

var body = "grant_type=client_credentials&client_id=" + encodeURIComponent(clientId) +
           "&client_secret=" + encodeURIComponent(clientSecret) +
           "&scope=" + encodeURIComponent(scope);

// Use Mirth’s HttpClient or a custom connector to POST and parse the response
// Store the access_token for use in the Authorization header
```

### 4.3 Step-by-Step Mirth Token Workflow

1. **Create Mirth Channel** – Source: LL Listener (receives HL7 from dialysis machine); Destination: HTTP Sender (calls PDMS API).
2. **Before HTTP Sender** – Add a preprocessor script or use OAuth 2.0 connector to obtain a token.
3. **Obtain Token** – POST to `https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token` with:
   - `grant_type=client_credentials`
   - `client_id={Mirth app registration client ID}`
   - `client_secret={secret from Azure AD}`
   - `scope=api://dialysis-pdms/Prescription.Read` (for QBP^D01) or `api://dialysis-pdms/Prescription.Write` (for RSP^K22 ingest)
4. **Parse Response** – Extract `access_token` from JSON response.
5. **Set Authorization Header** – In HTTP Sender, set `Authorization: Bearer {access_token}`.
6. **Set X-Tenant-Id** – Add header `X-Tenant-Id: default` (or tenant identifier) for multi-tenancy.

### 4.4 Scope Alignment

Ensure the scope requested in the token matches what the PDMS expects:

- PDMS checks `scope` or `scp` claim for values like `Prescription:Read` or `Prescription:Write`.
- Azure AD app roles use the `roles` claim (array); the current `ScopeOrBypassHandler` only reads `scope` and `scp`. If using Azure AD app roles, configure the API to expose OAuth2 scopes (e.g., via App ID URI and published scopes) so the token contains `scope`/`scp`, or extend the handler to also check the `roles` claim.

---

## 5. Example Requests

### 5.1 With Authorization Header

```http
POST /api/hl7/qbp-d01 HTTP/1.1
Host: localhost:5001
Content-Type: application/x-hl7-v2+er7
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIs...
X-Tenant-Id: default

MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
RCP|I||RD
```

### 5.2 cURL Example

```bash
curl -X POST "https://pdms.example.com/api/hl7/qbp-d01" \
  -H "Content-Type: application/x-hl7-v2+er7" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "X-Tenant-Id: default" \
  -d "MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
RCP|I||RD"
```

### 5.3 Development Bypass (Local Testing)

When `Authentication:JwtBearer:DevelopmentBypass` is `true` and the environment is Development, requests do **not** require a valid JWT. The `Authorization` header can be omitted for local testing.

---

## 6. Troubleshooting

| Symptom | Cause | Resolution |
|---------|-------|------------|
| 401 Unauthorized | Missing/invalid JWT, wrong audience, expired token | Verify `Authorization: Bearer {token}` header; ensure `aud` matches API; check token expiry |
| 403 Forbidden | Insufficient scope | Ensure token has required scope (e.g. `Prescription:Read` for QBP^D01). Request correct scope when obtaining token |
| Token endpoint 400 | Invalid client credentials or malformed request | Verify client_id, client_secret; use `application/x-www-form-urlencoded` for token request |
| Scope not in token | Azure AD app roles vs OAuth2 scopes | PDMS expects `scope` or `scp` claim. Configure API app with *Expose an API* and add scopes; request scope in token call |
| DevelopmentBypass not working | Environment not set to Development | Set `ASPNETCORE_ENVIRONMENT=Development` or ensure `DevelopmentBypass: true` is used only in Development |

## 7. Data Residency and C5 Compliance

- **Secrets**: Store client IDs and secrets in Azure Key Vault or equivalent; never commit to source control.
- **HTTPS**: All external traffic to PDMS APIs must use HTTPS.
- **Audit**: Security-relevant actions (e.g., prescription read, QBP^D01 query) are recorded via `IAuditRecorder` for C5 compliance.

---

## 8. Related Documents

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) – Overview of authentication and scope policies
- [ARCHITECTURE-CONSTRAINTS.md](ARCHITECTURE-CONSTRAINTS.md) – C5 and access control principles
- [IMMEDIATE-HIGH-PRIORITY-PLAN.md](IMMEDIATE-HIGH-PRIORITY-PLAN.md) – JWT implementation history
