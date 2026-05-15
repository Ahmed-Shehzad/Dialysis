import { useEffect, useRef, useState } from "react";
import {
  subscriptionSseUrl,
  type SubscriptionModule,
} from "@/features/subscriptions/api/subscriptionsApi";

type StreamStatus = "idle" | "connecting" | "connected" | "reconnecting" | "disconnected";

export type SubscriptionNotification = {
  /** Monotonic client-side id so React keys stay stable across re-renders. */
  seq: number;
  receivedAt: string;
  /** Parsed notification Bundle, or the raw frame text when it is not JSON. */
  payload: unknown;
};

export type UseSubscriptionStreamResult = {
  notifications: SubscriptionNotification[];
  status: StreamStatus;
  clear: () => void;
};

const MAX_BUFFER = 100;

/**
 * Opens the FHIR Subscription SSE channel for a registered subscription and buffers the
 * incoming `subscription-notification` Bundles.
 *
 * Auth note: the browser `EventSource` API cannot attach an `Authorization` header, so this
 * relies on cookie forwarding (`withCredentials`) and the dev-mode auth bypass. Token-bound
 * SSE (query-param access token) is a deliberate follow-up, mirroring the WebSocket channel.
 */
export const useSubscriptionStream = (
  module: SubscriptionModule | null,
  subscriptionId: string | null,
): UseSubscriptionStreamResult => {
  const [notifications, setNotifications] = useState<SubscriptionNotification[]>([]);
  const [status, setStatus] = useState<StreamStatus>("idle");
  const seqRef = useRef(0);

  useEffect(() => {
    if (!module || !subscriptionId) {
      setStatus("idle");
      return undefined;
    }

    let closed = false;
    setStatus("connecting");
    const source = new EventSource(subscriptionSseUrl(module, subscriptionId), {
      withCredentials: true,
    });

    const push = (raw: string) => {
      if (closed) return;
      let payload: unknown = raw;
      try {
        payload = JSON.parse(raw);
      } catch {
        // keep the raw frame — heartbeats / comments are not JSON
      }
      seqRef.current += 1;
      const entry: SubscriptionNotification = {
        seq: seqRef.current,
        receivedAt: new Date().toISOString(),
        payload,
      };
      setNotifications((prev) => {
        const next = [entry, ...prev];
        return next.length > MAX_BUFFER ? next.slice(0, MAX_BUFFER) : next;
      });
    };

    source.addEventListener("subscription-notification", (e) => push((e as MessageEvent).data));
    source.onopen = () => {
      if (!closed) setStatus("connected");
    };
    source.onerror = () => {
      if (closed) return;
      // EventSource auto-reconnects unless the connection was closed by the server.
      setStatus(source.readyState === EventSource.CLOSED ? "disconnected" : "reconnecting");
    };

    return () => {
      closed = true;
      source.close();
      setStatus("idle");
    };
  }, [module, subscriptionId]);

  return {
    notifications,
    status,
    clear: () => setNotifications([]),
  };
};
