import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import { completeTreatmentSession, getObservationsInTimeRange } from "../../api";
import { useSignalR } from "../../hooks/useSignalR";
import type { TreatmentSessionContext } from "../../types";
import { getObsValue, MDC } from "../../utils/observations";
import type { ObservationRecordedMessage } from "../../types";

interface RunningPanelProps {
    session: TreatmentSessionContext;
}

export function RunningPanel({
    session,
}: Readonly<RunningPanelProps>) {
    const queryClient = useQueryClient();
    const [endError, setEndError] = useState<string | null>(null);

    const invalidate = useCallback(() => {
        void queryClient.invalidateQueries({ queryKey: ["treatment-session", session.sessionId] });
        void queryClient.invalidateQueries({ queryKey: ["audit"] });
    }, [queryClient, session.sessionId]);

    const completeMutation = useMutation({
        mutationFn: () => completeTreatmentSession(session.sessionId),
        onSuccess: () => {
            setEndError(null);
            invalidate();
        },
        onError: (err) => {
            setEndError(err instanceof Error ? err.message : "Failed to end session");
        },
    });

    const { observations: liveObs } = useSignalR(session.sessionId, {
        onObservation: invalidate,
        onAlarm: invalidate,
    });

    const { data: fetchedData } = useQuery({
        queryKey: ["observations", session.sessionId],
        queryFn: () => {
            const end = new Date();
            const start = new Date(end.getTime() - 4 * 60 * 60 * 1000);
            return getObservationsInTimeRange(
                session.sessionId,
                start.toISOString(),
                end.toISOString()
            );
        },
        enabled: Boolean(session.sessionId),
        refetchInterval: 10_000,
    });

    const allObs = useMemo(() => {
        const fromApi =
            fetchedData?.observations?.map((o) => ({
                code: o.code,
                value: o.value,
                unit: o.unit,
                subId: o.subId,
            })) ?? [];
        const fromLive = liveObs.map((o: ObservationRecordedMessage) => ({
            code: o.code,
            value: o.value,
            unit: o.unit,
            subId: o.subId,
        }));
        const merged = [...fromApi];
        for (const lo of fromLive) {
            const idx = merged.findIndex((m) => m.code === lo.code);
            if (idx >= 0) merged[idx] = lo;
            else merged.push(lo);
        }
        return merged;
    }, [fetchedData, liveObs]);

    const bpSys = getObsValue(allObs, MDC.BP_SYS);
    const bpDia = getObsValue(allObs, MDC.BP_DIA);
    const hr = getObsValue(allObs, MDC.HEART_RATE);
    const ufActual = getObsValue(allObs, MDC.UF_ACTUAL);
    const ufTarget = getObsValue(allObs, MDC.UF_TARGET);
    const timeRemain = getObsValue(allObs, MDC.THERAPY_TIME_REMAIN);
    const qb = getObsValue(allObs, MDC.BLOOD_FLOW);
    const qd = getObsValue(allObs, MDC.DIALYSATE_FLOW);
    const tmp = getObsValue(allObs, MDC.TMP);

    const elapsed = formatElapsed(session.startedAt);

    return (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50/30 p-6">
            <div className="mb-4 flex items-center justify-between">
                <span className="rounded bg-emerald-600 px-2 py-0.5 text-xs font-medium text-white">
                    Running
                </span>
                <div className="flex gap-2">
                    <button
                        type="button"
                        className="rounded border border-emerald-600 bg-white px-3 py-1 text-sm text-emerald-700 hover:bg-emerald-50"
                    >
                        Pause
                    </button>
                    <button
                        type="button"
                        className="rounded border border-emerald-600 bg-white px-3 py-1 text-sm text-emerald-700 hover:bg-emerald-50"
                    >
                        Record Event
                    </button>
                    <button
                        type="button"
                        className="rounded border border-amber-600 bg-white px-3 py-1 text-sm text-amber-700 hover:bg-amber-50"
                    >
                        Adjust UF
                    </button>
                    <button
                        type="button"
                        className="rounded bg-amber-600 px-3 py-1 text-sm font-medium text-white hover:bg-amber-700 disabled:opacity-50"
                        onClick={() => completeMutation.mutate()}
                        disabled={completeMutation.isPending}
                    >
                        {completeMutation.isPending ? "Ending…" : "End Session"}
                    </button>
                </div>
            </div>

            {endError && (
                <p className="mb-4 text-sm text-red-600">{endError}</p>
            )}

            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
                <Metric label="BP" value={bpSys && bpDia ? `${bpSys}/${bpDia}` : "—"} unit="mmHg" />
                <Metric label="HR" value={hr ?? "—"} unit="bpm" />
                <Metric label="UF removed" value={ufActual ?? "—"} unit="mL" />
                <Metric label="UF target" value={ufTarget ?? "—"} unit="mL" />
                <Metric label="Time remaining" value={timeRemain ?? elapsed} />
                <Metric label="QB" value={qb ?? "—"} unit="mL/min" />
                <Metric label="QD" value={qd ?? "—"} unit="mL/min" />
                <Metric label="TMP" value={tmp ?? "—"} unit="mmHg" />
            </div>

            <div className="mt-6">
                <h4 className="mb-2 text-sm font-medium text-emerald-800">
                    Event log
                </h4>
                <div className="rounded border border-emerald-100 bg-white p-3 text-sm text-gray-600">
                    (Meds, bolus, symptoms — placeholder)
                </div>
            </div>
        </div>
    );
}

function Metric({
    label,
    value,
    unit,
}: {
    label: string;
    value: string;
    unit?: string;
}) {
    return (
        <div>
            <span className="text-xs font-medium text-emerald-800">{label}</span>
            <p className="mt-0.5 text-lg font-semibold">
                {value}
                {unit && <span className="ml-1 text-sm font-normal text-emerald-600">{unit}</span>}
            </p>
        </div>
    );
}

function formatElapsed(startedAt: string | undefined): string {
    if (!startedAt) return "—";
    const start = new Date(startedAt).getTime();
    const ms = Math.max(0, Date.now() - start);
    const m = Math.floor(ms / 60000);
    const s = Math.floor((ms % 60000) / 1000);
    return `${m}:${s.toString().padStart(2, "0")}`;
}
