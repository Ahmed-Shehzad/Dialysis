import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
  fetchPopulationControl,
  type OutreachResult,
  type PopulationControlResult,
  triggerOutreach,
} from "@/features/population/api/populationApi";
import { humanizeError } from "@/lib/api/humanizeError";

/**
 * Condition-control rate for a configured measure (e.g. "% of hypertensives with BP controlled"), plus
 * a one-click at-risk outreach to the uncontrolled patients. The control measures are config-driven
 * (Ehr:ControlMeasures); real outreach dispatch is gated server-side (Ehr:Population:Outreach:Enabled).
 */
export const ConditionControlPanel = () => {
  const [measureId, setMeasureId] = useState("");

  const control = useMutation<PopulationControlResult, Error, string>({
    mutationFn: (id: string) => fetchPopulationControl(id),
  });
  const outreach = useMutation<OutreachResult, Error, string>({
    mutationFn: (id: string) => triggerOutreach(id),
  });

  const result = control.data;

  return (
    <section className="space-y-3 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <h3 className="text-sm font-medium text-slate-200">Condition control</h3>
      <p className="text-xs text-slate-400">
        Enter a configured control-measure id (e.g. <span className="font-mono">HTN-BP</span>) to
        see the panel&apos;s control rate.
      </p>

      <div className="flex flex-wrap items-center gap-2">
        <input
          type="text"
          value={measureId}
          onChange={(e) => setMeasureId(e.target.value)}
          placeholder="Measure id"
          aria-label="Measure id"
          className="rounded-md border border-slate-700 bg-slate-950 px-2 py-1.5 font-mono text-sm text-slate-100"
        />
        <button
          type="button"
          onClick={() => measureId.trim() && control.mutate(measureId.trim())}
          disabled={control.isPending || measureId.trim().length === 0}
          className="rounded-md bg-clinic-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-clinic-500 disabled:opacity-50"
        >
          {control.isPending ? "Evaluating…" : "Check control"}
        </button>
      </div>

      {control.error && <p className="text-xs text-rose-300">{humanizeError(control.error)}</p>}

      {result && (
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Stat label="Control rate" value={`${result.controlRatePercent}%`} tone="emerald" />
            <Stat label="In cohort" value={result.inCohort} />
            <Stat label="Controlled" value={result.controlled} tone="emerald" />
            <Stat label="Uncontrolled" value={result.uncontrolled} tone="rose" />
          </div>

          <div className="flex items-center justify-between">
            <p className="text-sm text-slate-200">{result.title}</p>
            <button
              type="button"
              onClick={() => outreach.mutate(result.measureId)}
              disabled={outreach.isPending || result.uncontrolled === 0}
              className="rounded-md border border-amber-700 px-3 py-1.5 text-sm text-amber-200 transition hover:bg-amber-950/40 disabled:opacity-50"
            >
              {outreach.isPending ? "Reaching out…" : `Outreach ${result.uncontrolled} at-risk`}
            </button>
          </div>

          {outreach.error && (
            <p className="text-xs text-rose-300">{humanizeError(outreach.error)}</p>
          )}
          {outreach.data && (
            <p className="rounded-md border border-amber-800/50 bg-amber-950/20 p-2 text-xs text-amber-200">
              Targeted {outreach.data.targeted} patient(s).{" "}
              {outreach.data.dispatched
                ? "Notifications dispatched."
                : "Outreach dispatch is disabled — recorded the target list only."}
            </p>
          )}
        </div>
      )}
    </section>
  );
};

const statToneClass = (tone?: "emerald" | "rose"): string => {
  if (tone === "emerald") return "text-emerald-300";
  if (tone === "rose") return "text-rose-300";
  return "text-clinic-50";
};

const Stat = ({
  label,
  value,
  tone,
}: {
  label: string;
  value: string | number;
  tone?: "emerald" | "rose";
}) => (
  <div className="rounded-lg border border-slate-800 bg-slate-950/40 p-3">
    <p className="text-xs uppercase tracking-wide text-slate-400">{label}</p>
    <p className={`mt-1 text-2xl font-semibold ${statToneClass(tone)}`}>{value}</p>
  </div>
);
