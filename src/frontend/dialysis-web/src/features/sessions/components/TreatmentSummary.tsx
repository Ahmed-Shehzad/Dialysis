import { useQuery } from "@tanstack/react-query";
import { fetchSessionSummary, type SessionSummary } from "../api/sessionsApi";

const formatDateTime = (iso: string | null) =>
  iso ? new Date(iso).toLocaleString() : "—";

const statusBadge = (status: SessionSummary["status"]) => {
  const map: Record<SessionSummary["status"], string> = {
    Scheduled: "bg-slate-700 text-slate-200",
    InProgress: "bg-clinic-600 text-white",
    Paused: "bg-amber-600 text-white",
    Completed: "bg-emerald-700 text-white",
    Aborted: "bg-rose-700 text-white",
    Cancelled: "bg-slate-600 text-slate-200",
  };
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${map[status]}`}>
      {status}
    </span>
  );
};

const Stat = ({ label, value, accent }: { label: string; value: React.ReactNode; accent?: boolean }) => (
  <div>
    <div className="text-[10px] uppercase tracking-wide text-slate-500">{label}</div>
    <div className={`mt-0.5 text-sm font-medium ${accent ? "text-clinic-50" : "text-slate-200"}`}>
      {value}
    </div>
  </div>
);

export type TreatmentSummaryProps = {
  sessionId: string;
};

export const TreatmentSummary = ({ sessionId }: TreatmentSummaryProps) => {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["pdms", "sessions", sessionId, "summary"],
    queryFn: () => fetchSessionSummary(sessionId),
    refetchInterval: 10_000,
  });

  if (isLoading) return <div className="text-xs text-slate-400">Loading summary…</div>;
  if (error || !data) {
    return (
      <div className="rounded-md border border-rose-800 bg-rose-950/40 p-3 text-xs text-rose-200">
        Failed to load session summary.{" "}
        <button onClick={() => refetch()} className="underline">retry</button>
      </div>
    );
  }

  const s = data;
  const ufPct = s.ufAchievementPercent;
  const ufBar = ufPct === null ? null : Math.min(100, Math.max(0, ufPct));
  const ufColor = ufPct === null
    ? "bg-slate-700"
    : ufPct >= 95 && ufPct <= 105
      ? "bg-emerald-600"
      : ufPct < 80 || ufPct > 120
        ? "bg-rose-600"
        : "bg-amber-500";

  return (
    <section className="space-y-4 rounded-lg border border-slate-800 bg-slate-900/40 p-4">
      <header className="flex flex-wrap items-center gap-3">
        <h3 className="text-sm font-medium text-slate-200">Treatment summary</h3>
        {statusBadge(s.status)}
        {s.abortReasonCode && (
          <span className="text-xs text-rose-300">Abort reason: {s.abortReasonCode}</span>
        )}
      </header>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Stat label="Scheduled" value={formatDateTime(s.scheduledStartUtc)} />
        <Stat label="Actual start" value={formatDateTime(s.actualStartUtc)} />
        <Stat label="Actual end" value={formatDateTime(s.actualEndUtc)} />
        <Stat
          label="Duration"
          value={
            s.actualDurationMinutes != null
              ? `${s.actualDurationMinutes} / ${s.prescription.prescribedDurationMinutes} min`
              : `— / ${s.prescription.prescribedDurationMinutes} min`
          }
          accent
        />
      </div>

      <div className="rounded-md border border-slate-800 bg-slate-950/40 p-3">
        <div className="flex items-center justify-between text-xs text-slate-400">
          <span>Ultrafiltration achievement</span>
          <span>
            <span className="font-mono text-slate-200">
              {s.achievedUfVolumeLiters?.toFixed(2) ?? "—"}
            </span>
            {" / "}
            <span className="font-mono text-slate-300">
              {s.prescription.targetUfVolumeLiters.toFixed(2)}
            </span>
            {" L"}
            {ufPct !== null && <span className="ml-2 text-slate-400">({ufPct.toFixed(1)}%)</span>}
          </span>
        </div>
        {ufBar !== null && (
          <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-800">
            <div className={`h-full ${ufColor}`} style={{ width: `${ufBar}%` }} />
          </div>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div className="rounded-md border border-slate-800 bg-slate-950/40 p-3">
          <h4 className="mb-2 text-xs uppercase tracking-wide text-slate-500">Prescription</h4>
          <div className="grid grid-cols-2 gap-3">
            <Stat label="Dialyzer" value={s.prescription.dialyzerModel} />
            <Stat label="Anticoagulation" value={s.prescription.anticoagulationProtocolCode} />
            <Stat label="Blood flow" value={`${s.prescription.bloodFlowRateMlPerMin} mL/min`} />
            <Stat label="Dialysate flow" value={`${s.prescription.dialysateFlowRateMlPerMin} mL/min`} />
            <Stat label="K⁺ / Ca²⁺ / Na⁺" value={`${s.prescription.dialysatePotassiumMmolPerL} / ${s.prescription.dialysateCalciumMmolPerL} / ${s.prescription.dialysateSodiumMmolPerL}`} />
            <Stat label="Access" value={`${s.access.kind} — ${s.access.site}`} />
          </div>
        </div>

        <div className="rounded-md border border-slate-800 bg-slate-950/40 p-3">
          <h4 className="mb-2 text-xs uppercase tracking-wide text-slate-500">
            Hemodynamics ({s.readings.count} readings)
          </h4>
          {s.readings.count === 0 ? (
            <div className="text-xs text-slate-500">No intradialytic readings recorded.</div>
          ) : (
            <div className="grid grid-cols-2 gap-3">
              <Stat
                label="Systolic (min / avg / max)"
                value={`${s.readings.systolicMin} / ${s.readings.systolicAvg} / ${s.readings.systolicMax}`}
              />
              <Stat
                label="Diastolic (min / avg / max)"
                value={`${s.readings.diastolicMin} / ${s.readings.diastolicAvg} / ${s.readings.diastolicMax}`}
              />
              <Stat
                label="Heart rate (min / avg / max)"
                value={`${s.readings.heartRateMin} / ${s.readings.heartRateAvg} / ${s.readings.heartRateMax} bpm`}
              />
              <Stat
                label="Last UF rate"
                value={
                  s.readings.lastUltrafiltrationRateMlPerHour != null
                    ? `${s.readings.lastUltrafiltrationRateMlPerHour.toFixed(0)} mL/h`
                    : "—"
                }
              />
            </div>
          )}
        </div>
      </div>
    </section>
  );
};
