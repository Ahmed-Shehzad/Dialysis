import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router";
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
const CareCoordinationWorklistPage = lazyPage(
  () => import("@/modules/ehr/admin/CareCoordinationWorklistPage"),
  "CareCoordinationWorklistPage",
);
const AppointmentRequestsWorklist = lazyPage(
  () => import("@/modules/ehr/admin/AppointmentRequestsWorklist"),
  "AppointmentRequestsWorklist",
);
const PopulationQualityPage = lazyPage(
  () => import("@/modules/ehr/admin/PopulationQualityPage"),
  "PopulationQualityPage",
);
const SafetySurveillancePage = lazyPage(
  () => import("@/modules/ehr/admin/SafetySurveillancePage"),
  "SafetySurveillancePage",
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

// Single source of truth for the EHR child routes. The nav (EHR_NAV in AppShell) and the router are
// kept in lockstep by AppRouter.nav.test.tsx, which asserts every nav target has a registered route —
// so a nav link can never silently fall through to the catch-all redirect again.
export const EHR_ROUTES: ReadonlyArray<{ path: string; element: ReactNode }> = [
  { path: "patients", element: <PatientsPage /> },
  { path: "patients/:patientId", element: <EhrChartPage /> },
  { path: "workflows", element: <EhrWorkflowsPage /> },
  { path: "admin/billing/dialysis-charges", element: <BillingChargesPage /> },
  { path: "admin/billing/fee-schedule", element: <FeeSchedulePage /> },
  { path: "care-coordination/worklist", element: <CareCoordinationWorklistPage /> },
  { path: "appointment-requests", element: <AppointmentRequestsWorklist /> },
  { path: "population/quality", element: <PopulationQualityPage /> },
  { path: "safety/surveillance", element: <SafetySurveillancePage /> },
];

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
      {EHR_ROUTES.map((route) => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
