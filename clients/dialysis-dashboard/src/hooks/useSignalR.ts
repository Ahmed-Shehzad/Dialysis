import * as SignalR from "@microsoft/signalr";
import { useCallback, useEffect, useRef, useState } from "react";
import { getAuthToken } from "../auth/auth-token";
import type {
    AlarmRecordedMessage,
    ObservationRecordedMessage,
    SignalRTransportEnvelope,
} from "../types";

const HUB_URL = "/transponder/transport";
const SEND_METHOD = "Send";

function decodeEnvelopeBody(body: number[] | string): unknown {
    let bytes: Uint8Array;
    if (typeof body === "string") {
        const binary = atob(body);
        bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++)
            bytes[i] = binary.codePointAt(i) ?? 0;
    } else {
        bytes = new Uint8Array(body);
    }
    const decoder = new TextDecoder();
    const json = decoder.decode(bytes);
    return JSON.parse(json) as unknown;
}

function parseObservationMessage(
    data: unknown,
): ObservationRecordedMessage | null {
    const obj = data as Record<string, unknown>;
    if (
        obj &&
        typeof obj.sessionId === "string" &&
        typeof obj.code === "string"
    ) {
        return {
            sessionId: obj.sessionId,
            observationId:
                typeof obj.observationId === "string" ? obj.observationId : "",
            code: obj.code,
            value: obj.value as string | undefined,
            unit: obj.unit as string | undefined,
            subId: obj.subId as string | undefined,
            channelName: obj.channelName as string | undefined,
        };
    }
    return null;
}

function parseAlarmMessage(data: unknown): AlarmRecordedMessage | null {
    const obj = data as Record<string, unknown>;
    if (obj && typeof obj.alarmId === "string") {
        return {
            alarmId: obj.alarmId,
            alarmType: typeof obj.alarmType === "string" ? obj.alarmType : undefined,
            eventPhase: typeof obj.eventPhase === "string" ? obj.eventPhase : "",
            alarmState: typeof obj.alarmState === "string" ? obj.alarmState : "",
            deviceId: typeof obj.deviceId === "string" ? obj.deviceId : undefined,
            sessionId: typeof obj.sessionId === "string" ? obj.sessionId : undefined,
            occurredAt:
                typeof obj.occurredAt === "string"
                    ? obj.occurredAt
                    : new Date().toISOString(),
        };
    }
    return null;
}

export interface UseSignalROptions {
    /** Called when an observation is received (for stats invalidation etc.) */
    onObservation?: (obs: ObservationRecordedMessage) => void;
    /** Called when an alarm is received (for stats invalidation etc.) */
    onAlarm?: (alarm: AlarmRecordedMessage) => void;
}

export function useSignalR(
    sessionId: string | null,
    options?: UseSignalROptions,
) {
    const { onObservation, onAlarm } = options ?? {};
    const connectionRef = useRef<SignalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);
    const [observations, setObservations] = useState<
        ObservationRecordedMessage[]
    >([]);
    const [alarms, setAlarms] = useState<AlarmRecordedMessage[]>([]);
    const [error, setError] = useState<string | null>(null);

    const addObservation = useCallback(
        (obs: ObservationRecordedMessage) => {
            const withTimestamp = { ...obs, _receivedAt: Date.now() };
            setObservations((prev) => [...prev, withTimestamp].slice(-500));
            onObservation?.(obs);
        },
        [onObservation],
    );

    const addAlarm = useCallback(
        (alarm: AlarmRecordedMessage) => {
            setAlarms((prev) => [...prev, alarm].slice(-100));
            onAlarm?.(alarm);
        },
        [onAlarm],
    );

    useEffect(() => {
        if (!sessionId?.trim()) return;

        const token = getAuthToken();
        const url = token
            ? `${HUB_URL}?access_token=${encodeURIComponent(token)}`
            : HUB_URL;

        const connection = new SignalR.HubConnectionBuilder()
            .withUrl(url, { withCredentials: false })
            .withAutomaticReconnect()
            .build();

        connection.on(SEND_METHOD, (envelope: SignalRTransportEnvelope) => {
            try {
                const data = decodeEnvelopeBody(envelope.body);
                const msgType = envelope?.messageType ?? "";
                if (msgType.includes("ObservationRecorded")) {
                    const obs = parseObservationMessage(data);
                    if (obs?.sessionId === sessionId) addObservation(obs);
                } else if (msgType.includes("AlarmRecorded")) {
                    const alarm = parseAlarmMessage(data);
                    if (
                        alarm &&
                        (alarm.sessionId === sessionId || !alarm.sessionId)
                    )
                        addAlarm(alarm);
                }
            } catch {
                // ignore parse errors
            }
        });

        connection
            .start()
            .then(async () => {
                await connection.invoke("JoinGroup", `session:${sessionId}`);
                setIsConnected(true);
                setError(null);
            })
            .catch((e: Error) => setError(e.message));

        connectionRef.current = connection;

        return () => {
            connection.stop().catch(() => {});
            connectionRef.current = null;
            setIsConnected(false);
        };
    }, [sessionId, addObservation, addAlarm]);

    const clearData = useCallback(() => {
        setObservations([]);
        setAlarms([]);
    }, []);

    return { isConnected, observations, alarms, error, clearData };
}
