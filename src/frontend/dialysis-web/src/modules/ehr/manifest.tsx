import { Route } from "react-router-dom";
import { EhrWorkflowsPage } from "@/pages/EhrWorkflowsPage";
import { PatientChartPage } from "@/pages/PatientChartPage";
import { PatientsPage } from "@/pages/PatientsPage";
import type { ModuleManifest } from "@/shell/types";

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
