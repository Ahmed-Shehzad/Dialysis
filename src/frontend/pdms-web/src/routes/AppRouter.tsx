import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// PDMS (Chairside) is a single-context app served under the gateway's /pdms/* and its own BFF.
const SessionsPage = lazyPage(() => import("@/pages/SessionsPage"), "SessionsPage");
const SessionLivePage = lazyPage(() => import("@/pages/SessionLivePage"), "SessionLivePage");
const ChairBoardPage = lazyPage(
  () => import("@/modules/pdms/chairs/ChairBoardPage"),
  "ChairBoardPage",
);
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

// Single source of truth for the PDMS child routes. AppRouter.nav.test.tsx asserts every nav link
// (PDMS_NAV in AppShell) has a registered route, so a nav item can never dead-end at the catch-all.
export const PDMS_ROUTES: ReadonlyArray<{ path: string; element: ReactNode }> = [
  { path: "sessions", element: <SessionsPage /> },
  { path: "sessions/:sessionId", element: <SessionLivePage /> },
  { path: "chairs", element: <ChairBoardPage /> },
  { path: "admin/inventory", element: <InventoryPage /> },
  { path: "admin/reporting/templates", element: <ReportingTemplatesPage /> },
  { path: "admin/oncall/rotation", element: <OnCallRotationPage /> },
  { path: "admin/oncall/policies", element: <OnCallPoliciesPage /> },
  { path: "admin/oncall/audit", element: <OnCallAuditPage /> },
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
      <Route index element={<Navigate to="/sessions" replace />} />
      {PDMS_ROUTES.map((route) => (
        <Route key={route.path} path={route.path} element={route.element} />
      ))}
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
