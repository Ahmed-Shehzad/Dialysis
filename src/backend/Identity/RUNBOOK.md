# Identity RUNBOOK

Operational guide for the Keycloak-backed identity stack: the realm, the Identity BFF, the
per-context BFF auth shell (`src/backend/Shared/Dialysis.Module.Bff`), and the smoke flows.
Architecture rationale lives in [`ARCHITECTURE.md`](ARCHITECTURE.md).

## §1 Stack overview

- **IdP**: Keycloak, realm **`dialysis`**, auto-imported from
  [`keycloak/dialysis-realm.json`](keycloak/dialysis-realm.json). In the Aspire dev stack Keycloak
  listens on `:8081`; the identity-only compose (§2) defaults to `:8080`
  (`KEYCLOAK_HTTP_PORT`).
- **Token lifetimes** (realm JSON): access token **300 s** (`accessTokenLifespan`), SSO session
  idle **1800 s** (`ssoSessionIdleTimeout`), SSO session max 36000 s (`ssoSessionMaxLifespan`).
  The short access token is what makes BFF-side refresh (§5) mandatory.
- **Pinned ports**: the Identity BFF is pinned to **`:5275`**, the seven context BFFs to
  **`:5301–5307`** (his/ehr/pdms/smartconnect/hie/**admin `:5306`**/portal), and the edge Gateway
  to **`:9090`**, because the realm's clients only accept matching `redirect_uri`s — the context
  BFF clients allow `http://localhost:9090/{ctx}/*` (gateway origin) and the `dialysis-bff` client
  allows `http://localhost:5275/*` + `http://localhost:9090/*`. Changing any of these ports means
  editing `dialysis-realm.json` **and** the Gateway's `appsettings.json` clusters.
- **Clients** in the realm:
  - `dialysis-bff` — confidential; the Identity BFF's OIDC client.
  - `dialysis-{his,ehr,pdms,smartconnect,hie,admin,portal}-bff` — confidential; one per context
    BFF, redirect URIs scoped to `/{ctx}/*` on the gateway origin.
  - `dialysis-his-api` — bearer-only audience for RFC 8693 token exchange (§7); its secret is not
    used for login.
  - `smart-on-fhir` — public SMART-on-FHIR launch client.
  - `dialysis-data-simulator` — confidential `client_credentials` client for
    `tools/Dialysis.DataSimulator`.
- **Realm roles**: `his-admin`, `his-developer` (plus Keycloak built-ins); dev user `demo`
  (password `demo`) carries `his-developer`.

## §2 Identity-only smoke flow (docker-compose)

When you only need Keycloak + its Postgres (no Aspire stack):

```bash
docker compose -f src/backend/Identity/docker-compose.yml --env-file src/backend/Identity/.env up -d
```

[`docker-compose.yml`](docker-compose.yml) runs `quay.io/keycloak/keycloak:26.2.5` in `start-dev
--import-realm` mode against a `postgres:16-alpine` sidecar, mounts `./keycloak` read-only as the
import directory, and exposes `KEYCLOAK_HTTP_PORT` (default **8080**). Bootstrap admin is
`admin`/`admin` (`KC_BOOTSTRAP_ADMIN_USERNAME/_PASSWORD` overridable via the env file). Then run
the Identity BFF (`dotnet run --project src/backend/Identity/Dialysis.Identity.Bff`, listens on
`:5275`) and follow §7.

## §3 Realm import semantics

`--import-realm` only imports when the realm **does not already exist** — it never re-imports over
a live realm. Consequences:

- In the **Aspire AppHost**, Keycloak is deliberately **not** a persistent container: a long-lived
  container would make edits to `dialysis-realm.json` invisible. Re-running the AppHost re-imports
  the realm from scratch.
- In the **identity-only compose** (§2), the Postgres volume (`identity_keycloak_pg`) *is*
  persistent, so realm JSON edits only land after `docker compose ... down -v` (drop the volume)
  and a fresh `up`.

## §4 Session model (cookie + ticket store)

Every BFF (`AddModuleBff()` in `Dialysis.Module.Bff/ModuleBffExtensions.cs`; the Identity BFF
mirrors the same wiring in its `Program.cs`) authenticates with OIDC code flow + a cookie session:

- **Path-scoped cookie** — each context BFF's cookie is scoped to its `/{ctx}` path (the Identity
  BFF's `Dialysis.Identity.Bff` cookie to `/identity`), so per-context sessions never collide on
  the single shared gateway origin and don't pile onto each other's requests (the accumulation
  previously overflowed Kestrel's 32 KB header limit → HTTP 431).
- **Server-side ticket store** — `DistributedCacheTicketStore` (`ITicketStore` over
  `IDistributedCache`) keeps the `SaveTokens = true` access/id/refresh bundle out of the browser
  cookie; only a short session key travels. Keys are namespaced per BFF
  (`<cookie-name>:ticket:`); non-persistent sessions get an 8-hour fallback TTL.
- **Valkey backing** — when `Bff:DistributedCache:Valkey:ConnectionString` is set, the ticket
  store uses Valkey and the same `AddValkeyDistributedCache` call wires the **ASP.NET Data
  Protection key ring** into Valkey, so cookies decrypt and tickets resolve on any replica.
  Without it (local dev), an in-memory distributed cache is the fallback.

## §5 Token refresh (session continuity)

`TokenRefreshService` (`ITokenRefreshService`) is attached to the cookie handler's
`OnValidatePrincipal` event:

- When the ticket's saved `expires_at` is within **60 s** of now (`_refreshSkew`), the BFF calls
  Keycloak's `/protocol/openid-connect/token` endpoint with `grant_type=refresh_token` (client
  Basic auth), rewrites `access_token`/`refresh_token`/`id_token`/`expires_at` on the auth ticket,
  and sets `ShouldRenew = true` so the renewed ticket persists.
