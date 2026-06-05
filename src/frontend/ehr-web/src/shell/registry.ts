import { ehrModule } from "@/modules/ehr/manifest";
import type { ModuleManifest } from "./types";

/**
 * EHR is a single-context app, so the registry holds just its manifest — kept so `ModuleHeader`
 * and any registry-driven copy keep working unchanged. The router mounts EHR routes directly
 * (see `routes/AppRouter.tsx`); there is no cross-module switcher here.
 */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [ehrModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
