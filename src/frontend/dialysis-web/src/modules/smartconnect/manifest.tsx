import { Route } from "react-router-dom";
import { IntegrationsPage } from "@/pages/IntegrationsPage";
import type { ModuleManifest } from "@/shell/types";

export const smartConnectModule: ModuleManifest = {
  slug: "smartconnect",
  displayName: "Feeds",
  tagline: "HL7 v2 inbound · vendor adapters",
  requires: "smartconnect.feeds.view",
  enabled: true,
  renderRoutes: () => <Route path="integrations" element={<IntegrationsPage />} />,
};
