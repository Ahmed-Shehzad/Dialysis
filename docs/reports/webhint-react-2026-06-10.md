# webhint analysis — React applications

**Date:** 2026-06-10
**Engine:** `hint` v7.1.13, `web-recommended` configuration (31 hints: compat-api, axe accessibility, content-type, http-cache, sri, x-content-type-options, …), puppeteer connector on Chromium 148
**Scope:** production builds (`vite build`) of the per-module SPAs served locally via `vite preview` and scanned over HTTP. Scanned in full: `his-web` and `patient-portal-web`. Because the flagged surfaces are byte-identical duplicated files (`index.html`, `src/styles/index.css`, `postcss.config.js`, `nginx.conf`, Tailwind config), the findings — and the fixes — apply uniformly to all seven apps.

**Limitations:** the Aspire Gateway/BFF stack isn't running in this environment, so only the unauthenticated shell (redirect → `/login`) renders for DOM/axe checks; authenticated routes weren't exercised. Header-related hints reflect the local `vite preview` server, not the production nginx/Gateway chain — production-header fixes were made in `nginx.conf` but can only be verified on a deployed stack.

## Result: 1 error + 5 warnings → 0 errors per app

### Fixed

| Finding | Hint | Severity | Fix |
|---|---|---|---|
| `-webkit-text-size-adjust` shipped without the standard `text-size-adjust` in the same rule (Tailwind preflight) | `compat-api/css` | **error** | Added a 10-line PostCSS plugin (`text-size-adjust-standard` in `postcss.config.js`, ×7 apps) that clones the standard property next to the prefixed one inside the same rule — fixes it at the source for every rule that ever emits the prefix. Verified in the built CSS: `-webkit-…;-moz-…;text-size-adjust:100%` now sit together. |
| `Content-Type` served without `charset=utf-8` for css/js/json/svg | `content-type` | warning ×4 | Production fix in `nginx.conf` (×7): `charset utf-8;` + `charset_types` covering css/js/json/svg/xml/plain (nginx's default list omits them). The warning still shows on local `vite preview` (dev-only server); production serving is nginx. |

### Reviewed, intentionally not changed

- **`axe/color` — enhanced contrast (WCAG AAA, 7:1)** on the login button: white on `clinic-600` `#00855f` = 4.64:1. This **passes WCAG AA** (4.5:1 for normal text); the flag is the *enhanced* AAA rule. `clinic-600` is the brand action color used for primary buttons across all seven apps — reaching 7:1 would need ≈`#005c41`, a visibly darker re-brand of every primary action. That is a design decision, not a lint fix. If AAA conformance becomes a requirement (clinical-display contexts may warrant it), change `clinic-600` once in the shared `tailwind.config.js` ramp.
- **`compat-api/css` — "`text-size-adjust` is not supported by Firefox, Safari"** (warning): circular by construction — the same hint demands the standard property be added, then notes the property's incomplete support. With the `-webkit-`/`-moz-` fallbacks in the same rule this is informational; unsupporting browsers simply ignore it.
- **`detect-css-reflows/composite`** (hint-level): animating `opacity` in `@keyframes` "triggers Composite" — composite-only animation is the *recommended* cheap path; no action.

### Hints that passed cleanly

`button-type` (all buttons carry explicit `type`), `meta-charset-utf-8`, `meta-viewport`, `html` lang attribute, `disown-opener`, `no-inline-styles`, `sri`, `x-content-type-options`/security headers (already set in `nginx.conf`: `nosniff`, `X-Frame-Options DENY`, `Referrer-Policy no-referrer`), `no-vulnerable-javascript-libraries`, `stylesheet-limits`, `no-protocol-relative-urls`, `no-bom`.

## Reproducing the scan

```bash
# one-time: a Chromium for the puppeteer connector
npx playwright install chromium

# serve a production build
cd src/frontend/his-web && npm run build && npx vite preview --port 4173

# scan (hintrc: extends web-recommended + puppeteer connector with the
# playwright chromium's executablePath)
npx hint -c hintrc.json http://localhost:4173/his/
```

For the full production picture (real nginx + Gateway headers, authenticated routes), run the same scan against a deployed compose stack (`deploy/compose/dev`) instead of `vite preview`.
