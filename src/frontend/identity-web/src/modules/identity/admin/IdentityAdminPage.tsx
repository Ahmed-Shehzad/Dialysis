import { useMemo } from "react";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { decodeJwt, type JwtClaims } from "@/lib/auth/token";
import { ModuleHeader } from "@/shell/ModuleHeader";

const formatExpiry = (claims: JwtClaims | null): { label: string; tone: string } => {
  if (!claims?.exp) return { label: "no expiry claim", tone: "text-slate-300" };
  const remainingMs = claims.exp * 1000 - Date.now();
  if (remainingMs <= 0) return { label: "expired", tone: "text-rose-300" };
  const minutes = Math.floor(remainingMs / 60_000);
  const seconds = Math.floor((remainingMs % 60_000) / 1_000);
  const tone = minutes < 1 ? "text-amber-200" : "text-emerald-200";
  return { label: `${minutes}m ${seconds}s remaining`, tone };
};

const renderClaimValue = (value: unknown): string => {
  if (value === null || value === undefined) return "—";
  if (Array.isArray(value)) return value.join(", ");
  if (typeof value === "object") return JSON.stringify(value);
  return String(value);
};

/**
 * Identity admin: shows the signed-in user's claims, granted roles, and the lifetime of
 * the Keycloak access token the SPA is currently sending as Bearer on every gateway-routed
 * API call. The most common production triage is "why am I getting 401?" — and the answer
 * is almost always either an expired token or a missing claim. This page makes both
 * visible without DevTools.
 *
 * No write actions yet: user provisioning, role assignment, and audit log are exposed by
 * the Identity API (`/api/v1/users`, `/api/v1/roles`) but not routed through the gateway.
 * When that wiring lands the admin actions will appear here.
 */
export const IdentityAdminPage = () => {
  const { user, status, signOut } = useAuth();

  const tokenClaims = useMemo<JwtClaims | null>(
    () => (user?.accessToken ? decodeJwt(user.accessToken) : null),
    [user?.accessToken],
  );

  const expiry = formatExpiry(tokenClaims);

  if (status === "loading") {
    return <div className="text-sm text-slate-400">Loading identity…</div>;
  }
  if (status !== "authenticated" || !user) {
    return (
      <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-4 text-sm text-slate-300">
        Not signed in. Use the top-right Sign in button to authenticate against Keycloak.
      </div>
    );
  }

  const claimEntries = Object.entries(user.claims).sort(([a], [b]) => a.localeCompare(b));

  return (
    <div className="space-y-4">
      <ModuleHeader
        moduleSlug="identity"
        quickActions={[
          {
            label: "HIPAA safeguards",
            to: "/admin/hipaa",
            hint: "Federated safeguard health-check across every module",
          },
        ]}
        tour={[
          { title: "User", body: "name, roles, and the IdP they signed in through" },
          {
            title: "Access token",
            body: "raw JWT claims forwarded to module APIs on every request",
          },
          {
            title: "HIPAA",
            body: "follow the badge above for the encryption / audit / key-ring rollup",
          },
        ]}
      />

      <section className="grid gap-3 lg:grid-cols-2">
        <article className="space-y-2 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <h3 className="text-sm font-medium text-slate-200">User</h3>
          <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
            <dt className="text-slate-400">Username</dt>
            <dd className="font-mono text-slate-100">{user.username}</dd>
            <dt className="text-slate-400">Email</dt>
            <dd className="text-slate-100">{user.email ?? "—"}</dd>
            <dt className="text-slate-400">Roles</dt>
            <dd className="flex flex-wrap gap-1">
              {user.roles.length === 0 ? (
                <span className="text-slate-500">no roles in the token</span>
              ) : (
                user.roles.map((r) => (
                  <span
                    key={r}
                    className="rounded-full border border-slate-700 bg-slate-800/60 px-2 py-0.5 text-xs text-slate-200"
                  >
                    {r}
                  </span>
                ))
              )}
            </dd>
          </dl>
          <div className="pt-2">
            <button
              type="button"
              onClick={signOut}
              className="rounded-md border border-slate-700 bg-slate-800/60 px-3 py-1.5 text-sm text-slate-200 transition hover:border-slate-500 hover:bg-slate-700/60"
            >
              Sign out
            </button>
          </div>
        </article>

        <article className="space-y-2 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
          <h3 className="text-sm font-medium text-slate-200">Access token</h3>
          {user.accessToken ? (
            <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
              <dt className="text-slate-400">Subject</dt>
              <dd className="break-all font-mono text-slate-100">{tokenClaims?.sub ?? "—"}</dd>
              <dt className="text-slate-400">Lifetime</dt>
              <dd className={expiry.tone}>{expiry.label}</dd>
              <dt className="text-slate-400">Length</dt>
              <dd className="text-slate-100">{user.accessToken.length} chars</dd>
              <dt className="text-slate-400">Header</dt>
              <dd className="break-all font-mono text-xs text-slate-300">
                Bearer {user.accessToken.slice(0, 12)}…
              </dd>
            </dl>
          ) : (
            <p className="text-sm text-amber-300">
              No access token in the BFF response — gateway-routed API calls will 401. Check the
              BFF&apos;s <span className="font-mono">SaveTokens</span> +{" "}
              <span className="font-mono">GetTokenAsync</span> wiring.
            </p>
          )}
        </article>
      </section>

      <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
        <h3 className="mb-2 text-sm font-medium text-slate-200">All claims</h3>
        {claimEntries.length === 0 ? (
          <p className="text-sm text-slate-500">No claims on the session cookie.</p>
        ) : (
          <dl className="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm">
            {claimEntries.map(([key, value]) => (
              <div key={key} className="contents">
                <dt className="break-all font-mono text-xs text-slate-400">{key}</dt>
                <dd className="break-all font-mono text-xs text-slate-200">
                  {renderClaimValue(value)}
                </dd>
              </div>
            ))}
          </dl>
        )}
      </section>
    </div>
  );
};
