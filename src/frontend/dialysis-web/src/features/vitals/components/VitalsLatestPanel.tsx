import type { VitalsReading } from "../api/vitalsApi";

type Metric = {
  label: string;
  value: string | number;
  unit: string;
  tone: "neutral" | "warn" | "alert";
};

const evaluate = (reading: VitalsReading | undefined): Metric[] => {
  if (!reading) return [];
  return [
    {
      label: "Systolic",
      value: reading.systolicBloodPressure,
      unit: "mmHg",
      tone:
        reading.systolicBloodPressure < 90 || reading.systolicBloodPressure > 180
          ? "alert"
          : "neutral",
    },
    {
      label: "Diastolic",
      value: reading.diastolicBloodPressure,
      unit: "mmHg",
      tone: reading.diastolicBloodPressure < 50 ? "warn" : "neutral",
    },
    {
      label: "Heart rate",
      value: reading.heartRateBpm,
      unit: "bpm",
      tone: reading.heartRateBpm < 50 || reading.heartRateBpm > 120 ? "alert" : "neutral",
    },
    {
      label: "Arterial P.",
      value: reading.arterialPressureMmHg.toFixed(0),
      unit: "mmHg",
      tone: "neutral",
    },
    {
      label: "Venous P.",
      value: reading.venousPressureMmHg.toFixed(0),
      unit: "mmHg",
      tone: "neutral",
    },
    {
      label: "UF rate",
      value: reading.ultrafiltrationRateMlPerHour.toFixed(0),
      unit: "mL/h",
      tone: "neutral",
    },
  ];
};

const toneClass: Record<Metric["tone"], string> = {
  neutral: "border-slate-700",
  warn: "border-amber-400/70 ring-1 ring-amber-300/40",
  alert: "border-rose-500 ring-2 ring-rose-500/50",
};

export type VitalsLatestPanelProps = {
  latest: VitalsReading | undefined;
};

export const VitalsLatestPanel = ({ latest }: VitalsLatestPanelProps) => {
  const metrics = evaluate(latest);

  if (!latest) {
    return (
      <div className="rounded-lg border border-slate-700 bg-slate-900/60 p-6 text-slate-400">
        No reading yet. Live updates will appear here as soon as the machine streams.
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-6">
      {metrics.map((m) => (
        <div key={m.label} className={`rounded-lg border bg-slate-900/60 p-3 ${toneClass[m.tone]}`}>
          <div className="text-xs uppercase tracking-wide text-slate-400">{m.label}</div>
          <div className="mt-1 font-mono text-2xl text-slate-100">
            {m.value}
            <span className="ml-1 text-xs text-slate-400">{m.unit}</span>
          </div>
        </div>
      ))}
    </div>
  );
};
