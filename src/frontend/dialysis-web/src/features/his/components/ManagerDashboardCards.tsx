import { useQuery } from "@tanstack/react-query";
import { fetchManagerDashboard } from "../api/hisApi";

type StatCardProps = { label: string; value: number | string; hint?: string };

const StatCard = ({ label, value, hint }: StatCardProps) => (
  <div className="rounded-lg border border-slate-800 bg-slate-900/60 p-4">
    <div className="text-xs uppercase tracking-wide text-slate-400">{label}</div>
    <div className="mt-1 font-mono text-3xl text-slate-100">{value}</div>
    {hint ? <div className="mt-1 text-xs text-slate-500">{hint}</div> : null}
  </div>
);

export const ManagerDashboardCards = () => {
  const { data, isLoading, error } = useQuery({
    queryKey: ["his", "manager-dashboard"],
    queryFn: () => fetchManagerDashboard(),
    refetchInterval: 30_000,
  });

  if (isLoading) {
    return <div className="text-slate-400">Loading operations snapshot…</div>;
  }
  if (error || !data) {
    return (
      <div className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100">
        HIS unavailable — manager dashboard can&apos;t be loaded.
      </div>
    );
  }
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <StatCard
        label="Billing export jobs queued"
        value={data.queuedBillingExportJobsCount}
        hint="awaiting EHR consumption"
      />
      <StatCard label="Open quality tasks" value={data.openQualityWorkflowTasksCount} />
      <StatCard label="Recent import jobs" value={data.recentImportJobsCount} hint="last 24 hours" />
    </div>
  );
};
