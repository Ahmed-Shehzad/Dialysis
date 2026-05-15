import { apiClient } from "@/lib/api/apiClient";

export type AuthenticatedUser = {
  username: string;
  email?: string;
  roles: string[];
  claims: Record<string, unknown>;
  // BFF returns the Keycloak access token from its saved-tokens cookie ticket so the SPA
  // can forward it as `Authorization: Bearer …` on gateway-routed API calls (HIS/PDMS/etc).
  accessToken?: string;
};

// 5s upper bound on the /identity/user probe. If the BFF or gateway hangs (mismatched ports,
// upstream cold-start, lost connection), the SPA falls back to the anonymous state and renders
// the login page instead of looping on "Authenticating…" forever.
const AUTH_PROBE_TIMEOUT_MS = 5000;

export const fetchCurrentUser = async (): Promise<AuthenticatedUser> => {
  const response = await apiClient.get<{
    name?: string;
    email?: string;
    roles?: string[];
    claims?: Record<string, unknown>;
    accessToken?: string;
  }>("/identity/user", { timeout: AUTH_PROBE_TIMEOUT_MS });
  const data = response.data ?? {};
  return {
    username: data.name ?? "unknown",
    email: data.email,
    roles: data.roles ?? [],
    claims: data.claims ?? {},
    accessToken: data.accessToken,
  };
};

const currentOrigin = (): string => globalThis.window?.location?.origin ?? "";

const buildReturnTarget = (returnPath: string): string => currentOrigin() + returnPath;

export const buildLoginUrl = (returnPath = "/"): string =>
  "/identity/login?returnUrl=" + encodeURIComponent(buildReturnTarget(returnPath));

export const buildLogoutUrl = (returnPath = "/"): string =>
  "/identity/logout?returnUrl=" + encodeURIComponent(buildReturnTarget(returnPath));
