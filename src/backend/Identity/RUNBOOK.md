# Dialysis Identity (Keycloak + BFF)

## 1. Start Keycloak and Postgres

From the repository root:

```bash
docker compose -f src/backend/Identity/docker-compose.yml --env-file src/backend/Identity/.env.example up -d
```

(Optional) Copy `.env.example` to `.env` and set `POSTGRES_PASSWORD`, `KC_BOOTSTRAP_ADMIN_PASSWORD`, and `KEYCLOAK_HTTP_PORT`.

Wait until Keycloak is listening on `http://localhost:8080` (or your `KEYCLOAK_HTTP_PORT`). The realm `dialysis` is imported from `keycloak/dialysis-realm.json` on first start.

- Admin console: `http://localhost:8080/admin` (bootstrap user from compose env, default `admin` / `admin`).
- Realm issuer (HIS `Authority`): `http://localhost:8080/realms/dialysis`.

## 2. BFF (Dialysis.Identity.Bff)

Default dev URLs:

- BFF: `http://localhost:5275` (see `Dialysis.Identity.Bff/Properties/launchSettings.json`).
- Keycloak client `dialysis-bff` secret in realm import: `bff-dev-secret-change-me` (override in production via env or user-secrets).

Run:

```bash
dotnet run --project src/backend/Identity/Dialysis.Identity.Bff/Dialysis.Identity.Bff.csproj
```

Smoke flow:

1. Open `http://localhost:5275/login` — complete Keycloak login (demo user: `demo` / `demo`).
2. Open `http://localhost:5275/user` — expect JSON array of claim type/value pairs.
3. Token exchange: after login, call any route that triggers the HIS proxy (step 4); check logs if exchange fails (Keycloak returns error body).

## 3. HIS behind JWT + Keycloak

Run HIS (default dev URL `http://localhost:5288`).

Merge the following into `Dialysis.HIS.Api` configuration (e.g. `appsettings.Development.json`). Use `RoleClaimType` `roles` to match the realm mapper on `dialysis-bff`; map Keycloak realm role names to HIS permission strings:

```json
{
  "His": {
    "Authentication": {
      "Authority": "http://localhost:8080/realms/dialysis",
      "Audience": "dialysis-his-api",
      "RoleClaimType": "roles",
      "RolePermissionMap": {
        "his-developer": [
          "his.patientflow.register",
          "his.data.search",
          "his.ra.capabilities.read"
        ],
        "his-admin": [
          "his.security.users.register",
          "his.security.roles.assign",
          "his.patientflow.register",
          "his.patientflow.admit",
          "his.patientflow.discharge",
          "his.patientflow.referral.create",
          "his.scheduling.appointment.book",
          "his.scheduling.resources.read",
          "his.medication.order.place",
          "his.medication.order.discontinue",
          "his.medication.admin.record",
          "his.operations.staff.assign",
          "his.operations.inventory.move",
          "his.operations.billing.export",
          "his.data.import.submit",
          "his.data.search",
          "his.data.report",
          "his.data.share.read",
          "his.patientaccess.portal.read",
          "his.integration.device.ingest",
          "his.ra.capabilities.read",
          "his.ra.commands.write"
        ]
      }
    }
  }
}
```

Adjust `RolePermissionMap` to least privilege for each environment.

For local HTTP Keycloak, `Dialysis.HIS.Api` sets `JwtBearerOptions.RequireHttpsMetadata = false` in Development when the issuer scheme is `http`.

## 4. YARP proxy to HIS (optional)

With `ASPNETCORE_ENVIRONMENT=Development`, `appsettings.Development.json` sets `ReverseProxy:Clusters:his:Destinations:d1:Address` to `http://localhost:5288/`. Authenticated requests to `http://localhost:5275/his/...` are forwarded to `http://localhost:5288/...` with `Authorization: Bearer` set to a token obtained via **standard token exchange** for audience `dialysis-his-api`.

Example (after logging in at the BFF in the same browser session, use cookies — e.g. browser or cookie-aware client):

```bash
curl -sS -b cookies.txt -c cookies.txt "http://localhost:5275/his/v1/..." 
```

(Replace path with a real HIS versioned route; ensure the demo user’s mapped permissions allow the endpoint.)

## 5. Direct token exchange (curl)

Obtain an access token for `dialysis-bff` (authorization code flow is easiest via BFF login). Then:

```bash
curl -sS -X POST "http://localhost:8080/realms/dialysis/protocol/openid-connect/token" \
  -u "dialysis-bff:bff-dev-secret-change-me" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=urn:ietf:params:oauth:grant-type:token-exchange" \
  --data-urlencode "subject_token=PASTE_ACCESS_TOKEN" \
  --data-urlencode "subject_token_type=urn:ietf:params:oauth:token-type:access_token" \
  --data-urlencode "audience=dialysis-his-api"
```

The JSON response includes `access_token` suitable for `Authorization: Bearer` against HIS when `Audience` is `dialysis-his-api`.

