# Identity — Architecture (low-level)

Companion to [README.md](README.md), [RUNBOOK.md](RUNBOOK.md), and [identity_subdomain_structure.md](identity_subdomain_structure.md). Identity is a **Generic Subdomain** (Evans 2003, p. 281) — the platform deliberately keeps it thin. The module stands on a standard OIDC broker (Keycloak in dev) and exposes:

- `Dialysis.Identity.Bff` — browser entry, OIDC code-flow + secure cookie session, JWT proxy to module APIs.
- `Dialysis.Identity.Api` — local user provisioning + role assignment.
- `Dialysis.Identity.Provisioning` — outbox-driven sync to Keycloak.
- `Dialysis.Identity.Contracts` — integration events + claim shape contract.

The whole module is intentionally **replaceable**: any IdP that issues the same claim shape is a drop-in.

> Mermaid renders inline on GitHub/GitLab/JetBrains/VS Code; paste into <https://mermaid.live> if your viewer does not.

---

## 1. System architecture (component view)

```mermaid
flowchart LR
    subgraph "Browser"
        SPA["SPA / Patient Portal"]
    end

    subgraph "Dialysis.Identity.Bff (port 5275)"
        OIDCstart["GET /auth/login"]
        Callback["GET /auth/callback"]
        Proxy["Reverse proxy to module APIs<br/>(forwards Bearer)"]
        Cookie["Secure HttpOnly cookie session"]
        OIDCstart --> Callback --> Cookie
        Cookie --> Proxy
    end

    subgraph "Dialysis.Identity.Api"
        Prov["Provisioning controllers<br/>(local user CRUD, role assign)"]
        Auth["JwtBearer (admin)"]
    end

    subgraph "Dialysis.Identity.Provisioning"
        Out["Provisioning outbox consumer<br/>(IUserProvisioningGateway)"]
        Kg["KeycloakAdminClient"]
    end

    subgraph "Keycloak (dev: localhost:8081, realm 'dialysis')"
        Realm["Realm 'dialysis'<br/>clients: bff, smart-on-fhir<br/>users, roles, groups"]
        Token["/realms/dialysis/protocol/openid-connect/token"]
    end

    subgraph "Module APIs (HIS / EHR / PDMS / HIE / SmartConnect)"
        ModAuth["JwtBearer validation<br/>Authority + RolePermissionMap"]
        Curr["ICurrentUser → IModulePermissionCatalog"]
    end

    subgraph "Dialysis.Identity.Persistence (IdentityDbContext)"
        Schemas[("identity_users (local mirror),<br/>identity_provisioning (outbox state),<br/>transponder")]
    end

    subgraph "Keycloak Postgres (port 5444)"
        KCSchema[("Keycloak realm tables")]
    end

    SPA --> BFF
    BFF -- OIDC code+PKCE --> Realm
    Realm --> Token
    BFF --> Token
    Proxy -- "Bearer JWT" --> Modules
    Modules --> ModAuth
    ModAuth --> Curr

    SPA -. admin actions .-> IdAPI
    IdAPI --> DB
    DB -. provisioning outbox .-> Out
    Out --> Kg --> Realm

    Realm --> KCDB
```

**Claim contract (published language)**

Every module-side JWT is expected to carry the following claims; this is the **invariant** that any IdP swap must preserve:

| Claim | Meaning |
|---|---|
| `sub` | Stable subject id (NameIdentifier). |
| `email` / `email_verified` | Optional. |
| `roles` (or `groups`) | IdP role/group names — mapped per-module via `<Module>:Authentication:RolePermissionMap` to `IModulePermissionCatalog` permission strings. |
| `his_patient_id` | Optional. Used by HIS PatientAccess to scope portal endpoints. `sub` is accepted as a fallback when it equals route `patientId`. |
| `patient`, `encounter`, `fhirUser` | SMART-on-FHIR launch context (when issued via the `smart-on-fhir` Keycloak client). |
| `his_permission` / module-equivalent | Optional explicit permission claim (merged with `RolePermissionMap` output). Claim type is configurable per module. |

