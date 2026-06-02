import { useQuery } from "@tanstack/react-query";
import {
  fetchSessionReports,
  reportDownloadUrl,
  type SessionReport,
} from "@/features/reports/api/reportsApi";

/**
 * Lists every report produced for the session — discharge letter, billing summary,
 * shift roll-up — with download links to the rendered PDF. Drives the live-session
 * "Reports" tab.
 */
type Props = { sessionId: string };

export const SessionReportsTab = ({ sessionId }: Props) => {
  const query = useQuery({
    queryKey: ["pdms", "sessions", sessionId, "reports"],
    queryFn: () => fetchSessionReports(sessionId),
    refetchInterval: 30_000,
  });

  if (query.isLoading) {
    return <div className="text-sm text-slate-400">Loading reports…</div>;
  }
  if (query.isError) {
    return <div className="text-sm text-rose-300">Could not load reports. Retry shortly.</div>;
  }

  const rows = query.data ?? [];
  if (rows.length === 0) {
    return (
      <div className="rounded border border-dashed border-slate-700 p-6 text-sm text-slate-400">
        Reports are generated when the session completes — none yet for this session.
      </div>
    );
  }

  return (
    <ul className="space-y-2">
      {rows.map((row) => (
        <ReportRow key={row.id} report={row} />
      ))}
    </ul>
  );
};

const ReportRow = ({ report }: { report: SessionReport }) => (
  <li className="flex items-center justify-between rounded border border-slate-800 bg-slate-900/40 p-3 text-sm">
    <div>
      <div className="font-medium text-slate-100">{labelFor(report.kind)}</div>
      <div className="text-xs text-slate-500">
        {report.generatedAtUtc
          ? `Generated ${new Date(report.generatedAtUtc).toISOString().slice(0, 19)} UTC`
          : "Pending"}
        {" · "}
        Status: <span className="font-mono">{report.status}</span>
        {report.contentHash ? (
          <span className="ml-2 font-mono text-slate-600" title="SHA-256 of the rendered bytes">
            {report.contentHash.slice(0, 12)}…
          </span>
        ) : null}
      </div>
      {report.failureReason ? (
        <div className="mt-1 text-xs text-rose-300">Failure: {report.failureReason}</div>
      ) : null}
    </div>
    <a
      className="rounded border border-slate-700 px-3 py-1.5 text-xs text-slate-200 hover:border-slate-500"
      href={reportDownloadUrl(report.id)}
      target="_blank"
      rel="noreferrer"
      aria-disabled={report.status !== "Generated" && report.status !== "Delivered"}
    >
      Download PDF
    </a>
  </li>
);

const labelFor = (kind: SessionReport["kind"]): string => {
  switch (kind) {
    case "DischargeLetter":
      return "Discharge letter";
    case "BillingDocument":
      return "Billing summary";
    case "ShiftReport":
      return "Shift report";
  }
};
