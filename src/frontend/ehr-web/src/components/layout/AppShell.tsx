import { Suspense, type ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { ToastHost } from "@/features/durable-commands";
import { useBffNotifications } from "@/features/notifications/useBffNotifications";
import { useTheme } from "@/features/theme/ThemeProvider";
import { PatientContextBar } from "@/shell/PatientContextBar";

// Single-context (EHR) navigation. Paths are absolute within the app's `/ehr` router basename.
// Exported so AppRouter.nav.test.tsx can assert every nav target has a registered route.
export const EHR_NAV: ReadonlyArray<{ to: string; label: string }> = [
  { to: "/patients", label: "Patients" },
  { to: "/workflows", label: "Workflows" },
  { to: "/admin/billing/dialysis-charges", label: "Charges" },
  { to: "/admin/billing/fee-schedule", label: "Fee schedule" },
  { to: "/care-coordination/worklist", label: "Follow-up" },
  { to: "/appointment-requests", label: "Requests" },
  { to: "/population/quality", label: "Population" },
  { to: "/safety/surveillance", label: "Safety" },
];

const ThemeToggle = () => {
  const { theme, toggleTheme } = useTheme();
  const nextLabel = theme === "dark" ? "light" : "dark";
  return (
    <button
      type="button"
      onClick={toggleTheme}
      aria-label={`Switch to ${nextLabel} theme`}
      title={`Switch to ${nextLabel} theme`}
      className="rounded-md border border-slate-700 px-2 py-1 text-xs text-slate-300 transition hover:border-slate-500"
    >
      {theme === "dark" ? "☾ Dark" : "☀ Light"}
    </button>
  );
};

const navClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-1.5 text-sm font-medium transition ${
    isActive ? "bg-clinic-600 text-white" : "text-slate-300 hover:bg-slate-800"
  }`;

// Chart (EHR) chrome. The top nav lists this context's views; cross-context navigation
// (e.g. to the EHR app) is a full-page hop to another /{context} app, not an in-app link.
export const AppShell = ({ children }: { children?: ReactNode }) => {
  const { user, signOut, status } = useAuth();
  // Live BFF push: lab results etc. for the selected patient surface as toasts.
  useBffNotifications();

  return (
    <div className="min-h-full">
      <header className="border-b border-slate-800 bg-slate-900/80 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-3">
          <div className="flex items-center gap-6">
            <h1 className="text-lg font-semibold tracking-tight text-clinic-50">
              Dialysis · Chart
            </h1>
            <nav className="flex flex-wrap gap-1" aria-label="Chart">
              {EHR_NAV.map((item) => (
                <NavLink key={item.to} to={item.to} className={navClass}>
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3 text-sm text-slate-300">
            <ThemeToggle />
            {status === "authenticated" && user ? (
              <>
                <span>{user.username}</span>
                <button
                  type="button"
                  onClick={signOut}
                  className="rounded-md border border-slate-700 px-2 py-1 text-xs hover:border-slate-500"
                >
                  Sign out
                </button>
              </>
            ) : (
              <span className="text-slate-500">{status}</span>
            )}
          </div>
        </div>
      </header>
      <PatientContextBar />
      <main className="mx-auto max-w-7xl px-6 py-6">
        <Suspense
          fallback={
            <div role="status" className="text-sm text-slate-400">
              Loading…
            </div>
          }
        >
          {children ?? <Outlet />}
        </Suspense>
      </main>
      <ToastHost />
    </div>
  );
};
