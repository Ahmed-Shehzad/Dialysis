import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const SessionLivePage = lazyPage(() => import("@/pages/SessionLivePage"), "SessionLivePage");
const SessionsPage = lazyPage(() => import("@/pages/SessionsPage"), "SessionsPage");
const ChairBoardPage = lazyPage(
  () => import("@/modules/pdms/chairs/ChairBoardPage"),
  "ChairBoardPage",
);
const InventoryPage = lazyPage(() => import("@/modules/pdms/admin/InventoryPage"), "InventoryPage");
const ReportingTemplatesPage = lazyPage(
  () => import("@/modules/pdms/admin/ReportingTemplatesPage"),
  "ReportingTemplatesPage",
);

export const pdmsModule: ModuleManifest = {
  slug: "pdms",
  displayName: "Chairside",
  tagline: "Live treatment · vitals · machine alarms",
  description:
    "The nurse's treatment console — live vitals from each dialysis machine, machine-alarm acknowledgement, and the schedule of who's on which chair today.",
  requires: "pdms.treatment_sessions.view",
  enabled: true,
  home: "/sessions",
  renderRoutes: () => (
    <>
      <Route path="sessions" element={<SessionsPage />} />
      <Route path="sessions/:sessionId" element={<SessionLivePage />} />
      <Route path="chairs" element={<ChairBoardPage />} />
      <Route path="admin/inventory" element={<InventoryPage />} />
      <Route path="admin/reporting/templates" element={<ReportingTemplatesPage />} />
    </>
  ),
};
