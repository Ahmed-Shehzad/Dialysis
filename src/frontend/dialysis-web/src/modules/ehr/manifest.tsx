import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const EhrChartPage = lazyPage(() => import("@/modules/ehr/chart/EhrChartPage"), "EhrChartPage");
const EhrWorkflowsPage = lazyPage(() => import("@/pages/EhrWorkflowsPage"), "EhrWorkflowsPage");
const PatientsPage = lazyPage(() => import("@/pages/PatientsPage"), "PatientsPage");
const BillingChargesPage = lazyPage(
  () => import("@/modules/ehr/admin/BillingChargesPage"),
  "BillingChargesPage",
);

export const ehrModule: ModuleManifest = {
  slug: "ehr",
  displayName: "Chart",
  tagline: "Patient record · orders · notes",
  description:
    "The clinician's patient chart — search by name or MRN, review the longitudinal record, write a clinical note, or order labs without leaving the page.",
  requires: "ehr.patient_chart.view",
  enabled: true,
  home: "/patients",
  renderRoutes: () => (
    <>
      <Route path="patients" element={<PatientsPage />} />
      <Route path="patients/:patientId" element={<EhrChartPage />} />
      <Route path="workflows/ehr" element={<EhrWorkflowsPage />} />
      <Route path="admin/billing/dialysis-charges" element={<BillingChargesPage />} />
    </>
  ),
};
