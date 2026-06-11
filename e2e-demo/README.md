# MVP demo — scenario film (live stack)

A single continuous Playwright walkthrough that enacts a real **patient-journey scenario** across the
**entire platform** — every SPA and every BFF — through the live Aspire Gateway, over real
DataSimulator-seeded data, and records one captioned video that doubles as an MVP demo. A two-row
caption narrates every beat on screen; the run also writes a narration timeline
(`narration.json`) that `narrate.mjs` can turn into a spoken-audio MP4 (see below).

This is the opposite of the per-app `src/frontend/<app>/e2e` suites: those mock every BFF call and
spin up a single Vite dev server in isolation. This project **mocks nothing** — it logs in through
the real Keycloak realm and exercises the real backend end-to-end.

The film is the screen companion to the written scenario in
[docs/business/mvp-demo-scenario.md](../docs/business/mvp-demo-scenario.md) — *"The Missed Session
That Almost Became a Hospitalization"* (patient **Marcus Bell**, 58, ESKD on in-center hemodialysis).

## What it covers

A branded title card, then **seven acts** with act-divider cards, each driving real screens:

1. **The Missed Session** — HIS today board, missed-appointment detection, EHR care-coordination worklist, AI risk
2. **Reaching Out** — patient portal outreach, make-up session approved
3. **Friday: He Comes Back Heavy** — EHR chart on arrival (K⁺ 6.6, +3.8 kg), PDMS treatment board, the chairside **live session**
4. **Intradialytic Hypotension** — the alarm fires (BP 78/44), on-call escalation policy + dispatch audit
5. **Documents, Signed & Exchanged** — HIE documents board, a branded AcroForm invoice filled and saved (`POST …/fill`), PAdES signature, the discharge letter
6. **The Network & The Claim** — HIE FHIR exchange (US Core), SmartConnect HL7 ORU inbound, HIS billing export (CPT → 837)
7. **What the Organization Sees** — chair utilization, HIPAA safeguards, GDPR data-subject rights + identity/RBAC

…and a closing "Hospitalization Avoided." outcome card.

## Prerequisites

1. **Bring the whole stack up** (single dev entrypoint) and wait until the Gateway answers at
   `http://localhost:9090` and Keycloak's realm is importable:

   ```bash
   dotnet run --project src/aspire/Dialysis.AppHost
   ```

   The DataSimulator auto-starts and seeds clinical/operational data through the BFFs — let it run a
   few minutes first so the list pages are populated.

2. **Install this project's Playwright** (the chromium browser is shared with the per-app suites, so
   no large re-download):

   ```bash
   cd e2e-demo
   npm install
   ```

## Run it

```bash
cd e2e-demo
npm run demo            # records the scenario film (~15 min at defaults; scales with DEMO_DWELL_MS)
npm run show            # opens the HTML report (video embedded)
node narrate.mjs        # optional, macOS: synthesize the narration audio and mux a narrated MP4
```

Useful overrides (env vars):

| Var             | Default                 | Purpose                                          |
| --------------- | ----------------------- | ------------------------------------------------ |
| `DEMO_BASE_URL` | `http://localhost:9090` | Gateway origin                                   |
| `DEMO_USER`     | `demo`                  | Keycloak username                                |
| `DEMO_PASS`     | `demo`                  | Keycloak password                                |
| `DEMO_DWELL_MS` | `17000`                 | Per-scene dwell — raise for a slower, longer film |
| `DEMO_CARD_MS`  | `8500`                  | Title / act-divider card dwell                   |
| `DEMO_SLOWMO`   | `110`                   | Per-action slow-motion (ms) for smooth UI motion |
| `DEMO_VOICE`    | `Samantha`              | macOS `say` voice used by `narrate.mjs`          |

## Where the artifacts land

Everything is written to the gitignored `e2e-artifacts/mvp-demo/` tree, overwritten each run:

- **Video** — `e2e-artifacts/mvp-demo/test-results/**/video.webm`
- **HTML report** — `e2e-artifacts/mvp-demo/report/index.html` (video embedded; per-scene console log)
- **Narration timeline** — `e2e-artifacts/mvp-demo/narration.json` (per-beat spoken text + video offsets)

For a **narrated MP4** (macOS only — needs `say` + `ffmpeg`/`ffprobe` on PATH), run
`node narrate.mjs` from `e2e-demo/` after `npm run demo`; it synthesizes each cue, places it at its
recorded offset, and muxes the audio onto the video →
`e2e-artifacts/mvp-demo/dialysis-mvp-demo.mp4`. For a plain (silent) MP4:

```bash
ffmpeg -i e2e-artifacts/mvp-demo/test-results/*/video.webm dialysis-mvp-demo.mp4
```

## How it stays robust

- **Cross-context navigation is a full-page hop** (`page.goto` to each `/{ctx}/…` URL) — required,
  since the SPAs are independent apps behind the Gateway. After the first login into a context the
  OIDC round-trip drops the deep link onto the SPA index, so the scene re-navigates to its target.
- **Real OIDC, handled by URL state** — the SPA login page (`/{ctx}/login`) launches the challenge;
  the BFF's in-flight `/{ctx}/identity/*` redirect is treated as "wait, don't click"; the Keycloak
  form (`/auth/realms/…`, first context only — SSO thereafter) takes `demo`/`demo`.
- **Defensive interactions** — list→detail drill-ins and drawer/tab opens are best-effort; a missing
  row or button is logged and skipped, never aborting the recording.
- **Captions survive navigation** — the two-row caption overlay is re-injected after every scene.
- The run **verifies** each scene rendered real content and prints a scenes / interaction-checks
  summary; it only fails when fewer than half the scenes render (or under 60% of the interaction
  checks pass) — i.e. when the platform was broadly unreachable, not on a single flaky screen.