---

## 2. Workflow — Browser login via BFF (OIDC code + PKCE)

```mermaid
sequenceDiagram
    autonumber
    participant User as User (browser)
    participant SPA as SPA
    participant BFF as Identity.Bff
    participant KC as Keycloak (realm 'dialysis')
    participant Mod as Module API (e.g. HIS)

    User->>SPA: open app
    SPA->>BFF: GET /auth/login?returnUrl=/
    BFF->>BFF: generate state + PKCE code_verifier
    BFF-->>User: 302 to KC /authorize<br/>(client_id=bff, code_challenge, scope=openid roles)
    User->>KC: GET /authorize (with state)
    KC-->>User: login page
    User->>KC: credentials (+ MFA if enabled)
    KC-->>User: 302 to BFF /auth/callback?code&state
    User->>BFF: GET /auth/callback
    BFF->>KC: POST /token<br/>(authorization_code + code_verifier)
    KC-->>BFF: id_token + access_token + refresh_token
    BFF->>BFF: write secure HttpOnly cookie session<br/>(tokens never reach the SPA)
    BFF-->>User: 302 to returnUrl
    User->>SPA: page loads
    SPA->>BFF: GET /api/v1.0/... (cookie)
    BFF->>BFF: attach Authorization: Bearer access_token
    BFF->>Mod: forward request
    Mod->>Mod: JwtBearer validate against Keycloak JWKS
    Mod->>Mod: ICurrentUser merges RolePermissionMap + claims
    Mod-->>BFF: 200 response
    BFF-->>SPA: 200 response

    Note over BFF: token rotation — when access_token expires,<br/>BFF silently refreshes using refresh_token<br/>before forwarding the next request
```

---

## 3. Workflow — Provisioning outbox sync

The Provisioning slice writes local user mutations to its DbContext and to the Transponder outbox in **one** transaction. A background outbox relay then drives Keycloak admin calls — the local store is the source of truth for what was *requested*; Keycloak is the source of truth for what was *applied*.

```mermaid
sequenceDiagram
    autonumber
    participant Admin as Admin caller
    participant API as Identity.Api
    participant H as ProvisionUserHandler
    participant Ctx as IdentityDbContext
    participant OBX as transponder.outbox
    participant Relay as Provisioning relay
    participant Kg as KeycloakAdminClient
    participant KC as Keycloak realm

    Admin->>API: POST /provisioning/users
    API->>H: ProvisionUserCommand
    H->>Ctx: insert LocalUser
    H->>Ctx: enqueue UserProvisionedIntegrationEvent v1
    H->>Ctx: SaveChangesAsync (one UoW)
    Ctx-->>H: committed
    API-->>Admin: 202 Accepted

    Note over Relay,Kg: out-of-band
    Relay->>OBX: poll unprocessed
    Relay->>Kg: createUser + assignRoles
    Kg->>KC: HTTP admin API
    alt success
        KC-->>Kg: 201
        Kg-->>Relay: ok
        Relay->>OBX: mark published
        Relay->>Ctx: write ProvisioningOutcome row<br/>(see IdentityProvisioningOutboxTests)
    else conflict / error
        KC-->>Kg: 409 / 5xx
        Kg-->>Relay: error
        Relay->>Relay: backoff + retry
    end
```

Tests for the outbox path live in [Dialysis.Identity.Tests/IdentityProvisioningOutboxTests.cs](Dialysis.Identity.Tests/IdentityProvisioningOutboxTests.cs).

---

## 4. Activity — User lifecycle

```mermaid
stateDiagram-v2
    [*] --> Drafted: ProvisionUser (local)
    Drafted --> Provisioning: outbox row enqueued
    Provisioning --> Active: Keycloak create succeeded<br/>publishes UserProvisionedIntegrationEvent
    Provisioning --> ProvisioningFailed: max attempts<br/>(ops attention required)
    ProvisioningFailed --> Provisioning: manual retry
    Active --> RolesUpdated: AssignRole / RevokeRole<br/>publishes UserRolesUpdatedIntegrationEvent
    RolesUpdated --> Active
    Active --> Disabled: DisableUser<br/>publishes UserDisabledIntegrationEvent
    Disabled --> Active: ReEnableUser
    Disabled --> [*]: PurgeUser (hard delete after retention window)
```

