import { Route } from "react-router-dom";
import { HisTodayPage } from "@/modules/his/today/HisTodayPage";
import { HisWorkflowsPage } from "@/pages/HisWorkflowsPage";
import type { ModuleManifest } from "@/shell/types";

export const hisModule: ModuleManifest = {
  slug: "his",
  displayName: "Front Desk",
  tagline: "Patient access · scheduling · queue",
  requires: "his.patient_access.view",
  enabled: true,
  home: "/his/today",
  renderRoutes: () => (
    <>
      <Route path="his/today" element={<HisTodayPage />} />
      <Route path="workflows/his" element={<HisWorkflowsPage />} />
    </>
  ),
};