## 6. Changing secrets

After editing `keycloak/dialysis-realm.json`, reset the Postgres volume or re-import the realm; align `Identity:Keycloak:ClientSecret` in the BFF and any automation secrets.

## 7. SMART-on-FHIR smoke test

The Keycloak realm ships an additional public client `smart-on-fhir` (PKCE-only) configured for SMART app launch. Discovery is served at the resource module's `/.well-known/smart-configuration` once `<Module>:Fhir:Smart:Enabled=true`.

### 7.1 Inspect the discovery document

With HIS running and `His:Fhir:Smart:Enabled=true`:

```bash
curl -sS http://localhost:5288/.well-known/smart-configuration | jq .
```

You should see `issuer` pointing at Keycloak, `authorization_endpoint` / `token_endpoint` on Keycloak, and `capabilities` advertising `launch-ehr`, `launch-standalone`, `permission-patient`, etc.

### 7.2 Standalone launch with PKCE

```bash
# 1. Generate a PKCE pair (manual or via your client library).
# 2. Open the browser flow:
open "http://localhost:8080/realms/dialysis/protocol/openid-connect/auth?\
client_id=smart-on-fhir&\
response_type=code&\
redirect_uri=http://localhost:9090/cb&\
scope=openid+fhirUser+launch/patient+patient/Patient.read+offline_access&\
state=demo&\
aud=dialysis-his-api&\
code_challenge=PKCE_CHALLENGE&\
code_challenge_method=S256"
# 3. After login, exchange code for token:
curl -sS -X POST "http://localhost:8080/realms/dialysis/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=authorization_code" \
  --data-urlencode "client_id=smart-on-fhir" \
  --data-urlencode "code=PASTE_FROM_REDIRECT" \
  --data-urlencode "redirect_uri=http://localhost:9090/cb" \
  --data-urlencode "code_verifier=PKCE_VERIFIER"
```

### 7.3 Call a FHIR endpoint with the SMART access token

```bash
curl -sS -H "Authorization: Bearer ACCESS_TOKEN" \
  http://localhost:5288/fhir/Patient/{id}
```

The resource module enforces the SMART scope via `SmartScopePolicyProvider` (e.g., `patient/Patient.read`) before delegating to the `IFhirReader<Patient>` for the requested id.

### 7.4 Kick off a Bulk Data `$export` with a SMART access token

When the resource host runs with `<Module>:Fhir:Smart:Enabled=true` **and** `<Module>:Fhir:BulkData:Enabled=true`, the `$export` route is wrapped in a `system/*.read` requirement by default (override via `<Module>:Fhir:BulkData:RequireScope`):

```bash
# 1. Get a token with the system scope (client credentials grant for backend services).
ACCESS_TOKEN=$(curl -sS -X POST \
  "http://localhost:8080/realms/dialysis/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data-urlencode "grant_type=client_credentials" \
  --data-urlencode "client_id=smart-on-fhir-system" \
  --data-urlencode "client_secret=..." \
  --data-urlencode "scope=system/*.read" | jq -r .access_token)

# 2. Kick off the export.
curl -sS -i -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Prefer: respond-async" \
  "http://localhost:5288/fhir/\$export"
# Expect: HTTP/1.1 202 Accepted
#         Content-Location: /fhir/bulk-data/jobs/<job-id>

# 3. Poll status until 200 with a manifest.
curl -sS -H "Authorization: Bearer $ACCESS_TOKEN" \
  "http://localhost:5288/fhir/bulk-data/jobs/<job-id>" | jq .

# 4. Download an output NDJSON file.
curl -sS -H "Authorization: Bearer $ACCESS_TOKEN" \
  "http://localhost:5288/fhir/bulk-data/jobs/<job-id>/files/Encounter.ndjson"
```

A token missing `system/*.read` returns `401 Unauthorized`. A token whose scope is `user/*.read` or `patient/*.read` does **not** satisfy `system/*.read`; the operator-shell flow requires a service token, not an end-user token.

### 7.5 Adding the `smart-on-fhir` client to a fresh realm

If you reset the realm and need to add the client manually:

1. Keycloak admin → Clients → Create.
2. Client ID: `smart-on-fhir`, type **public**, **PKCE enforced** (S256).
3. Valid redirect URIs: `http://localhost:9090/cb`, plus production apps as needed.
4. Optional protocol mappers to project `patient`, `encounter`, `fhirUser` claims from user attributes onto the access token (needed for `IFhirLaunchContextAccessor`).
5. Save. The discovery document immediately reflects the new authorization endpoint and supported scopes.

For the `$export` system-credentials flow add a second client:

1. Client ID: `smart-on-fhir-system`, type **confidential**, **service accounts enabled**.
2. Client scopes: include a `system/*.read` scope so the issued access token carries it.
3. Use `grant_type=client_credentials` and `scope=system/*.read` to acquire bearer tokens for backend pipelines, payer integrations, and TEFCA outbound flows.
