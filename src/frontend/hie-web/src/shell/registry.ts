import { hieModule } from "@/modules/hie/manifest";
import type { ModuleManifest } from "./types";

/** HIE is a single-context app; registry holds just its manifest (for ModuleHeader copy). */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [hieModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
