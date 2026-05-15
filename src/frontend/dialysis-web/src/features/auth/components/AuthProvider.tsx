import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { configureApiClient } from "@/lib/api/apiClient";
import { tokenStore, decodeJwt, isExpired } from "@/lib/auth/token";
import { fetchCurrentUser, type AuthenticatedUser } from "../api/authApi";

type AuthState = {
  user: AuthenticatedUser | null;
  status: "idle" | "loading" | "authenticated" | "anonymous";
  signIn: () => void;
  signOut: () => void;
  getAccessToken: () => string | null;
};

const AuthContext = createContext<AuthState | null>(null);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);
  const [status, setStatus] = useState<AuthState["status"]>("idle");

  const getAccessToken = () => {
    const token = tokenStore.get();
    if (!token) return null;
    return isExpired(decodeJwt(token)) ? null : token;
  };

  useEffect(() => {
    configureApiClient({
      tokenProvider: () => getAccessToken(),
      onUnauthorized: () => {
        tokenStore.set(null);
        setUser(null);
        setStatus("anonymous");
      },
    });
  }, []);

  // Hard upper bound on how long the auth probe is allowed to keep the SPA in "loading".
  // If fetchCurrentUser() neither resolves nor rejects within this window (e.g. an HMR-stale
  // axios bundle, an upstream that never closes the connection), we force the SPA into the
  // anonymous state so the user sees the login page rather than the silent spinner.
  const AUTH_PROBE_HARD_TIMEOUT_MS = 7000;

  useEffect(() => {
    let cancelled = false;
    setStatus("loading");

    const hardTimeout = globalThis.setTimeout(() => {
      if (cancelled) return;
      cancelled = true;
      console.warn(
        "[auth] fetchCurrentUser exceeded "
          + AUTH_PROBE_HARD_TIMEOUT_MS
          + "ms — forcing anonymous state. Check Network tab for /identity/user.",
      );
      setUser(null);
      setStatus("anonymous");
    }, AUTH_PROBE_HARD_TIMEOUT_MS);

    fetchCurrentUser()
      .then((current) => {
        if (cancelled) return;
        cancelled = true;
        globalThis.clearTimeout(hardTimeout);
        // Stash the Keycloak access token the BFF returned so the apiClient interceptor
        // (configured above) forwards it as Bearer on every gateway-routed API call.
        // tokenStore.set(null) clears any stale token if the BFF didn't return one.
        // Diagnostic: log presence + length so 401s on /api/* can be triaged in DevTools
        // without inspecting the /identity/user payload manually.
        const tokenLen = current.accessToken?.length ?? 0;
        console.info(
          "[auth] /identity/user returned",
          tokenLen > 0
            ? "accessToken (length=" + tokenLen + ") — apiClient will send Bearer"
            : "NO accessToken — gateway will 401 on /api/*. Check BFF SaveTokens + GetTokenAsync.",
        );
        tokenStore.set(current.accessToken ?? null);
        setUser(current);
        setStatus("authenticated");
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        cancelled = true;
        globalThis.clearTimeout(hardTimeout);
        console.info("[auth] /identity/user returned non-success — treating as anonymous", err);
        setUser(null);
        setStatus("anonymous");
      });

    return () => {
      cancelled = true;
      globalThis.clearTimeout(hardTimeout);
    };
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      user,
      status,
      signIn: () => {
        // Build an absolute URL on the gateway origin (or current origin as fallback).
        // Using location.assign + console.log so we can verify in DevTools exactly which
        // URL the browser is navigating to — "the page just refreshes" usually means the
        // navigation target is wrong, not that the click did nothing.
        const apiBase = (import.meta.env.VITE_API_BASE_URL ?? globalThis.location.origin)
          .replace(/\/$/, "");
        const target = apiBase
          + "/identity/login?returnUrl="
          + encodeURIComponent(apiBase + "/");
        console.info("[auth] signIn → navigating to", target);
        globalThis.location.assign(target);
      },
      signOut: () => {
        tokenStore.set(null);
        const apiBase = (import.meta.env.VITE_API_BASE_URL ?? globalThis.location.origin)
          .replace(/\/$/, "");
        const target = apiBase
          + "/identity/logout?returnUrl="
          + encodeURIComponent(apiBase + "/");
        console.info("[auth] signOut → navigating to", target);
        globalThis.location.assign(target);
      },
      getAccessToken,
    }),
    [user, status],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthState => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
};
