import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// HIS is a single-context app (served under the gateway's /his/* and its own BFF), so the router
// mounts its pages directly rather than iterating a module registry. Each page is its own chunk.
const HisTodayPage = lazyPage(() => import("@/modules/his/today/HisTodayPage"), "HisTodayPage");
const HisWorkflowsPage = lazyPage(() => import("@/pages/HisWorkflowsPage"), "HisWorkflowsPage");
const BillingExportsPage = lazyPage(
  () => import("@/modules/his/admin/BillingExportsPage"),
  "BillingExportsPage",
);

// After this many milliseconds in "loading", surface a manual sign-in button so the user
// always has an out — even if the auth probe is genuinely stuck on some upstream hop.
const LOADING_FALLBACK_AFTER_MS = 2000;

const AuthenticatingFallback = () => {
  const { signIn } = useAuth();
  const [showAction, setShowAction] = useState(false);
  useEffect(() => {
    const id = globalThis.setTimeout(() => setShowAction(true), LOADING_FALLBACK_AFTER_MS);
    return () => globalThis.clearTimeout(id);
  }, []);
  return (
    <div className="flex flex-col items-start gap-3 p-8 text-slate-400">
      <span>Authenticating…</span>
      {showAction && (
        <button
          type="button"
          onClick={() => signIn()}
          className="rounded bg-slate-700 px-3 py-1 text-sm text-slate-100 hover:bg-slate-600"
        >
          Stuck? Sign in
        </button>
      )}
    </div>
  );
};

const ProtectedRoute = ({ children }: { children: ReactNode }) => {
  const { status } = useAuth();
  if (status === "loading" || status === "idle") {
    return <AuthenticatingFallback />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

export const AppRouter = () => (
  <Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route
      path="/"
      element={
        <ProtectedRoute>
          <AppShell />
        </ProtectedRoute>
      }
    >
      <Route index element={<Navigate to="/today" replace />} />
      <Route path="today" element={<HisTodayPage />} />
      <Route path="workflows" element={<HisWorkflowsPage />} />
      <Route path="admin/billing/exports" element={<BillingExportsPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
