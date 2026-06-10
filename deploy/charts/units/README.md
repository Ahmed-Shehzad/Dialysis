# Per-unit Helm charts (`deploy/charts/units/`)

The platform's deployment story has two Kubernetes shapes, both **generated from the Aspire
AppHost** (the single source of truth — never hand-edit anything under this folder):

- `deploy/charts/dialysis-<env>/` — the classic **full-stack** chart: the entire topology in one
  release.
- `deploy/charts/units/<env>/dialysis-<unit>/` — **deployment units**: independently installable
  bounded-context slices that together reconstitute the same topology, for a
  microservices-style ops model (independent rollout/rollback/scaling per bounded context) while
  the codebase stays a monorepo/modular monolith.

Units are generated **for `prod` only** by design: dev is the Aspire F5 loop and staging is the
full-stack chart; carving the topology into independently deployable releases is a
production-operations concern. (`./build.sh PublishKubernetesUnit --unit <u> --environment dev|staging`
still works for ad-hoc experiments, but only `units/prod/` is committed and drift-gated.)

## What a unit is

A unit is the set of resources one team can deploy, upgrade, and roll back independently. Each
unit chart is named `dialysis-unit-<unit>`, installs with release name `dialysis-<unit>`, and —
crucially — targets the **same namespace as the full chart** (`dialysis-<env>`), because the
units find each other via stable in-cluster Service DNS names (Aspire names every Service
`<resource>-service`, with no release-name prefix).

| Unit | Contents |
|---|---|
| `platform` | edge gateway (+ namespace-wide NetworkPolicies + the Ingress), RabbitMQ, Valkey |
| `identity` | Keycloak, identity BFF (OIDC handshake), admin BFF, identity-web (admin console) |
| `his` | his-api, his-bff, his-web, postgres-his |
| `ehr` | ehr-api, ehr-bff, ehr-web, postgres-ehr |
| `pdms` | pdms-api, pdms-bff, pdms-web, postgres-pdms (TimescaleDB) |
| `smartconnect` | smartconnect-api, smartconnect-bff, smartconnect-web, postgres-smartconnect |
| `hie` | hie-api, hie-bff, hie-web, postgres-hie |
| `lab` | lab-api, postgres-lab (headless — no BFF/SPA) |
| `portal` | portal-bff, patient-portal-web (the portal domain lives in EHR/HIS) |

SonarQube is a dev-time analyzer and is **never** part of any unit (it only ships in the
full-stack artifacts). There is no separate "Identity API / Identity Postgres" workload — Keycloak
runs `KC_DB=dev-mem` in the generated chart, and the identity/admin BFFs park their Hangfire
schemas on the HIS/HIE module databases (external connection strings in the identity unit, see
below).

## Install order

