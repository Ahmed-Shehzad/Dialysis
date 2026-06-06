import { useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  type CohortPatientGaps,
  fetchCohortQuality,
} from "@/features/population/api/populationApi";
import { humanizeError } from "@/lib/api/humanizeError";
import { ConditionControlPanel } from "@/modules/ehr/admin/ConditionControlPanel";

/**
 * Population / cohort quality — open care gaps across the active panel, rolled up per measure with a
 * per-patient drill-in. Runs the same quality-measure evaluator the chart uses, the basis for outreach.
 */
export const PopulationQualityPage = () => {
  const [selectedMeasure, setSelectedMeasure] = useState<string | null>(null);

  const quality = useQuery({
    queryKey: ["ehr", "population", "quality"],
    queryFn: () => fetchCohortQuality(),
  });

  const data = quality.data;
  const filteredPatients: CohortPatientGaps[] =
    data && selectedMeasure
      ? data.patientBreakdown.filter((p) => p.gaps.some((g) => g.measureId === selectedMeasure))
      : (data?.patientBreakdown ?? []);

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs uppercase tracking-wide text-slate-400">Population health</p>
        <h2 className="text-2xl font-semibold text-clinic-50">Quality gaps across the panel</h2>
        <p className="text-xs text-slate-400">
          Open quality-measure gaps for active patients — pick a measure to see who needs outreach.
        </p>
      </header>

      <ConditionControlPanel />

      {quality.isLoading && <p className="text-sm text-slate-400">Evaluating the cohort…</p>}
      {quality.error && (
        <p role="alert" className="text-sm text-rose-300">
          {humanizeError(quality.error)}
        </p>
      )}

      {data && (
        <>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <Stat label="Patients evaluated" value={data.patientsEvaluated} />
            <Stat label="With an open gap" value={data.patientsWithAnyGap} />
            <Stat label="Measures with gaps" value={data.measureGaps.length} />
          </div>

          <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
            <h3 className="mb-2 text-sm font-medium text-slate-200">Gaps by measure</h3>
            {data.measureGaps.length === 0 ? (
              <p className="text-xs text-slate-500">
                No open gaps — either the panel is clean or no quality measures are configured.
              </p>
            ) : (
              <ul className="space-y-2">
                {data.measureGaps.map((m) => {
                  const active = selectedMeasure === m.measureId;
                  return (
                    <li key={m.measureId}>
                      <button
                        type="button"
                        onClick={() => setSelectedMeasure(active ? null : m.measureId)}
                        aria-pressed={active}
                        className={`flex w-full items-center justify-between rounded-md border px-3 py-2 text-left text-sm transition ${
                          active
                            ? "border-clinic-500 bg-clinic-900/60 text-clinic-50"
                            : "border-slate-700 text-slate-200 hover:border-slate-500"
                        }`}
                      >
                        <span>
                          <span className="font-medium">{m.title}</span>
                          <span className="ml-2 text-xs text-slate-400">{m.measureId}</span>
                        </span>
                        <span className="rounded-full bg-amber-900/50 px-2.5 py-0.5 text-xs text-amber-200">
                          {m.patientsWithGap} {m.patientsWithGap === 1 ? "patient" : "patients"}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </section>

          <section className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-sm font-medium text-slate-200">
                Patients {selectedMeasure ? "for this measure" : "with open gaps"}
              </h3>
              {selectedMeasure && (
                <button
                  type="button"
                  onClick={() => setSelectedMeasure(null)}
                  className="text-xs text-clinic-300 hover:underline"
                >
                  Clear filter
                </button>
              )}
            </div>
            {filteredPatients.length === 0 ? (
              <p className="text-xs text-slate-500">No patients to outreach.</p>
            ) : (
              <ul className="divide-y divide-slate-800 text-sm">
                {filteredPatients.map((p) => (
                  <li key={p.patientId} className="flex items-center justify-between gap-3 py-2">
                    <span>
                      <Link
                        to={`/patients/${p.patientId}`}
                        className="text-clinic-200 hover:underline"
                      >
                        {p.name}
                      </Link>
                      <span className="ml-2 text-xs text-slate-500">{p.medicalRecordNumber}</span>
                    </span>
                    <span className="flex flex-wrap justify-end gap-1">
                      {p.gaps
                        .filter((g) => !selectedMeasure || g.measureId === selectedMeasure)
                        .map((g) => (
                          <span
                            key={g.measureId}
                            title={g.detail}
                            className="rounded-full bg-slate-800 px-2 py-0.5 text-xs text-slate-300"
                          >
                            {g.title}
                          </span>
                        ))}
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

const Stat = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-lg border border-slate-800 bg-slate-900/40 p-4">
    <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
    <p className="mt-1 text-2xl font-semibold text-clinic-50">{value}</p>
  </div>
);
