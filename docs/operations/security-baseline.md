# Security baseline

What the platform's automated security posture looks like — what each scanner catches,
what it doesn't, and the human-driven activities that fill the rest.

## Scanners running in CI

The `.github/workflows/security-scan.yml` workflow runs five jobs on every push to
`main` or `claude/**`, every PR, and weekly on a schedule:

| Job | Tool | What it catches | What it misses |
|---|---|---|---|
| **GitGuardian** | Repo-level GitGuardian Security Checks | Secrets / API keys / tokens in the working tree AND in the git history (proprietary SaaS ruleset) | Secrets that never went through git (env files, vault entries) |
| **dotnet-deps** | `dotnet list package --vulnerable --include-transitive` | CVEs in any NuGet dep (direct + transitive) | License risks, abandoned packages |
| **npm-deps** | `npm audit --audit-level=high` | CVEs in the SPA dep tree (production + dev) | Same as above |
| **trivy-fs** | Aqua Trivy filesystem scan | OS-package CVEs in Dockerfiles, IaC misconfigs in Helm values | Runtime config (env vars) |
| **zap-baseline** *(disabled)* | OWASP ZAP baseline scan | Missing security headers, mixed content, weak cookie attrs, info disclosure | Currently commented out — points at the Aspire dev smoke target which can't pass a hardening scan. Re-enable when a production-hardened staging target is available (rule overrides in `tests/security/zap-baseline-rules.tsv` already encode what we want failing). |

`.gitleaks.toml` ships as a fallback config for teams that want a self-hosted secret scanner (compliance ask, regulator audit, GitGuardian cost concern). To re-enable in CI, restore the `gitleaks` job in `.github/workflows/security-scan.yml`.

Run on every code change → cost is mostly compute; benefit is regression coverage of
the well-known issue surface.

## Penetration testing

Automated baseline scans (ZAP) are necessary but insufficient — they look for known
patterns, not novel logic flaws. The full penetration testing program has three tiers:

### Tier 1: continuous (automated, every CI run)

The five-job `security-scan.yml` workflow above. Findings land in the GitHub Security
tab as SARIF; failures block the PR. Already in place.

### Tier 2: scheduled (deeper, monthly)

Authenticated ZAP full scan plus targeted fuzzing against PHI endpoints. Runs against
a staging clone of prod, not the live stack:

```bash
# Drive ZAP with a real bearer token so it can crawl past the auth wall.
docker run --rm -t \
  -v "${PWD}":/zap/wrk:rw \
  -e ZAP_AUTH_TOKEN="$(./scripts/get-test-bearer.sh)" \
  ghcr.io/zaproxy/zaproxy:stable zap-full-scan.py \
    -t https://dialysis-staging.example/ \
    -r zap-full.html \
    -j -m 60 \
    -c tests/security/zap-baseline-rules.tsv \
    --hook=tests/security/zap-auth-hook.py
```

What it adds over the baseline scan: traversal of authenticated routes, fuzzing of
parameter values across the FHIR + admin APIs, deeper crawl with a JS-aware spider.

### Tier 3: human-driven (quarterly + before each major release)

A red-team engagement against a staging environment. The scope explicitly includes:

| Surface | Focus area | Why |
|---|---|---|
| Identity BFF | OIDC flow integrity, refresh-token rotation, kc_idp_hint allowlist | Federation surface — the multi-IdP plumbing we landed in PR #137 |
| PHI endpoints | Authorization bypass, IDOR, mass-assignment | PHI access is the highest-impact failure mode |
| `/api/v1.0/command-status/{correlationId}` | Cross-tenant correlation-id probing | The status endpoint authorizes per-row by `requestedBySubject` — the red team's job is to find ways to skirt that |
| `/api/v1.0/data-subject-rights/*` | Art. 17 erasure approval forgery | A spoofed approval is a regulatory + clinical disaster |
| FHIR Inbound | TEFCA IAS JWT trust validation, profile validation | We trust partner-issued JWTs — the trust anchor handling has to be airtight |
| HIE Documents | Tampering with PAdES signatures, retention policy bypass | Documents are clinical evidence |

Engagement deliverables: written report with CVSS-scored findings, fix verification
on a follow-up scan, public summary added to `docs/compliance/`.

## What the scanners don't replace

| Defense | Where it lives | Why scanners can't validate it |
|---|---|---|
| Per-row authorization | `IModuleAuthorizationService`, `[PhiAccess]` attribute | Logic correctness — needs unit tests + manual review |
| Permission gating | `PermissionGate` + BFF `permissions` claim | Same |
| Rate limiting + backpressure | `ModuleRateLimitingExtensions` + RMQ `x-max-length-bytes` | Behavior under stress — needs `tests/load/k6/` |
| Audit trail completeness | `[PhiAccess]` filter writes to `fhir_audit.audit_events` | Requires query + manual verification per PR that touches an audit-tracked endpoint |
| Encryption in transit | TLS termination at gateway | Operator concern, validated by the ZAP scan's HTTPS rules |
| Encryption at rest | Postgres TDE, Valkey RBAC | Operator concern, validated by the operator-cluster install checklist |

## Live response

When a CVE lands in a pinned package the scanner catches it on the next run. The
response procedure:

1. Open a fix PR with the upgraded `PackageVersion` in `Directory.Packages.props`
   (or `package.json` for the SPA).
2. Wait for the full CI matrix — both `solution-ci.yml` and `frontend-ci.yml` plus
   the security-scan workflow.
3. If the dep change is isolated (no API change), merge directly to `main`.
4. If the dep change brings a breaking API change, follow the same review path as
   any feature PR.
5. For CRITICAL severity with active exploitation: roll the fix without waiting for
   the full review; document the deviation in the PR body and the
   `docs/compliance/incident-log.md` (forthcoming).

Weekly schedule on the workflow means even when nothing changes, fresh CVEs surface
without manual intervention.

## Security-relevant runbooks

- `src/backend/Identity/RUNBOOK.md` — IdP + BFF auth (sections 1-8)
- `docs/architecture/durable-writes.md` — durability + status endpoint authorization
- `deploy/k8s/operators/README.md` — operator install + cluster prerequisites
- `docs/compliance/C5.md` (Cloud Computing Compliance Catalogue) — controls inventory
- `docs/compliance/dsgvo.md` — GDPR/BDSG controls mapped to enforcement points
