# Dialysis Web

React + TypeScript SPA for the Dialysis modular monolith. Talks to the per-module APIs
through the YARP gateway at `http://localhost:5000` (configurable via `VITE_GATEWAY_URL`).

## Structure (Bulletproof React)

```
src/
  app/            # provider composition (QueryClient, BrowserRouter, AuthProvider)
  components/     # shared UI (layout, atoms)
  features/
    auth/         # session + identity (AuthProvider, useAuth, /identity/* calls)
    sessions/     # dialysis session list + reading history
    vitals/       # SignalR stream hook, latest-vitals panel, D3 chart
  lib/
    api/          # axios instance + interceptors
    auth/         # JWT helpers
    realtime/     # SignalR connection builder
  pages/          # route targets (Login, Dashboard, SessionLive)
  routes/         # router + ProtectedRoute
  styles/         # tailwind entry
```

Design rules:

- **SRP** — each feature folder owns its API, hooks, and components; no cross-feature imports
  except through stable contracts under `lib/`.
- **OCP** — the D3 chart's `SERIES` table lets you add a trace without touching render logic.
- **DIP** — `useVitalsStream` depends on `useAuth`'s `getAccessToken`, not on a concrete token store.
- **ISP** — `AuthProvider` exposes only `{ user, status, signIn, signOut, getAccessToken }`.
- **LSP** — `VitalsLatestPanel` accepts any object satisfying the `VitalsReading` contract.

## Dev

```bash
npm install
npm run dev     # http://localhost:5173 — proxies /api,/fhir,/hubs,/identity,/auth → gateway
npm run build
npm run typecheck
```

The gateway must be running for auth + APIs:

```bash
dotnet run --project src/backend/Shared/Dialysis.Module.Gateway
```
