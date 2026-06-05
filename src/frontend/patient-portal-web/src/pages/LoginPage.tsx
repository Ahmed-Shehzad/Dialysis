import { useEffect, useState } from "react";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { fetchIdentityProviders, type IdentityProvider } from "@/features/auth/api/authApi";

export const LoginPage = () => {
  const { signIn } = useAuth();
  const [providers, setProviders] = useState<IdentityProvider[]>([]);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    fetchIdentityProviders().then((list) => {
      if (cancelled) return;
      setProviders(list);
      setLoaded(true);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-950">
      <div className="w-full max-w-md rounded-xl border border-slate-800 bg-slate-900/70 p-8 shadow-xl">
        <h1 className="text-2xl font-semibold text-clinic-50">Dialysis Care Operations</h1>
        <p className="mt-2 text-sm text-slate-400">
          Sign in with your clinic identity to view sessions, monitor live vitals, and audit
          activity. Authentication is brokered through the Identity BFF and Keycloak.
        </p>

        {loaded && providers.length > 0 && (
          <div className="mt-6 space-y-2">
            {providers.map((p) => (
              <button
                key={p.alias}
                type="button"
                onClick={() => signIn(p.alias)}
                className="flex w-full items-center justify-center gap-2 rounded-lg border border-slate-700 bg-slate-800 px-4 py-2 font-medium text-slate-100 hover:bg-slate-700"
              >
                {p.iconUri ? (
                  <img src={p.iconUri} alt="" className="h-4 w-4" aria-hidden="true" />
                ) : null}
                <span>Continue with {p.displayName}</span>
              </button>
            ))}
            <div className="relative my-2 text-center">
              <span className="bg-slate-900/70 px-2 text-xs uppercase tracking-wider text-slate-500">
                or
              </span>
            </div>
          </div>
        )}

        <button
          type="button"
          onClick={() => signIn()}
          className="mt-2 w-full rounded-lg bg-clinic-600 px-4 py-2 font-medium text-white hover:bg-clinic-700"
        >
          Sign in
        </button>
      </div>
    </div>
  );
};
