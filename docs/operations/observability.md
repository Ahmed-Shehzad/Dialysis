# Observability

How to see what the platform is doing in production. The Aspire AppHost wires
OTLP for traces + metrics + logs to every module API and the gateway, exported via
the OTel collector to whichever backend the operator chooses. Dev runs against the
Aspire dashboard; staging + prod target a Prometheus + Grafana + Loki stack on
the cluster.

## What's emitted

### Metrics

Per-module meters wired through `ModuleTelemetryOptions.AdditionalMeters`:

| Meter | Source | Highlights |
|---|---|---|
| `Microsoft.AspNetCore.Hosting` | Built-in | `http.server.request.duration`, status-code distribution |
| `Microsoft.AspNetCore.RateLimiting` | Built-in | `aspnetcore.rate_limiting.queued_requests`, `…rejected_requests` |
| `Microsoft.EntityFrameworkCore` | Built-in (when EF.Core SqlMetrics enabled) | `db.client.connections.usage`, command duration |
| `Dialysis.DurableCommandBus` | BuildingBlock | See below |
| `RabbitMQ.Client.OpenTelemetry` | Transport | Publish-confirm latency, consumer ack rate |

`Dialysis.DurableCommandBus` emits the metrics dashboard JSON
(`deploy/k8s/observability/dashboards/durable-command-bus.json`) graphs:

| Instrument | Type | Tags | What it tells you |
|---|---|---|---|
| `dialysis.durable_commands.enqueued` | counter | `module`, `command_type` | Rate of commands accepted by the publisher (publisher-confirm ACK received). |
| `dialysis.durable_commands.applied` | counter | `module`, `command_type` | Rate of commands the consumer committed to the database. Lag behind enqueue = consumer fall-behind. |
| `dialysis.durable_commands.failed` | counter | `module`, `command_type`, `reason` (`catalog_or_payload` \| `handler_exception`) | Terminal failures. Distinguished by failure mode so dashboards can surface the right alert. |
| `dialysis.durable_commands.latency` | histogram (seconds) | `module`, `command_type` | EnqueuedAtUtc → AppliedAtUtc end-to-end. Headline durability latency. |
| `dialysis.durable_commands.enqueue_latency` | histogram (ms) | `module`, `command_type` | Bus-side publish-confirm round-trip. Drives the 202-response SLO. |

### Traces

Every command applied via the durable consumer creates a parent span on the consumer's
`DurableCommandConsumer.HandleAsync`, with child spans for `ledger.TryClaim`,
`handler.HandleAsync`, `ledger.MarkApplied`. The `DurableCommandScope.Activate(commandId)`
also propagates the command id as a trace attribute, so a trace can be located via the
status endpoint's `correlationId` field.

### Logs

Structured (key=value) via `ILogger` defaults; `Microsoft.Extensions.Logging.Console`'s
JSON formatter is on in non-Development. Every durable-bus operation logs at Information
level with:

- `commandId` (Guid) — primary key on the ledger
- `commandTypeKey` (string) — assembly-qualified name
- `module` (string) — slug
- `correlationId` (string) — surfaced to the 202 caller
- `consumerInstanceId` (string) — for poison-message forensics

Failures log at Warning with the exception attached. The combination of `correlationId`
+ structured log fields means a support ticket's polling URL is enough to find every
log line touching that command across the publish + consume hops.

## Dashboards

`deploy/k8s/observability/dashboards/`:

- `durable-command-bus.json` — the headline dashboard. Six panels: enqueue rate,
  applied rate, latency p99/p50, failure rate by reason, publish-confirm latency p99,
  RMQ queue depth. Template variable `$module` defaults to "All".

Import:

```bash
# kubectl + sidecar approach (Grafana operator's dashboards-provisioner)
kubectl create configmap dialysis-dashboards \
  --from-file=deploy/k8s/observability/dashboards/ \
  --namespace monitoring \
  --dry-run=client -o yaml \
  | kubectl apply -f -
```

The CloudNativePG operator + the RabbitMQ Cluster Operator both ship their own
dashboards via their respective Helm charts; install those alongside for full
durability-tier coverage.

## Alerts

`deploy/k8s/observability/alerts/durable-command-bus.yaml` is a Prometheus
`PrometheusRule` CR with four rules:

| Alert | Severity | Trigger |
|---|---|---|
| `DurableCommandBusBacklogGrowing` | warning | RMQ queue depth > 1000 sustained 5 min |
| `DurableCommandHandlerFailures` | critical | Handler exceptions > 0.1/s sustained 10 min |
| `DurableCommandLatencyP99High` | warning | Enqueue→applied p99 > 5s sustained 10 min |
| `DurableCommandEnqueuePublishFailing` | critical | Bus throwing `DurableCommandException` > 0.5/s sustained 5 min |

Apply:

```bash
kubectl apply -n monitoring -f deploy/k8s/observability/alerts/durable-command-bus.yaml
```

Each alert carries a `runbook_url` pointing at the durable-writes architecture doc.

## Structured logging conventions

When adding a new feature that produces operationally-interesting logs:

| Convention | Example | Why |
|---|---|---|
| Lower-snake-cased field names | `command_id`, `patient_id`, `session_id` | Stable across log destinations; both Serilog + the JSON console formatter respect them |
| One log per state transition | `"Reading recorded"` not `"Reading recorded successfully"` | Easier to alert on substring; avoids prose drift |
| PII redaction at the log site, not downstream | Wrap with `[Loggable]` attribute or strip in the formatter | Less risk of accidental clinical-record leakage |
| `correlationId` always when crossing process boundaries | Set by `CorrelationIdMiddleware` for HTTP; by `DurableCommandConsumer` for RMQ | Tail a request across the gateway → module → consumer → DB write |
| Exception logs at Warning unless the system can't continue | A failed durable command is Warning (broker redelivers); a missing realm secret is Error (host can't start) | Avoid pager fatigue |

The HIPAA `[PhiAccess]` attribute on a controller method writes a `FhirAudit.AuditEvent` row that includes the actor + resource + outcome — that's an audit log, not an ops log, and it lives in the per-module `*_audit` schema. The ops log goes through the OTel pipeline to wherever Loki / Elasticsearch / a hosted SaaS catches it.

## Anti-patterns to watch for

1. **Logging the envelope's `PayloadJson`** — it can contain PII. Log `commandId` + `commandTypeKey` only.
2. **High-cardinality labels** on metrics — never tag by `patient_id` or `command_id`; cardinality explodes.
3. **Sampling at the application layer** — let the collector decide. The Aspire AppHost's `OTEL_TRACES_SAMPLER` controls the sample ratio per env.
4. **Custom dashboards in the AppHost** — keep them in `deploy/k8s/observability/` so they version with the code.
