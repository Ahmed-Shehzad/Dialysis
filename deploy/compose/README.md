# Docker Compose deployment artifacts

Everything under `deploy/compose/` is **generated from the Aspire AppHost** — the AppHost
(`src/aspire/Dialysis.AppHost`) is the single source of truth for the deployment topology.
There is no hand-curated `docker-compose.override.yaml`: every concern Aspire's compose
publisher doesn't model out-of-the-box (build stanzas, host ports, ASP.NET production
hardening, the OTLP collector, replica counts, healthcheck cadence) lives in
`src/aspire/Dialysis.AppHost/ComposePublishExtensions.cs` and is shaped per environment by
the `DIALYSIS_DEPLOY_ENV` variable (see `DeploymentEnvironment.cs`).

**Do not hand-edit `deploy/compose/<env>/docker-compose.yaml`.** Change the AppHost, then
regenerate:

```bash
./build.sh PublishCompose --environment prod   # one environment → deploy/compose/prod/
./build.sh PublishAllCompose                   # fan out across dev / staging / prod
```

A CI drift gate (`.github/workflows/deploy-artifacts.yml`, the "Deploy Artifacts" workflow)
regenerates the artifacts from the AppHost with `--deploy false` and fails the PR on
`git diff --exit-code` — so any AppHost change must be committed together with the
regenerated `deploy/compose/<env>/` (and `deploy/charts/dialysis-<env>/`) folders.

## Layout

```
deploy/compose/
├── otel-collector.yaml          # OTLP collector config, bind-mounted by staging/prod
├── otel-collector.schema.json
├── dev/                         # F5-equivalent topology, as a compose project
├── staging/                     # production hardening on, single replicas, relaxed healthchecks
└── prod/                        # production hardening + RequireAuthority, 2 replicas, tight healthchecks
    ├── docker-compose.yaml      # generated — never hand-edit
    ├── .env                     # generated parameter template — operator fills the values
    └── aspire-manifest.json     # publish byproduct (not consumed at runtime)
```

## Per-environment matrix

The three shapes share ~90% of the topology (same services, same images, same wiring); the
diff is a small set of strings, all derived from `DeploymentEnvironment.cs`:

| Concern | dev | staging | prod |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` (every host) | `Development` | `Staging` | `Production` |
| Module HSTS + ForwardedHeaders (`<Module>__UseHsts`, `<Module>__UseForwardedHeaders`) | off | `true` | `true` |
| Gateway HSTS + ForwardedHeaders (`Gateway__UseHsts`, `Gateway__UseForwardedHeaders`) | off | `true` | `true` |
| `<Module>__Authentication__RequireAuthorityWhenNotDevelopment` | off | off (deliberately — lets an operator probe the stack without a real Keycloak realm in front) | `true` |
| OTLP wiring (`<Module>__Telemetry__OtlpEndpoint` → `http://otel-collector:4317`) | not set | set | set |
| `otel-collector` service (otel/opentelemetry-collector-contrib:0.110.0, ports 4317 gRPC / 4318 HTTP, bind-mounts `../otel-collector.yaml`) | absent | present | present |
| Replicas (module APIs + gateway, `deploy.replicas`) | 1 | 1 | 2 |
| Postgres healthcheck interval (`pg_isready`) | 5s | 10s (relaxed to keep small boxes calm) | 5s |

Everything else — host port mappings, build stanzas, service set — is identical across the
three environments.

### Host port reference (all environments)

| Service | Host port |
|---|---|
| Gateway (browser entry) | 9090 |
| Identity BFF | 5275 |
| Context BFFs his / ehr / pdms / smartconnect / hie / admin / portal | 5301–5307 |
| Module APIs HIS / EHR / PDMS / SmartConnect / HIE / Lab (container port 8080) | 5288 / 5289 / 5290 / 5291 / 5292 / 5293 |
| Postgres HIS / SmartConnect / EHR / PDMS / HIE / Lab | 5440 / 5441 / 5442 / 5443 / 5445 / 5446 |
| Keycloak | 8081 (container 8080) |
| RabbitMQ / management | 5672 / 15672 |
| Valkey | 6379 |
| SPAs his / ehr / pdms / smartconnect / hie / identity(admin) / portal (nginx on container port 80; reached via the Gateway in normal use) | 5331–5337 |
| SonarQube | 9000 |
| OTLP collector (staging/prod only) | 4317 gRPC / 4318 HTTP |

The Gateway is the browser entry point (`http://localhost:9090`); the per-service host
ports exist for direct debugging. **BFF + Gateway ports are pinned** because the Keycloak
clients in `src/backend/Identity/keycloak/dialysis-realm.json` only accept those
`redirect_uri`s — do not remap them without updating the realm and the Gateway clusters.

## Running an environment locally

```bash
cd deploy/compose/prod        # or dev / staging
# 1. Fill in .env (see the contract below)
# 2. Build + start everything from the repo sources:
docker compose up -d --build
```

`--build` builds all repo-built images: module APIs and BFFs via the parameterised
`Dockerfile.module` (`MODULE_PROJECT`/`MODULE_DLL` build args), the gateway via
`Dockerfile.gateway`, and each SPA via the `Dockerfile` in its `src/frontend/<app>/`
folder. Module hosts are stateless and scale horizontally
(`docker compose up -d --scale his-api=3`) — Valkey backs the distributed cache, the BFF
session ticket store, and the ASP.NET Data Protection key ring, so multi-replica is safe.

Keycloak runs with `start-dev --import-realm` and `KC_DB: dev-mem` — the realm is
re-imported from the bind-mounted `src/backend/Identity/keycloak/` folder on every fresh
container, exactly like the Aspire dev loop.

## The `.env` contract

