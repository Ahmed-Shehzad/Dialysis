import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  fetchPatientChart,
  fetchPatientLabResults,
  fetchPatientNotes,
  fetchReferrals,
  labAbnormalFlagLabel,
} from "@/features/ehr/api/ehrApi";
import { fetchPatientHospitalEvents } from "@/features/care-coordination/api/careCoordinationApi";
import { fetchPatientInsights } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";

type TimelineKind = "Note" | "Lab" | "Referral" | "Hospital" | "External" | "Chart";

interface TimelineEntry {
  id: string;
  when: string;
  kind: TimelineKind;
  title: string;
  detail?: string;
  source: string;
}

const KIND_META: Record<TimelineKind, { label: string; dot: string; chip: string }> = {
  Note: { label: "Notes", dot: "bg-sky-400", chip: "bg-sky-900/50 text-sky-200" },
  Lab: { label: "Labs", dot: "bg-emerald-400", chip: "bg-emerald-900/50 text-emerald-200" },
  Referral: { label: "Referrals", dot: "bg-violet-400", chip: "bg-violet-900/50 text-violet-200" },
  Hospital: { label: "Hospital", dot: "bg-amber-400", chip: "bg-amber-900/50 text-amber-200" },
  External: { label: "Outside org", dot: "bg-rose-400", chip: "bg-rose-900/50 text-rose-200" },
  Chart: { label: "Chart", dot: "bg-slate-400", chip: "bg-slate-800 text-slate-300" },
};

const ALL_KINDS = Object.keys(KIND_META) as TimelineKind[];

/**
 * A single chronological feed merging the chart's otherwise-fragmented sections — notes, lab
 * results, referrals, internal hospital events, outside-org encounters (HIE), and dated chart
 * items — into one timeline. Pure client-side merge of queries that already exist; no backend.
 */
