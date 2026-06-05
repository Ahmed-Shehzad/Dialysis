# Deployment compose

Production-ready topology for the Dialysis modular monolith. Three environment shapes
(`dev`, `staging`, `prod`) are generated from the .NET Aspire AppHost
(`src/aspire/Dialysis.AppHost`). **There is no hand-curated overlay** — every overlay
concern (build stanzas, host port mappings, ASP.NET production hardening, the OTLP
collector, healthchecks, replica counts) lives in the AppHost.

## Layout

```
deploy/compose/
  otel-collector.yaml     # bind-mounted into the OTEL collector at /etc/otel-collector.yaml
  dev/
    docker-compose.yaml   # generated — ASP.NET Development env, no HSTS, single replica
    .env                  # generated — image tags + port placeholders
    aspire-manifest.json  # generated — Aspire's intermediate description (traceability only)
  staging/
    docker-compose.yaml   # generated — Production hardening, single replica, looser healthchecks
    .env
    aspire-manifest.json
  prod/
    docker-compose.yaml   # generated — Production hardening, 2 replicas, RequireAuthority on
    .env
    aspire-manifest.json
```

The three folders share 90 % of the topology; the diff is the small set of strings the
helper extensions under `src/aspire/Dialysis.AppHost/ComposePublishExtensions.cs` read
from `DeploymentEnvironment`.

## What each shape gives you

| Concern | `dev` | `staging` | `prod` |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Staging` | `Production` |
| HSTS / ForwardedHeaders | off | on | on |
| `Authentication__RequireAuthorityWhenNotDevelopment` | off | off | on |
| OTLP collector + `<Module>__Telemetry__OtlpEndpoint` | not wired | wired to `otel-collector:4317` | wired to `otel-collector:4317` |
| `deploy.replicas` (module APIs + gateway) | 1 | 1 | 2 |
| Postgres healthcheck interval | 5 s | 10 s | 5 s |
| Host port mappings | same as prod | same as prod | the canonical set (`5288–5292`, `5275`, `9090`, `8080`, `8081`, `9000`, `5440–5445`, `5672`, `15672`, `6379`) |

## Regenerate

After **any** AppHost change (new module, env-var rename, port change):

```bash
# Regenerate one environment
./build.sh PublishCompose --environment dev
./build.sh PublishCompose --environment staging
./build.sh PublishCompose --environment prod

# Or do all three in one go (run this before committing any AppHost change)
./build.sh PublishAllCompose
```

The NUKE target sets `DIALYSIS_DEPLOY_ENV=<env>` and wraps
`dotnet run --project src/aspire/Dialysis.AppHost --publisher compose --output-path
deploy/compose/<env> --deploy false`. The `--deploy false` flag tolerates failures from
the deploy step (which requires a running Docker daemon) so artifact regeneration
doesn't depend on having Docker on the host.

Commit the regenerated `docker-compose.yaml` + `.env` + `aspire-manifest.json` alongside
the AppHost change so reviewers see the topology delta inline.

## Run

```bash
cd deploy/compose/prod          # or dev / staging
docker compose up -d --build
```

This builds the module-API + gateway + identity-BFF images from the repo using
`Dockerfile.module` / `Dockerfile.gateway` (the build stanzas Aspire wrote); pulls the
infra images (Postgres, Valkey, RabbitMQ, Keycloak, SonarQube); and brings the whole
topology up. The browser entry point is **`http://localhost:9090`** — same as the dev
Aspire loop.

> **Per-context BFFs + web apps.** Since the BFF-per-context refactor, the seven per-context
> BFFs (`his-bff` … `portal-bff`) and seven SPAs (`his-web` … `patient-portal-web`) are emitted
> as services that reference a pre-built `${<CTX>_*_IMAGE}` rather than carrying a `build:`
> stanza, and the gateway's per-context cluster destinations still resolve to `localhost`. A
> follow-up adds the build stanzas + container-hostname cluster overrides so `docker compose up
> --build` brings the per-context topology up end-to-end. Until then this command stands up the
> module APIs, gateway, identity BFF, and infra.

