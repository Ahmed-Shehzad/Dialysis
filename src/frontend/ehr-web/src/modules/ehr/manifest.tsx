import { Route } from "react-router";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const EhrChartPage = lazyPage(() => import("@/modules/ehr/chart/EhrChartPage"), "EhrChartPage");
const EhrWorkflowsPage = lazyPage(() => import("@/pages/EhrWorkflowsPage"), "EhrWorkflowsPage");
const PatientsPage = lazyPage(() => import("@/pages/PatientsPage"), "PatientsPage");
const BillingChargesPage = lazyPage(
  () => import("@/modules/ehr/admin/BillingChargesPage"),
  "BillingChargesPage",
);
const FeeSchedulePage = lazyPage(
  () => import("@/modules/ehr/admin/FeeSchedulePage"),
  "FeeSchedulePage",
);
const BillingWorklistPage = lazyPage(
  () => import("@/modules/ehr/admin/BillingWorklistPage"),
  "BillingWorklistPage",
);
const CareCoordinationWorklistPage = lazyPage(
  () => import("@/modules/ehr/admin/CareCoordinationWorklistPage"),
  "CareCoordinationWorklistPage",
);
const PopulationQualityPage = lazyPage(
  () => import("@/modules/ehr/admin/PopulationQualityPage"),
  "PopulationQualityPage",
);
const AppointmentRequestsWorklist = lazyPage(
  () => import("@/modules/ehr/admin/AppointmentRequestsWorklist"),
  "AppointmentRequestsWorklist",
);
const SafetySurveillancePage = lazyPage(
  () => import("@/modules/ehr/admin/SafetySurveillancePage"),
  "SafetySurveillancePage",
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
      <Route path="admin/billing/fee-schedule" element={<FeeSchedulePage />} />
      <Route path="admin/billing/worklist" element={<BillingWorklistPage />} />
      <Route path="care-coordination/worklist" element={<CareCoordinationWorklistPage />} />
      <Route path="population/quality" element={<PopulationQualityPage />} />
      <Route path="safety/surveillance" element={<SafetySurveillancePage />} />
      <Route path="appointment-requests" element={<AppointmentRequestsWorklist />} />
    </>
  ),
};
