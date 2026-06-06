import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  fetchPatientInsights,
  type AllergyConflictAlert,
  type DuplicateMedicationAlert,
  type DuplicateTestAlert,
  type InsightsItem,
  type PatientInsightsSummary,
} from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

const formatDateTime = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleString() : "—";
const formatDate = (iso?: string | null): string =>
  iso ? new Date(iso).toLocaleDateString() : "—";

/**
 * Community Health Record: the cross-source view of what HIE has received about a patient from
 * outside organisations (counts by category, recent activity, source orgs, freshness, and
 * duplicate-test alerts). Backs GET /hie/ops/insights/patient/{ref}. Keyed by the patient's
 * external subject reference — the id as it appears on the received resources.
 */
export const CommunityHealthRecordPanel = () => {
  const [input, setInput] = useState("");
  const [reference, setReference] = useState<string | null>(null);

  const insights = useQuery({
    queryKey: ["hie", "ops", "insights", reference],
    queryFn: () => fetchPatientInsights(reference as string),
    enabled: reference !== null && reference.length > 0,
    staleTime: 30_000,
  });

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header>
        <h3 className="text-sm font-medium text-slate-100">Community Health Record</h3>
        <p className="text-xs text-slate-400">
          Cross-source summary of records received about a patient from outside organisations.
        </p>
      </header>

      <form
        className="flex flex-wrap items-end gap-3"
        onSubmit={(e) => {
          e.preventDefault();
          setReference(input.trim());
        }}
      >
        <label className="text-xs text-slate-300">
          <span className="mr-2">Patient reference</span>
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="external patient id"
            className="w-56 rounded-md border border-slate-700 bg-slate-950 px-2 py-1 font-mono text-xs text-slate-100 focus:border-clinic-500 focus:outline-none"
          />
        </label>
        <button
          type="submit"
          disabled={input.trim().length === 0}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-clinic-700 disabled:opacity-40"
        >
          Load
        </button>
      </form>

      {insights.isLoading && <div className="text-xs text-slate-400">Loading…</div>}

      {insights.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-2 text-xs text-rose-100"
        >
          {humanizeError(insights.error)}
        </div>
      )}

      {insights.data && <SummaryView summary={insights.data} />}
    </section>
  );
};

export const SummaryView = ({ summary }: { summary: PatientInsightsSummary }) => {
  if (summary.counts.total === 0) {
    return (
      <div className="rounded-md border border-dashed border-slate-700 p-3 text-xs text-slate-500">
        No external records received for this patient yet.
      </div>
    );
  }

  return (
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
          Updated: <span className="text-slate-300">{formatDateTime(summary.lastUpdatedUtc)}</span>
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
