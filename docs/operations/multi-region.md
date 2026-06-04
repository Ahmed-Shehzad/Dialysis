# Multi-region — active-passive scaffolding

Single-region deployments cap latency for ~half the user base at the speed of
light to your data center. Multi-region brings the application closer to the
user; active-passive is the realistic shape for a clinical platform because
**write conflicts in clinical state are a regulatory nightmare** — active-active
would require eventual-consistency conflict resolution on patient records, which
no operator wants to defend.

This doc covers the scaffolding shipped today, what failover looks like, and
which parts are still "next-PR" rather than already wired.

## Topology

```
                    ┌──────────────────┐
                    │ DNS (Route 53 /  │
                    │ Cloudflare)      │
                    └────────┬─────────┘
                             │  weighted: PRIMARY=100, SECONDARY=0
              ┌──────────────┼──────────────┐
              ▼                             ▼
   ┌──────────────────┐         ┌──────────────────┐
   │ PRIMARY region   │         │ SECONDARY region │
   │  (e.g. us-east-1)│         │  (e.g. us-west-2)│
   │                  │         │                  │
   │  full stack:     │         │  full stack:     │
   │  gateway+APIs    │         │  gateway+APIs    │
   │  RMQ cluster     │         │  RMQ cluster     │
   │  CNPG clusters   │         │  CNPG clusters   │
   │  (writable)      │         │  (replica-only)  │
   │                  │  WAL    │                  │
   │  S3 archive ─────┼─►───────┤                  │
   └──────────────────┘         └──────────────────┘
```

PRIMARY: writable Postgres + active RMQ. Accepts 100 % of traffic.

SECONDARY: standby Postgres bootstrapped from the PRIMARY's WAL archive,
continuously streaming. The RMQ cluster in SECONDARY is idle until promoted.
Traffic stays at 0 % until DNS cuts over.

## What's shipped today

| Concern | Surface |
|---|---|
| Postgres warm standby per clinical module | `deploy/k8s/operators/templates/cnpg-multi-region-standby.yaml` — three `Cluster` CRs (HIS, EHR, PDMS) with `replica.enabled: true` + `externalClusters.barmanObjectStore` pointing at the primary's WAL archive |
| Region-aware render | `deploy/k8s/operators/render.sh` skips the standby manifest when `DIALYSIS_REGION_ROLE=primary` and skips the primary cluster manifest when `=secondary` — same chart, opposite roles per region |
| Region tag on telemetry | `ModuleTelemetryExtensions` reads `DIALYSIS_REGION` env var and stamps `cloud.region` onto every span + metric + log via OTel resource attributes. Dashboards filter by region for free |
| Per-env defaults | `deploy/k8s/operators/values/prod.env` carries `DIALYSIS_REGION_ROLE=primary`, `DIALYSIS_REGION=us-east-1`, `DIALYSIS_PRIMARY_WAL_BUCKET=s3://dialysis-wal-prod` |

## What's NOT shipped (next-PR territory)

| Concern | Why it's deferred |
|---|---|
| Cross-region RMQ federation | The durable command bus runs region-affinitized — commands enqueue + apply in the same region. A multi-region RMQ federation (shovel-based) is needed only if a brokered command must apply in BOTH regions, which is rare. Add when the use case shows up |
| Automatic DNS failover | We assume the operator drives the cutover via the DNS console (or a runbook script). Auto-failover via health-check probes is doable (Route 53 health checks + weighted records) but requires the operator to define "what's a region's health" precisely |
| Conflict resolution for the brief active-active window after promotion | Promotion is fast (~seconds) but during the window a stale primary could still accept writes. We rely on fencing — promoting requires the operator to explicitly demote the old primary first. Add a fencing controller when the use case demands |
| Per-region observability dashboards | The `cloud.region` tag is on every metric; build the dashboards when there's a real second region to compare. The existing dashboards already pivot on `module` + `command_type`, adding `region` is mechanical |

## Failover runbook

Assumes the SECONDARY region is healthy + caught up (replication lag should be
< 30s steady-state; check via `kubectl cnpg status pg-his -n dialysis-prod-sec`
on the secondary).

