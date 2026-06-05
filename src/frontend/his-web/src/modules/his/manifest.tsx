import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

// Each page is in its own chunk so the initial bundle stays lean — the Front Desk views
// only load when the user actually navigates into the HIS module.
const HisTodayPage = lazyPage(() => import("@/modules/his/today/HisTodayPage"), "HisTodayPage");
const HisWorkflowsPage = lazyPage(() => import("@/pages/HisWorkflowsPage"), "HisWorkflowsPage");
const BillingExportsPage = lazyPage(
  () => import("@/modules/his/admin/BillingExportsPage"),
  "BillingExportsPage",
);
const DeviceRegistryPage = lazyPage(
  () => import("@/modules/his/admin/DeviceRegistryPage"),
  "DeviceRegistryPage",
);

export const hisModule: ModuleManifest = {
  slug: "his",
  displayName: "Front Desk",
  tagline: "Patient access · scheduling · queue",
  description:
    "Reception's workspace — check patients in, register walk-ins, and watch the live queue: who's expected, who's waiting, and who's in treatment right now.",
  requires: "his.patient_access.view",
  enabled: true,
  home: "/his/today",
  renderRoutes: () => (
    <>
      <Route path="his/today" element={<HisTodayPage />} />
      <Route path="workflows/his" element={<HisWorkflowsPage />} />
      <Route path="admin/billing/exports" element={<BillingExportsPage />} />
      <Route path="admin/devices" element={<DeviceRegistryPage />} />
    </>
  ),
};
