# SmartConnect operator shell

TypeScript sources for the static operator UI served at `/smartconnect/index.html`.

## Layout

- `src/app.ts` — entry point. Mounts the auth bar, starts the hash router, dispatches the initial panel.
- `src/router.ts`, `src/auth.ts`, `src/api.ts`, `src/dom.ts` — small framework-free building blocks.
- `src/panels/*.ts` — one file per UI panel; each exports a `render(target, params)` function.
- `src/styles.css` — single stylesheet; imported from `app.ts` so esbuild bundles it.

## Build

The committed bundle under `../wwwroot/smartconnect/app.js` (+ `app.css`, `app.js.map`) is what the API host serves; CI does not run Node. Rebuild after touching anything under `src/`:

```
npm install      # first time only
npm run build    # writes ../wwwroot/smartconnect/app.{js,css}
```

During active development:

```
npm run watch    # rebuilds on save
npm run typecheck
```

`dotnet build` will also invoke `npm run build` automatically when `node_modules/` exists (see the `BuildOperatorShell` target in the API .csproj). On machines without Node the target is a no-op — the committed bundle is used as-is.

## What the shell talks to

All calls go through `api.ts`, which prepends the bearer token from `localStorage["smartconnect.token"]` when set. Endpoints used:

- Flows: `GET /api/v1/admin/flows`
- Messages: `GET /api/v1/admin/messages`, `POST .../{id}/reprocess`
- Attachments: `GET /api/v1/admin/messages/{id}/attachments`, `GET /attachments/{id}`, `DELETE /attachments/{id}`
- Alerts: `GET/POST /api/v1/admin/alert-rules`, `POST .../{id}/test`, `GET /alert-events`
- Code Template Libraries: `GET /api/v1/admin/code-template-libraries`
- Variable Maps: `GET/PUT/DELETE /api/v1/admin/config-map/{scope}/{key?}`
- Pruner: `GET /api/v1/admin/pruner/options`
- Health: `GET /health`

## Out of scope here

No virtual-DOM framework, no JS test runner, no internationalization. See the plan in the repo root for what stays REST-only.
