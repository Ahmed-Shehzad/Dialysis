import { ehrModule } from "@/modules/ehr/manifest";
import { hieModule } from "@/modules/hie/manifest";
import { hisModule } from "@/modules/his/manifest";
import { identityModule } from "@/modules/identity/manifest";
import { pdmsModule } from "@/modules/pdms/manifest";
import { smartConnectModule } from "@/modules/smartconnect/manifest";
import type { ModuleManifest } from "./types";

/**
 * Single source of truth for module composition. The router, the (future) module switcher,
 * and the role-aware home page all read from this list. Adding a module = drop a folder under
 * `src/modules/` exporting a `ModuleManifest` and append it here.
 */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [
  hisModule,
  ehrModule,
  pdmsModule,
  smartConnectModule,
  hieModule,
  identityModule,
];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
