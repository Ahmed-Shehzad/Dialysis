import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const FhirAuthoringPage = lazyPage(() => import("@/pages/FhirAuthoringPage"), "FhirAuthoringPage");
const FhirExchangePage = lazyPage(() => import("@/pages/FhirExchangePage"), "FhirExchangePage");
const SubscriptionsPage = lazyPage(() => import("@/pages/SubscriptionsPage"), "SubscriptionsPage");
const DocumentsPage = lazyPage(() => import("@/modules/hie/admin/DocumentsPage"), "DocumentsPage");
const TefcaPartnersPage = lazyPage(
  () => import("@/modules/hie/admin/TefcaPartnersPage"),
  "TefcaPartnersPage",
);
const DocumentRetentionPage = lazyPage(
  () => import("@/modules/hie/admin/DocumentRetentionPage"),
  "DocumentRetentionPage",
);

export const hieModule: ModuleManifest = {
  slug: "hie",
  displayName: "Exchange",
  tagline: "FHIR partners · consent · subscriptions",
  description:
    "Health Information Exchange — send a FHIR Bundle to a partner organisation, look up patients by demographics, and manage the consent policies that gate what's shared.",
  requires: "hie.outbound.view",
  enabled: true,
  home: "/fhir-exchange",
  renderRoutes: () => (
    <>
      <Route path="fhir-exchange" element={<FhirExchangePage />} />
      <Route path="fhir-authoring" element={<FhirAuthoringPage />} />
      <Route path="subscriptions" element={<SubscriptionsPage />} />
      <Route path="admin/documents" element={<DocumentsPage />} />
      <Route path="admin/documents/retention" element={<DocumentRetentionPage />} />
      <Route path="admin/tefca/partners" element={<TefcaPartnersPage />} />
    </>
  ),
};
