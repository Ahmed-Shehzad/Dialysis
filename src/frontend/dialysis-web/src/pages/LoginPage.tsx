import { useAuth } from "@/features/auth/components/AuthProvider";

export const LoginPage = () => {
  const { signIn } = useAuth();
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-950">
      <div className="w-full max-w-md rounded-xl border border-slate-800 bg-slate-900/70 p-8 shadow-xl">
        <h1 className="text-2xl font-semibold text-clinic-50">Dialysis Care Operations</h1>
        <p className="mt-2 text-sm text-slate-400">
          Sign in with your clinic identity to view sessions, monitor live vitals, and audit
          activity. Authentication is brokered through the Identity BFF and Keycloak.
        </p>
        <button
          type="button"
          onClick={signIn}
          className="mt-6 w-full rounded-lg bg-clinic-600 px-4 py-2 font-medium text-white hover:bg-clinic-700"
        >
          Sign in
        </button>
      </div>
    </div>
  );
};
