import type { VitalsReading } from "@/features/vitals/api/vitalsApi";

type Tone = "neutral" | "warn" | "alert";

interface PrimaryVital {
  label: string;
  value: string;
  unit: string;
  tone: Tone;
}

interface SecondaryVital {
  label: string;
  value: string;
  unit: string;
}

const TONE_TILE: Record<Tone, string> = {
  // Large tile borders + a ring on warn/alert so the eye is drawn at a distance.
  neutral: "border-slate-700 bg-slate-900/70",
  warn: "border-amber-400/70 bg-amber-950/40 ring-2 ring-amber-300/40",
  alert: "border-rose-500 bg-rose-950/40 ring-2 ring-rose-500/60 animate-pulse",
};

const TONE_VALUE: Record<Tone, string> = {
  neutral: "text-slate-50",
  warn: "text-amber-100",
  alert: "text-rose-100",
};

const formatInt = (n: number): string => Math.round(n).toString();

const buildPrimaries = (r: VitalsReading): PrimaryVital[] => {
  // Clinical alert ranges mirror the smaller `VitalsLatestPanel`; chairside re-uses the
  // same thresholds so visual escalation is consistent across the two presentations.
  const systolicTone: Tone =
    r.systolicBloodPressure < 90 || r.systolicBloodPressure > 180 ? "alert" : "neutral";
  const diastolicTone: Tone = r.diastolicBloodPressure < 50 ? "warn" : "neutral";
  const hrTone: Tone = r.heartRateBpm < 50 || r.heartRateBpm > 120 ? "alert" : "neutral";

  return [
    {
      label: "Systolic",
      value: formatInt(r.systolicBloodPressure),
      unit: "mmHg",
      tone: systolicTone,
    },
    {
      label: "Diastolic",
      value: formatInt(r.diastolicBloodPressure),
      unit: "mmHg",
      tone: diastolicTone,
    },
    {
      label: "Heart rate",
      value: formatInt(r.heartRateBpm),
      unit: "bpm",
      tone: hrTone,
    },
    {
      label: "UF rate",
      value: formatInt(r.ultrafiltrationRateMlPerHour),
      unit: "mL/h",
      tone: "neutral",
    },
  ];
};

const buildSecondaries = (r: VitalsReading): SecondaryVital[] => [
  { label: "Arterial P.", value: r.arterialPressureMmHg.toFixed(0), unit: "mmHg" },
  { label: "Venous P.", value: r.venousPressureMmHg.toFixed(0), unit: "mmHg" },
  { label: "Conductivity", value: r.conductivityMsPerCm.toFixed(1), unit: "mS/cm" },
];

interface KioskVitalsProps {
  latest: VitalsReading | undefined;
}

/**
 * Chairside-format live vitals. Four hemodynamic primaries get oversized tiles a nurse can
 * read from across the room; three machine-side secondaries sit in a denser strip below.
 * Out-of-range readings get a coloured ring and (for `alert`) a slow pulse so escalation is
 * obvious without staring at the numbers.
 *
 * Empty state matches `VitalsLatestPanel` wording so users coming from the older view see
 * the same reassurance message.
 */
export const KioskVitals = ({ latest }: KioskVitalsProps) => {
  if (!latest) {
    return (
      <div className="rounded-xl border border-slate-700 bg-slate-900/60 p-8 text-center text-slate-400">
        No reading yet. Live updates will appear here as soon as the machine streams.
      </div>
    );
  }

  const primaries = buildPrimaries(latest);
  const secondaries = buildSecondaries(latest);

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {primaries.map((v) => (
          <div
            key={v.label}
            className={`rounded-xl border-2 px-5 py-4 transition ${TONE_TILE[v.tone]}`}
          >
            <p className="text-xs uppercase tracking-wide text-slate-400">{v.label}</p>
            <p
              className={`mt-1 font-mono text-5xl font-semibold tabular-nums ${TONE_VALUE[v.tone]}`}
            >
              {v.value}
              <span className="ml-1 text-base text-slate-400">{v.unit}</span>
            </p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-3 gap-3">
        {secondaries.map((v) => (
          <div
            key={v.label}
            className="rounded-lg border border-slate-800 bg-slate-900/40 px-3 py-2"
          >
            <p className="text-xs uppercase tracking-wide text-slate-400">{v.label}</p>
            <p className="mt-0.5 font-mono text-xl text-slate-100 tabular-nums">
              {v.value}
              <span className="ml-1 text-xs text-slate-400">{v.unit}</span>
            </p>
          </div>
        ))}
      </div>
    </div>
  );
};
