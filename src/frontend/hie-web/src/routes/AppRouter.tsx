import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "@/components/layout/AppShell";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { LoginPage } from "@/pages/LoginPage";
import { lazyPage } from "@/shared/lazyPage";

// HIE (Exchange) is a single-context app served under the gateway's /hie/* and its own BFF.
const FhirExchangePage = lazyPage(() => import("@/pages/FhirExchangePage"), "FhirExchangePage");
const FhirAuthoringPage = lazyPage(() => import("@/pages/FhirAuthoringPage"), "FhirAuthoringPage");
const SubscriptionsPage = lazyPage(() => import("@/pages/SubscriptionsPage"), "SubscriptionsPage");
const DocumentsPage = lazyPage(() => import("@/modules/hie/admin/DocumentsPage"), "DocumentsPage");
const DocumentRetentionPage = lazyPage(
  () => import("@/modules/hie/admin/DocumentRetentionPage"),
  "DocumentRetentionPage",
);
const TefcaPartnersPage = lazyPage(
  () => import("@/modules/hie/admin/TefcaPartnersPage"),
  "TefcaPartnersPage",
);
const MpiStewardPage = lazyPage(() => import("@/modules/hie/admin/MpiStewardPage"), "MpiStewardPage");
const TerminologyAuthoringPage = lazyPage(
  () => import("@/modules/hie/admin/TerminologyAuthoringPage"),
  "TerminologyAuthoringPage",
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
      <Route index element={<Navigate to="/fhir-exchange" replace />} />
      <Route path="fhir-exchange" element={<FhirExchangePage />} />
      <Route path="fhir-authoring" element={<FhirAuthoringPage />} />
      <Route path="subscriptions" element={<SubscriptionsPage />} />
      <Route path="admin/documents" element={<DocumentsPage />} />
      <Route path="admin/documents/retention" element={<DocumentRetentionPage />} />
      <Route path="admin/tefca/partners" element={<TefcaPartnersPage />} />
      <Route path="admin/mpi/reviews" element={<MpiStewardPage />} />
      <Route path="admin/terminology" element={<TerminologyAuthoringPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>
);
