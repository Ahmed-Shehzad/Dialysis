import type { ReactNode } from "react";
import { useAuth } from "@/features/auth/components/AuthProvider";
import type { ModulePermission } from "./types";

interface PermissionGateProps {
  /** Permission string from the owning module's catalog (e.g. `his.patient_access.view`). */
  permission?: ModulePermission;
  /** Rendered when the user is not allowed. Defaults to rendering nothing. */
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Wraps content that requires authentication (and optionally a specific permission).
 *
 * Today only the authenticated status is checked: the BFF does not yet expose per-module
 * permission claims to the SPA, so the `permission` prop currently serves as documentation
 * of intent at call sites. When `/identity/user` starts returning the user's permission set,
 * replace the inner check with a lookup against it — call sites will not need to change.
 */
export const PermissionGate = ({ permission, fallback = null, children }: PermissionGateProps) => {
  const { status } = useAuth();
  if (status !== "authenticated") {
    return <>{fallback}</>;
  }
  // Permission is accepted but not yet enforced; reference it so lint doesn't flag it as unused
  // and so call sites already pass the right scope when the BFF wires permissions through.
  void permission;
  return <>{children}</>;
};
