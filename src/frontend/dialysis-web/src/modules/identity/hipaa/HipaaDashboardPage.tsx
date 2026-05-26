import { useQuery } from "@tanstack/react-query";
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

// The endpoint is mapped on every module host; the gateway federates them. For this first
// iteration the dashboard reads from the HIS host (proven by the reference wire-up in PR #2).
// A follow-up will let an operator switch between modules.
const SAFEGUARD_ENDPOINT = "/api/his/admin/hipaa/safeguards";

const statusStyle: Record<SafeguardStatus, string> = {
  Active: "text-emerald-300 border-emerald-700/40 bg-emerald-900/20",
  Missing: "text-rose-300 border-rose-700/40 bg-rose-900/20",
  Degraded: "text-amber-300 border-amber-700/40 bg-amber-900/20",
  NotApplicable: "text-slate-300 border-slate-700/40 bg-slate-900/20",
};

export const HipaaDashboardPage = () => {
  const snapshot = useQuery({
    queryKey: ["hipaa", "safeguards", SAFEGUARD_ENDPOINT],
    queryFn: async () => {
      const res = await apiClient.get<SafeguardSnapshot>(SAFEGUARD_ENDPOINT);
      return res.data;
    },
    refetchInterval: 60_000,
  });

  return (
    <section className="space-y-4 p-4">
      <header>
        <h1 className="text-lg font-semibold text-clinic-100">HIPAA compliance dashboard</h1>
        <p className="text-xs text-slate-400">
          Live evaluation of the Security Rule safeguards wired into this host. Status updates every
          minute. <span className="text-slate-500">Source: HIS module.</span>
        </p>
      </header>

      {snapshot.isLoading && <div className="text-xs text-slate-400">Loading safeguards…</div>}
      {snapshot.error && (
        <div className="rounded-md border border-rose-700/40 bg-rose-900/10 p-3 text-xs text-rose-200">
          Could not reach the safeguards endpoint. Confirm the HIS host is up and that
          <code className="ml-1 mr-1 font-mono">/admin/hipaa/safeguards</code> is routed by the
          gateway.
        </div>
      )}

      {snapshot.data && (
        <>
          <div className="text-xs text-slate-500">
            Evaluated at <time>{new Date(snapshot.data.evaluatedAt).toLocaleString()}</time>
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
                {snapshot.data.safeguards.map((s) => (
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