export const PatientTimeline = ({ patientId }: { patientId: string }) => {
  const [active, setActive] = useState<Set<TimelineKind>>(new Set(ALL_KINDS));

  const notes = useQuery({
    queryKey: ["ehr", "notes", patientId],
    queryFn: () => fetchPatientNotes(patientId),
  });
  const labs = useQuery({
    queryKey: ["ehr", "lab-results", patientId],
    queryFn: () => fetchPatientLabResults(patientId),
  });
  const referrals = useQuery({
    queryKey: ["ehr", "referrals", patientId],
    queryFn: () => fetchReferrals(patientId),
  });
  const chart = useQuery({
    queryKey: ["ehr", "chart", patientId],
    queryFn: () => fetchPatientChart(patientId),
  });
  const hospital = useQuery({
    queryKey: ["ehr", "hospital-events", patientId],
    queryFn: () => fetchPatientHospitalEvents(patientId),
  });
  const insights = useQuery({
    queryKey: ["ehr", "hie", "insights", patientId],
    queryFn: () => fetchPatientInsights(patientId),
    retry: false,
  });

  const entries = useMemo<TimelineEntry[]>(() => {
    const out: TimelineEntry[] = [];

    for (const n of notes.data ?? []) {
      out.push({
        id: `note-${n.id}`,
        when: n.signedAtUtc ?? n.createdAtUtc,
        kind: "Note",
        title: n.assessment?.trim() || "Clinical note",
        detail: n.subjective?.trim() || undefined,
        source: "EHR",
      });
    }

    for (const l of labs.data ?? []) {
      const abnormal = l.abnormalFlag !== 1;
      out.push({
        id: `lab-${l.id}`,
        when: l.observedAtUtc,
        kind: "Lab",
        title: `${l.loincCode} — ${l.valueText}${l.unitCode ? ` ${l.unitCode}` : ""}`,
        detail: abnormal ? `Flag: ${labAbnormalFlagLabel(l.abnormalFlag)}` : undefined,
        source: "EHR",
      });
    }

    for (const r of referrals.data ?? []) {
      out.push({
        id: `ref-${r.id}`,
        when: r.requestedAtUtc,
        kind: "Referral",
        title: `Referral → ${r.destinationPartnerId}`,
        detail: r.referralReason ?? undefined,
        source: "EHR",
      });
    }

    for (const h of hospital.data ?? []) {
      const external = h.kind === "ExternalEncounter";
      out.push({
        id: `hosp-${h.id}`,
        when: h.occurredAtUtc,
        kind: external ? "External" : "Hospital",
        title: external ? "Seen at outside org" : h.kind === "Admitted" ? "Admitted" : "Discharged",
        detail: h.detail ?? undefined,
        source: h.source,
      });
    }

    const chartData = chart.data;
    if (chartData) {
      for (const item of [...chartData.medications, ...chartData.immunizations]) {
        out.push({
          id: `chart-${item.id}`,
          when: item.recordedAtUtc,
          kind: "Chart",
          title: `${item.kind}: ${item.display}`,
          detail: item.value ?? undefined,
          source: "EHR",
        });
      }
    }

    for (const i of insights.data?.recent ?? []) {
      if (!i.date) continue;
      out.push({
        id: `hie-${i.resourceType}-${i.date}-${i.display ?? ""}`,
        when: i.date,
        kind: "External",
        title: `${i.resourceType}${i.display ? `: ${i.display}` : ""}`,
        detail: undefined,
        source: i.sourceOrganization,
      });
    }

    return out
      .filter((e) => !Number.isNaN(Date.parse(e.when)))
      .sort((a, b) => Date.parse(b.when) - Date.parse(a.when));
  }, [notes.data, labs.data, referrals.data, hospital.data, chart.data, insights.data]);

  const visible = entries.filter((e) => active.has(e.kind));
  const loading =
    notes.isLoading ||
    labs.isLoading ||
    referrals.isLoading ||
    hospital.isLoading ||
    chart.isLoading;
  const firstError = notes.error ?? labs.error ?? referrals.error ?? hospital.error ?? chart.error;

  const toggle = (kind: TimelineKind) =>
    setActive((prev) => {
      const next = new Set(prev);
      if (next.has(kind)) next.delete(kind);
      else next.add(kind);
      return next;
    });

  return (
    <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-medium text-slate-200">
          Timeline <span className="text-slate-500">(everything, in order)</span>
        </h3>
        <div className="flex flex-wrap gap-1">
          {ALL_KINDS.map((kind) => {
            const on = active.has(kind);
            return (
              <button
                key={kind}
                type="button"
                onClick={() => toggle(kind)}
                aria-pressed={on}
                className={`rounded-full px-2.5 py-0.5 text-xs transition ${
                  on ? KIND_META[kind].chip : "bg-slate-950 text-slate-500 hover:text-slate-300"
                }`}
              >
                {KIND_META[kind].label}
              </button>
            );
          })}
        </div>
      </div>

      {loading && <p className="text-xs text-slate-400">Loading timeline…</p>}
      {firstError && <p className="text-xs text-amber-300">{humanizeError(firstError)}</p>}

      {!loading && visible.length === 0 && (
        <p className="text-xs text-slate-500">Nothing recorded for the selected categories.</p>
      )}

      {visible.length > 0 && (
        <ol className="relative space-y-3 border-l border-slate-800 pl-4">
          {visible.map((e) => (
            <li key={e.id} className="relative">
              <span
                className={`absolute -left-[1.30rem] top-1.5 h-2 w-2 rounded-full ${KIND_META[e.kind].dot}`}
                aria-hidden="true"
              />
              <div className="flex flex-wrap items-baseline justify-between gap-x-3">
                <p className="text-sm text-slate-100">{e.title}</p>
                <time className="text-xs text-slate-500" dateTime={e.when}>
                  {new Date(e.when).toLocaleString()}
                </time>
              </div>
              <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500">
                <span className={`rounded px-1.5 py-0.5 ${KIND_META[e.kind].chip}`}>
                  {KIND_META[e.kind].label}
                </span>
                <span>{e.source}</span>
                {e.detail && <span className="text-slate-400">· {e.detail}</span>}
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
};
