import type { ReactNode } from "react";
import { useAuth } from "@/features/auth/components/AuthProvider";
import type { ModulePermission } from "./types";

interface PermissionGateProps {
  /**
   * Permission string from the owning module's catalog (e.g. `his.patientflow.queue.read`).
   * When omitted, the gate only requires the user be authenticated.
   */
  permission?: ModulePermission;
  /** Rendered when the user is not allowed. Defaults to rendering nothing. */
  fallback?: ReactNode;
  children: ReactNode;
}

/**
 * Gates content on authentication + (optionally) a specific permission. Permissions are sourced
 * from the BFF's `/identity/user` response, which projects the Keycloak `dialysis_permission`
 * claim. If the BFF hasn't yet observed a permission (anonymous user, IdP without a permission
 * mapper) the gate stays closed and renders `fallback`.
 */
export const PermissionGate = ({ permission, fallback = null, children }: PermissionGateProps) => {
  const { status, user } = useAuth();
  if (status !== "authenticated" || user === null) {
    return <>{fallback}</>;
  }
  if (permission && !user.permissions.includes(permission)) {
    return <>{fallback}</>;
  }
  return <>{children}</>;
};
