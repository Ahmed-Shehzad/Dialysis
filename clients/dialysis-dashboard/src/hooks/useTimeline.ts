import { useMemo } from "react";
import { useQueries } from "@tanstack/react-query";
import type { AuditEventResource } from "../api";
import {
    getAlarmAuditEvents,
    getAlarmsBySession,
    getTreatmentAuditEvents,
    getTreatmentSession,
} from "../api";
import type { TimelineEvent } from "../types";

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
    const action = evt.action === "C" ? "Created" : evt.action === "R" ? "Read" : evt.action === "U" ? "Updated" : evt.action === "D" ? "Deleted" : "Executed";
    const what = evt.outcomeDesc ?? `${action} ${resourceType ?? "resource"}${resourceId ? ` ${resourceId}` : ""}`;

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
        const list: TimelineEvent[] = [];

        if (!sessionId) return list;

        const sess = session.data;

        if (sess?.startedAt) {
            list.push({
                id: `state-start-${sessionId}`,
                type: "state-transition",
                when: sess.startedAt,
                who: "System",
                what: "Session started",
                detail: "Treatment session began",
            });
        }

        if (sess?.endedAt) {
            list.push({
                id: `state-end-${sessionId}`,
                type: "state-transition",
                when: sess.endedAt,
                who: "System",
                what: "Session completed",
                detail: "Treatment session ended",
            });
        }

        const treatmentEntries = treatmentAudit.data?.entry ?? [];
        for (const e of treatmentEntries) {
            const r = e.resource;
            if (!r || r.resourceType !== "AuditEvent") continue;
            const getVal = (type: string) => {
                const d = r.entity?.[0]?.detail?.find((x: { type?: string }) => x.type === type)?.value;
                return typeof d === "string" ? d : d?.value;
            };
            const resourceId = getVal("ResourceId");
            if (resourceId !== sessionId && resourceId !== sess?.sessionId) continue;
            const evt = parseAuditEvent(r, "treatment");
            if (evt) list.push(evt);
        }

        const alarmEntries = alarmAudit.data?.entry ?? [];
        for (const e of alarmEntries) {
            const r = e.resource;
            if (!r || r.resourceType !== "AuditEvent") continue;
            const getVal = (type: string) => {
                const d = r.entity?.[0]?.detail?.find((x: { type?: string }) => x.type === type)?.value;
                return typeof d === "string" ? d : d?.value;
            };
            const resourceId = getVal("ResourceId");
            const desc = r.outcomeDesc ?? "";
            if (resourceId !== sessionId && !desc.toLowerCase().includes(sessionId.toLowerCase())) continue;
            const evt = parseAuditEvent(r, "alarm");
            if (evt) list.push(evt);
        }

        const sessionAlarms = alarms.data ?? [];
        for (const a of sessionAlarms) {
            if (a.sessionId !== sessionId) continue;
            list.push({
                id: `alarm-${a.id}`,
                type: "alarm",
                when: a.occurredAt,
                who: "Device",
                what: a.alarmType ?? a.alarmState ?? "Alarm",
                detail: `${a.eventPhase} – ${a.alarmState}`,
            });
        }

        list.sort((a, b) => new Date(b.when).getTime() - new Date(a.when).getTime());

        return list.slice(0, 50);
    }, [sessionId, session.data, treatmentAudit.data, alarmAudit.data, alarms.data]);

    return {
        events,
        isLoading: treatmentAudit.isLoading || alarmAudit.isLoading,
    };
}
