import { useQuery } from "@tanstack/react-query";
import { getPatientByMrn, getTreatmentSession } from "../api";
import { useAlerts } from "../hooks/useAlerts";
import { useTimeline } from "../hooks/useTimeline";

/**
 * Minimal UI Rule: At any moment, the screen should clearly answer five questions.
 * This component provides a compact checklist for clinicians.
 */
interface FiveQuestionsSummaryProps {
    sessionId: string | null;
}

export function FiveQuestionsSummary({ sessionId }: Readonly<FiveQuestionsSummaryProps>) {
    const { data: session } = useQuery({
        queryKey: ["treatment-session", sessionId],
        queryFn: () => getTreatmentSession(sessionId!),
        enabled: Boolean(sessionId),
    });

    const { data: patient } = useQuery({
        queryKey: ["patient", session?.patientMrn],
        queryFn: () => getPatientByMrn(session!.patientMrn!),
        enabled: Boolean(session?.patientMrn),
    });

    const { alerts } = useAlerts(sessionId);
    const { events } = useTimeline(sessionId);

    const patientName = patient
        ? `${patient.firstName ?? ""} ${patient.lastName ?? ""}`.trim() || "—"
        : "—";
    const sessionStatus = session?.status ?? "—";
    const hasActiveAlerts = alerts.some((a) => (a.severity === "critical" || a.severity === "warning") && !a.acknowledged);
    const canAct = Boolean(session?.status);
    const recentCount = events.length;
    if (!sessionId) return null;

    const items = [
        {
            q: "Who is this?",
            a: patientName ?? "—",
            ok: Boolean(patientName),
        },
        {
            q: "What state is the session in?",
            a: sessionStatus ?? "—",
            ok: Boolean(sessionStatus),
        },
        {
            q: "Is the patient safe?",
            a: hasActiveAlerts ? "Review alerts" : "Yes",
            ok: !hasActiveAlerts,
        },
        {
            q: "What can I do next?",
            a: canAct ? "See workflow panel" : "—",
            ok: canAct,
        },
        {
            q: "What changed recently?",
            a: recentCount > 0 ? `${recentCount} events` : "—",
            ok: true,
        },
    ];

    return (
        <details className="rounded border border-slate-200 bg-slate-50">
            <summary className="cursor-pointer px-3 py-2 text-xs font-medium text-slate-600">
                Minimal UI checklist
            </summary>
            <ul className="list-none space-y-1 px-3 pb-2 text-xs">
                {items.map((item, i) => (
                    <li
                        key={i}
                        className={`flex items-center gap-2 ${item.ok ? "text-slate-700" : "text-amber-700"}`}
                    >
                        <span
                            className={item.ok ? "text-emerald-600" : "text-amber-600"}
                            aria-hidden
                        >
                            {item.ok ? "✓" : "○"}
                        </span>
                        <span className="text-slate-500">{item.q}</span>
                        <span className="font-medium">{item.a}</span>
                    </li>
                ))}
            </ul>
        </details>
    );
}
