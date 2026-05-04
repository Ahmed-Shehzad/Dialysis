# HIS — production deployment and environment contract

Engineering-focused checklist (no clinical/regulatory attestation). Aligns with [his_production_security_backlog.md](./his_production_security_backlog.md) and [README.md](./README.md).

## 1. Environment variables and Key Vault mapping

Use **double underscore** (`__`) for nested keys in Linux containers and GitHub Actions (e.g. `His__Transponder__EnableOutboxRelay`).

| Configuration key | Purpose | Example env / secret name |
|---------------------|---------|---------------------------|
| `ConnectionStrings:His` | SQL Server for `HisDbContext` + Transponder outbox/inbox | Secret: `HisSqlConnectionString` |
| `His:Authentication:Authority` | JWT issuer (OIDC metadata) | Config: `HisAuthenticationAuthority` |
| `His:Authentication:Audience` | Optional JWT audience | Secret or config |
| `His:Authentication:RequireAuthorityWhenNotDevelopment` | When `true`, host fails fast if Authority is empty outside Development | Config |
| `His:Authentication:PermissionClaimType` | Claim type for HIS permission strings (default `his_permission`) | Config |
| `His:Authentication:RoleClaimType` | Role/group claim type for `RolePermissionMap` | Config |
| `His:Authentication:RolePermissionMap` | JSON map IdP role → HIS permission strings | Key Vault secret or config provider |
| `His:Transponder:EnableOutboxRelay` | Register outbox relay to `ITransponderBus` | Config |
| `His:Transponder:RabbitMq:ConnectionUri` | AMQP URI for RabbitMQ transport | Secret: `HisRabbitMqConnectionUri` |
| `His:Transponder:RabbitMq:QueueName` / `ExchangeName` | Optional broker topology overrides | Config |
| `His:Laboratory:BaseUri` | Optional HTTP lab gateway base | Config |
| `His:Pharmacy:BaseUri` | Optional HTTP pharmacy gateway base | Config |
| `His:RequireHttpsRedirection` / `His:UseHsts` / `His:UseForwardedHeaders` | Edge posture behind ingress | Config |
| `His:Telemetry:EnableOpenTelemetry` | Reserved; wire OpenTelemetry SDK when adopting repo-wide telemetry | Config |

**Non-secret sample** (values illustrative only; do not reuse passwords in production):

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__His="Server=tcp:sql.example;Database=His;User ID=his_api;Password=<from-vault>;Encrypt=True;TrustServerCertificate=False;"
export His__Authentication__Authority="https://idp.example/realms/his"
export His__Authentication__RequireAuthorityWhenNotDevelopment="true"
export His__Transponder__EnableOutboxRelay="true"
export His__Transponder__RabbitMq__ConnectionUri="amqps://user:<from-vault>@rabbit.example:5671/"
export His__UseForwardedHeaders="true"
export His__RequireHttpsRedirection="true"
export His__UseHsts="true"
```

## 2. TLS, reverse proxy, and CORS

- Terminate TLS at ingress; set `His:UseForwardedHeaders` so `X-Forwarded-Proto` / `X-Forwarded-For` reflect the client.
- Enable `His:RequireHttpsRedirection` and `His:UseHsts` in production hosts behind HTTPS ingress; keep health probes on HTTPS or exempt probes at the load balancer.
- **OpenAPI** (`/openapi/v*.json`): restrict by network policy or feature flag in production if the API surface must not be public.
- **CORS**: if browser clients are introduced, configure an explicit policy (origins/methods) instead of `AllowAnyOrigin` for production.

## 3. Database migrations job

- Apply EF migrations with the same connection string the API uses: `dotnet ef database update --project src/backend/HIS/Dialysis.HIS.Persistence/Dialysis.HIS.Persistence.csproj --startup-project src/backend/HIS/Dialysis.HIS.Persistence/Dialysis.HIS.Persistence.csproj` (see `HisDbContextDesignTimeFactory` for connection resolution), or run the API once with SQL configured so `HisDatabaseInitializer` executes `MigrateAsync` on startup (Kubernetes `Job` pattern acceptable for controlled rollouts).
- Prefer a dedicated migration job or pipeline stage **before** switching traffic when you need deterministic, reviewed schema changes.

## 4. Backup and restore scope

Include at least:

- Schemas **`his_*`** (bounded contexts and `his_ra`), **`his_migrations`** history, and **`transponder`** (outbox/inbox tables used by Transponder on `HisDbContext`).

Coordinate restore order and broker state (queues may contain undelivered messages) with your DR runbooks.

## 5. Observability

- **Correlation**: `CorrelationIdMiddleware` attaches a correlation id to requests (see API middleware).
- **Health**: `GET /health` (HATEOAS) and **`GET /health/ready`** (SQL `CanConnectAsync`) for load balancers and Kubernetes readiness.
- **OpenTelemetry / App Insights**: `His:Telemetry:EnableOpenTelemetry` and `His:Telemetry:OtlpExporterEndpoint` are reserved in `appsettings.json` for a future SDK hook; wire the standard ASP.NET exporter when your platform standard is chosen.

## 6. CI golden path

Automated proof of **SQL + Rabbit + outbox relay** is in [`.github/workflows/his-ci.yml`](../../../.github/workflows/his-ci.yml) and summarized in [his_transponder_e2e_runbook.md](./his_transponder_e2e_runbook.md).
