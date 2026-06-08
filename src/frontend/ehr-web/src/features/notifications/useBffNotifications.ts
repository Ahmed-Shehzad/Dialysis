import { useEffect } from "react";
import { HubConnectionState } from "@microsoft/signalr";
import { notify, type ToastKind } from "@/features/durable-commands";
import { buildHubConnection } from "@/lib/realtime/signalrConnection";
import { usePatientContext } from "@/shell/PatientContextProvider";

// BFF-owned notifications hub for this context (gateway-routed to the EHR BFF, cookie-authenticated
// on the same origin). The BFF consumes integration events off RabbitMQ and pushes them here.
const HUB_URL = "/ehr/events";

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
 * and surfaces each push as a toast. Best-effort: if the hub is unavailable (e.g. dev without
 * RabbitMQ/backplane) it silently no-ops. Re-subscribes when the selected patient changes.
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

    // Keep the start promise so cleanup can await it before stopping. Calling stop() while the
    // negotiate request is still in flight throws "The connection was stopped during negotiation"
    // — which React 18 StrictMode triggers on every mount via its immediate mount→unmount→mount,
    // snowballing into a negotiate flood that exhausts the browser's connection handles.
    const startPromise = connection
      .start()
      .then(() =>
        active && patientId ? connection.invoke("WatchPatientAsync", patientId) : undefined,
      )
      .catch(() => undefined);

    return () => {
      active = false;
      void startPromise.finally(() => {
        if (connection.state !== HubConnectionState.Disconnected) void connection.stop();
      });
    };
  }, [patientId]);
};
