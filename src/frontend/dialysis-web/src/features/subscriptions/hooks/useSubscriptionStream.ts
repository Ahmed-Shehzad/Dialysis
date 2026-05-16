import { useEffect, useRef, useState } from "react";
import {
  isValidSubscriptionId,
  subscriptionSseUrl,
  type SubscriptionModule,
} from "@/features/subscriptions/api/subscriptionsApi";
import { useAuth } from "@/features/auth/components/AuthProvider";

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
 * Auth note: the browser `EventSource` API cannot attach an `Authorization` header, so the
 * bearer is passed as an `access_token` query parameter (the same pattern SignalR uses; the
 * gateway accepts it for streaming paths). `withCredentials` is also set so cookie-auth
 * deployments keep working.
 */
export const useSubscriptionStream = (
  module: SubscriptionModule | null,
  subscriptionId: string | null,
): UseSubscriptionStreamResult => {
  const { getAccessToken } = useAuth();
  const [notifications, setNotifications] = useState<SubscriptionNotification[]>([]);
  const [status, setStatus] = useState<StreamStatus>("idle");
  const seqRef = useRef(0);

  useEffect(() => {
    if (!module || !subscriptionId) {
      setStatus("idle");
      return undefined;
    }

    if (!isValidSubscriptionId(subscriptionId)) {
      // Never open an EventSource for an unvalidated id — fail closed instead of
      // writing unsanitized input into the SSE URL.
      setStatus("disconnected");
      return undefined;
    }

    let closed = false;
    setStatus("connecting");
    const source = new EventSource(subscriptionSseUrl(module, subscriptionId, getAccessToken()), {
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
  }, [module, subscriptionId, getAccessToken]);

  return {
    notifications,
    status,
    clear: () => setNotifications([]),
  };
};
