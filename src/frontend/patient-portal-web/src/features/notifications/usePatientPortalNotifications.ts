import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { notify, type ToastKind } from "@/features/durable-commands";
import { buildHubConnection } from "@/lib/realtime/signalrConnection";

// The portal BFF's notifications hub (gateway-routed to the PatientPortal BFF, cookie-authenticated on
// the portal origin). The BFF consumes the patient-facing integration events off RabbitMQ and pushes
// them here, scoped to the patient's group.
const HUB_URL = "/portal/events";

/** "Go look" signal shape pushed by the BFF — metadata only; the SPA refetches via the API. */
interface BffNotification {
  type: string;
  title: string;
  summary?: string;
  patientId?: string;
  link?: string;
}

const kindFor = (type: string): ToastKind =>
  type.includes("adverse") || type.includes("alarm") ? "error" : "info";

/**
 * Opens a SignalR connection to the portal BFF notifications hub, watches the signed-in patient, and
 * surfaces each push as a toast — then invalidates the relevant query so the panel refetches. Best
 * effort: if the hub is unavailable (e.g. dev without RabbitMQ) it silently no-ops.
 */
export const usePatientPortalNotifications = (patientId: string | null): void => {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!patientId) return;
    const connection = buildHubConnection({ url: HUB_URL, accessTokenFactory: () => null });
    let active = true;

    connection.on("notification", (message: BffNotification) => {
      const text = message.summary ? `${message.title} — ${message.summary}` : message.title;
      notify({ kind: kindFor(message.type), message: text });
      if (message.type === "secure-message") {
        void queryClient.invalidateQueries({ queryKey: ["patient-portal", "messages", patientId] });
      } else if (message.type === "appointment-request") {
        void queryClient.invalidateQueries({
          queryKey: ["patient-portal", "appointment-requests", patientId],
        });
      }
    });

    connection
      .start()
      .then(() => (active ? connection.invoke("WatchPatientAsync", patientId) : undefined))
      .catch(() => undefined);

    return () => {
      active = false;
      void connection.stop();
    };
  }, [patientId, queryClient]);
};