---

## 5. Composition root

```mermaid
flowchart TB
    BFFprog["Program.cs (Identity.Bff)"]
    BFFprog --> AddOidc["AddAuthentication(OIDC + Cookie)<br/>+ AddSession + AddYarp (reverse proxy)"]
    BFFprog --> Proxy["Route /api/* → module APIs<br/>(forward Bearer access_token)"]

    APIprog["Program.cs (Identity.Api)"]
    APIprog --> AddModuleHost["AddModuleHost of IdentityPermissionCatalog<br/>(ModuleSlug = 'identity')"]
    APIprog --> AddIdentity["AddIdentityModule(configuration)"]
    AddIdentity --> Persist["AddDbContext of IdentityDbContext<br/>(Postgres 'postgres-identity')"]
    AddIdentity --> Prov["AddProvisioning()<br/>(KeycloakAdminClient + outbox consumer)"]
    AddIdentity --> Bus["AddTransponder + outbox relay"]
```

**Configuration keys (BFF)**

- `Authentication:Oidc:Authority` — Keycloak realm URL.
- `Authentication:Oidc:ClientId` / `ClientSecret` — confidential client `bff`.
- `Authentication:Oidc:Scope` — space-delimited (`openid profile email roles offline_access`).
- `Cookie:Name` / `Cookie:Domain` / `Cookie:SecurePolicy` — session cookie attributes.
- `ReverseProxy:Routes:*` — YARP route map to module APIs.

---

## 6. Data layout

```mermaid
erDiagram
    IdentityDbContext ||--o{ identity_users : "LocalUser (mirror)"
    IdentityDbContext ||--o{ identity_provisioning : "ProvisioningOutcome,<br/>RoleAssignmentMirror"
    IdentityDbContext ||--o{ transponder : "Outbox, Inbox"
    KeycloakRealm ||--o{ KeycloakUsers : "users (authoritative)"
    KeycloakRealm ||--o{ KeycloakRoles : "roles, groups (authoritative)"
```

---

## 7. Cross-context contracts

| Counterparty | Role | Vehicle |
|---|---|---|
| HIS / EHR / PDMS / HIE / SmartConnect | **Supplier** of identity claims (Conformist on the consumer side). | OIDC JWT; module-side `RolePermissionMap` |
| Keycloak | **Vendor** — opaque external IdP. | OIDC discovery + Admin REST |
| FHIR SMART-on-FHIR clients | **Customer**: launch + standalone flows authorized via `smart-on-fhir` Keycloak client. | OIDC + PKCE, launch-context claims |

---

## 8. Operational notes

- The Keycloak realm `dialysis` is **auto-imported from [keycloak/dialysis-realm.json](keycloak/dialysis-realm.json)** on container start. Update that JSON to add clients / roles; commit alongside code changes.
- The Identity docker-compose lives at [docker-compose.yml](docker-compose.yml) (Keycloak + Postgres on port 5444); the root `docker-compose.modules.yml` runs the broader containerized stack, and the Aspire AppHost (`dotnet run --project src/aspire/Dialysis.AppHost`) is the local dev entrypoint.
- BFF + JWT smoke test → [RUNBOOK.md](RUNBOOK.md).

---

## 9. Where to look next

- BFF host → `Dialysis.Identity.Bff/Program.cs` and `appsettings.json`.
- Provisioning slice → `Dialysis.Identity.Provisioning/` and `IdentityProvisioningOutboxTests`.
- Realm definition → [keycloak/dialysis-realm.json](keycloak/dialysis-realm.json).
- Long-form rationale → [identity_subdomain_structure.md](identity_subdomain_structure.md).
