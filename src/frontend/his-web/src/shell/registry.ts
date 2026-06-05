import { hisModule } from "@/modules/his/manifest";
import type { ModuleManifest } from "./types";

/**
 * HIS is a single-context app, so the registry holds just its manifest — kept so `ModuleHeader`
 * and any registry-driven copy keep working unchanged. The router mounts HIS routes directly
 * (see `routes/AppRouter.tsx`); there is no cross-module switcher here.
 */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [hisModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
