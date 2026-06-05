import { useEffect } from "react";
import { notify, type ToastKind } from "@/features/durable-commands";
import { buildHubConnection } from "@/lib/realtime/signalrConnection";
import { usePatientContext } from "@/shell/PatientContextProvider";

// BFF-owned notifications hub for this context (gateway-routed to the HIS BFF, cookie-authenticated
// on the same origin). The BFF consumes integration events off RabbitMQ and pushes them here.
const HUB_URL = "/his/events";

/** "Go look" signal shape pushed by the BFF — metadata only; the SPA refetches via the API. */
export interface BffNotification {
  type: string;
  title: string;
  summary?: string;
  patientId?: string;
  link?: string;
  occurredOn?: string;
}

const kindFor = (type: string): ToastKind =>
  type.includes("adverse") || type.includes("alarm") ? "error" : "info";

/**
 * Opens a SignalR connection to the context's BFF notifications hub, watches the selected patient,
 * and surfaces each push as a toast. Best-effort: if the hub is unavailable it silently no-ops.
 * Re-subscribes when the selected patient changes.
 */
export const useBffNotifications = (): void => {
  const { patient } = usePatientContext();
  const patientId = patient?.id ?? null;

  useEffect(() => {
    const connection = buildHubConnection({ url: HUB_URL, accessTokenFactory: () => null });
    let active = true;

    connection.on("notification", (message: BffNotification) => {
      const text = message.summary ? `${message.title} — ${message.summary}` : message.title;
      notify({ kind: kindFor(message.type), message: text });
    });

    connection
      .start()
      .then(() =>
        active && patientId ? connection.invoke("WatchPatientAsync", patientId) : undefined,
      )
      .catch(() => undefined);

    return () => {
      active = false;
      void connection.stop();
    };
  }, [patientId]);
};
