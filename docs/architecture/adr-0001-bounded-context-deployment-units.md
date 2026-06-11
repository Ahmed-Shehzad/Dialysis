# ADR-0001 — Bounded-context deployment units (horizontal scaling first)

**Status:** Accepted (2026-06-10)
**Decision drivers:** the architect's direction to shift from vertical to horizontal scaling and
to evolve the modular monolith toward microservices **without leaving the monorepo**, with each
bounded context shipping as **one independently deployable unit**.

## Decision

1. **Unit = one Helm release per bounded context, separate processes inside.** A unit carries
   everything its context owns — module API, context BFF, context SPA (nginx), and the context's
   PostgreSQL — and deploys/upgrades/rolls back independently of every other unit. The API and
   BFF remain separate processes so they scale independently (chairside read bursts hit the API
   tier; session churn hits the BFF tier).
2. **Unit inventory:** `his`, `ehr`, `pdms`, `smartconnect`, `hie` (API + BFF + SPA + DB),
   `lab` (API + DB, headless), `identity` (Identity API/DB + Identity BFF + Admin BFF +
   identity-web + Keycloak), `portal` (PatientPortal BFF + patient-portal-web), and
   `platform` (gateway + RabbitMQ + Valkey). All units share one namespace per environment
   (`dialysis-<env>`) so in-cluster DNS forms the cross-unit contract.
3. **The Aspire AppHost stays the single source of truth.** Unit artifacts are *generated*
   (`DIALYSIS_DEPLOY_UNIT` + the NUKE `PublishKubernetesUnit`/`PublishAllKubernetesUnits`
   targets → `deploy/charts/units/<env>/dialysis-<unit>/`) and drift-gated exactly like the
   full-stack artifacts. When the variable is unset, the full-stack shapes are byte-identical
   to before — the monolithic deployment remains available as a fallback and for dev/staging.
4. **Cross-unit dependencies are configuration, not model references.** Inside a unit chart,
   RabbitMQ/Valkey/Keycloak appear as connection-string/parameter values defaulting to the
   platform/identity units' stable in-cluster DNS names, overridable per cluster at
   `helm install` time.
5. **Communication contract is unchanged**: cross-context flow rides integration events over
   RabbitMQ (`<Module>.Contracts` + Transponder) — never direct references. The architecture
   tests keep enforcing this, which is precisely what makes per-context deployment safe.

## Horizontal-scaling guarantees (what makes replicas > 1 correct)

| Concern | Mechanism | Status |
|---|---|---|
| Session state | BFF cookie ticket store + Data Protection key ring in Valkey | in place |
| SignalR fan-out | Valkey backplane on the BFF event hubs and the PDMS vitals hub | in place (config-gated; wired in prod shapes) |
| Background scheduling | PostgreSQL-backed Hangfire (distributed locks per job) | in place |
| Consumer idempotency | Transponder EF inbox (`DeduplicationKey` unique index) + durable-command `command_ledger` claims | in place |
| Broker availability | RabbitMQ quorum queues (3-replica operator cluster) | in place |
| **Outbox relay** | **Per-database Postgres advisory lock per polling tick** — one replica relays, others skip; lock explicitly released (Npgsql pool reset does not release advisory locks); crash → server releases on disconnect | **added with this ADR** (`TransponderOutboxRelayHostedService`, covered by `OutboxRelayAdvisoryLockTests`) |
| Resource governance | Requests/limits, PDBs (replicas > 1), HPA (CPU + queue-depth for PDMS), PgBouncer | in place |
| Database HA | CNPG sync replication (clinical tier) / async (integration tier), WAL→S3 PITR | in place |

Known accepted limits: the relay lock serializes outbox publishing per module database (the
relay is I/O-bound and per-module, so this is throughput-sufficient; if a module ever outgrows
it, partition the outbox before parallelizing relays). In-memory fallbacks
(`InMemoryDocumentBlobStore`, in-memory distributed cache) exist only where Valkey
configuration is absent — production shapes always configure Valkey.

## Consequences

- Teams ship one bounded context end-to-end (API+BFF+SPA+DB schema) without coordinating a
  platform release — the microservices operating model, monorepo retained.
- The gateway remains the single browser origin; adding/removing a unit means updating its
  ReverseProxy cluster values on the platform unit, not re-deploying contexts.
- Image publishing per unit rides the existing `PushImages` flow (images are per-service
  already; a unit deploy needs only its own images present in the registry).
- Install order for a fresh cluster: `platform` → `identity` → context units (documented in
  `deploy/charts/units/README.md`).
- The full-stack chart (`deploy/charts/dialysis-<env>`) stays generated and drift-gated; it is
  the integration-test and staging shape, and the escape hatch if unit-granular operations are
  ever not worth their overhead.
