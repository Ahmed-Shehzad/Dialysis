import { useQuery } from "@tanstack/react-query";
import {
  fetchPatientLabResults,
  labAbnormalFlagLabel,
  type LabAbnormalFlag,
  type LabResultListItem,
} from "@/features/ehr/api/ehrApi";
import { humanizeError } from "@/lib/api/humanizeError";

const FLAG_TONE: Record<LabAbnormalFlag, string> = {
  1: "border-emerald-700/70 bg-emerald-950/40 text-emerald-100", // Normal
  2: "border-amber-700/70 bg-amber-950/30 text-amber-100", // Low
  3: "border-amber-700/70 bg-amber-950/30 text-amber-100", // High
  4: "border-rose-700/70 bg-rose-950/40 text-rose-100", // Critical
  5: "border-slate-700 bg-slate-900/40 text-slate-300", // AbnormalNos
};

const formatDateTime = (iso: string): string => new Date(iso).toLocaleString();

const formatValue = (result: LabResultListItem): string =>
  result.unitCode ? `${result.valueText} ${result.unitCode}` : result.valueText;

/**
 * Patient-side view of recent lab results. Reads from EHR's new
 * `GET /api/v1.0/patients/{id}/lab-results` endpoint with a 180-day lookback.
 * Surfaces LOINC code, value+unit, reference range, and an abnormal-flag badge
 * (Normal / Low / High / Critical / Abnormal).
 *
 * Read-only — interpretation belongs in the chart, not the portal. The patient sees
 * "what was measured, when, and whether it was flagged" so they can ask the right
 * question at the next visit.
 */
export const LabResultsPanel = ({ patientId }: { patientId: string }) => {
  const results = useQuery({
    queryKey: ["patient-portal", "lab-results", patientId],
    queryFn: () => fetchPatientLabResults(patientId),
    staleTime: 60_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-200">Lab results</h3>
        <p className="text-xs text-slate-400">
          Last 180 days, most recent first. Talk to clinic staff before acting on a flag.
        </p>
      </header>

      {results.isLoading && <div className="text-xs text-slate-400">Loading your lab results…</div>}

      {results.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(results.error)}
        </div>
      )}

      {results.data && results.data.length === 0 && (
        <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
          No lab results on file in the last 180 days.
        </div>
      )}

      {results.data && results.data.length > 0 && (
        <ul className="divide-y divide-slate-800 text-sm">
          {results.data.map((r) => (
            <li key={r.id} className="grid grid-cols-12 items-start gap-2 py-2">
              <span className="col-span-3 text-xs text-slate-400">
                {formatDateTime(r.observedAtUtc)}
              </span>
              <span
                className="col-span-3 font-mono text-xs text-slate-300"
                title={`LOINC ${r.loincCode}`}
              >
                {r.loincCode}
              </span>
              <span className="col-span-3 text-slate-200">{formatValue(r)}</span>
              <span
                className="col-span-1 text-xs text-slate-500"
                title={r.referenceRangeText ?? ""}
              >
                {r.referenceRangeText ?? "—"}
              </span>
              <span className="col-span-2 text-right">
                <span
                  className={`rounded-full border px-2 py-0.5 text-xs ${FLAG_TONE[r.abnormalFlag]}`}
                >
                  {labAbnormalFlagLabel(r.abnormalFlag)}
                </span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
