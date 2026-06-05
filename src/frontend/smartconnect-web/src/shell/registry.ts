import { smartConnectModule } from "@/modules/smartconnect/manifest";
import type { ModuleManifest } from "./types";

/** SmartConnect is a single-context app; registry holds just its manifest (for ModuleHeader copy). */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [smartConnectModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