Scale a module horizontally:

```bash
docker compose up -d --scale his-api=3
```

Valkey backs the ASP.NET Data Protection key ring so multi-replica is safe.

## Tear down

```bash
docker compose down
# Add -v to wipe the per-module Postgres + SonarQube volumes too.
```

## Why no override file?

PR #131 pushed every overlay concern into the AppHost via
`PublishAsDockerComposeService` callbacks. Specifically:

1. **`build:` stanzas** — `ApplyModuleDockerfileBuild(service, project, dll)` writes the
   compose `build` block pointing at `Dockerfile.module` with the per-service
   `MODULE_PROJECT` + `MODULE_DLL` build args. The gateway gets its own `Dockerfile.gateway`;
   the SPA gets its per-app `Dockerfile` next to the sources.
2. **Host port mappings** — `ApplyHostPort(service, hostPort, containerPort)` writes
   compose-short-syntax `"<host>:<container>"` strings into `service.Ports`. Every public
   port (gateway 9090, web 8080, Keycloak 8081, SonarQube 9000, per-module Postgres
   5440–5445, RabbitMQ 5672 / 15672, Valkey 6379) is set here.
3. **ASP.NET production hardening** — `ApplyAspNetEnvironment` + `ApplyModuleHardening`
   set `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS=http://+:<port>`, `<Module>__UseHsts`,
   `<Module>__UseForwardedHeaders`, `<Module>__Authentication__RequireAuthorityWhenNotDevelopment`,
   and `<Module>__Telemetry__OtlpEndpoint`. The HSTS / Authority knobs only flip on under
   `staging` / `prod`; the dev shape stays clean.
4. **Postgres healthchecks** — `WithPublishedDatabasePort` writes a `pg_isready` check
   onto each per-module Postgres service.
