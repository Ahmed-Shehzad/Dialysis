import { pdmsModule } from "@/modules/pdms/manifest";
import type { ModuleManifest } from "./types";

/**
 * PDMS is a single-context app; the registry holds just its manifest (kept for `ModuleHeader`
 * copy). The router mounts PDMS routes directly — no cross-module switcher.
 */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [pdmsModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
