import type { ReactNode } from "react";

/**
 * Identifier for a module's URL prefix and registry key. Backend modules each map to one
 * frontend module — adding a new one means adding a slug here and a folder under `src/modules/`.
 */
export type ModuleSlug = "his" | "ehr" | "pdms" | "smartconnect" | "hie" | "identity";

/**
 * A permission string scoped to a backend module's permission catalog
 * (e.g. `his.patient_access.view`). Acts as documentation today; once the BFF surfaces
 * permission claims to the SPA, `PermissionGate` will start enforcing it.
 */
export type ModulePermission = string;

export interface ModuleManifest {
  slug: ModuleSlug;
  /** Name shown to clinical / operations users in the module switcher. Workflow language, not jargon. */
  displayName: string;
  /** Short subtitle clarifying who the module is for ("Front desk", "Chairside", …). */
  tagline?: string;
  /** Permission required to surface this module to a user. Optional during Phase 0. */
  requires?: ModulePermission;
  /**
   * Whether the module is ready to appear in the registry-driven router and switcher.
   * Set `false` to ship dark (route stays unmounted, module hidden from navigation).
   */
  enabled: boolean;
  /**
   * Absolute path the module switcher should navigate to when the user picks this module.
   * Typically the module's "home" view — the receptionist's Today screen, the nurse's chart
   * list, the operator's feeds dashboard. Optional for modules that aren't navigable yet.
   */
  home?: string;
  /**
   * Returns the `<Route>` elements this module mounts under the authenticated shell.
   * Kept JSX-flavoured rather than `RouteObject[]` to match the existing declarative router.
   */
  renderRoutes(): ReactNode;
}
