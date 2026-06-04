# End-to-end tests

Playwright specs driving the SPA + every backend module through the YARP gateway against the
**live Aspire stack**. Every spec signs in via the same Keycloak-OIDC code flow a real
operator goes through, so an `e2e` pass exercises the full chain — SPA → gateway → BFF →
Keycloak → module API → Postgres + Valkey + RabbitMQ + Transponder outbox.

## Why no `webServer`?

The Aspire app-host pulls up **five module APIs, five module databases, the BFF, Keycloak,
Valkey, RabbitMQ, and the gateway** plus the Vite SPA. That's too heavy to start from a
`webServer` block in `playwright.config.ts`. Instead the config skips the `webServer`,
points at `E2E_BASE_URL` (default `http://localhost:9090`, the gateway), and individual
specs self-skip if the gateway isn't reachable.

## Prerequisites

```bash
# 1. Bring up the full stack — five APIs, five DBs, BFF, Keycloak, gateway.
dotnet run --project src/aspire/Dialysis.AppHost

# 2. Wait until the Aspire dashboard reports every resource as "Running" (~30 s on a warm
#    cache, ~3 min on a cold one — Keycloak realm import is the long pole).

# 3. Confirm the gateway is up.
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:9090/_gateway   # → 200
```

If you only need the identity smoke (no module APIs), `src/backend/Identity/docker-compose.yml`

- the BFF host is enough — see `src/backend/Identity/RUNBOOK.md`.

## Running the tests

```bash
cd src/frontend/dialysis-web
npm install                            # one-time
npm run test:e2e                       # headless, default Chromium
npm run test:e2e -- --reporter=line    # one line per spec, useful in CI logs
```

### Headed / "demo" mode

Watch the test execute in a real browser — great for showing a flow to a stakeholder, or
debugging a flaky assertion.

```bash
E2E_HEADED=1 npm run test:e2e
```

`slowMo: 400` is wired into the headed launch so the click → redirect → login → dashboard
sequence is human-followable. Override the browser binary with `E2E_BROWSER_PATH=…` if you
want Brave / Edge / a Chromium dev build instead of bundled Chromium.

### Filtering

```bash
# One spec.
npm run test:e2e -- e2e/admin/documents.spec.ts

# One test inside a spec.
npm run test:e2e -- -g "round-trips an upload"

# A whole subtree.
npm run test:e2e -- e2e/admin
```

### Interactive runner

```bash
npm run test:e2e:ui
```

Opens Playwright's UI mode: per-step time-travel, locator picker, console log per assertion.
Best tool for authoring new specs.

## Environment variables

| Variable                        | Default                 | Purpose                                                                                           |
| ------------------------------- | ----------------------- | ------------------------------------------------------------------------------------------------- |
| `E2E_BASE_URL`                  | `http://localhost:9090` | Gateway origin. Override for a deployment-compose target.                                         |
| `E2E_KC_USERNAME`               | `demo`                  | Keycloak user for `signIn()`. Realm seed is `demo / demo`.                                        |
| `E2E_KC_PASSWORD`               | `demo`                  | Keycloak password matching above.                                                                 |
| `E2E_HEADED`                    | `0`                     | `1` → headed launch with `slowMo`.                                                                |
| `E2E_BROWSER_PATH`              | bundled Chromium        | Override the browser binary (Brave, Edge, …).                                                     |
| `E2E_DOCUMENTS_SIGNING_ENABLED` | `0`                     | `1` → run `documents-signing.spec.ts`. Requires a real platform-cert PFX on the host (see below). |

## Spec catalog

