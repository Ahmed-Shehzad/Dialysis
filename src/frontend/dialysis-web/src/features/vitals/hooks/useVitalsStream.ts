import { useEffect, useRef, useState } from "react";
import { type HubConnection, HubConnectionState } from "@microsoft/signalr";
import { buildHubConnection } from "@/lib/realtime/signalrConnection";
import { useAuth } from "@/features/auth/components/AuthProvider";
import { VITALS_HUB_URL, type SessionCost, type VitalsReading } from "../api/vitalsApi";

type ConnectionStatus = "idle" | "connecting" | "connected" | "reconnecting" | "disconnected";

export type UseVitalsStreamResult = {
  readings: VitalsReading[];
  /** Latest server-computed running cost estimate for the session, or null until first tick. */
  cost: SessionCost | null;
  status: ConnectionStatus;
  reset: () => void;
};

const MAX_BUFFER = 500;

export const useVitalsStream = (sessionId: string | undefined): UseVitalsStreamResult => {
  const { getAccessToken } = useAuth();
  const connectionRef = useRef<HubConnection | null>(null);
  const [readings, setReadings] = useState<VitalsReading[]>([]);
  const [cost, setCost] = useState<SessionCost | null>(null);
  const [status, setStatus] = useState<ConnectionStatus>("idle");

  useEffect(() => {
    if (!sessionId) return undefined;

    let cancelled = false;
    const connection = buildHubConnection({
      url: VITALS_HUB_URL,
      accessTokenFactory: () => getAccessToken(),
    });
    connectionRef.current = connection;

    const handleReading = (incoming: VitalsReading) => {
      if (cancelled) return;
      setReadings((prev) => {
        const next = [...prev, incoming];
        if (next.length > MAX_BUFFER) next.splice(0, next.length - MAX_BUFFER);
        return next;
      });
    };

    const handleCost = (incoming: SessionCost) => {
      if (cancelled) return;
      setCost(incoming);
    };

    connection.on("reading", handleReading);
    connection.on("cost", handleCost);
    connection.onreconnecting(() => setStatus("reconnecting"));
    connection.onreconnected(() => setStatus("connected"));
    connection.onclose(() => setStatus("disconnected"));

    setStatus("connecting");
    connection
      .start()
      .then(async () => {
        if (cancelled) return;
        setStatus("connected");
        await connection.invoke("JoinSessionAsync", sessionId);
      })
      .catch(() => {
        if (!cancelled) setStatus("disconnected");
      });

    return () => {
      cancelled = true;
      connection.off("reading", handleReading);
      connection.off("cost", handleCost);
      if (connection.state === HubConnectionState.Connected) {
        void connection.invoke("LeaveSessionAsync", sessionId).catch(() => undefined);
      }
      void connection.stop();
      connectionRef.current = null;
    };
  }, [sessionId, getAccessToken]);

  return {
    readings,
    cost,
    status,
    reset: () => {
      setReadings([]);
      setCost(null);
    },
  };
};
