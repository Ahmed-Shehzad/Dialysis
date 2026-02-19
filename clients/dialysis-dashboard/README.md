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

## Endpoints Used

- `GET /api/reports/sessions-summary`
- `GET /api/reports/alarms-by-severity`
- `GET /api/reports/prescription-compliance`

See [docs/UI-INTEGRATION-GUIDE.md](../../docs/UI-INTEGRATION-GUIDE.md).