5. **OTEL collector** — `ConfigureComposeFile` (Aspire's whole-file mutation hook) injects
   the OTEL collector service into the published YAML for the hardened environments. The
   collector binds `../otel-collector.yaml` (the file next to the per-env folder).
6. **Replicas** — `ApplyReplicas` writes `deploy.replicas` per service; `2` for module
   APIs / gateway under `prod`, `1` everywhere else. Scale-out via `--scale` still works.

## BFF client secrets

Every per-context BFF (`his`, `ehr`, `pdms`, `smartconnect`, `hie`, `admin`, `portal`) and the
legacy identity BFF authenticates to Keycloak as a **confidential client** (`publicClient:
false`). The BFF must present its client secret on the token + pushed-authorization (PAR)
endpoints — without it, the `/{ctx}/identity/login` challenge fails with `invalid_request /
"Authentication failed"` and the BFF returns **HTTP 500**.

The AppHost declares each secret as an Aspire **secret parameter** rather than a baked string:

| Parameter (`builder.AddParameter`) | Feeds env var | Keycloak client |
|---|---|---|
| `his-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-his-bff` |
| `ehr-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-ehr-bff` |
| `pdms-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-pdms-bff` |
| `smartconnect-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-smartconnect-bff` |
| `hie-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-hie-bff` |
| `admin-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-admin-bff` |
| `portal-bff-client-secret` | `Bff__Keycloak__ClientSecret` | `dialysis-portal-bff` |
| `identity-bff-client-secret` | `Identity__Keycloak__ClientSecret` | `dialysis-bff` |

Each parameter **defaults to the dev secret** committed in
`src/backend/Identity/keycloak/dialysis-realm.json` (`<ctx>-bff-dev-secret-change-me`), so the
dev F5 loop works with no extra steps. Because the value is a `secret: true` parameter, Aspire
emits the dev default only into the generated **`.env.Development`** and leaves the base **`.env`**
entry **blank** — it is *not* baked into the image. So `docker compose` runs that load
`.env.Development` get the dev secret out of the box, while the production `.env` stays empty for
the operator to fill.

**For `prod` you MUST override both sides:**

1. Set a real secret per BFF in the deployment environment. For compose, edit the generated
   `prod/.env` (or inject from your secret store / CI):

   ```dotenv
   his-bff-client-secret=<real-secret-from-vault>
   smartconnect-bff-client-secret=<real-secret-from-vault>
   # … one line per BFF parameter above
   ```

   (ASP.NET also reads the canonical env vars directly — e.g.
   `Bff__Keycloak__ClientSecret` / `Identity__Keycloak__ClientSecret` — if you prefer to set
   those instead of the parameter names.)

2. Set the **matching** secret on the corresponding client in your prod Keycloak realm. The
   k8s chart deliberately ships Keycloak *without* the bundled dev realm (operators import
   their own), so the two sides only line up if you configure them to.

Leaving the `…-change-me` defaults in `prod` is a hard failure: login 500s the moment the
operator-managed realm rotates the client secret away from the dev value.

## Relationship to other compose files in the repo

| File | Lives | Purpose | Keep? |
|---|---|---|---|
| `deploy/compose/{dev,staging,prod}/docker-compose.yaml` | this folder | Aspire-generated, per-env topology | **Source of truth.** Regenerated by NUKE. |
| `src/backend/Identity/docker-compose.yml` | Identity module | Identity-only smoke flow (Keycloak + Postgres + BFF on port 5444) | **Keep** — different scope; documented in `RUNBOOK.md`. |
| `src/backend/HIS/docker-compose.integration.yml` | HIS module | HIS outbox golden-path Testcontainers fixture (`HIS_CI_OUTBOX_E2E=1`) | **Keep** — referenced by `.github/workflows/his-ci.yml`. |
| ~~`deploy/compose/docker-compose.override.yaml`~~ | — | Hand-curated overlay before PR #131 | **Deleted** — every concern is in the AppHost now. |
| ~~`docker-compose.modules.yml`~~ | repo root | Hand-maintained topology before Aspire publisher | **Deleted** in PR #130. |

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `docker compose config` reports an undefined service | The AppHost added a new resource but you didn't regenerate. `./build.sh PublishAllCompose`. |
| Publish fails: `Sequence contains more than one matching element` (no `docker-compose.yaml` written) | Aspire 13.4.x runs the publisher's `before-start` phase — and its non-idempotent `prepare-deployment-targets-*` step — twice, so every compute resource accrues two `DeploymentTargetAnnotation`s. The AppHost installs a `dialysis-dedupe-deployment-targets-*` pipeline step to collapse the duplicates; keep it until the upstream double-run is fixed. |
| Module API → Keycloak fails inside compose | Compose network DNS — `keycloak` resolves only inside the compose project. Verify all services are on the auto-generated network. |
| OIDC redirect_uri mismatch | The published YAML uses container hostnames; the realm import (`src/backend/Identity/keycloak/dialysis-realm.json`) registers `http://localhost:9090/*` and `http://localhost:5275/*` — those work via the host port mappings the AppHost writes. Don't change either side unless the other moves with it. |
| `/{ctx}/identity/login` returns HTTP 500 (`invalid_request` / "Authentication failed") | The BFF's confidential-client secret is empty or doesn't match its Keycloak client. See §"BFF client secrets" — set the `<ctx>-bff-client-secret` parameter and the matching realm client secret. |
| `docker compose up` hangs on image pull | Docker Hub rate-limited. Authenticate (`docker login`) or pre-pull the infra images. |
| `dev` shape behaves like prod | Check that `./build.sh PublishCompose --environment dev` actually wrote to `deploy/compose/dev/` and that you ran `docker compose` from that folder, not from `deploy/compose/prod/`. |
