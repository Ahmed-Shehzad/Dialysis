import { apiClient } from "@/lib/api/apiClient";

/**
 * FHIR R4 Subscription Backport endpoints are mapped by the building block on each module
 * host at `/fhir/...` and exposed through the gateway under `/fhir/{module}/...`
 * (YARP routes `fhir-his`, `fhir-ehr`, `fhir-pdms`). Each module publishes its own
 * SubscriptionTopic catalog and broadcasts matching integration events.
 */
export const SUBSCRIPTION_MODULES = ["his", "ehr", "pdms"] as const;
export type SubscriptionModule = (typeof SUBSCRIPTION_MODULES)[number];

export const MODULE_LABELS: Record<SubscriptionModule, string> = {
  his: "HIS — admissions",
  ehr: "EHR — lab results",
  pdms: "PDMS — adverse events",
};

const fhirBase = (module: SubscriptionModule) => `/fhir/${module}`;

/** `GET /fhir/{module}/SubscriptionTopic` — the per-host topic catalog. */
export type SubscriptionTopic = {
  url: string;
  title: string;
  description: string;
  filterParameterNames: string[];
};

/** `POST /fhir/{module}/Subscription` body. Mirrors the backend SubscriptionCreateRequest. */
export type CreateSubscriptionRequest = {
  topic: string;
  channelType: "ServerSentEvents" | "WebSocket" | "RestHook" | "Email" | "Sms";
  channelEndpoint: string;
  secret?: string;
  filters?: Record<string, string>;
};

/** `FhirSubscriptionRegistration` — enums may serialize as string or number; keep both. */
export type SubscriptionRegistration = {
  id: string;
  topicUrl: string;
  channelType: string | number;
  channelEndpoint: string;
  channelHeader: string | null;
  filterParameters: Record<string, string>;
  status: string | number;
  consecutiveFailures: number;
};

export const listSubscriptionTopics = async (
  module: SubscriptionModule,
): Promise<SubscriptionTopic[]> => {
  const response = await apiClient.get<SubscriptionTopic[]>(
    `${fhirBase(module)}/SubscriptionTopic`,
  );
  return response.data ?? [];
};

export const createSubscription = async (
  module: SubscriptionModule,
  request: CreateSubscriptionRequest,
): Promise<SubscriptionRegistration> => {
  const response = await apiClient.post<SubscriptionRegistration>(
    `${fhirBase(module)}/Subscription`,
    request,
    { headers: { "Content-Type": "application/json" } },
  );
  return response.data;
};

export const deleteSubscription = async (module: SubscriptionModule, id: string): Promise<void> => {
  await apiClient.delete(`${fhirBase(module)}/Subscription/${id}`);
};

/**
 * Absolute (gateway-relative) URL of the SSE channel for a registered subscription.
 * The backend holds `text/event-stream` open and pushes `event: subscription-notification`
 * frames whose `data:` is a FHIR `Bundle.type=subscription-notification`.
 */
export const subscriptionSseUrl = (module: SubscriptionModule, subscriptionId: string): string =>
  `${fhirBase(module)}/subscription/sse?subscription=${encodeURIComponent(subscriptionId)}`;
