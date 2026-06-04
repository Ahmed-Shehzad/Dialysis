import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const AdminHubPage = lazyPage(
  () => import("@/modules/identity/admin/AdminHubPage"),
  "AdminHubPage",
);

const IdentityAdminPage = lazyPage(
  () => import("@/modules/identity/admin/IdentityAdminPage"),
  "IdentityAdminPage",
);

const HipaaDashboardPage = lazyPage(
  () => import("@/modules/identity/hipaa/HipaaDashboardPage"),
  "HipaaDashboardPage",
);

const RopaPage = lazyPage(() => import("@/modules/identity/admin/RopaPage"), "RopaPage");
const ConsentsPage = lazyPage(
  () => import("@/modules/identity/admin/ConsentsPage"),
  "ConsentsPage",
);
const DataSubjectRightsPage = lazyPage(
  () => import("@/modules/identity/admin/DataSubjectRightsPage"),
  "DataSubjectRightsPage",
);
const DemoControlPanelPage = lazyPage(
  () => import("@/modules/demo/DemoControlPanelPage"),
  "DemoControlPanelPage",
);

// First Identity surface — a read-only "who am I?" page that shows the signed-in user's
// claims, roles, and access-token lifetime. Pure SPA work: the page reads from the
// existing AuthProvider; no new API endpoints. Provisioning / role management (already
// implemented on the Identity API but not gateway-routed) will land here as future
// slices wire those endpoints through the gateway.
export const identityModule: ModuleManifest = {
  slug: "identity",
  displayName: "Admin",
  tagline: "Identity · roles · audit · HIPAA · GDPR",
  description:
    "Platform administrator's console — inspect the signed-in user's identity claims and roles, review HIPAA safeguard status across every module, audit who's seen what, and run GDPR records-of-processing-activities + data-subject-rights workflows.",
  enabled: true,
  home: "/admin",
  renderRoutes: () => (
    <>
      <Route path="admin" element={<AdminHubPage />} />
      <Route path="admin/identity" element={<IdentityAdminPage />} />
      <Route path="admin/hipaa" element={<HipaaDashboardPage />} />
      <Route path="admin/data-protection/ropa" element={<RopaPage />} />
      <Route path="admin/data-protection/consents" element={<ConsentsPage />} />
      <Route path="admin/data-protection/data-subject-rights" element={<DataSubjectRightsPage />} />
      <Route path="demo" element={<DemoControlPanelPage />} />
    </>
  ),
};