- Failures — no `refresh_token` on the ticket, or Keycloak rejecting the grant — call
  `RejectPrincipal()` (plus cookie sign-out), so the SPA bounces back through login instead of
  401-ing on a stale session.
- The OIDC handler requests the **`offline_access`** scope at sign-in; without it Keycloak omits
  the refresh token and the BFF could never roll the session forward.

## §6 Permission claims: `dialysis_permission` → `/identity/user` → `PermissionGate`

- The realm emits a **JSON-typed `dialysis_permission` claim** with `userinfo.token.claim: true`
  (in the dev realm it's a hardcoded `dev-dialysis-permissions` protocol mapper on each BFF
  client; a production realm maps it from roles/groups). The BFFs set
  `GetClaimsFromUserInfoEndpoint = true`, so the claim lands on the cookie principal.
- The BFF's `GET {base}/identity/user` endpoint flattens the claim(s) via `ExtractPermissions` —
  each claim value is either a JSON array literal or a scalar permission string — into a top-level
  **`permissions: string[]`** array (alongside `name`, `email`, `roles`, `claims`, and the current
  `accessToken`).
- The SPA's `PermissionGate` (e.g. `src/frontend/identity-web/src/shell/PermissionGate.tsx`) does a
  plain `permissions.includes(required)` check; call sites pass typed strings from the module's
  `<Module>Permissions` catalog. An absent claim yields an empty array → permission-gated UI hides.

## §7 Module-behind-JWT smoke test

Verifies a module API accepts a BFF-issued bearer end to end:

1. Bring up Keycloak (§2) and the HIS API with `His:Authentication:Authority` pointing at the
   realm issuer (`http://localhost:8080/realms/dialysis`), and the Identity BFF with
   `ReverseProxy:Clusters:his:Destinations:d1:Address` set to the HIS API address.
2. Sign in: `GET http://localhost:5275/identity/login` → Keycloak form (`demo`/`demo`) → cookie
   session. `GET /identity/user` shows the claims + `accessToken`.
3. Call the BFF's HIS proxy: `GET http://localhost:5275/identity/his/api/v1.0/...`. The YARP
   transform asks `HisAccessTokenProvider` for a token before forwarding.
4. `HisAccessTokenProvider` performs an **RFC 8693 token exchange**
   (`grant_type=urn:ietf:params:oauth:grant-type:token-exchange`) of the session's access token
   to **audience `dialysis-his-api`** (`Identity:Keycloak:HisAudienceClientId`), and caches the
   exchanged token per subject in `IMemoryCache` — **4-minute TTL** by default (or
   `expires_in − 30 s`, capped at 600 s).
5. A `200` from the HIS API proves: realm import → BFF login → token exchange → module JWT-bearer
   validation all line up. A `401` at step 3 usually means the audience client or the module's
   `Authentication:Authority` is misconfigured.

The context BFFs run the simpler variant of the same proof: their YARP transform forwards the
session's own access token as the bearer on `/{ctx}/api/*`, no exchange involved.

## §8 Multi-IdP federation (Okta / Auth0 / Entra via Keycloak brokering)

Upstream IdPs are **brokered through Keycloak — never wired into the BFFs directly**. Keycloak
stays the only OIDC client the BFFs talk to.

- The realm ships `identityProviders[]` entries for **`okta`, `auth0`, `entra` — all disabled
  placeholders** in `dialysis-realm.json`. Enabling one = filling in its upstream client
  credentials/issuer and flipping `enabled` (then re-importing per §3).
- The BFF surfaces enabled brokers to the SPA via **`IIdentityProviderCatalog`** (bound to
  `Bff:Federation:Providers`; the Identity BFF's twin binds `Identity:Federation`) and
  **`GET {base}/identity/providers`** — alias, display name, optional icon. An entry's `Alias`
  must match the realm broker alias verbatim.
- Login: **`GET {base}/identity/login?provider=<alias>`**. The handler accepts the alias **only if
  `IIdentityProviderCatalog.IsKnown(alias)`** (allowlist — unknown aliases can't probe Keycloak's
  broker endpoints), stashes it in `AuthenticationProperties.Items["kc_idp_hint"]`, and the OIDC
  `OnRedirectToIdentityProvider` event forwards it as the **`kc_idp_hint`** parameter — Keycloak
  skips its own login page and redirects straight to the upstream IdP.

## §9 Production hardening

- **`KeycloakSecretGuard.EnsureProductionClientSecret`** runs at startup in every BFF: outside the
  `Development` environment, a missing client secret **or one still containing the `change-me`
  marker** (the repo ships `*-bff-dev-secret-change-me` placeholders in base `appsettings.json`
  for the dev realm) throws `InvalidOperationException` — fail-fast instead of authenticating
  against Keycloak with a publicly-known credential. It is a no-op in Development so the F5 loop
  keeps working.
- Override secrets via environment variables or a secret store: `Bff__Keycloak__ClientSecret` for
  context BFFs (`Bff:Keycloak` section), `Identity__Keycloak__ClientSecret` for the Identity BFF
  (`Identity:Keycloak` section). Rotate the realm-side client secrets in lockstep.
- HTTPS metadata is required automatically whenever the configured Authority is `https`
  (`RequireHttpsMetadata` follows the authority scheme).
