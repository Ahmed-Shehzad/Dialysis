import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { fetchEhrPatient, fetchPatientChart, type ChartItem } from "@/features/ehr/api/ehrApi";
import { fetchConsentsForPatient } from "@/features/hie/api/hieApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { AddNoteDialog } from "@/modules/ehr/chart/AddNoteDialog";
import { OrderLabsDialog } from "@/modules/ehr/chart/OrderLabsDialog";
import { usePatientContext } from "@/shell/PatientContextProvider";

const isActive = (item: ChartItem): boolean => {
  const status = item.status?.toLowerCase() ?? "";
  return status === "" || status === "active" || status === "current";
};

/** Most-recent `ChartItem` by `recordedAtUtc` for items matching the predicate. */
const latestOf = (
  items: readonly ChartItem[],
  predicate: (it: ChartItem) => boolean,
): ChartItem | undefined =>
  [...items].filter(predicate).sort((a, b) => b.recordedAtUtc.localeCompare(a.recordedAtUtc))[0];

const VitalTile = ({ label, value, unit }: { label: string; value?: string; unit?: string }) => (
  <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-3">
    <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
    <p className="mt-1 text-2xl font-semibold text-clinic-50">
      {value ?? "—"}
      {value && unit && <span className="ml-1 text-sm text-slate-400">{unit}</span>}
    </p>
  </div>
);

const splitValueUnit = (raw: string | null | undefined): { value?: string; unit?: string } => {
  if (!raw) return {};
  // Most chart values arrive as "120 mmHg", "72.4 kg", "37.1 °C". Split on the first space.
  const m = raw.trim().match(/^([\d./-]+)\s*(.*)$/);
  if (!m) return { value: raw };
  return { value: m[1], unit: m[2] || undefined };
};

const findVital = (vitals: readonly ChartItem[], matcher: RegExp): ChartItem | undefined =>
  latestOf(vitals, (v) => matcher.test(v.display) || matcher.test(v.code));

/**
 * Nurse-focused chart. Replaces the older code-heavy diagnostic view at the same URL.
 *
 * Layout follows the question "what does a nurse need to know in 5 seconds?":
 *   1. Allergies — surfaced as a high-contrast banner so they cannot be missed.
 *   2. At-a-glance vitals — latest BP, weight, temperature, pulse as large tiles.
 *   3. Active problems and medications, status-filtered.
 *   4. HIE consents — small but visible (cross-organisation disclosure boundary).
 *
 * Codes (LOINC / SNOMED) live in tooltips, not the primary view, because clinical staff
 * read display text. Backend already de-references; we just hide what they don't need.
 */
