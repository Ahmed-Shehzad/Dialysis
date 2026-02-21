import { useAlerts } from "../hooks/useAlerts";
import type { Alert } from "../types";

interface AlertsPanelProps {
    sessionId: string | null;
}

const SEVERITY_STYLES: Record<
    Alert["severity"],
    { bg: string; border: string; badge: string; icon: string }
> = {
    critical: {
        bg: "bg-red-50",
        border: "border-red-200",
        badge: "bg-red-600 text-white",
        icon: "🔴",
    },
    warning: {
        bg: "bg-amber-50",
        border: "border-amber-200",
        badge: "bg-amber-600 text-white",
        icon: "🟡",
    },
    info: {
        bg: "bg-blue-50",
        border: "border-blue-200",
        badge: "bg-blue-600 text-white",
        icon: "🔵",
    },
};

export function AlertsPanel({ sessionId }: Readonly<AlertsPanelProps>) {
    const { alerts, acknowledge, isLoading } = useAlerts(sessionId);

    if (!sessionId) return null;

    if (isLoading && alerts.length === 0) {
        return (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-500">
                Loading alerts…
            </div>
        );
    }

    if (alerts.length === 0) {
        return (
            <div className="rounded-lg border border-emerald-200 bg-emerald-50/50 p-4 text-sm text-emerald-800">
                No active alerts
            </div>
        );
    }

    const unackedCritical = alerts.filter((a) => a.severity === "critical" && !a.acknowledged);

    return (
        <div className="rounded-lg border border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-4 py-2">
                <h3 className="m-0 text-sm font-semibold text-slate-800">
                    Alerts & Exceptions
                    {unackedCritical.length > 0 && (
                        <span className="ml-2 rounded bg-red-600 px-2 py-0.5 text-xs text-white">
                            {unackedCritical.length} critical
                        </span>
                    )}
                </h3>
            </div>
            <ul className="divide-y divide-slate-100">
                {alerts.map((alert) => (
                    <AlertRow
                        key={alert.id}
                        alert={alert}
                        onAcknowledge={() => acknowledge(alert.id)}
                    />
                ))}
            </ul>
        </div>
    );
}

function AlertRow({
    alert,
    onAcknowledge,
}: {
    alert: Alert;
    onAcknowledge: () => void;
}) {
    const style = SEVERITY_STYLES[alert.severity];
    const needsAck = alert.severity === "critical" && !alert.acknowledged;

    return (
        <li
            className={`flex items-start gap-3 px-4 py-3 ${style.bg} ${style.border} ${needsAck ? "border-l-4" : ""}`}
        >
            <span className="text-lg" aria-hidden>
                {style.icon}
            </span>
            <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                    <span
                        className={`rounded px-1.5 py-0.5 text-xs font-medium ${style.badge}`}
                    >
                        {alert.severity}
                    </span>
                    <span className="font-medium text-slate-800">{alert.title}</span>
                </div>
                {alert.detail && (
                    <p className="mt-0.5 text-sm text-slate-600">{alert.detail}</p>
                )}
                <div className="mt-2 flex flex-wrap gap-2">
                    {alert.actionLabel && (
                        <a
                            href={alert.actionLink ?? "#"}
                            onClick={(e) => {
                                if (!alert.actionLink) e.preventDefault();
                            }}
                            className="text-sm font-medium text-blue-600 hover:underline"
                        >
                            {alert.actionLabel}
                        </a>
                    )}
                    {needsAck && (
                        <button
                            type="button"
                            onClick={onAcknowledge}
                            className="rounded bg-slate-700 px-2 py-1 text-xs font-medium text-white hover:bg-slate-800"
                        >
                            Acknowledge
                        </button>
                    )}
                    {alert.acknowledged && (
                        <span className="text-xs text-slate-500">Acknowledged</span>
                    )}
                </div>
            </div>
        </li>
    );
}
