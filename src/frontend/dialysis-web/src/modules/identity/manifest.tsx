import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const IdentityAdminPage = lazyPage(
  () => import("@/modules/identity/admin/IdentityAdminPage"),
  "IdentityAdminPage",
);

const HipaaDashboardPage = lazyPage(
  () => import("@/modules/identity/hipaa/HipaaDashboardPage"),
  "HipaaDashboardPage",
);

// First Identity surface — a read-only "who am I?" page that shows the signed-in user's
// claims, roles, and access-token lifetime. Pure SPA work: the page reads from the
// existing AuthProvider; no new API endpoints. Provisioning / role management (already
// implemented on the Identity API but not gateway-routed) will land here as future
// slices wire those endpoints through the gateway.
export const identityModule: ModuleManifest = {
  slug: "identity",
  displayName: "Admin",
  tagline: "Identity · roles · audit · HIPAA",
  description:
    "Platform administrator's console — inspect the signed-in user's identity claims and roles, review HIPAA safeguard status across every module, and audit who's seen what.",
  enabled: true,
  home: "/admin/identity",
  renderRoutes: () => (
    <>
      <Route path="admin/identity" element={<IdentityAdminPage />} />
      <Route path="admin/hipaa" element={<HipaaDashboardPage />} />
    </>
  ),
};
