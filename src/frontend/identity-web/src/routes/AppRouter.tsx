import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// Admin console (identity-web): single-context app under the gateway's /admin/* and its own BFF.
const AdminHubPage = lazyPage(
  () => import("@/modules/identity/admin/AdminHubPage"),
  "AdminHubPage",
);
const IdentityAdminPage = lazyPage(
  () => import("@/modules/identity/admin/IdentityAdminPage"),
  "IdentityAdminPage",
);
const HipaaDashboardPage = lazyPage(
  () => import("@/modules/identity/hipaa/HipaaDashboardPage"),
  "HipaaDashboardPage",
);
const RopaPage = lazyPage(() => import("@/modules/identity/admin/RopaPage"), "RopaPage");
const ConsentsPage = lazyPage(
  () => import("@/modules/identity/admin/ConsentsPage"),
  "ConsentsPage",
);
const DataSubjectRightsPage = lazyPage(
  () => import("@/modules/identity/admin/DataSubjectRightsPage"),
  "DataSubjectRightsPage",
);
const DemoControlPanelPage = lazyPage(
  () => import("@/modules/demo/DemoControlPanelPage"),
  "DemoControlPanelPage",
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
      <Route index element={<AdminHubPage />} />
      <Route path="identity" element={<IdentityAdminPage />} />
      <Route path="hipaa" element={<HipaaDashboardPage />} />
      <Route path="data-protection/ropa" element={<RopaPage />} />
      <Route path="data-protection/consents" element={<ConsentsPage />} />
      <Route path="data-protection/data-subject-rights" element={<DataSubjectRightsPage />} />
      <Route path="demo" element={<DemoControlPanelPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
