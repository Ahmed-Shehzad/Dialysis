# Worked example — onboarding a real Okta tenant

The realm import + BFF wiring from PR #137 ship the *mechanism* for multi-IdP
federation. This doc is the concrete recipe for activating Okta as a real tenant
IdP — what credentials you need from Okta, what to change in
`dialysis-realm.json`, what to set in the BFF's per-environment config, and how
to validate the end-to-end SSO flow. Replace "Okta" with "Auth0" or "Microsoft
Entra ID" — same shape, different endpoint URLs.

## Prerequisites on the Okta side

You'll need the following from the customer's Okta admin (or your own developer
tenant — `https://developer.okta.com/signup` is free):

| Field | Where in Okta | Example value |
|---|---|---|
| Tenant URL | Top right of admin console | `https://acme-clinic.okta.com` |
| Issuer URI | Security → API → Authorization Servers → default | `https://acme-clinic.okta.com/oauth2/default` |
| Client ID | Applications → your OIDC app → General | `0oa1abc2defGHIJK3l4m5` |
| Client Secret | Same screen, regenerate if needed | `OktaSecret-XX-yy-zz` (treat as a credential) |
| Authorization URL | Authorization Server → Metadata URI → `authorization_endpoint` | `https://acme-clinic.okta.com/oauth2/default/v1/authorize` |
| Token URL | `token_endpoint` | `https://acme-clinic.okta.com/oauth2/default/v1/token` |
| UserInfo URL | `userinfo_endpoint` | `https://acme-clinic.okta.com/oauth2/default/v1/userinfo` |
| JWKS URI | `jwks_uri` | `https://acme-clinic.okta.com/oauth2/default/v1/keys` |
| Default scope | (always) | `openid profile email` |

