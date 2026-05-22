import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const PatientPortalPage = lazyPage(
  () => import("@/modules/patient-portal/PatientPortalPage"),
  "PatientPortalPage",
);

// First patient-facing surface in the SPA. Reads from HIS's existing
// /api/v1.0/patient-access/.../portal-summary endpoint (gated by
// his.patientaccess.portal.read + a his_patient_id claim that matches the
// route id). Pure SPA work — no backend changes.
export const patientPortalModule: ModuleManifest = {
  slug: "patient-portal",
  displayName: "My portal",
  tagline: "Patient view — appointments, medications, admissions",
  enabled: true,
  home: "/portal",
  renderRoutes: () => <Route path="portal" element={<PatientPortalPage />} />,
};
