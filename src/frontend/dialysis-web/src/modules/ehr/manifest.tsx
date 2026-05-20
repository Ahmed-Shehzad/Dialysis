import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const EhrWorkflowsPage = lazyPage(() => import("@/pages/EhrWorkflowsPage"), "EhrWorkflowsPage");
const PatientChartPage = lazyPage(() => import("@/pages/PatientChartPage"), "PatientChartPage");
const PatientsPage = lazyPage(() => import("@/pages/PatientsPage"), "PatientsPage");

export const ehrModule: ModuleManifest = {
  slug: "ehr",
  displayName: "Chart",
  tagline: "Patient record · orders · notes",
  requires: "ehr.patient_chart.view",
  enabled: true,
  home: "/patients",
  renderRoutes: () => (
    <>
      <Route path="patients" element={<PatientsPage />} />
      <Route path="patients/:patientId" element={<PatientChartPage />} />
      <Route path="workflows/ehr" element={<EhrWorkflowsPage />} />
    </>
  ),
};
