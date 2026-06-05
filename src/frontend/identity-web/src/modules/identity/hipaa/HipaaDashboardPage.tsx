import { useQueries } from "@tanstack/react-query";
import { useState } from "react";
import { apiClient } from "@/lib/api/apiClient";

type SafeguardCategory = "Administrative" | "Physical" | "Technical" | "Organizational";
type SafeguardStatus = "Active" | "Missing" | "Degraded" | "NotApplicable";

type SafeguardEntry = {
  id: string;
  name: string;
  category: SafeguardCategory;
  securityRuleCitation: string;
  status: SafeguardStatus;
  evidence: string;
};

type SafeguardSnapshot = {
  evaluatedAt: string;
  safeguards: SafeguardEntry[];
};

// Federated dashboard: queries every module's /admin/hipaa/safeguards endpoint via the gateway.
// Each module's HIS-shaped Program.cs maps the endpoint on the same path; the gateway routes by
// /api/{module}/* prefix. Operators get one screen showing the whole estate's posture.
const MODULES = [
  { slug: "his", displayName: "HIS" },
  { slug: "ehr", displayName: "EHR" },
  { slug: "pdms", displayName: "PDMS" },
  { slug: "smartconnect", displayName: "SmartConnect" },
  { slug: "hie", displayName: "HIE" },
] as const;

type ModuleSlug = (typeof MODULES)[number]["slug"];

const statusStyle: Record<SafeguardStatus, string> = {
  Active: "text-emerald-300 border-emerald-700/40 bg-emerald-900/20",
  Missing: "text-rose-300 border-rose-700/40 bg-rose-900/20",
  Degraded: "text-amber-300 border-amber-700/40 bg-amber-900/20",
  NotApplicable: "text-slate-300 border-slate-700/40 bg-slate-900/20",
};

// Cross-module overall: any module Missing → overall Missing; any Degraded → Degraded; else Active.
const overallStatus = (statuses: SafeguardStatus[]): SafeguardStatus => {
  if (statuses.includes("Missing")) return "Missing";
  if (statuses.includes("Degraded")) return "Degraded";
  if (statuses.every((s) => s === "NotApplicable")) return "NotApplicable";
  return "Active";
};

export const HipaaDashboardPage = () => {
  const [selectedModule, setSelectedModule] = useState<ModuleSlug>("his");

  const snapshots = useQueries({
    queries: MODULES.map((m) => ({
      queryKey: ["hipaa", "safeguards", m.slug],
      queryFn: async () => {
        const res = await apiClient.get<SafeguardSnapshot>(`/api/${m.slug}/admin/hipaa/safeguards`);
        return res.data;
      },
      refetchInterval: 60_000,
      retry: 0,
    })),
  });

  const activeIndex = Math.max(
    0,
    MODULES.findIndex((m) => m.slug === selectedModule),
  );
  const activeSnapshot = snapshots[activeIndex]!;

  return (
    <section className="space-y-4 p-4">
      <header>
        <h1 className="text-lg font-semibold text-clinic-100">HIPAA compliance dashboard</h1>
        <p className="text-xs text-slate-400">
          Federated view across every module host. Each card below shows that module's overall
          safeguard posture; pick one to drill into the per-safeguard breakdown. Refetched every
          minute.
        </p>
      </header>

      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">
        {MODULES.map((m, i) => {
          const q = snapshots[i]!;
          const statuses = q.data?.safeguards.map((s) => s.status) ?? [];
          const overall = q.data ? overallStatus(statuses) : null;
          const selected = m.slug === selectedModule;
          return (
            <button
              key={m.slug}
              type="button"
              onClick={() => setSelectedModule(m.slug)}
              className={`rounded-md border p-3 text-left transition ${
                selected
                  ? "border-clinic-500/60 bg-clinic-900/30"
                  : "border-slate-800 bg-slate-900/30 hover:border-slate-700"
              }`}
            >
              <div className="text-xs font-semibold text-slate-200">{m.displayName}</div>
              {q.isLoading && <div className="mt-1 text-[10px] text-slate-500">Loading…</div>}
              {q.error && <div className="mt-1 text-[10px] text-rose-300">Unreachable</div>}
              {overall && (
                <span
                  className={`mt-1 inline-flex rounded border px-2 py-0.5 text-[10px] uppercase tracking-wide ${statusStyle[overall]}`}
                >
                  {overall}
                </span>
              )}
              {q.data && (
                <div className="mt-1 text-[10px] text-slate-500">
                  {q.data.safeguards.length} safeguards
                </div>
              )}
            </button>
          );
        })}
      </div>

      {activeSnapshot.isLoading && (
        <div className="text-xs text-slate-400">Loading safeguards…</div>
      )}
      {activeSnapshot.error && (
        <div className="rounded-md border border-rose-700/40 bg-rose-900/10 p-3 text-xs text-rose-200">
          Could not reach{" "}
          <code className="font-mono">/api/{selectedModule}/admin/hipaa/safeguards</code>. Confirm
          the module host is up and routed by the gateway.
        </div>
      )}

      {activeSnapshot.data && (
        <>
          <div className="text-xs text-slate-500">
            <span className="font-medium text-slate-300">{selectedModule.toUpperCase()}</span>{" "}
            evaluated at <time>{new Date(activeSnapshot.data.evaluatedAt).toLocaleString()}</time>
          </div>
          <div className="overflow-hidden rounded-md border border-slate-800">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-900/60 text-slate-400">
                <tr>
                  <th className="px-3 py-2">Safeguard</th>
                  <th className="px-3 py-2">Category</th>
                  <th className="px-3 py-2">Citation</th>
                  <th className="px-3 py-2">Status</th>
                  <th className="px-3 py-2">Evidence</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {activeSnapshot.data.safeguards.map((s) => (
                  <tr key={s.id} className="hover:bg-slate-900/30">
                    <td className="px-3 py-2 align-top font-medium text-slate-200">{s.name}</td>
                    <td className="px-3 py-2 align-top text-slate-300">{s.category}</td>
                    <td className="px-3 py-2 align-top font-mono text-slate-400">
                      {s.securityRuleCitation}
                    </td>
                    <td className="px-3 py-2 align-top">
                      <span
                        className={`inline-flex rounded border px-2 py-0.5 text-[10px] uppercase tracking-wide ${statusStyle[s.status]}`}
                      >
                        {s.status}
                      </span>
                    </td>
                    <td className="px-3 py-2 align-top text-slate-300">{s.evidence}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  );
};

export default HipaaDashboardPage;
