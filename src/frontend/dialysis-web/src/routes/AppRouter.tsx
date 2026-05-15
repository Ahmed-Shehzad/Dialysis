import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { DashboardPage } from "@/pages/DashboardPage";
import { LoginPage } from "@/pages/LoginPage";
import { SessionLivePage } from "@/pages/SessionLivePage";
import { SessionsPage } from "@/pages/SessionsPage";
import { PatientsPage } from "@/pages/PatientsPage";
import { PatientChartPage } from "@/pages/PatientChartPage";
import { IntegrationsPage } from "@/pages/IntegrationsPage";
import { HisWorkflowsPage } from "@/pages/HisWorkflowsPage";
import { EhrWorkflowsPage } from "@/pages/EhrWorkflowsPage";
import { FhirExchangePage } from "@/pages/FhirExchangePage";
import { useEffect, useState, type ReactNode } from "react";

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
          onClick={signIn}
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
      <Route index element={<DashboardPage />} />
      <Route path="patients" element={<PatientsPage />} />
      <Route path="patients/:patientId" element={<PatientChartPage />} />
      <Route path="sessions" element={<SessionsPage />} />
      <Route path="sessions/:sessionId" element={<SessionLivePage />} />
      <Route path="integrations" element={<IntegrationsPage />} />
      <Route path="workflows/his" element={<HisWorkflowsPage />} />
      <Route path="workflows/ehr" element={<EhrWorkflowsPage />} />
      <Route path="fhir-exchange" element={<FhirExchangePage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
