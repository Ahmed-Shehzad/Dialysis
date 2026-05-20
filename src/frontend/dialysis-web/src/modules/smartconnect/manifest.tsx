import { Route } from "react-router-dom";
import { lazyPage } from "@/shared/lazyPage";
import type { ModuleManifest } from "@/shell/types";

const IntegrationsPage = lazyPage(() => import("@/pages/IntegrationsPage"), "IntegrationsPage");

export const smartConnectModule: ModuleManifest = {
  slug: "smartconnect",
  displayName: "Feeds",
  tagline: "HL7 v2 inbound · vendor adapters",
  requires: "smartconnect.feeds.view",
  enabled: true,
  home: "/integrations",
  renderRoutes: () => <Route path="integrations" element={<IntegrationsPage />} />,
};
