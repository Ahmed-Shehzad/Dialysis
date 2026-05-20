import type { ModuleManifest } from "@/shell/types";

// Identity admin (users, roles, audit) is planned but not yet built. The manifest is
// registered so the module switcher and feature work can be developed against it; flip
// `enabled` to true and populate `renderRoutes()` when the first Identity admin page lands.
export const identityModule: ModuleManifest = {
  slug: "identity",
  displayName: "Admin",
  tagline: "Users · roles · audit",
  requires: "identity.users.manage",
  enabled: false,
  renderRoutes: () => null,
};
