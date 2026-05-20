import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const FhirAuthoringPage = lazyPage(() => import("@/pages/FhirAuthoringPage"), "FhirAuthoringPage");
const FhirExchangePage = lazyPage(() => import("@/pages/FhirExchangePage"), "FhirExchangePage");
const SubscriptionsPage = lazyPage(() => import("@/pages/SubscriptionsPage"), "SubscriptionsPage");

export const hieModule: ModuleManifest = {
  slug: "hie",
  displayName: "Exchange",
  tagline: "FHIR partners · consent · subscriptions",
  requires: "hie.outbound.view",
  enabled: true,
  home: "/fhir-exchange",
  renderRoutes: () => (
    <>
      <Route path="fhir-exchange" element={<FhirExchangePage />} />
      <Route path="fhir-authoring" element={<FhirAuthoringPage />} />
      <Route path="subscriptions" element={<SubscriptionsPage />} />
    </>
  ),
};
