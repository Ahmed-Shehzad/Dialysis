import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// PDMS (Chairside) is a single-context app served under the gateway's /pdms/* and its own BFF.
const SessionsPage = lazyPage(() => import("@/pages/SessionsPage"), "SessionsPage");
const SessionLivePage = lazyPage(() => import("@/pages/SessionLivePage"), "SessionLivePage");
const ChairBoardPage = lazyPage(() => import("@/modules/pdms/chairs/ChairBoardPage"), "ChairBoardPage");
const InventoryPage = lazyPage(() => import("@/modules/pdms/admin/InventoryPage"), "InventoryPage");
const ReportingTemplatesPage = lazyPage(
  () => import("@/modules/pdms/admin/ReportingTemplatesPage"),
  "ReportingTemplatesPage",
);
const OnCallRotationPage = lazyPage(
  () => import("@/modules/pdms/admin/OnCallRotationPage"),
  "OnCallRotationPage",
);
const OnCallPoliciesPage = lazyPage(
  () => import("@/modules/pdms/admin/OnCallPoliciesPage"),
  "OnCallPoliciesPage",
);
const OnCallAuditPage = lazyPage(
  () => import("@/modules/pdms/admin/OnCallAuditPage"),
  "OnCallAuditPage",
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
      <Route index element={<Navigate to="/sessions" replace />} />
      <Route path="sessions" element={<SessionsPage />} />
      <Route path="sessions/:sessionId" element={<SessionLivePage />} />
      <Route path="chairs" element={<ChairBoardPage />} />
      <Route path="admin/inventory" element={<InventoryPage />} />
      <Route path="admin/reporting/templates" element={<ReportingTemplatesPage />} />
      <Route path="admin/oncall/rotation" element={<OnCallRotationPage />} />
      <Route path="admin/oncall/policies" element={<OnCallPoliciesPage />} />
      <Route path="admin/oncall/audit" element={<OnCallAuditPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
