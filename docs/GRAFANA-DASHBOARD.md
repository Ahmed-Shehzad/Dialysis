# Grafana Dashboard for Dialysis PDMS

Example Prometheus queries and dashboard setup for the PDMS Gateway metrics.

---

## Prerequisites

- Gateway exposing `/metrics` (Prometheus format)
- Prometheus scraping the Gateway
- Grafana configured with Prometheus as data source

---

## Key Metrics (OpenTelemetry / ASP.NET Core)

| Metric | Description |
|--------|-------------|
| `http_server_request_duration_seconds` | Request duration histogram |
| `http_server_request_duration_seconds_count` | Total request count |
| `http_server_request_duration_seconds_sum` | Sum of durations |
| `http_server_requests_in_progress` | In-flight requests |

---

## Example Prometheus Queries

### Request rate (requests/sec)

```promql
rate(http_server_request_duration_seconds_count[5m])
```

### P95 latency (seconds)

```promql
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))
```

### Requests by path (top 10)

```promql
topk(10, sum by (http_route) (rate(http_server_request_duration_seconds_count[5m])))
```

### Error rate (5xx)

```promql
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m])) * 100
```

---

## Dashboard JSON (Minimal)

Import this JSON into Grafana (Create → Import → paste):

```json
{
  "title": "Dialysis PDMS Gateway",
  "panels": [
    {
      "title": "Request Rate",
      "type": "graph",
      "targets": [{
        "expr": "sum(rate(http_server_request_duration_seconds_count[5m]))",
        "legendFormat": "req/s"
      }]
    },
    {
      "title": "P95 Latency",
      "type": "graph",
      "targets": [{
        "expr": "histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))",
        "legendFormat": "P95"
      }]
    }
  ]
}
```

---

## References

- [HEALTH-CHECK.md](HEALTH-CHECK.md) – Gateway `/health` and `/metrics`
- [OpenTelemetry ASP.NET Core](https://opentelemetry.io/docs/instrumentation/net/aspnetcore/)
