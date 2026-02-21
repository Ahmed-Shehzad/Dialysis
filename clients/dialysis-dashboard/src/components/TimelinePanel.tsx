import { useTimeline } from "../hooks/useTimeline";
import type { TimelineEvent } from "../types";

const TYPE_STYLES: Record<
    TimelineEvent["type"],
    { badge: string; icon: string }
> = {
    "state-transition": { badge: "bg-slate-600 text-white", icon: "↻" },
    audit: { badge: "bg-blue-600 text-white", icon: "📋" },
    alarm: { badge: "bg-red-600 text-white", icon: "⚠" },
    "key-event": { badge: "bg-amber-600 text-white", icon: "•" },
};

function formatWhen(iso: string): string {
    const d = new Date(iso);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffM = Math.floor(diffMs / 60000);
    const diffH = Math.floor(diffM / 60);
    const diffD = Math.floor(diffH / 24);

    if (diffM < 1) return "Just now";
    if (diffM < 60) return `${diffM}m ago`;
    if (diffH < 24) return `${diffH}h ago`;
    if (diffD < 7) return `${diffD}d ago`;
    return d.toLocaleString();
}

interface TimelinePanelProps {
    sessionId: string | null;
}

export function TimelinePanel({ sessionId }: Readonly<TimelinePanelProps>) {
    const { events, isLoading } = useTimeline(sessionId);

    if (!sessionId) return null;

    return (
        <div className="rounded-lg border border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-4 py-2">
                <h3 className="m-0 text-sm font-semibold text-slate-800">
                    Timeline & Audit
                </h3>
                <p className="m-0 mt-0.5 text-xs text-slate-500">
                    State transitions, who did what, when
                </p>
            </div>
            <div className="max-h-64 overflow-y-auto p-4">
                {isLoading && events.length === 0 ? (
                    <p className="text-sm text-slate-500">Loading…</p>
                ) : events.length === 0 ? (
                    <p className="text-sm text-slate-500">
                        No events yet
                    </p>
                ) : (
                    <ul className="space-y-3">
                        {events.map((evt) => (
                            <TimelineRow key={evt.id} event={evt} />
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
}

function TimelineRow({ event }: { event: TimelineEvent }) {
    const style = TYPE_STYLES[event.type];

    return (
        <li className="flex gap-3 text-sm">
            <span
                className={`flex h-6 w-6 shrink-0 items-center justify-center rounded text-xs ${style.badge}`}
                aria-hidden
            >
                {style.icon}
            </span>
            <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-baseline gap-2">
                    <span className="font-medium text-slate-800">
                        {event.what}
                    </span>
                    <span className="text-xs text-slate-500">
                        {formatWhen(event.when)}
                    </span>
                </div>
                {event.who && (
                    <p className="mt-0.5 text-xs text-slate-600">
                        by {event.who}
                    </p>
                )}
                {event.detail && event.detail !== event.what && (
                    <p className="mt-0.5 text-xs text-slate-500">
                        {event.detail}
                    </p>
                )}
            </div>
        </li>
    );
}
