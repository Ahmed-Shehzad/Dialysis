import { apiClient } from "@/lib/api/apiClient";

// Context prefix (e.g. "/his") derived from the Vite `base` (`/{ctx}/`, set per app in
// vite.config.ts). It is the same in dev and prod because each SPA is served under its
// context prefix via the Gateway — deriving it here keeps this file byte-identical
// across all seven apps.
export const CONTEXT_PREFIX = import.meta.env.BASE_URL.replace(/\/+$/, "");

export type AuthenticatedUser = {
  username: string;
  email?: string;
  roles: string[];
  // Permission strings the BFF derived from the Keycloak `dialysis_permission` claim. Empty
  // array when the upstream IdP has no permission mapper configured; PermissionGate then
  // hides any gated UI.
  permissions: string[];
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
    permissions?: string[];
    claims?: Record<string, unknown>;
    accessToken?: string;
  }>(`${CONTEXT_PREFIX}/identity/user`, { timeout: AUTH_PROBE_TIMEOUT_MS });
  const data = response.data ?? {};
  return {
    username: data.name ?? "unknown",
    email: data.email,
    roles: data.roles ?? [],
    permissions: data.permissions ?? [],
    claims: data.claims ?? {},
    accessToken: data.accessToken,
  };
};

const currentOrigin = (): string => globalThis.window?.location?.origin ?? "";

const buildReturnTarget = (returnPath: string): string => currentOrigin() + returnPath;

export const buildLoginUrl = (returnPath = "/", provider?: string): string => {
  const url =
    CONTEXT_PREFIX +
    "/identity/login?returnUrl=" +
    encodeURIComponent(buildReturnTarget(returnPath));
  return provider ? url + "&provider=" + encodeURIComponent(provider) : url;
};

export const buildLogoutUrl = (returnPath = "/"): string =>
  CONTEXT_PREFIX +
  "/identity/logout?returnUrl=" +
  encodeURIComponent(buildReturnTarget(returnPath));

export type IdentityProvider = {
  alias: string;
  displayName: string;
  iconUri?: string | null;
};

// Empty array when federation is not configured — the login page then renders only the local
// Keycloak "Sign in" button (the BFF /identity/login with no ?provider= behaves identically to
// the pre-federation flow).
export const fetchIdentityProviders = async (): Promise<IdentityProvider[]> => {
  try {
    const response = await apiClient.get<{ providers?: IdentityProvider[] }>(
      `${CONTEXT_PREFIX}/identity/providers`,
      {
        timeout: AUTH_PROBE_TIMEOUT_MS,
      },
    );
    return response.data?.providers ?? [];
  } catch {
    return [];
  }
};
