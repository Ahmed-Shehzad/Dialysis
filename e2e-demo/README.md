# Full-system MVP demo (live stack, ≥ 20-minute video)

A single continuous Playwright walkthrough that drives the **entire platform** — every SPA and every
BFF — through the live Aspire Gateway, over real DataSimulator-seeded data, and records one
**≥ 20-minute video** that doubles as an MVP demo. The video has on-screen captions (no audio
needed).

This is the opposite of the per-app `src/frontend/<app>/e2e` suites: those mock every BFF call and
spin up a single Vite dev server in isolation. This project **mocks nothing** — it logs in through
the real Keycloak realm and exercises the real backend end-to-end.

## What it covers

One login (real OIDC) then a paced tour of all seven contexts:

- **HIS** — operations dashboard, workflows, billing-export queue (Execute), device registry
- **EHR** — patient index, longitudinal chart drill-in, charges/claims, fee schedule, care
  coordination, appointment requests, population quality, safety surveillance
- **PDMS** — sessions, the chairside **live session** (intradialytic vitals, MAR, documents tab),
  chair board, inventory, reporting templates, on-call rotation/policies/audit
- **SmartConnect** — integration flows + the channel editor
- **HIE** — FHIR exchange/authoring/subscriptions, the **branded PDF document viewer** (AcroForm +
  PAdES signing), retention, TEFCA partners, MPI steward, terminology
- **Admin / Identity** — hub, identity, HIPAA safeguards, RoPA, consents, data-subject rights
- **Patient portal** — aggregated patient self-service

## Prerequisites

1. **Bring the whole stack up** (single dev entrypoint) and wait until every resource is healthy in
   the Aspire dashboard:

   ```bash
   dotnet run --project src/aspire/Dialysis.AppHost
   ```

   The Gateway must answer at `http://localhost:9090`. The DataSimulator auto-starts and seeds
   clinical/operational data through the BFFs every ~30 s — let it run a few minutes first so the
   list pages are populated.

2. **Install this project's Playwright** (the chromium browser is shared with the per-app suites, so
   no large re-download):

   ```bash
   cd e2e-demo
   npm install
   ```

## Run it

```bash
cd e2e-demo
npm run demo            # records the full walkthrough
npm run show            # opens the HTML report (video embedded)
```

Useful overrides (env vars):

| Var              | Default                 | Purpose                                              |
| ---------------- | ----------------------- | ---------------------------------------------------- |
| `DEMO_BASE_URL`  | `http://localhost:9090` | Gateway origin                                       |
| `DEMO_USER`      | `demo`                  | Keycloak username                                    |
| `DEMO_PASS`      | `demo`                  | Keycloak password                                    |
| `DEMO_DWELL_MS`  | `24000`                 | Per-stop dwell — raise for a longer video            |
| `DEMO_MIN_MS`    | `1230000` (20.5 min)    | Tail guard keeps recording until this is cleared     |
| `DEMO_SLOWMO`    | `110`                   | Per-action slow-motion (ms) for smooth, watchable UI |

## Where the artifacts land

Everything is written to the gitignored `e2e-artifacts/mvp-demo/` tree (overwritten each run):

- **Video** — `e2e-artifacts/mvp-demo/test-results/**/video.webm` (the ≥ 20-minute demo)
- **HTML report** — `e2e-artifacts/mvp-demo/report/index.html` (video embedded; per-step console log)

Convert to MP4 for sharing if desired:

```bash
ffmpeg -i e2e-artifacts/mvp-demo/test-results/*/video.webm dialysis-mvp-demo.mp4
```

## How it stays robust

- **Cross-context navigation is a full-page hop** (`page.goto` to each `/{ctx}/…` URL) — required,
  since the SPAs are independent apps behind the Gateway.
- **One login, SSO thereafter** — the first protected route triggers the Keycloak form; subsequent
  contexts authenticate silently via the Keycloak SSO session.
- **Defensive interactions** — list→detail drill-ins and drawer/tab opens are best-effort; a missing
  row or button is logged and skipped, never aborting the recording.
- **Tail guard** — if the planned tour finishes under 20 minutes it revisits "hero" pages until the
  recording clears the threshold, so the video is always ≥ 20 minutes.
- The run **verifies** each stop rendered real content and prints a `stops rendered / total` summary;
  it only fails if the platform was broadly unreachable.