### Planned (maintenance window)

```bash
# 1. Quiesce the primary — refuse new writes by scaling the gateway to 0.
#    Existing in-flight durable commands drain through their consumers; new
#    requests get connection refused (the LB will then route via DNS-fallback).
kubectl scale deployment gateway -n dialysis-prod -- replicas=0

# 2. Wait for the durable-command queues to drain (RMQ mgmt UI). Typically < 30s
#    at steady-state telemetry rate.

# 3. Promote the SECONDARY's standby clusters.
SECONDARY_KUBECONFIG=~/.kube/dialysis-prod-sec
for mod in his ehr pdms; do
  KUBECONFIG=$SECONDARY_KUBECONFIG \
    kubectl patch cluster.postgresql.cnpg.io pg-$mod \
      -n dialysis-prod \
      --type=json \
      -p='[{"op": "remove", "path": "/spec/replica"}]'
done

# 4. Update DNS — flip weights. With Route 53:
aws route53 change-resource-record-sets --hosted-zone-id ZXXXX \
  --change-batch file://dns-flip-to-secondary.json

# 5. Verify on the secondary
KUBECONFIG=$SECONDARY_KUBECONFIG kubectl get cluster.postgresql.cnpg.io -n dialysis-prod
# All clusters should show role: Primary

# 6. Restart the secondary's gateway replicas (they were 0 by default)
KUBECONFIG=$SECONDARY_KUBECONFIG kubectl scale deployment gateway -n dialysis-prod -- replicas=3
```

### Unplanned (primary region lost)

Skip steps 1-2 (no functional primary to quiesce). Steps 3-6 unchanged.

**Data loss window**: bounded by the WAL archive flush cadence (default 5 min)
PLUS the standby's streaming lag (steady-state < 30s). Worst case: ~5 minutes of
writes lost. Mitigation: tune the CNPG `wal_archive_timeout` lower if RPO is
critical.

## Reverting (failback to original primary)

Once the original primary region is back online:

```bash
# 1. Original primary is now a replica of the new (formerly secondary) primary.
#    Apply cnpg-multi-region-standby.yaml against it.
PRIMARY_KUBECONFIG=~/.kube/dialysis-prod
DIALYSIS_REGION_ROLE=secondary \
DIALYSIS_PRIMARY_WAL_BUCKET=s3://dialysis-wal-prod-sec \
./deploy/k8s/operators/render.sh prod \
  | KUBECONFIG=$PRIMARY_KUBECONFIG kubectl apply -n dialysis-prod -f -

# 2. Let it catch up (kubectl cnpg status will show role: Standby + small lag)

# 3. Quiesce the secondary (now writing) and repeat the promotion sequence on
#    the primary cluster. DNS flip back.
```

## Cost considerations

| Resource | Steady-state cost in SECONDARY |
|---|---|
| CNPG replica nodes | Same compute as the primary (you pay for the running pods) |
| S3 inter-region transfer | WAL stream is typically 10s-100s of MB/hour per module → cents/day |
| RMQ cluster | Running but idle. Compute cost ≈ same as primary; storage near zero |
| Module API pods | HPA can scale these to `minReplicas` when SECONDARY traffic is 0; recommend `minReplicas: 1` to keep cold-start out of the failover path |
| Gateway | Same as APIs |

Active-passive isn't free, but the steady-state cost is bounded — typically
60-80% of primary-region cost. The alternative (cold restore from backup) trades
hours of RPO + RTO for the cost saving, which is usually wrong for clinical data.

## Validation drill

Quarterly recommended:

1. Pick a non-business-hours window.
2. Run the **Planned** failover sequence.
3. Verify all module APIs respond on the secondary's gateway endpoint within
   2 minutes of DNS propagation.
4. Sample a known patient record via the SPA → confirm read-after-write
   consistency (write a vital sign, read it back, check `cloud.region` on the
   resulting span equals the secondary).
5. Run the **Revert** sequence.

Document each drill's `time-to-failover` + `time-to-revert`; both should track
under 10 minutes after the runbook stabilizes.
