import { useState } from "react";
import { AlarmsBySeverityCard } from "./components/AlarmsBySeverityCard";
import { PrescriptionComplianceCard } from "./components/PrescriptionComplianceCard";
import { SessionsSummaryCard } from "./components/SessionsSummaryCard";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { useAuth } from "./auth/useAuth";

function formatDateForInput(d: Date): string {
    return d.toISOString().slice(0, 10);
}

function App() {
    const { setToken, isAuthenticated } = useAuth();
    const weekAgo = new Date();
    weekAgo.setDate(weekAgo.getDate() - 7);
    const now = new Date();
    const [from, setFrom] = useState(formatDateForInput(weekAgo));
    const [to, setTo] = useState(formatDateForInput(now));
    const [tokenInput, setTokenInput] = useState("");

    const fromIso = (from && new Date(from + "T00:00:00").toISOString()) || undefined;
    const toIso = (to && new Date(to + "T23:59:59").toISOString()) || undefined;

    const handleTokenSubmit = (e: React.SubmitEvent<HTMLFormElement>) => {
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
                </header>
                <main className="grid gap-6 grid-cols-[repeat(auto-fill,minmax(280px,1fr))]">
                    <SessionsSummaryCard from={fromIso} to={toIso} />
                    <AlarmsBySeverityCard from={fromIso} to={toIso} />
                    <PrescriptionComplianceCard from={fromIso} to={toIso} />
                </main>
            </div>
        </ErrorBoundary>
    );
}

export default App;
