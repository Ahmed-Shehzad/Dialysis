import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import type { AuditEventBundle, AuditEventResource } from "../api";
import {
    getAlarmAuditEvents,
    getAlarmsBySession,
    getTreatmentAuditEvents,
    getTreatmentSession,
} from "../api";
import type { AlarmContext, TimelineEvent, TreatmentSessionContext } from "../types";

function getAuditDetailValue(resource: AuditEventResource, detailType: string): string | undefined {
    const d = resource.entity?.[0]?.detail?.find((x: { type?: string }) => x.type === detailType)
        ?.value;
    return typeof d === "string" ? d : d?.value;
}

function fhirAuditActionToLabel(action: string | undefined): string {
    switch (action) {
        case "C":
            return "Created";
        case "R":
            return "Read";
        case "U":
            return "Updated";
        case "D":
            return "Deleted";
        default:
            return "Executed";
    }
}

function parseAuditEvent(evt: AuditEventResource, source: string): TimelineEvent | null {
    const recorded = evt.recorded;
    if (!recorded) return null;

    const entity = evt.entity?.[0];
    const getDetailValue = (type: string): string | undefined => {
        const d = entity?.detail?.find((x: { type?: string }) => x.type === type);
        const v = d?.value;
        return typeof v === "string" ? v : v?.value;
    };
    const resourceType = entity?.name ?? getDetailValue("ResourceType");
    const resourceId = getDetailValue("ResourceId");
    const who = evt.agent?.[0]?.name ?? evt.agent?.[0]?.altId ?? "System";
    const action = fhirAuditActionToLabel(evt.action);
    const resourceIdSuffix = resourceId ? ` ${resourceId}` : "";
    const what =
        evt.outcomeDesc ?? `${action} ${resourceType ?? "resource"}${resourceIdSuffix}`;

    return {
        id: `audit-${source}-${recorded}-${resourceId ?? Math.random()}`,
        type: "audit",
        when: recorded,
        who,
        what,
        detail: evt.outcomeDesc,
        resourceType: resourceType ?? undefined,
        resourceId: resourceId ?? undefined,
    };
}

function sessionLifecycleEvents(
    sessionId: string,
    session: TreatmentSessionContext | null | undefined,
): TimelineEvent[] {
    const out: TimelineEvent[] = [];
    if (session?.startedAt) {
        out.push({
            id: `state-start-${sessionId}`,
            type: "state-transition",
            when: session.startedAt,
            who: "System",
            what: "Session started",
            detail: "Treatment session began",
        });
    }
    if (session?.endedAt) {
        out.push({
            id: `state-end-${sessionId}`,
            type: "state-transition",
            when: session.endedAt,
            who: "System",
            what: "Session completed",
            detail: "Treatment session ended",
        });
    }
    return out;
}

function collectTreatmentAuditTimelineEvents(
    bundle: AuditEventBundle | undefined,
    sessionId: string,
    session: TreatmentSessionContext | null | undefined,
): TimelineEvent[] {
    const out: TimelineEvent[] = [];
    for (const e of bundle?.entry ?? []) {
        const r = e.resource;
        if (r?.resourceType !== "AuditEvent") continue;
        const resourceId = getAuditDetailValue(r, "ResourceId");
        if (resourceId !== sessionId && resourceId !== session?.sessionId) continue;
        const evt = parseAuditEvent(r, "treatment");
        if (evt) out.push(evt);
    }
    return out;
}

function collectAlarmAuditTimelineEvents(
    bundle: AuditEventBundle | undefined,
    sessionId: string,
): TimelineEvent[] {
    const out: TimelineEvent[] = [];
    const sessionIdLower = sessionId.toLowerCase();
    for (const e of bundle?.entry ?? []) {
        const r = e.resource;
        if (r?.resourceType !== "AuditEvent") continue;
        const resourceId = getAuditDetailValue(r, "ResourceId");
        const desc = r.outcomeDesc ?? "";
        if (resourceId !== sessionId && !desc.toLowerCase().includes(sessionIdLower)) continue;
        const evt = parseAuditEvent(r, "alarm");
        if (evt) out.push(evt);
    }
    return out;
}

function collectLiveAlarmTimelineEvents(
    sessionAlarms: AlarmContext[] | undefined,
    sessionId: string,
): TimelineEvent[] {
    const out: TimelineEvent[] = [];
    for (const a of sessionAlarms ?? []) {
        if (a.sessionId !== sessionId) continue;
        out.push({
            id: `alarm-${a.id}`,
            type: "alarm",
            when: a.occurredAt,
            who: "Device",
            what: a.alarmType ?? a.alarmState ?? "Alarm",
            detail: `${a.eventPhase} – ${a.alarmState}`,
        });
    }
    return out;
}

function sortAndLimitTimeline(events: TimelineEvent[], limit: number): TimelineEvent[] {
    events.sort((a, b) => new Date(b.when).getTime() - new Date(a.when).getTime());
    return events.slice(0, limit);
}

export function useTimeline(sessionId: string | null) {
    const [treatmentAudit, alarmAudit, alarms, session] = useQueries({
        queries: [
            {
                queryKey: ["audit", "treatment"],
                queryFn: () => getTreatmentAuditEvents(100),
                enabled: Boolean(sessionId),
                refetchInterval: 30_000,
            },
            {
                queryKey: ["audit", "alarm"],
                queryFn: () => getAlarmAuditEvents(100),
                enabled: Boolean(sessionId),
                refetchInterval: 30_000,
            },
            {
                queryKey: ["alarms", sessionId],
                queryFn: () => getAlarmsBySession(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
            {
                queryKey: ["treatment-session", sessionId],
                queryFn: () => getTreatmentSession(sessionId!),
                enabled: Boolean(sessionId),
                refetchInterval: 15_000,
            },
        ],
    });

    const events = useMemo((): TimelineEvent[] => {
        if (!sessionId) return [];

        const sess = session.data;
        const list: TimelineEvent[] = [
            ...sessionLifecycleEvents(sessionId, sess),
            ...collectTreatmentAuditTimelineEvents(treatmentAudit.data, sessionId, sess),
            ...collectAlarmAuditTimelineEvents(alarmAudit.data, sessionId),
            ...collectLiveAlarmTimelineEvents(alarms.data, sessionId),
        ];
        return sortAndLimitTimeline(list, 50);
    }, [sessionId, session.data, treatmentAudit.data, alarmAudit.data, alarms.data]);

    return {
        events,
        isLoading: treatmentAudit.isLoading || alarmAudit.isLoading,
    };
}
