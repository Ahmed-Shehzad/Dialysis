# SMART on FHIR

The Dialysis PDMS Gateway supports SMART on FHIR in two roles:

1. **SMART client** – PDMS calls external EHRs with OAuth2 access tokens
2. **SMART server** – EHRs launch apps that obtain tokens from PDMS and call our FHIR API

---

## SMART Client (PDMS → EHR)

When pushing data to an EHR that requires OAuth2, configure client credentials.

### Configuration

```json
{
  "Integration": {
    "EhrFhirBaseUrl": "https://ehr.example.com/fhir/r4",
    "TokenEndpoint": "https://ehr.example.com/oauth/token",
    "ClientId": "pdms-client",
    "ClientSecret": "your-secret",
    "Scope": "fhirUser"
  }
}
```

- **TokenEndpoint** – Optional. If omitted, the client discovers it from `{FhirBaseUrl}/.well-known/smart-configuration`.
- **ClientId**, **ClientSecret** – Required for SMART. If absent, EHR push uses no Bearer token.
- **Scope** – Optional. Defaults to `fhirUser`.

### Flow

1. `POST /api/v1/outbound/ehr/push/{patientId}` is called.
2. If ClientId and ClientSecret are set, the client requests an access token via OAuth2 client credentials.
3. The token is sent as `Authorization: Bearer <token>` with the FHIR bundle POST.
4. If the EHR does not use OAuth2, leave ClientId/ClientSecret empty.

---

## SMART Server (EHRs → PDMS)

PDMS can act as a SMART authorization server so EHRs can launch apps that call our FHIR API.

### Configuration

```json
{
  "Smart": {
    "BaseUrl": "https://pdms.example.com",
    "SigningKey": "<base64-encoded-32-byte-key>",
    "ClientId": "ehr-app",
    "ClientSecret": "app-secret"
  }
}
```

Generate a signing key:

```bash
openssl rand -base64 32
```

- **BaseUrl** – Public URL of the PDMS Gateway. Used in discovery and token validation.
- **SigningKey** – HMAC-SHA256 key for signing access tokens (base64).
- **ClientId**, **ClientSecret** – Optional. If set, only this client can use the auth flow. If empty, any client is accepted (dev only).

### Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /.well-known/smart-configuration` | Discovery – authorization and token URLs |
| `GET /auth/authorize` | OAuth2 authorize – returns auth code |
| `POST /auth/token` | OAuth2 token – exchanges code for access token |

### Authorization Flow (standalone launch)

1. App calls:  
   `GET /auth/authorize?client_id=xxx&response_type=code&redirect_uri=xxx&scope=xxx&state=xxx&tenant=default`
2. PDMS auto-approves and redirects to:  
   `{redirect_uri}?code=xxx&state=xxx`
3. App exchanges code:  
   `POST /auth/token` with `grant_type=authorization_code`, `code`, `redirect_uri`, `client_id`, `client_secret`
4. PDMS returns `access_token` (JWT).
5. App calls FHIR API with `Authorization: Bearer <access_token>`.

### Tenant in Token

- Use the `tenant` query parameter in `/auth/authorize` to set tenant context.
- The access token includes a `tenant_id` claim.
- If `X-Tenant-Id` is not sent, the middleware uses `tenant_id` from the token.

### FHIR Endpoints and Authorization

- JWT Bearer validation is enabled when `Smart` is configured.
- FHIR endpoints do not require auth by default (backward compatible).
- To require auth, add `[Authorize]` to controllers or individual actions.
- Valid Bearer tokens are accepted; unauthenticated calls still work unless `[Authorize]` is applied.

---

## References

- [SMART App Launch](http://hl7.org/fhir/smart-app-launch/)
- [SMART on FHIR – HL7](https://www.hl7.org/fhir/smart.html)
