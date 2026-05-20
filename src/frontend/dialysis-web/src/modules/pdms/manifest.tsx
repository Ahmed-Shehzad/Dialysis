import { Route } from "react-router-dom";
import { SessionLivePage } from "@/pages/SessionLivePage";
import { SessionsPage } from "@/pages/SessionsPage";
import type { ModuleManifest } from "@/shell/types";

export const pdmsModule: ModuleManifest = {
  slug: "pdms",
  displayName: "Chairside",
  tagline: "Live treatment · vitals · machine alarms",
  requires: "pdms.treatment_sessions.view",
  enabled: true,
  home: "/sessions",
  renderRoutes: () => (
    <>
      <Route path="sessions" element={<SessionsPage />} />
      <Route path="sessions/:sessionId" element={<SessionLivePage />} />
    </>
  ),
};
