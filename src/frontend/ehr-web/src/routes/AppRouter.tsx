import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// EHR (Chart) is a single-context app served under the gateway's /ehr/* and its own BFF.
const PatientsPage = lazyPage(() => import("@/pages/PatientsPage"), "PatientsPage");
const EhrChartPage = lazyPage(() => import("@/modules/ehr/chart/EhrChartPage"), "EhrChartPage");
const EhrWorkflowsPage = lazyPage(() => import("@/pages/EhrWorkflowsPage"), "EhrWorkflowsPage");
const BillingChargesPage = lazyPage(
  () => import("@/modules/ehr/admin/BillingChargesPage"),
  "BillingChargesPage",
);
const FeeSchedulePage = lazyPage(
  () => import("@/modules/ehr/admin/FeeSchedulePage"),
  "FeeSchedulePage",
);

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
      <Route index element={<Navigate to="/patients" replace />} />
      <Route path="patients" element={<PatientsPage />} />
      <Route path="patients/:patientId" element={<EhrChartPage />} />
      <Route path="workflows" element={<EhrWorkflowsPage />} />
      <Route path="admin/billing/dialysis-charges" element={<BillingChargesPage />} />
      <Route path="admin/billing/fee-schedule" element={<FeeSchedulePage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
