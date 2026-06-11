# smartconnect-web — Integration Operator Shell

The SmartConnect browser app: a **Mirth-Connect-style operator console** for HL7 v2 inbound feeds, channels, and vendor adapters. Backed by the **`Dialysis.SmartConnect.Bff`** per-context BFF.

|                                |                                                               |
| ------------------------------ | ------------------------------------------------------------- |
| Context base / router basename | `/smartconnect`                                               |
| Standalone dev port (Vite)     | `5334`                                                        |
| Backing BFF                    | `Dialysis.SmartConnect.Bff` (`smartconnect-bff`, port `5304`) |
| BFF aggregations               | none                                                          |
| Real-time push                 | No (`AddModuleBff` only)                                      |

## What it does

- **Integrations console** (`/smartconnect/integrations`) — a tabbed operator shell: **Flows** (channels, lifecycle, statistics), **Dependency Graph** (`@xyflow/react`), **HL7 Workbench** (paste / parse / validate / dispatch HL7 v2), **Messages** (browse + reprocess-from-ledger), **Configuration Map**, **Code Templates**, **Alerts**, **Audit Events**, **Retention** (pruner).
- **Channel editor** (`/smartconnect/integrations/editor/:flowId`) — multi-step channel build (`NewChannelDialog`, `AdapterParametersForm`, pipeline node drawer).

The operator API it drives is `/smartconnect/api/v1/admin/*` (see the module's operator shell). The module also serves a vanilla-TS operator shell at its own `/`, but this React app is the primary console.

## Stack & scripts

React 18 + Vite 8 + TypeScript 6 + TanStack Query 5, **npm** (Node ≥ 20.19); `react-router` v7 with `BrowserRouter basename="/smartconnect"`; Tailwind CSS 4 (CSS-first, no `tailwind.config.js`). Pipeline graphs via `@xyflow/react`.

```bash
npm run dev        # Vite :5334
npm run build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/smartconnect/`; the SmartConnect BFF handles auth and proxies `/smartconnect/api` + `/smartconnect/hubs`. `enforceGatewayOrigin()` keeps the cookie intact.

> Cross-context navigation must be a full-page hop.

See [src/backend/SmartConnect/ARCHITECTURE.md](../../backend/SmartConnect/ARCHITECTURE.md) for the flow engine and operator API, and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) for auth. Shared conventions: [root README](../../../README.md#frontend).
