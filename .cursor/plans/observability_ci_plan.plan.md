---
name: Observability and CI
overview: Add OpenTelemetry metrics to Gateway; create GitHub Actions CI for build and test.
todos:
  - id: obs-gateway
    content: Add OpenTelemetry metrics + Prometheus /metrics to Gateway
    status: completed
  - id: ci-workflow
    content: Create GitHub Actions CI (build, test)
    status: completed
  - id: docs-update
    content: Document metrics and CI in docs
    status: completed
---

## Observability

- Gateway is the main entry point; add OpenTelemetry there.
- Use AspNetCore instrumentation (HTTP request duration, count).
- Expose Prometheus `/metrics` for scraping.

## CI

- Build solution, run Dialysis tests.
- No load test in CI (requires docker compose); use deploy/staging for that.