| Path                                         | Coverage                                                                                                                                                         |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `auth.spec.ts`                               | Full SPA → BFF → Keycloak → module-API auth chain; guards against the apiClient regression where decodeJwt silently failed and Bearer headers stopped attaching. |
| `decode-jwt.spec.ts`                         | Targeted unit-style regression check for the JWT decoder.                                                                                                        |
| `diagnose-token.spec.ts` / `probe-*.spec.ts` | Diagnostic probes — kept around because they print useful state on a broken stack. Not strictly required for a passing run.                                      |
| `visual-demo.spec.ts`                        | Captures `__shots__/` screenshots for the demo deck.                                                                                                             |
| `admin/admin-pages.spec.ts`                  | Broad smoke net: every operator admin page loads its heading after sign-in. Adding a new admin page means adding one row here.                                   |
| `admin/fee-schedule.spec.ts`                 | CPT fee schedule — full CRUD (PR #122).                                                                                                                          |
| `admin/inventory.spec.ts`                    | Medication inventory — loads + branches into the action drawer when stock exists.                                                                                |
| `admin/reporting-templates.spec.ts`          | Multi-language ePA templates — new template → save draft → publish (PR #121).                                                                                    |
| `admin/documents.spec.ts`                    | Documents board — upload + soft-delete (PR #128). Self-skips upload if no patient exists.                                                                        |
| `admin/documents-signing.spec.ts`            | PAdES-B platform-cert sign (PR #129). Opt-in via `E2E_DOCUMENTS_SIGNING_ENABLED=1` + host-side cert.                                                             |
| `medications/record-administration.spec.ts`  | Chairside MAR — record an administration through the live-session dialog (PR #112).                                                                              |
| `smartconnect/*.spec.ts`                     | SmartConnect channel + flow editors.                                                                                                                             |

## Helpers

| Path                       | Purpose                                                                                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `helpers/signIn.ts`        | Shared Keycloak-OIDC code-flow login. Every patient-or-admin spec calls this first.                                                                                  |
| `helpers/selectPatient.ts` | Opens the patient-context picker, picks the first result, and returns its display name — or returns `null` so the spec can branch onto the "no patient seeded" path. |
| `helpers/minimalPdf.ts`    | A 400-byte hand-rolled valid PDF for upload fixtures; deterministic and macro-free so it tests the upload + index flag-detector correctly.                           |

## Tips for writing new specs

1. **Always call `signIn()` first.** It's idempotent within one test.
2. **Wait on responses, not animations.** `Promise.all([page.waitForResponse(...), button.click()])`
   is the canonical pattern in this repo. Animation timeouts cause flakes; HTTP completion is deterministic.
3. **Self-skip when the production-clean stack can't satisfy a precondition.** Use
   `test.skip(true, "explanation")` rather than failing — the stack ships clean by design.
   See `documents.spec.ts` for the canonical branch on `selectAnyPatient()` returning `null`.
4. **Pick selectors by accessibility role.** `getByRole("button", { name: /…/i })` survives
   className refactors; `data-testid` is a fallback for non-semantic elements.
5. **Run with `--headed` while authoring**, then switch to headless for the final commit.

## Troubleshooting

| Symptom                                                         | Likely cause                                                                                                                                                      |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Gateway /_gateway returned 404 — is Aspire up?` (spec skipped) | Stack isn't running. `dotnet run --project src/aspire/Dialysis.AppHost`.                                                                                          |
| OIDC redirect loops or `redirect_uri mismatch`                  | Gateway port drifted from `:9090` or BFF from `:5275`. Both are pinned in `dialysis-realm.json`.                                                                  |
| `decodeJwt` returned `null`                                     | Keycloak realm wasn't imported (`--import-realm` only runs when realm absent). Re-running the AppHost re-imports.                                                 |
| Test cold-starts time out at 60 s                               | First Vite transform on a freshly-built stack — increase per-spec timeout (most specs already set 180 s) or warm the SPA with `curl http://localhost:9090/` once. |
| Sign-document spec returns 500                                  | Platform cert isn't configured. Set `Documents:Signing:PlatformCertificate:PfxPath` + `PfxPassword`, then re-run with `E2E_DOCUMENTS_SIGNING_ENABLED=1`.          |
| Playwright says "another instance is running"                   | An earlier headed run didn't tear down. `pkill -f playwright` and re-run.                                                                                         |

## CI

`frontend-ci.yml` runs `npm run lint`, `npm run typecheck`, and `npm run build` on every PR.
Playwright specs are **not** in CI today — they need the live Aspire stack, which is heavy.
Run them locally as part of pre-merge verification for any UI-touching PR.