Aspire emits every parameter and unresolved default as a `.env` entry; the committed
`.env` files are **templates with empty values** that the operator must populate before
`docker compose up`. The variables fall into four groups:

1. **Image names** (`*_IMAGE`: `HIS_API_IMAGE`, `HIS_BFF_IMAGE`, `HIS_WEB_IMAGE`,
   `GATEWAY_IMAGE`, `IDENTITY_BFF_IMAGE`, …) — the name each locally-built (or
   registry-pulled) image gets. When pulling from a registry, point these at the
   registry-qualified names produced by `./build.sh PushImages` (the committed artifacts
   stay registry-free by design; see `docs/operations/container-registry.md`).
2. **Container ports** (`HIS_API_PORT`, `EHR_API_PORT`, `PDMS_API_PORT`,
   `SMARTCONNECT_API_PORT`, `HIE_API_PORT`, `LAB_API_PORT`, `GATEWAY_PORT`,
   `IDENTITY_BFF_PORT`) — the in-network port other services use to reach the host. Module
   APIs bind container port **8080** (`ASPNETCORE_URLS=http://+:8080` is baked into the
   compose file), so set every `*_API_PORT=8080`; `GATEWAY_PORT=9090` and
   `IDENTITY_BFF_PORT=5275` match their pinned bindings.
3. **Infrastructure secrets** (`POSTGRES_HIS_PASSWORD`, `POSTGRES_EHR_PASSWORD`,
   `POSTGRES_PDMS_PASSWORD`, `POSTGRES_SMARTCONNECT_PASSWORD`, `POSTGRES_HIE_PASSWORD`,
   `POSTGRES_LAB_PASSWORD`, `RABBITMQ_PASSWORD`, `VALKEY_PASSWORD`, `SONAR_PG_PASSWORD`) —
   per-store credentials, interpolated into every consumer's connection string.
4. **BFF client secrets** (`*_BFF_CLIENT_SECRET` — see the next section) and **bind-mount
   sources** (`KEYCLOAK_BINDMOUNT_0` → `src/backend/Identity/keycloak`,
   `SONARQUBE_BOOTSTRAP_BINDMOUNT_0` → `tools/sonarqube/bootstrap.sh`; give them paths
   relative to the env folder or absolute paths).

## BFF client secrets

Every per-context BFF (his / ehr / pdms / smartconnect / hie / admin / portal) **and** the
Identity BFF is a **confidential Keycloak client**: it must authenticate to Keycloak's
token and pushed-authorization endpoints with a client secret, or the
`/{ctx}/identity/login` challenge 500s ("Authentication failed").

- The AppHost declares each secret as an Aspire **secret parameter**
  (`his-bff-client-secret`, `ehr-bff-client-secret`, `pdms-bff-client-secret`,
  `smartconnect-bff-client-secret`, `hie-bff-client-secret`, `admin-bff-client-secret`,
  `portal-bff-client-secret`, `identity-bff-client-secret`) with dev defaults that match
  the secrets baked into `src/backend/Identity/keycloak/dialysis-realm.json`
  (`his-bff-dev-secret-change-me`, …, `bff-dev-secret-change-me` for the identity BFF).
  Declaring them as parameters means Aspire surfaces each one as a generated `.env` entry
  (`HIS_BFF_CLIENT_SECRET`, `EHR_BFF_CLIENT_SECRET`, `PDMS_BFF_CLIENT_SECRET`,
  `SMARTCONNECT_BFF_CLIENT_SECRET`, `HIE_BFF_CLIENT_SECRET`, `ADMIN_BFF_CLIENT_SECRET`,
  `PORTAL_BFF_CLIENT_SECRET`, `IDENTITY_BFF_CLIENT_SECRET`) instead of baking the dev
  secret into an image — a real deployment overrides it from a secret store.
- The compose file feeds each secret to its BFF as `Bff__Keycloak__ClientSecret`
  (context BFFs) / `Identity__Keycloak__ClientSecret` (Identity BFF).
- **Fail-fast outside Development**: `KeycloakSecretGuard.EnsureProductionClientSecret`
  (`src/backend/Shared/Dialysis.Module.Bff/Configuration/KeycloakSecretGuard.cs`) throws at
  startup when `ASPNETCORE_ENVIRONMENT` is not `Development` and the configured secret is
  missing **or still contains the `change-me` placeholder marker**. So in `staging` and
  `prod` shapes the BFFs refuse to boot until you set real secrets; in the `dev` shape the
  realm's shipped dev secrets work as-is.

To rotate: change the client secret on the Keycloak client (or in `dialysis-realm.json`
for the dev realm) and set the matching `.env` variable — the two must agree, since the
BFF presents the secret to Keycloak on every code exchange and token refresh.

## Relationship to the other deployment outputs

- **Kubernetes / Helm**: the same AppHost publishes per-env charts to
  `deploy/charts/dialysis-<env>/` via `./build.sh PublishKubernetes --environment <env>`
  (or `PublishAllKubernetes`). The chart ships Keycloak without the realm import (the k8s
  publisher rejects bind mounts); operators wire `Authentication__Authority` per cluster.
- **HA operators**: CloudNativePG / RabbitMQ Cluster Operator CRs are hand-maintained under
  `deploy/k8s/operators/` (Aspire doesn't model arbitrary CRDs).
- **Image registry**: `./build.sh PushImages --registry <host>/<repo>` builds and pushes
  all 22 repo-built images tagged with the GitVersion SemVer. Registry qualification is
  publish-time-only (`DIALYSIS_IMAGE_REGISTRY` / `DIALYSIS_IMAGE_TAG`); the committed
  compose folders deliberately keep build-only services with local names so the drift gate
  stays registry-free.
