import { useQuery } from "@tanstack/react-query";
import {
  fetchMyInsights,
  type AllergyConflictAlert,
  type InsightsItem,
} from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDateTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleString() : "—";

/**
 * "My outside records": a patient's own consolidated Community Health Record — what other
 * organisations have shared about them, gated by their identity claim under the Individual Access
 * Services purpose. Reads HIE through the patient-portal BFF aggregation. Quiet until records arrive.
 */
export const MyOutsideRecordsCard = ({ patientId }: { patientId: string }) => {
  const insights = useQuery({
    queryKey: ["patient-portal", "hie", "my-insights", patientId],
    queryFn: () => fetchMyInsights(patientId),
    enabled: patientId.length > 0,
    staleTime: 30_000,
  });

  const summary = insights.data;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">
        My outside records{" "}
        <span className="text-slate-500">(shared by other care organisations)</span>
      </h3>

      {insights.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
      {insights.error && <p className="text-xs text-amber-300">{humanizeError(insights.error)}</p>}

      {summary && summary.counts.total === 0 && (
        <p className="text-xs text-slate-500">
          No records have been shared about you from outside organisations yet.
        </p>
      )}

      {summary && summary.counts.total > 0 && (
        <div className="space-y-3">
          <div className="grid grid-cols-3 gap-2 sm:grid-cols-4 lg:grid-cols-7">
            <CountTile label="Visits" value={summary.counts.encounters} />
            <CountTile label="Labs" value={summary.counts.observations} />
            <CountTile label="Meds" value={summary.counts.medications} />
            <CountTile label="Allergies" value={summary.counts.allergies} />
            <CountTile label="Problems" value={summary.counts.problems} />
            <CountTile label="Documents" value={summary.counts.documents} />
            <CountTile label="Procedures" value={summary.counts.procedures} />
          </div>

          {summary.allergyConflictAlerts.length > 0 && (
            <ul className="space-y-1">
              {summary.allergyConflictAlerts.map((a, i) => (
                <AllergyConflictRow key={`${a.medicationDisplay}-${i}`} alert={a} />
              ))}
            </ul>
          )}

          <div className="text-xs text-slate-400">
            Shared by{" "}
            <span className="font-mono text-slate-300">
              {summary.sourceOrganizations.join(", ") || "—"}
            </span>{" "}
            · updated {formatDateTime(summary.lastUpdatedUtc)}
          </div>

          <ClinicalList title="Medications" items={summary.medications} />
          <ClinicalList title="Allergies" items={summary.allergies} />
          <ClinicalList title="Problems" items={summary.problems} />
        </div>
      )}
    </section>
  );
};

const CountTile = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-md border border-slate-800 bg-slate-950/60 p-2 text-center">
    <div className="text-lg font-semibold text-slate-100">{value}</div>
    <div className="text-[11px] uppercase tracking-wide text-slate-500">{label}</div>
  </div>
);

const AllergyConflictRow = ({ alert }: { alert: AllergyConflictAlert }) => (
  <li className="rounded-md border border-rose-700/70 bg-rose-950/40 px-2 py-1 text-xs text-rose-100">
    Possible allergy conflict: <span className="font-mono">{alert.medicationDisplay}</span> vs your
    recorded allergy <span className="font-mono">{alert.allergyDisplay}</span> — please discuss with
    your care team.
  </li>
);

const ClinicalList = ({ title, items }: { title: string; items: InsightsItem[] }) => {
  if (items.length === 0) return null;
  return (
    <div>
      <h4 className="text-[11px] uppercase tracking-wide text-slate-500">{title}</h4>
      <ul className="flex flex-wrap gap-1 pt-1">
        {items.map((item, index) => (
          <li
            key={`${title}-${index}`}
            className="rounded-full border border-slate-700 bg-slate-950/60 px-2 py-0.5 text-xs text-slate-300"
            title={item.sourceOrganization}
          >
            {item.display ?? item.resourceType}
          </li>
        ))}
      </ul>
    </div>
  );
};