Create the Okta application as:
- **Application type**: OIDC — Web Application
- **Grant types**: Authorization Code (no PKCE — Keycloak is a confidential client)
- **Sign-in redirect URIs**: `https://<your-keycloak-host>/realms/dialysis/broker/okta/endpoint` (Keycloak's broker callback)
- **Sign-out redirect URIs**: `https://<your-keycloak-host>/realms/dialysis/broker/okta/endpoint/logout_response`

## Step 1 — Realm: activate the Okta broker

Open the Keycloak admin at `http://localhost:8081/admin` (or your prod URL),
sign in with the bootstrap admin, navigate to **Identity providers → okta**,
and:

1. Flip **Enabled** to `On`.
2. Fill in **Client ID** and **Client Secret** from the Okta application.
3. Replace every `CHANGE_ME-*` value in the four endpoint URLs with the real
   Okta tenant URLs (table above).
4. **Save**.

Equivalent edit to `dialysis-realm.json` (for the realm-import path) — replace
the placeholder block with:

```json
{
  "alias": "okta",
  "displayName": "Okta",
  "providerId": "oidc",
  "enabled": true,
  "trustEmail": true,
  "storeToken": false,
  "addReadTokenRoleOnCreate": false,
  "firstBrokerLoginFlowAlias": "first broker login",
  "config": {
    "clientId": "0oa1abc2defGHIJK3l4m5",
    "clientSecret": "OktaSecret-XX-yy-zz",
    "authorizationUrl": "https://acme-clinic.okta.com/oauth2/default/v1/authorize",
    "tokenUrl":         "https://acme-clinic.okta.com/oauth2/default/v1/token",
    "userInfoUrl":      "https://acme-clinic.okta.com/oauth2/default/v1/userinfo",
    "issuer":           "https://acme-clinic.okta.com/oauth2/default",
    "jwksUrl":          "https://acme-clinic.okta.com/oauth2/default/v1/keys",
    "defaultScope": "openid profile email",
    "useJwksUrl": "true",
    "validateSignature": "true",
    "syncMode": "IMPORT"
  }
}
```

`syncMode: IMPORT` copies Okta user attributes into the Keycloak user on first
broker login; `FORCE` re-syncs every login (use when you want Okta to remain
the source of truth even after the first import).

## Step 2 — Map Okta groups to Dialysis roles

By default, brokered logins don't carry any role claims into the Dialysis access
token. Add an attribute-mapper in Keycloak so Okta's `groups` claim becomes a
role assignment that downstream `<Module>:Authentication:RolePermissionMap`
config can resolve.

1. **Identity providers → okta → Mappers → Create**.
2. Sync mode override: `FORCE`.
3. Mapper type: **Advanced Claim to Role**.
4. Claim: `groups` (or whatever attribute Okta sends — depends on the Okta
   application's claim mapper).
5. Claim value: `Dialysis-Clinicians` (or your group name).
6. Role: `his-developer` (or whichever Dialysis realm role you want to attach).

Repeat for each Okta group → Dialysis role pair. The realm currently ships
`his-admin` and `his-developer` as example roles — add more via
`dialysis-realm.json` if your tenant has finer-grained groupings.

## Step 3 — BFF + SPA: surface the broker

Edit the BFF's `appsettings.<env>.json`:

```json
{
  "Identity": {
    "Federation": {
      "Providers": [
        {
          "Alias": "okta",
          "DisplayName": "Sign in with Acme Okta",
          "IconUri": "/icons/okta.svg",
          "Enabled": true
        }
      ]
    }
  }
}
```

`Alias` MUST match the Keycloak broker alias exactly. The SPA login page
auto-fetches `/identity/providers` on mount, so once the BFF is redeployed
the Okta button appears alongside the local Keycloak fallback.

## Step 4 — End-to-end smoke

```bash
# 1. Provider catalog surfaces Okta
curl -sS https://your-bff/identity/providers | jq
# → { "providers": [ { "alias": "okta", "displayName": "Sign in with Acme Okta", "iconUri": "/icons/okta.svg" } ] }

# 2. Login redirect carries the kc_idp_hint
curl -sS -D - "https://your-bff/identity/login?provider=okta&returnUrl=/" | grep -i location
# Expect a 302 to https://<keycloak>/realms/dialysis/protocol/openid-connect/auth?...&kc_idp_hint=okta&...

# 3. Browser flow — open the SPA → click "Sign in with Acme Okta" → Okta login → land back on the SPA authenticated
# 4. Verify the access token shows the brokered identity:
curl -sS -H "Authorization: Bearer <token>" https://your-bff/identity/user | jq
# → roles + sub reflect the Okta user, claims include the realm-role attribute mapper output
```

## Step 5 — Tenant-level isolation (multi-tenant SaaS only)

If you're running ONE Keycloak realm per tenant (the cleanest multi-tenant
shape), each tenant gets its own `dialysis-realm-<slug>.json` with the broker
config inlined. The BFF per-tenant connection string + the `Identity:Keycloak:
Authority` env var (`http://keycloak:8080/realms/<tenant-slug>`) are the only
moving pieces. The Federation provider list is per-realm too, so each tenant
independently controls which upstream IdPs are exposed.

If you're running ONE realm for ALL tenants, you'll want a custom
`IIdentityProviderCatalog` implementation that filters the provider list by
the host's `Identity:Tenant` config — out of scope for this doc but listed in
`CLAUDE.md`'s "future work" section.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| 400 Invalid Redirect at Okta | The Sign-in redirect URI on the Okta app doesn't match Keycloak's broker callback | Copy the URL from Keycloak's "Identity Providers → okta → Redirect URI" field exactly into Okta |
| `kc_idp_hint` query param ignored | Hint alias doesn't match a Keycloak `identityProviders[].alias` | Verify the case + spelling in both `dialysis-realm.json` and BFF config |
| Empty roles after Okta login | Group-to-role mapper missing or claim name wrong | Check Okta's "Tokens" tab — confirm the access/id token includes a `groups` claim; tighten the Keycloak mapper accordingly |
| HSTS / CSP errors from the SPA on the brokered redirect | Brokered IdP host isn't in the gateway's CORS allowlist | Add the Okta tenant origin to `Gateway:Cors:AllowedOrigins` in the gateway config |

## Same recipe, different IdPs

- **Auth0**: replace endpoint URLs with the Auth0 tenant equivalents
  (`/authorize`, `/oauth/token`, `/userinfo`, `/.well-known/jwks.json`).
  Configure the Auth0 Application as a "Regular Web Application" with the
  same Keycloak broker callback URI.
- **Microsoft Entra ID**: endpoint pattern is
  `https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/...`.
  Register an App in Entra → set redirect URI to Keycloak's broker
  callback → grant the `openid profile email` Microsoft Graph
  delegated permissions.

The `dialysis-realm.json` ships placeholders for all three; only the credential
values + the BFF Provider list differ between them.
