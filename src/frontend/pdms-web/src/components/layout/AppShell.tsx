import { Suspense, type ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { ToastHost } from "@/features/durable-commands";
import { useTheme } from "@/features/theme/ThemeProvider";
import { PatientContextBar } from "@/shell/PatientContextBar";

// Single-context (PDMS) navigation. Paths are absolute within the app's `/pdms` router basename.
const PDMS_NAV: ReadonlyArray<{ to: string; label: string }> = [
  { to: "/sessions", label: "Sessions" },
  { to: "/chairs", label: "Chairs" },
  { to: "/admin/inventory", label: "Inventory" },
  { to: "/admin/reporting/templates", label: "Reporting" },
  { to: "/admin/oncall/rotation", label: "On-call" },
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

// Chairside (PDMS) chrome. The top nav lists this context's views; cross-context navigation
// (e.g. to the EHR app) is a full-page hop to another /{context} app, not an in-app link.
export const AppShell = ({ children }: { children?: ReactNode }) => {
  const { user, signOut, status } = useAuth();

  return (
    <div className="min-h-full">
      <header className="border-b border-slate-800 bg-slate-900/80 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-3">
          <div className="flex items-center gap-6">
            <h1 className="text-lg font-semibold tracking-tight text-clinic-50">Dialysis · Chairside</h1>
            <nav className="flex flex-wrap gap-1" aria-label="Chairside">
              {PDMS_NAV.map((item) => (
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
