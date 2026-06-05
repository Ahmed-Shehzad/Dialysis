import { identityModule } from "@/modules/identity/manifest";
import type { ModuleManifest } from "./types";

/** Admin console is a single-context app; registry holds just the identity manifest (ModuleHeader copy). */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [identityModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