1. **`platform`** — brings up RabbitMQ + Valkey (every other unit's bus/cache) and the gateway +
   Ingress (the only browser-facing surface).
2. **`identity`** — brings up Keycloak (every BFF's and API's OIDC authority).
3. **The contexts** — `his`, `ehr`, `pdms`, `smartconnect`, `hie`, `lab`, `portal`, in any order.
   (Note the Hangfire coupling: the `identity` unit's BFFs and the `portal` unit's BFF need the
   HIS — and for the admin BFF the HIE — Postgres to exist before their pods go healthy, so in
   practice bring `his`/`hie` up before expecting `identity`/`portal` pods to settle.)

```bash
helm install dialysis-platform deploy/charts/units/prod/dialysis-platform -n dialysis-prod --create-namespace
helm install dialysis-identity deploy/charts/units/prod/dialysis-identity -n dialysis-prod
helm install dialysis-his      deploy/charts/units/prod/dialysis-his      -n dialysis-prod
# ... and so on for ehr / pdms / smartconnect / hie / lab / portal
```

## The cross-unit DNS / values contract

Cross-unit dependencies are **not** modeled as in-chart workloads; they are external references
the AppHost emits as Helm values, defaulted to the sibling units' stable in-cluster Service DNS
names. When all units share one namespace and you didn't rename Services, the host parts of the
defaults work as-is — but every cross-unit credential ships as a `CHANGE-ME` placeholder that an
operator **must** replace at install time (the in-chart hosts stay, only the password changes).
The hosts carry resolvable defaults deliberately: the publish pipeline needs resolvable values to
render the chart, and `CHANGE-ME` makes a forgotten override fail loudly at the broker/database
rather than silently authenticating:

| Value (per consuming unit) | Meaning | Default |
|---|---|---|
| `parameters.<consumer>.rabbitmq_connection` | RabbitMQ broker (platform unit); feeds `ConnectionStrings__rabbitmq` + `<Module>__Transponder__RabbitMq__ConnectionUri` | `amqp://guest:CHANGE-ME@rabbitmq-service:5672` |
| `parameters.<consumer>.valkey_connection` | Valkey (platform unit); feeds the distributed-cache, ticket-store, and SignalR-backplane keys | `valkey-service:6379,password=CHANGE-ME` |
| `parameters.<bff>.hangfire_his_connection` / `hangfire_hie_connection` (identity + portal units) | Hangfire storage on the HIS / HIE units' Postgres | `Host=postgres-<module>-service;Port=5432;Username=postgres;Password=CHANGE-ME;Database=dialysis_<module>` |
| `secrets.<bff>.<x>_bff_client_secret` | the BFF's confidential Keycloak client secret (the BFFs fail fast outside Development while it still says `change-me`) | dev placeholder |
| `secrets.postgres_<module>.postgres_<module>_password` | the unit's own Postgres password | generated/empty — set per cluster |

Config values with working in-namespace defaults (override only when your cluster shape differs):

| Value | Default |
|---|---|
| `parameters.keycloak_authority.value` (every non-identity unit) | `http://keycloak-service:8081/realms/dialysis` — the identity unit's Keycloak Service exposes port **8081** → targetPort 8080, so cross-unit consumers dial 8081. (The full chart's *internal* configs say `:8080`; that value does not resolve through the Service and needs an operator override there too — the unit defaults are the corrected ones.) |
| `config.<bff>.Bff__Module__Aggregations__N__Address` | `http://<api>-service:8080` for each cross-unit module API the BFF aggregates |
| `config.<bff>.Bff__Module__ModuleApiAddress` | within-unit APIs inherit the full chart's quirk of resolving to `""` (the BFF then falls back to its appsettings default) — set it to `http://<api>-service:8080` explicitly; the admin/portal BFFs (whose primary API lives in another unit) already default to the sibling Service DNS |
| `config.gateway.ReverseProxy__Clusters__<id>__Destinations__d1__Address` (platform unit) | one per gateway cluster: BFFs `http://<ctx>-bff-service:5301..5307/`, identity `http://identity-bff-service:5275/`, webs `http://<ctx>-web-service:5331..5337/` (`admin-web` → `identity-web-service`, `portal-web` → `patient-portal-web-service`), `keycloak` → `http://keycloak-service:8081/` |
| `config.<bff>.ASPNETCORE_URLS` / `config.gateway.ASPNETCORE_URLS` | `http://+:<port>` — unit charts bind all interfaces (the full chart's `http://localhost:<port>` is unreachable from other pods) |

Keycloak realm note: the generated chart ships Keycloak **without** the `dialysis` realm (the k8s
publisher cannot model the realm-import bind mount). Import
`src/backend/Identity/keycloak/dialysis-realm.json` via your own ConfigMap/init-container, or
point `parameters.keycloak_authority.value` at an externally managed Keycloak.

NetworkPolicies and the Ingress are platform-unit concerns: the `deny-cross-namespace-ingress`
policy plus the gateway allow-rule and the `dialysis` Ingress are emitted only into
`dialysis-platform`. Workload hardening (resource envelopes, replica counts, PDBs) is emitted
into whichever unit owns the workload, identical to the full chart.

## How to regenerate

```bash
./build.sh PublishKubernetesUnit --unit his            # one unit (default --environment prod)
./build.sh PublishAllKubernetesUnits                   # all nine prod unit charts
./build.sh PublishDeployArtifacts                      # everything: compose + full charts + unit charts
```

Each target wraps the Aspire k8s publisher with `DIALYSIS_DEPLOY_UNIT=<unit>` (see
`src/aspire/Dialysis.AppHost/DeploymentUnit.cs` for the membership logic). When that variable is
unset the AppHost publishes the classic full-stack artifacts byte-for-byte unchanged — the CI
drift gate (`.github/workflows/deploy-artifacts.yml`) regenerates **both** shapes and fails the
PR if anything under `deploy/` differs from the AppHost. After any AppHost change, re-run
`./build.sh PublishDeployArtifacts` and commit the result.
