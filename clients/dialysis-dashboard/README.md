# Dialysis PDMS Dashboard

React + TypeScript SPA that consumes PDMS Reports APIs.

## Prerequisites

- Node.js 18+
- PDMS running (e.g. `docker compose up -d` with Gateway on port 5001)

## Run

```bash
npm install
npm run dev
```

Open http://localhost:5173. The dev server proxies `/api` to the Gateway (localhost:5001). If your Gateway runs on a different port, update `vite.config.ts` proxy target.

## Build

```bash
npm run build
npm run preview  # preview production build
```

## Features

- **Reports cards**: Sessions summary, alarms by severity, prescription compliance (date range)
- **Real-time monitoring**: SignalR connection to receive live observations and alarms. Line, area (mountain), and bar charts update as data streams in.

## Endpoints Used

- `GET /api/reports/sessions-summary`
- `GET /api/reports/alarms-by-severity`
- `GET /api/reports/prescription-compliance`
- `GET /api/treatment-sessions/fhir` (session list for real-time subscribe)
- SignalR: `/transponder/transport` (real-time observations and alarms)

## Real-Time Data

1. Run `./run-simulator.sh` (or `dotnet run --project DataProducerSimulator`) to produce continuous HL7 data.
2. In the dashboard, scroll to "Real-Time Monitoring", select or enter a session ID, click Subscribe.
3. Charts update automatically as observations arrive.

### Live charts not showing?

| Check | What to do |
|-------|------------|
| **No sessions in dropdown** | Run `./run-simulator.sh` first. Wait ~30s, then refresh the dashboard. Sessions are created when the simulator posts ORU^R01. |
| **"○ Connecting…" never becomes "● Connected"** | Ensure PDMS is running (`docker compose up -d`). SignalR is at `/transponder/transport`; the Vite proxy forwards it to the Gateway. Check browser DevTools → Network for WebSocket errors or 401. |
| **"● Connected" but no charts** | The simulator picks sessions from the same API. Select a session from the dropdown (don’t type a random ID). Simulator sends observations every 5s; wait a few seconds. |
| **Gateway/APIs** | Dashboard proxies to `localhost:5001`. Ensure Gateway and Treatment API are up. |

See [docs/UI-INTEGRATION-GUIDE.md](../../docs/UI-INTEGRATION-GUIDE.md).
