import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import {
    Area,
    AreaChart,
    Bar,
    BarChart,
    CartesianGrid,
    Legend,
    Line,
    LineChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts";
import { getObservationsInTimeRange, getTreatmentSessions } from "../api";
import { useSignalR } from "../hooks/useSignalR";
import type { ObservationRecordedMessage } from "../types";

const MAX_POINTS = 150;

interface DataPoint {
    time: string;
    timestamp: number;
    [key: string]: string | number | undefined;
}

/** MDC code → { name, description } for chart labels (IEEE 11073 / dialysis). */
const MDC_METRICS: Record<string, { name: string; description: string }> = {
    MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE: {
        name: "Blood Flow Rate",
        description: "Blood flow in milliliters per minute",
    },
    MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING: {
        name: "Blood Flow Rate Setting",
        description: "Prescribed blood flow in milliliters per minute",
    },
    MDC_HDIALY_BLD_PUMP_PRESS_VEN: {
        name: "Venous Pressure",
        description: "Venous pressure in mmHg",
    },
    MDC_HDIALY_BLD_PUMP_PRESS_ART: {
        name: "Arterial Pressure",
        description: "Arterial pressure in mmHg",
    },
    MDC_HDIALY_BLD_PUMP_MODE: {
        name: "Blood Pump Mode",
        description: "Operating mode of the blood pump",
    },
    MDC_HDIALY_UF_RATE: {
        name: "UF Rate",
        description: "Ultrafiltration rate in milliliters per hour",
    },
    MDC_HDIALY_UF_RATE_SETTING: {
        name: "UF Rate Setting",
        description: "Prescribed ultrafiltration rate in mL/h",
    },
    MDC_HDIALY_UF_TARGET_VOL: {
        name: "UF Target Volume",
        description: "Target volume of fluid to remove in mL",
    },
    MDC_PRESS_BLD_ART: {
        name: "Arterial Pressure",
        description: "Pressure in the arterial blood line (mmHg)",
    },
    MDC_PRESS_BLD_VEN: {
        name: "Venous Pressure",
        description: "Pressure in the venous blood line (mmHg)",
    },
    MDC_PRESS_BLD_SYS: {
        name: "Systolic Blood Pressure",
        description: "Systolic blood pressure in mmHg",
    },
    MDC_PRESS_BLD_DIA: {
        name: "Diastolic Blood Pressure",
        description: "Diastolic blood pressure in mmHg",
    },
    MDC_DIA_BLD_FLOW_RATE: {
        name: "Actual Blood Flow Rate",
        description: "Actual blood flow in milliliters per minute",
    },
    MDC_DIA_BLD_FLOW_RATE_PRES: {
        name: "Prescribed Blood Flow Rate",
        description: "Prescribed blood flow in mL/min",
    },
    MDC_DIA_UF_RATE: {
        name: "Actual UF Rate",
        description: "Actual ultrafiltration rate in mL/h",
    },
    MDC_DIA_UF_RATE_PRES: {
        name: "Prescribed UF Rate",
        description: "Prescribed ultrafiltration rate in mL/h",
    },
    MDC_DIA_TEMP_DIALYSATE: {
        name: "Dialysate Temperature",
        description: "Temperature of the dialysate in °C",
    },
    MDC_TEMP_BLD: {
        name: "Blood Temperature",
        description: "Blood temperature in °C",
    },
};

/** Build descriptive chart label: channel – metric (unit) – description. */
function toChartLabel(obs: ObservationRecordedMessage): string {
    const mdcCode = obs.code.split("^")[1] ?? obs.code;
    const metric = MDC_METRICS[mdcCode] ?? {
        name: (mdcCode.startsWith("MDC_") ? mdcCode.slice(4) : mdcCode)
            .replaceAll("_", " ")
            .replace(/\b\w/g, (c) => c.toUpperCase()),
        description: "",
    };
    const unit = obs.unit?.split("^")[0]?.trim();
    const channel = obs.channelName;

    let label: string;
    if (channel && metric.name.toLowerCase() !== channel.toLowerCase()) {
        label = unit
            ? `${channel} – ${metric.name} (${unit})`
            : `${channel} – ${metric.name}`;
    } else {
        label = unit ? `${metric.name} (${unit})` : metric.name;
    }
    return metric.description ? `${label} – ${metric.description}` : label;
}

function buildChartData(
    observations: ObservationRecordedMessage[],
): DataPoint[] {
    const byTime = new Map<number, DataPoint>();
    let lastTs = 0;
    for (const obs of observations) {
        const obsTs = (
            obs as ObservationRecordedMessage & { _receivedAt?: number }
        )._receivedAt;
        const ts = obsTs ?? lastTs;
        if (obsTs != null) lastTs = obsTs;
        const key = Math.floor(ts / 5000) * 5000;
        const numVal = obs.value ? Number.parseFloat(obs.value) : Number.NaN;
        if (!byTime.has(key)) {
            byTime.set(key, {
                time: new Date(key).toLocaleTimeString(),
                timestamp: key,
            });
        }
        const pt = byTime.get(key)!;
        const label = toChartLabel(obs);
        if (!Number.isNaN(numVal)) pt[label] = numVal;
    }
    return Array.from(byTime.values())
        .sort((a, b) => a.timestamp - b.timestamp)
        .slice(-MAX_POINTS);
}

function getNumericSeries(
    observations: ObservationRecordedMessage[],
): { code: string; label: string }[] {
    const seen = new Set<string>();
    const result: { code: string; label: string }[] = [];
    for (const obs of observations) {
        const numVal = obs.value ? Number.parseFloat(obs.value) : Number.NaN;
        if (!Number.isNaN(numVal) && !seen.has(obs.code)) {
            seen.add(obs.code);
            const label = toChartLabel(obs);
            result.push({ code: label, label });
        }
    }
    return result;
}

const COLORS = [
    "#3b82f6",
    "#10b981",
    "#f59e0b",
    "#ef4444",
    "#8b5cf6",
    "#ec4899",
    "#06b6d4",
    "#84cc16",
    "#f97316",
    "#6366f1",
    "#14b8a6",
    "#e11d48",
];

function StatusBadge({ isConnected }: Readonly<{ isConnected: boolean }>) {
    const text = isConnected ? "● Connected" : "○ Connecting…";
    const cls = isConnected ? "text-green-600" : "text-amber-600";
    return <span className={`text-sm ${cls}`}>{text}</span>;
}

interface RealTimeChartsProps {
    sessionId?: string | null;
    onSessionChange?: (sessionId: string | null) => void;
}

export function RealTimeCharts({
    sessionId: propSessionId,
    onSessionChange,
}: Readonly<RealTimeChartsProps> = {}) {
    const [localSessionId, setLocalSessionId] = useState("");
    const isControlled = onSessionChange != null;
    const activeSessionId = isControlled
        ? (propSessionId ?? null)
        : (localSessionId || null);

    const queryClient = useQueryClient();
    const { data: sessions = [], isSuccess: sessionsLoaded } = useQuery({
        queryKey: ["treatment-sessions"],
        queryFn: () => getTreatmentSessions(50),
        refetchInterval: 30_000, // Refresh every 30s to stay in sync with DataProducerSimulator
    });

    const invalidateStats = useCallback(() => {
        void queryClient.invalidateQueries({ queryKey: ["sessions-summary"] });
        void queryClient.invalidateQueries({
            queryKey: ["alarms-by-severity"],
        });
        void queryClient.invalidateQueries({
            queryKey: ["prescription-compliance"],
        });
    }, [queryClient]);

    const { isConnected, observations, alarms, error, clearData } = useSignalR(
        activeSessionId,
        {
            onObservation: invalidateStats,
            onAlarm: invalidateStats,
        },
    );

    const { data: fetchedData } = useQuery({
        queryKey: ["observations", activeSessionId],
        queryFn: () => {
            if (!activeSessionId) {
                return Promise.resolve({ sessionId: "", observations: [] });
            }
            const end = new Date();
            const start = new Date(end.getTime() - 60 * 60 * 1000);
            return getObservationsInTimeRange(
                activeSessionId,
                start.toISOString(),
                end.toISOString(),
            );
        },
        enabled: Boolean(activeSessionId),
        staleTime: 0,
        refetchInterval: 10_000,
    });

    const effectiveObservations = useMemo(() => {
        const fetchedFormatted: (ObservationRecordedMessage & {
            _receivedAt?: number;
        })[] =
            fetchedData?.observations?.map((o) => ({
                sessionId: fetchedData.sessionId,
                observationId: o.id,
                code: o.code,
                value: o.value,
                unit: o.unit,
                subId: o.subId,
                channelName: o.channelName,
                _receivedAt: new Date(o.observedAtUtc).getTime(),
            })) ?? [];
        return [...fetchedFormatted, ...observations];
    }, [fetchedData, observations]);

    const chartData = useMemo(
        () => buildChartData(effectiveObservations),
        [effectiveObservations],
    );

    const series = useMemo(
        () => getNumericSeries(effectiveObservations),
        [effectiveObservations],
    );

    const handleSubscribe = () => {
        const id = (propSessionId ?? localSessionId).trim();
        if (id) {
            onSessionChange?.(id);
            setLocalSessionId(id);
            clearData();
        }
    };

    const handleSelectSession = (id: string) => {
        const value = id || null;
        setLocalSessionId(value ?? "");
        onSessionChange?.(value);
        clearData();
    };

    return (
        <div className="p-5 border border-gray-200 rounded-lg bg-white shadow-sm space-y-4">
            <h3 className="m-0 text-base font-semibold">
                Real-Time Monitoring
            </h3>

            <div className="flex flex-wrap gap-4 items-center">
                <label className="flex items-center gap-2">
                    <span className="text-sm text-gray-600">Session ID</span>
                    <input
                        type="text"
                        value={(propSessionId ?? localSessionId) || ""}
                        onChange={(e) => setLocalSessionId(e.target.value)}
                        placeholder="e.g. THERAPY123ABC"
                        className="border border-gray-300 rounded px-2 py-1 text-sm w-48"
                    />
                </label>
                {sessions.length > 0 && (
                    <select
                        value={(propSessionId ?? localSessionId) || ""}
                        onChange={(e) => handleSelectSession(e.target.value)}
                        className="border border-gray-300 rounded px-2 py-1 text-sm"
                        aria-label="Select treatment session"
                    >
                        <option value="">Select session…</option>
                        {sessions.map((s) => (
                            <option key={s} value={s}>
                                {s}
                            </option>
                        ))}
                    </select>
                )}
                <button
                    type="button"
                    onClick={handleSubscribe}
                    disabled={!(propSessionId ?? localSessionId).trim()}
                    className="px-3 py-1 rounded text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    Subscribe
                </button>
                {activeSessionId && <StatusBadge isConnected={isConnected} />}
            </div>

            {error && <p className="text-sm text-red-600">SignalR: {error}</p>}

            {activeSessionId && chartData.length > 0 && (
                <div className="space-y-6">
                    <div>
                        <h4 className="text-sm font-medium mb-2">
                            Observations – Line Chart
                        </h4>
                        <div className="h-64">
                            <ResponsiveContainer width="100%" height="100%">
                                <LineChart
                                    data={chartData}
                                    margin={{
                                        top: 5,
                                        right: 20,
                                        left: 0,
                                        bottom: 5,
                                    }}
                                >
                                    <CartesianGrid
                                        strokeDasharray="3 3"
                                        stroke="#e5e7eb"
                                    />
                                    <XAxis
                                        dataKey="time"
                                        tick={{ fontSize: 10 }}
                                    />
                                    <YAxis tick={{ fontSize: 10 }} />
                                    <Tooltip />
                                    <Legend />
                                    {series.map((s, i) => (
                                        <Line
                                            key={s.code}
                                            type="monotone"
                                            dataKey={s.code}
                                            name={s.label}
                                            stroke={COLORS[i % COLORS.length]}
                                            dot={false}
                                            isAnimationActive={false}
                                        />
                                    ))}
                                </LineChart>
                            </ResponsiveContainer>
                        </div>
                    </div>

                    <div>
                        <h4 className="text-sm font-medium mb-2">
                            Observations – Area (Mountain) Chart
                        </h4>
                        <div className="h-64">
                            <ResponsiveContainer width="100%" height="100%">
                                <AreaChart
                                    data={chartData}
                                    margin={{
                                        top: 5,
                                        right: 20,
                                        left: 0,
                                        bottom: 5,
                                    }}
                                >
                                    <CartesianGrid
                                        strokeDasharray="3 3"
                                        stroke="#e5e7eb"
                                    />
                                    <XAxis
                                        dataKey="time"
                                        tick={{ fontSize: 10 }}
                                    />
                                    <YAxis tick={{ fontSize: 10 }} />
                                    <Tooltip />
                                    <Legend />
                                    {series.map((s, i) => (
                                        <Area
                                            key={s.code}
                                            type="monotone"
                                            dataKey={s.code}
                                            name={s.label}
                                            stroke={COLORS[i % COLORS.length]}
                                            fill={COLORS[i % COLORS.length]}
                                            fillOpacity={0.3}
                                            isAnimationActive={false}
                                        />
                                    ))}
                                </AreaChart>
                            </ResponsiveContainer>
                        </div>
                    </div>

                    {chartData.length > 0 && series.length > 0 && (
                        <div>
                            <h4 className="text-sm font-medium mb-2">
                                Latest Values – Bar Chart
                            </h4>
                            <div className="h-48">
                                <ResponsiveContainer width="100%" height="100%">
                                    <BarChart
                                        data={chartData.slice(-1)}
                                        margin={{
                                            top: 5,
                                            right: 20,
                                            left: 0,
                                            bottom: 5,
                                        }}
                                    >
                                        <CartesianGrid
                                            strokeDasharray="3 3"
                                            stroke="#e5e7eb"
                                        />
                                        <XAxis
                                            dataKey="time"
                                            tick={{ fontSize: 10 }}
                                        />
                                        <YAxis tick={{ fontSize: 10 }} />
                                        <Tooltip />
                                        <Legend />
                                        {series.map((s, i) => (
                                            <Bar
                                                key={s.code}
                                                dataKey={s.code}
                                                name={s.label}
                                                fill={COLORS[i % COLORS.length]}
                                                isAnimationActive={false}
                                            />
                                        ))}
                                    </BarChart>
                                </ResponsiveContainer>
                            </div>
                        </div>
                    )}
                </div>
            )}

            {activeSessionId && alarms.length > 0 && (
                <div>
                    <h4 className="text-sm font-medium mb-2">
                        Alarms ({alarms.length})
                    </h4>
                    <ul className="max-h-40 overflow-y-auto text-sm space-y-1">
                        {alarms
                            .slice(-20)
                            .reverse()
                            .map((a) => (
                                <li
                                    key={a.alarmId}
                                    className="flex justify-between items-center px-2 py-1 rounded bg-red-50 border border-red-100"
                                >
                                    <span>
                                        {a.alarmType ?? a.alarmState} –{" "}
                                        {a.eventPhase}
                                    </span>
                                    <span className="text-gray-500 text-xs">
                                        {new Date(
                                            a.occurredAt,
                                        ).toLocaleTimeString()}
                                    </span>
                                </li>
                            ))}
                    </ul>
                </div>
            )}

            {sessionsLoaded && (!sessions || sessions.length === 0) && (
                <p className="text-sm text-amber-700 bg-amber-50 p-3 rounded border border-amber-200">
                    <strong>No sessions yet.</strong> Run{" "}
                    <code className="bg-amber-100 px-1 rounded">
                        ./run-simulator.sh
                    </code>{" "}
                    to create sessions. Refresh this page after ~30 seconds to
                    see them in the dropdown.
                </p>
            )}

            {activeSessionId &&
                chartData.length === 0 &&
                !error &&
                sessions &&
                sessions.length > 0 && (
                    <div className="text-sm text-gray-600 bg-gray-50 p-3 rounded border border-gray-200 space-y-1">
                        <p>
                            <strong>Connected.</strong> Waiting for
                            observations…
                        </p>
                        <p className="text-xs text-gray-500">
                            Ensure{" "}
                            <code className="bg-gray-200 px-1 rounded">
                                ./run-simulator.sh
                            </code>{" "}
                            is running. It sends ORU^R01 every 2s (round-robin)
                            for sessions like{" "}
                            <code className="bg-gray-200 px-1 rounded">
                                {activeSessionId}
                            </code>
                            . Charts appear as data arrives.
                        </p>
                    </div>
                )}
        </div>
    );
}
