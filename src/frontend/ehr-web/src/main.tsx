import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { AppProviders } from "@/app/AppProviders";
import { AppRouter } from "@/routes/AppRouter";
import "@/styles/index.css";

// Enforce same-origin with the gateway. The BFF session cookie is scoped to the gateway
// origin (e.g. http://localhost:9090); if the SPA is opened on a different origin (e.g.
// the Vite dev server at http://localhost:5173 surfaced by the Aspire dashboard), every
// /identity/user call becomes a cross-origin request and the auth cookie is blocked by
// third-party-cookie restrictions → SPA loops on "Authenticating…". Fix: when
// VITE_API_BASE_URL is set and the current origin doesn't match, hard-redirect to the
// gateway origin before bootstrapping React. Preserves path + query so deep links work.
const enforceGatewayOrigin = (): boolean => {
  const apiBase = import.meta.env.VITE_API_BASE_URL;
  if (!apiBase) return false;
  let target: URL;
  try {
    target = new URL(apiBase);
  } catch {
    return false;
  }
  if (globalThis.location.origin === target.origin) return false;
  const next = new URL(globalThis.location.href);
  next.protocol = target.protocol;
  next.host = target.host;
  globalThis.location.replace(next.toString());
  return true;
};

if (!enforceGatewayOrigin()) {
  const root = document.getElementById("root");
  if (!root) throw new Error("Missing #root element");

  createRoot(root).render(
    <StrictMode>
      <AppProviders>
        <AppRouter />
      </AppProviders>
    </StrictMode>,
  );
}
