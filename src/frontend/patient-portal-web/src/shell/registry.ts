import { patientPortalModule } from "@/modules/patient-portal/manifest";
import type { ModuleManifest } from "./types";

/** Patient portal is a single-context app; registry holds just its manifest (ModuleHeader copy). */
export const MODULE_MANIFESTS: readonly ModuleManifest[] = [patientPortalModule];

export const enabledModules = (): readonly ModuleManifest[] =>
  MODULE_MANIFESTS.filter((m) => m.enabled);