export const EhrChartPage = () => {
  const { patientId } = useParams<{ patientId: string }>();
  const { patient, select } = usePatientContext();
  const [noteOpen, setNoteOpen] = useState(false);
  const [labsOpen, setLabsOpen] = useState(false);

  const chart = useQuery({
    queryKey: ["ehr", "chart", patientId],
    queryFn: () => fetchPatientChart(patientId as string),
    enabled: Boolean(patientId),
  });
  const detail = useQuery({
    queryKey: ["ehr", "patient", patientId],
    queryFn: () => fetchEhrPatient(patientId as string),
    enabled: Boolean(patientId),
  });
  const consents = useQuery({
    queryKey: ["hie", "consents", patientId],
    queryFn: () => fetchConsentsForPatient(patientId as string),
    enabled: Boolean(patientId),
  });

  // Promote the resolved name + MRN into the cross-module patient context so subsequent
  // navigation (back to HIS, into PDMS chairside, …) carries the real identifier rather
  // than the route's bare GUID. We only write when the URL has shifted to a different
  // patient — otherwise React Query's cached identity keeps this effect a no-op.
  useEffect(() => {
    if (!patientId) return;
    if (patient?.id === patientId) return;
    const d = detail.data;
    if (d) {
      select({
        id: d.id,
        displayName: `${d.givenName} ${d.familyName}`.trim(),
        mrn: d.medicalRecordNumber,
      });
    } else if (!detail.isLoading) {
      // Endpoint returned 404 or we're still in the very first render. Surface a placeholder
      // so the bar at least reflects the route while the fetch settles.
      select({ id: patientId, displayName: `Patient ${patientId.slice(0, 8)}…` });
    }
  }, [patientId, patient, detail.data, detail.isLoading, select]);

  const allergies = chart.data?.allergies ?? [];
  const problems = useMemo(
    () => (chart.data?.problems ?? []).filter(isActive),
    [chart.data?.problems],
  );
  const meds = useMemo(
    () => (chart.data?.medications ?? []).filter(isActive),
    [chart.data?.medications],
  );
  const vitals = chart.data?.vitals ?? [];

  const bp = findVital(vitals, /blood pressure|systolic|8480|8867/iu);
  const weight = findVital(vitals, /weight|29463|3141/iu);
  const temp = findVital(vitals, /temperature|8310|8331/iu);
  const pulse = findVital(vitals, /pulse|heart rate|8867/iu);

  if (!patientId) return <div className="text-slate-400">Missing patient id.</div>;

  // Header identity prefers the cross-module context (so navigation from HIS keeps the same
  // name the receptionist saw), then the fetched detail, then a placeholder. The branches
  // collapse cleanly once the effect above has promoted detail into context, but the
  // fallback chain matters during the first render or on a fresh deep link.
  const detailFullName = detail.data
    ? `${detail.data.givenName} ${detail.data.familyName}`.trim()
    : undefined;
  const displayName =
    patient && patient.id === patientId
      ? patient.displayName
      : (detailFullName ?? `Patient ${patientId.slice(0, 8)}…`);
  const mrn = patient && patient.id === patientId ? patient.mrn : detail.data?.medicalRecordNumber;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-slate-400">Chart</p>
          <h2 className="text-2xl font-semibold text-clinic-50">{displayName}</h2>
          {mrn && <p className="text-xs text-slate-400">{mrn}</p>}
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setNoteOpen(true)}
            className="rounded-md border border-slate-700 px-3 py-1.5 text-sm text-slate-200 transition hover:border-slate-500"
          >
            + Add note
          </button>
          <button
            type="button"
            onClick={() => setLabsOpen(true)}
            className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500"
          >
            + Order labs
          </button>
        </div>
      </header>

      {noteOpen && <AddNoteDialog patientId={patientId} onClose={() => setNoteOpen(false)} />}

      {labsOpen && <OrderLabsDialog patientId={patientId} onClose={() => setLabsOpen(false)} />}

      {chart.isLoading && <div className="text-sm text-slate-400">Loading chart…</div>}
      {chart.error && (
        <div
          role="alert"
          className="rounded-md border border-rose-700 bg-rose-900/40 p-3 text-rose-100"
        >
          {humanizeError(chart.error)}
        </div>
      )}

      {chart.data && (
        <>
          {allergies.length > 0 && (
            <section
              role="alert"
              aria-label="Allergies"
              className="rounded-lg border-2 border-rose-600 bg-rose-950/60 p-3"
            >
              <p className="text-xs font-semibold uppercase tracking-wide text-rose-200">
                Allergies — verify before any new order
              </p>
              <ul className="mt-1 flex flex-wrap gap-2">
                {allergies.map((a) => (
                  <li
                    key={a.id}
                    title={a.code}
                    className="rounded-full bg-rose-700/50 px-3 py-1 text-sm text-rose-50"
                  >
                    {a.display}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section aria-label="Latest vitals">
            <h3 className="mb-2 text-sm font-medium text-slate-300">Latest vitals</h3>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
              <VitalTile label="Blood pressure" {...splitValueUnit(bp?.value)} />
              <VitalTile label="Pulse" {...splitValueUnit(pulse?.value)} />
              <VitalTile label="Temperature" {...splitValueUnit(temp?.value)} />
              <VitalTile label="Weight" {...splitValueUnit(weight?.value)} />
            </div>
          </section>

          <div className="grid gap-4 lg:grid-cols-2">
            <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
              <h3 className="mb-2 text-sm font-medium text-slate-200">
                Active problems <span className="text-slate-500">({problems.length})</span>
              </h3>
              {problems.length === 0 ? (
                <p className="text-xs text-slate-500">No active problems on the chart.</p>
              ) : (
                <ul className="divide-y divide-slate-800 text-sm">
                  {problems.map((p) => (
                    <li key={p.id} className="flex items-center justify-between py-2">
                      <span className="text-slate-200" title={p.code}>
                        {p.display}
                      </span>
                      {p.status && (
                        <span className="text-xs uppercase tracking-wide text-slate-500">
                          {p.status}
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
              <h3 className="mb-2 text-sm font-medium text-slate-200">
                Active medications <span className="text-slate-500">({meds.length})</span>
              </h3>
              {meds.length === 0 ? (
                <p className="text-xs text-slate-500">No active medications on the chart.</p>
              ) : (
                <ul className="divide-y divide-slate-800 text-sm">
                  {meds.map((m) => (
                    <li key={m.id} className="grid grid-cols-12 gap-2 py-2">
                      <span className="col-span-7 text-slate-200" title={m.code}>
                        {m.display}
                      </span>
                      <span className="col-span-5 text-xs text-slate-400">{m.value ?? ""}</span>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>

          <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
            <h3 className="mb-2 text-sm font-medium text-slate-200">
              HIE consents <span className="text-slate-500">(cross-organisation disclosure)</span>
            </h3>
            {consents.isLoading && <p className="text-xs text-slate-400">Loading…</p>}
            {consents.error && (
              <p className="text-xs text-amber-300">{humanizeError(consents.error)}</p>
            )}
            {consents.data && consents.data.length === 0 && (
              <p className="text-xs text-slate-500">No consent grants recorded.</p>
            )}
            {consents.data && consents.data.length > 0 && (
              <ul className="divide-y divide-slate-800 text-sm">
                {consents.data.map((c) => (
                  <li key={c.id} className="grid grid-cols-12 gap-2 py-2">
                    <span className="col-span-4 text-slate-300">{c.partnerId}</span>
                    <span className="col-span-3 text-slate-300">{c.scope}</span>
                    <span className="col-span-2 text-xs uppercase text-slate-400">
                      {typeof c.direction === "number"
                        ? c.direction === 1
                          ? "Inbound"
                          : "Outbound"
                        : c.direction}
                    </span>
                    <span className="col-span-3 text-xs text-slate-400">
                      effective {new Date(c.effectiveFromUtc).toLocaleDateString()}
                      {c.revokedAtUtc ? " (revoked)" : ""}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}
    </div>
  );
};
