import { useCallback, useState } from "react";
import { useQueries } from "@tanstack/react-query";
import {
    getAlarmsBySession,
    getBloodLeakRisk,
    getHypotensionRisk,
    getPrescriptionComplianceCds,
    getTreatmentSession,
    getVenousPressureRisk,
} from "../api";
import { getObsValue, MDC } from "../utils/observations";
import type { Alert, AlarmContext } from "../types";

const STORAGE_KEY = "pdms:acknowledged-alerts";

function loadAcknowledged(): Set<string> {
    if (typeof window === "undefined") return new Set();
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        const ids = stored ? (JSON.parse(stored) as string[]) : [];
        return new Set(ids);
    } catch {
        return new Set();
    }
}

function saveAcknowledged(ids: Set<string>): void {
    if (typeof window === "undefined") return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...ids]));
}

interface DetectedIssueLike {
    resourceType?: string;
    id?: string;
    detail?: string;
    severity?: string;
}

function extractDetectedIssues(
    bundle: { entry?: Array<{ resource?: DetectedIssueLike }> } | null
): Array<{ id: string; detail?: string; severity?: string }> {
    if (!bundle?.entry) return [];
    return bundle.entry
        .map((e) => e.resource)
        .filter((r): r is DetectedIssueLike => r != null && r.resourceType === "DetectedIssue")
        .map((r) => ({
            id: r.id ?? "unknown",
            detail: r.detail,
            severity: r.severity,
        }));
}

function mapCdsToAlerts(
    source: string,
    issues: Array<{ id: string; detail?: string; severity?: string }>,
    type: Alert["type"],
    title: string,
    defaultSeverity: Alert["severity"],
    actionLabel?: string,
    actionPayload?: Alert["actionPayload"]
): Alert[] {
    return issues.map((issue) => ({
        id: `cds-${source}-${issue.id}`,
        type,
        severity: (issue.severity?.toLowerCase() as Alert["severity"]) ?? defaultSeverity,
        title,
        detail: issue.detail,
        actionLink: undefined,
        actionLabel,
        actionPayload,
        source: "cds",
        acknowledged: false,
    }));
}

function mapAlarmsToAlerts(alarms: AlarmContext[]): Alert[] {
    const active = alarms.filter(
        (a) =>
            a.alarmState?.toLowerCase() === "active" ||
            a.alarmState?.toLowerCase() === "latched" ||
            a.alarmState?.toLowerCase() === "acknowledged"
    );
    return active.map((a) => {
        const id = `alarm-${a.id}`;
        const priority = a.priority?.toLowerCase();
        const severity: Alert["severity"] =
            priority === "high" || priority === "critical" ? "critical" : priority === "medium" ? "warning" : "info";
        return {
            id,
            type: "device-alarm" as const,
            severity,
            title: a.alarmType ?? a.alarmState ?? "Device alarm",
            detail: `Occurred ${a.occurredAt ? new Date(a.occurredAt).toLocaleString() : ""}`,
            actionLink: undefined,
            actionLabel: "View alarm",
            source: "alarm" as const,
            occurredAt: a.occurredAt,
            acknowledged: false,
        };
    });
}

const SEVERITY_ORDER: Record<Alert["severity"], number> = {
    critical: 0,
    warning: 1,
    info: 2,
};

export function useAlerts(sessionId: string | null) {
    const [hypo, compliance, venous, bloodLeak, alarms, session] = useQueries({
        queries: [
            {
                queryKey: ["cds", "hypotension", sessionId],
                queryFn: () => getHypotensionRisk(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
            {
                queryKey: ["cds", "prescription-compliance", sessionId],
                queryFn: () => getPrescriptionComplianceCds(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
            {
                queryKey: ["cds", "venous-pressure", sessionId],
                queryFn: () => getVenousPressureRisk(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
            {
                queryKey: ["cds", "blood-leak", sessionId],
                queryFn: () => getBloodLeakRisk(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
            {
                queryKey: ["alarms", sessionId],
                queryFn: () => getAlarmsBySession(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 10_000,
            },
            {
                queryKey: ["treatment-session", sessionId],
                queryFn: () => getTreatmentSession(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
        ],
    });

    const sess = session.data;

    const alerts: Alert[] = [];

    const hypoIssues = extractDetectedIssues(hypo.data ?? null);
    if (hypoIssues.length > 0) {
        alerts.push(
            ...mapCdsToAlerts(
                "hypotension",
                hypoIssues,
                "hypotension",
                "Hypotension risk",
                "critical",
                "Check BP"
            )
        );
    }

    const complianceIssues = extractDetectedIssues(compliance.data ?? null);
    if (complianceIssues.length > 0) {
        alerts.push(
            ...mapCdsToAlerts(
                "compliance",
                complianceIssues,
                "prescription-mismatch",
                "Prescription mismatch",
                "warning",
                "Review prescription",
                sess?.patientMrn ? { patientMrn: sess.patientMrn } : undefined
            )
        );
    }

    const venousIssues = extractDetectedIssues(venous.data ?? null);
    if (venousIssues.length > 0) {
        alerts.push(
            ...mapCdsToAlerts(
                "venous",
                venousIssues,
                "prescription-mismatch",
                "Venous pressure risk",
                "critical",
                "Check access"
            )
        );
    }

    const bloodLeakIssues = extractDetectedIssues(bloodLeak.data ?? null);
    if (bloodLeakIssues.length > 0) {
        alerts.push(
            ...mapCdsToAlerts(
                "blood-leak",
                bloodLeakIssues,
                "device-alarm",
                "Blood leak detected",
                "critical",
                "Inspect circuit"
            )
        );
    }

    alerts.push(...mapAlarmsToAlerts(alarms.data ?? []));

    if (sess?.status?.toLowerCase() === "completed") {
        const obs = sess.observations ?? [];
        const postWeight = getObsValue(obs, MDC.WGT_POSTDIAL);
        if (!postWeight) {
            alerts.push({
                id: `derived-missed-doc-${sessionId}`,
                type: "missed-documentation",
                severity: "warning",
                title: "Missed documentation",
                detail: "Post-dialysis weight not recorded",
                actionLabel: "Record post-weight",
                source: "derived",
                acknowledged: false,
            });
        }
    }

    alerts.sort((a, b) => SEVERITY_ORDER[a.severity] - SEVERITY_ORDER[b.severity]);

    const [acked, setAcked] = useState<Set<string>>(loadAcknowledged);

    const acknowledge = useCallback((id: string) => {
        setAcked((prev) => {
            const next = new Set(prev);
            next.add(id);
            saveAcknowledged(next);
            return next;
        });
    }, []);

    const alertsWithAck = alerts.map((a) => ({
        ...a,
        acknowledged: acked.has(a.id) || a.acknowledged,
    }));

    return { alerts: alertsWithAck, acknowledge, isLoading: hypo.isLoading || alarms.isLoading };
}
