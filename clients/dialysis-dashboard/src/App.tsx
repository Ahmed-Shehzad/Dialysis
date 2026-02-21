import { useState, useEffect } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { AlarmsBySeverityCard } from "./components/AlarmsBySeverityCard";
import { PrescriptionComplianceCard } from "./components/PrescriptionComplianceCard";
import { RealTimeCharts } from "./components/RealTimeCharts";
import { AlertsPanel } from "./components/AlertsPanel";
import { FiveQuestionsSummary } from "./components/FiveQuestionsSummary";
import { SessionContextBar } from "./components/SessionContextBar";
import { TimelinePanel } from "./components/TimelinePanel";
import { WorkflowLayer } from "./components/WorkflowLayer";
import { SessionsSummaryCard } from "./components/SessionsSummaryCard";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { useAuth } from "./auth/useAuth";
import { getTreatmentSessions } from "./api";

function formatDateForInput(d: Date): string {
    return d.toISOString().slice(0, 10);
}

type DatePreset = "7" | "30" | "90";

function applyPreset(days: DatePreset): { from: string; to: string } {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - Number(days));
    return { from: formatDateForInput(from), to: formatDateForInput(to) };
}

function App() {
    const { token, setToken, isAuthenticated } = useAuth();
    const queryClient = useQueryClient();
    const weekAgo = new Date();
    weekAgo.setDate(weekAgo.getDate() - 7);
    const now = new Date();
    const [from, setFrom] = useState(formatDateForInput(weekAgo));
    const [to, setTo] = useState(formatDateForInput(now));
    const [tokenInput, setTokenInput] = useState("");
    const [activeSessionId, setActiveSessionId] = useState<string | null>(null);

    const { data: sessions = [] } = useQuery({
        queryKey: ["treatment-sessions"],
        queryFn: () => getTreatmentSessions(50),
        refetchInterval: 30_000,
    });

    useEffect(() => {
        queryClient.invalidateQueries();
    }, [token, queryClient]);

    const fromIso = (from && new Date(from + "T00:00:00").toISOString()) || undefined;
    const toIso = (to && new Date(to + "T23:59:59").toISOString()) || undefined;

    const handlePreset = (days: DatePreset) => {
        const { from: f, to: t } = applyPreset(days);
        setFrom(f);
        setTo(t);
    };

    const handleTokenSubmit = (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const trimmed = tokenInput.trim();
        setToken(trimmed || null);
    };

    return (
        <ErrorBoundary>
            <div className="max-w-240 mx-auto p-8 font-sans">
                <header className="mb-8">
                    <h1 className="m-0 mb-2 text-[1.75rem]">
                        Dialysis PDMS Dashboard
                    </h1>
                    <p className="m-0 text-gray-600 text-sm mb-4">
                        Reports from PDMS Gateway (ensure services are running)
                    </p>
                    <div className="flex flex-wrap gap-4 items-center">
                        <div className="flex flex-wrap gap-2 items-center">
                            <span className="text-sm text-gray-600">Presets:</span>
                            {(["7", "30", "90"] as const).map((days) => (
                                <button
                                    key={days}
                                    type="button"
                                    onClick={() => handlePreset(days)}
                                    className="px-2 py-1 rounded text-sm bg-gray-200 hover:bg-gray-300"
                                >
                                    Last {days} days
                                </button>
                            ))}
                        </div>
                        <label className="flex items-center gap-2">
                            <span className="text-sm text-gray-600">From</span>
                            <input
                                type="date"
                                value={from}
                                onChange={(e) => setFrom(e.target.value)}
                                className="border border-gray-300 rounded px-2 py-1 text-sm"
                            />
                        </label>
                        <label className="flex items-center gap-2">
                            <span className="text-sm text-gray-600">To</span>
                            <input
                                type="date"
                                value={to}
                                onChange={(e) => setTo(e.target.value)}
                                className="border border-gray-300 rounded px-2 py-1 text-sm"
                            />
                        </label>
                    </div>
                    <form onSubmit={handleTokenSubmit} className="flex flex-wrap gap-2 items-center mt-3">
                        <input
                            type="password"
                            placeholder="JWT token (optional; dev bypass when absent)"
                            value={tokenInput}
                            onChange={(e) => setTokenInput(e.target.value)}
                            className="border border-gray-300 rounded px-2 py-1 text-sm w-64"
                        />
                        <button
                            type="submit"
                            className="px-2 py-1 rounded text-sm bg-gray-200 hover:bg-gray-300"
                        >
                            Set
                        </button>
                        {isAuthenticated && (
                            <button
                                type="button"
                                onClick={() => { setToken(null); setTokenInput(""); }}
                                className="px-2 py-1 rounded text-sm text-red-700 hover:bg-red-100"
                            >
                                Clear
                            </button>
                        )}
                    </form>
                    <div className="flex flex-wrap gap-3 items-center mt-4 pt-4 border-t border-gray-200">
                        <span className="text-sm font-medium text-gray-700">
                            Focus session
                        </span>
                        {sessions.length > 0 && (
                            <select
                                value={activeSessionId ?? ""}
                                onChange={(e) =>
                                    setActiveSessionId(
                                        e.target.value || null
                                    )
                                }
                                className="border border-gray-300 rounded px-2 py-1 text-sm min-w-[12rem]"
                                aria-label="Select treatment session"
                            >
                                <option value="">— None —</option>
                                {sessions.map((s) => (
                                    <option key={s} value={s}>
                                        {s}
                                    </option>
                                ))}
                            </select>
                        )}
                        {sessions.length === 0 && (
                            <span className="text-sm text-gray-500">
                                No sessions (run simulator)
                            </span>
                        )}
                    </div>
                </header>
                <SessionContextBar
                    sessionId={activeSessionId}
                    refetchIntervalMs={10_000}
                />
                {activeSessionId && (
                    <AlertsPanel sessionId={activeSessionId} />
                )}
                <main className="grid gap-6">
                    {activeSessionId && (
                        <div className="grid gap-4 sm:grid-cols-[1fr,auto]">
                            <TimelinePanel sessionId={activeSessionId} />
                            <FiveQuestionsSummary sessionId={activeSessionId} />
                        </div>
                    )}
                    <WorkflowLayer
                        sessionId={activeSessionId}
                        patientMrn={undefined}
                        onSessionChange={setActiveSessionId}
                    />
                    {activeSessionId && (
                        <RealTimeCharts
                            sessionId={activeSessionId}
                            onSessionChange={setActiveSessionId}
                        />
                    )}
                    <div className="grid gap-6 grid-cols-[repeat(auto-fill,minmax(280px,1fr))]">
                        <SessionsSummaryCard from={fromIso} to={toIso} />
                        <AlarmsBySeverityCard from={fromIso} to={toIso} />
                        <PrescriptionComplianceCard from={fromIso} to={toIso} />
                    </div>
                </main>
            </div>
        </ErrorBoundary>
    );
}

export default App;
