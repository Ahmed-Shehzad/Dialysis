import { Route } from "react-router-dom";
import { FhirAuthoringPage } from "@/pages/FhirAuthoringPage";
import { FhirExchangePage } from "@/pages/FhirExchangePage";
import { SubscriptionsPage } from "@/pages/SubscriptionsPage";
import type { ModuleManifest } from "@/shell/types";

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
