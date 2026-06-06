import { useQuery } from "@tanstack/react-query";
import {
  fetchPatientInsights,
  type AllergyConflictAlert,
  type DuplicateMedicationAlert,
  type DuplicateTestAlert,
  type InsightsItem,
} from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDate = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleDateString() : "—";
const formatDateTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleString() : "—";

/**
 * Community Health Record card for the patient chart: the cross-source view of what HIE has
 * received about this patient from outside organisations (counts by category, recent activity,
 * source orgs, freshness, and duplicate-test alerts). Reads the HIE insights endpoint through the
 * EHR BFF aggregation. Stays quiet (empty state) until outside records have arrived.
 */
export const CommunityHealthRecordCard = ({ patientId }: { patientId: string }) => {
  const insights = useQuery({
    queryKey: ["ehr", "hie", "insights", patientId],
    queryFn: () => fetchPatientInsights(patientId),
    enabled: patientId.length > 0,
    staleTime: 30_000,
  });

  const summary = insights.data;

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="mb-2 text-sm font-medium text-slate-200">
        Community Health Record{" "}
        <span className="text-slate-500">(records from outside organisations)</span>
      </h3>

      {insights.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
      {insights.error && <p className="text-xs text-amber-300">{humanizeError(insights.error)}</p>}

      {summary && summary.counts.total === 0 && (
        <p className="text-xs text-slate-500">No external records received for this patient.</p>
      )}

      {summary && summary.counts.total > 0 && (
        <div className="space-y-3">
          <div className="grid grid-cols-3 gap-2 sm:grid-cols-4 lg:grid-cols-8">
            <CountTile label="Encounters" value={summary.counts.encounters} />
            <CountTile label="Labs" value={summary.counts.observations} />
            <CountTile label="Meds" value={summary.counts.medications} />
            <CountTile label="Allergies" value={summary.counts.allergies} />
            <CountTile label="Problems" value={summary.counts.problems} />
            <CountTile label="Documents" value={summary.counts.documents} />
            <CountTile label="Procedures" value={summary.counts.procedures} />
            <CountTile label="Other" value={summary.counts.other} />
          </div>

          {summary.allergyConflictAlerts.length > 0 && (
            <ul className="space-y-1">
              {summary.allergyConflictAlerts.map((a, i) => (
                <AllergyConflictRow key={`${a.medicationDisplay}-${i}`} alert={a} />
              ))}
            </ul>
          )}

          {summary.duplicateMedicationAlerts.length > 0 && (
            <ul className="space-y-1">
              {summary.duplicateMedicationAlerts.map((a) => (
                <DuplicateMedicationRow key={a.code} alert={a} />
              ))}
            </ul>
          )}

          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-400">
            <span>
              Sources:{" "}
              <span className="font-mono text-slate-300">
                {summary.sourceOrganizations.join(", ") || "—"}
              </span>
            </span>
            <span>
              Updated:{" "}
              <span className="text-slate-300">{formatDateTime(summary.lastUpdatedUtc)}</span>
            </span>
          </div>

          {summary.duplicateTestAlerts.length > 0 && (
            <ul className="space-y-1">
              {summary.duplicateTestAlerts.map((a) => (
                <DuplicateAlertRow key={a.code} alert={a} />
              ))}
            </ul>
          )}

          <ClinicalList title="Medications" items={summary.medications} />
          <ClinicalList title="Allergies" items={summary.allergies} />
          <ClinicalList title="Problems" items={summary.problems} />

          {summary.recent.length > 0 && (
            <ul className="divide-y divide-slate-800 text-sm">
              {summary.recent.map((item, index) => (
                <RecentRow key={`${item.resourceType}-${index}`} item={item} />
              ))}
            </ul>
          )}
        </div>
      )}
    </section>
  );
};

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

const AllergyConflictRow = ({ alert }: { alert: AllergyConflictAlert }) => (
  <li className="rounded-md border border-rose-700/70 bg-rose-950/40 px-2 py-1 text-xs text-rose-100">
    Allergy conflict: active <span className="font-mono">{alert.medicationDisplay}</span> vs
    recorded allergy <span className="font-mono">{alert.allergyDisplay}</span> (
    {alert.sources.join(", ")}) — reconcile before continuing.
  </li>
);

const DuplicateMedicationRow = ({ alert }: { alert: DuplicateMedicationAlert }) => (
  <li className="rounded-md border border-amber-700/60 bg-amber-950/30 px-2 py-1 text-xs text-amber-100">
    Duplicate medication <span className="font-mono">{alert.display ?? alert.code}</span> from{" "}
    {alert.sourceCount} sources ({alert.sources.join(", ")}) — reconcile.
  </li>
);

const CountTile = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-md border border-slate-800 bg-slate-950/60 p-2 text-center">
    <div className="text-lg font-semibold text-slate-100">{value}</div>
    <div className="text-[11px] uppercase tracking-wide text-slate-500">{label}</div>
  </div>
);

const DuplicateAlertRow = ({ alert }: { alert: DuplicateTestAlert }) => (
  <li className="rounded-md border border-amber-700/60 bg-amber-950/30 px-2 py-1 text-xs text-amber-100">
    Duplicate test <span className="font-mono">{alert.display ?? alert.code}</span> from{" "}
    {alert.sourceCount} sources ({alert.sources.join(", ")}) — reconcile before re-ordering.
  </li>
);

const RecentRow = ({ item }: { item: InsightsItem }) => (
  <li className="grid grid-cols-12 items-center gap-2 py-2">
    <span className="col-span-2 truncate text-xs text-slate-200">{item.resourceType}</span>
    <span className="col-span-5 truncate text-xs text-slate-300" title={item.display ?? ""}>
      {item.display ?? "—"}
    </span>
    <span
      className="col-span-3 truncate font-mono text-xs text-slate-400"
      title={item.sourceOrganization}
    >
      {item.sourceOrganization}
    </span>
    <span className="col-span-2 text-right text-xs text-slate-400">{formatDate(item.date)}</span>
  </li>
);
