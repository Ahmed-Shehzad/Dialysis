import { apiClient } from "@/lib/api/apiClient";

/**
 * FHIR R4 Subscription Backport endpoints are mapped by the building block on each module
 * host at `/fhir/...`. hie-web reaches them through the hie-bff cross-context aggregation
 * (`/hie/api/_x/{module}/{rest}` → `{module}-api/{rest}`, bearer-forwarded), so
 * `/hie/api/_x/ehr/fhir/SubscriptionTopic` hits the EHR host's `/fhir/SubscriptionTopic`.
 * Each module publishes its own SubscriptionTopic catalog and broadcasts matching events.
 */
export const SUBSCRIPTION_MODULES = ["his", "ehr", "pdms"] as const;
export type SubscriptionModule = (typeof SUBSCRIPTION_MODULES)[number];

export const MODULE_LABELS: Record<SubscriptionModule, string> = {
  his: "HIS — admissions",
  ehr: "EHR — lab results",
  pdms: "PDMS — adverse events",
};

const fhirBase = (module: SubscriptionModule) => `/hie/api/_x/${module}/fhir`;

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

/** `POST /fhir/{module}/subscription/$simulate` body. */
export type SimulateEventRequest = {
  topic: string;
  /** Match attributes — pass the subscription's filters so the simulated event matches it. */
  attributes?: Record<string, string>;
  subject?: string;
  note?: string;
};

export type SimulateEventResult = {
  topic: string;
  matched: number;
  resourceType: string;
};

/**
 * Fires a synthetic event through the real matcher + dispatcher so the live SSE/WebSocket
 * feed can be demonstrated without driving the upstream clinical workflow. `matched` reports
 * how many active subscriptions the event fanned out to.
 */
export const simulateSubscriptionEvent = async (
  module: SubscriptionModule,
  request: SimulateEventRequest,
): Promise<SimulateEventResult> => {
  const response = await apiClient.post<SimulateEventResult>(
    `${fhirBase(module)}/subscription/$simulate`,
    request,
    { headers: { "Content-Type": "application/json" } },
  );
  return response.data;
};

/**
 * Backend-issued subscription ids are `Guid.NewGuid().ToString("N")` — exactly 32 hex
 * characters. Validating against that shape is the contextual input check that prevents an
 * unexpected value from ever being written into the SSE URL / EventSource sink (defence in
 * depth on top of `encodeURIComponent`, guarding against DOM-based XSS).
 */
const SUBSCRIPTION_ID_PATTERN = /^[a-fA-F0-9]{32}$/;

export const isValidSubscriptionId = (value: string): boolean =>
  SUBSCRIPTION_ID_PATTERN.test(value);

/**
 * Absolute (gateway-relative) URL of the SSE channel for a registered subscription.
 * The backend holds `text/event-stream` open and pushes `event: subscription-notification`
 * frames whose `data:` is a FHIR `Bundle.type=subscription-notification`.
 *
 * Throws if {@link subscriptionId} is not a well-formed id rather than emitting a URL built
 * from unvalidated input.
 *
 * The browser `EventSource` API cannot set an `Authorization` header, so the bearer is
 * passed as an `access_token` query parameter — the same pattern SignalR uses, which the
 * gateway already accepts for streaming paths.
 */
export const subscriptionSseUrl = (
  module: SubscriptionModule,
  subscriptionId: string,
  accessToken?: string | null,
): string => {
  if (!SUBSCRIPTION_MODULES.includes(module)) {
    throw new Error(`Unknown subscription module: ${String(module)}`);
  }
  if (!isValidSubscriptionId(subscriptionId)) {
    throw new Error("Refusing to open SSE stream for a malformed subscription id.");
  }
  const token = accessToken ? `&access_token=${encodeURIComponent(accessToken)}` : "";
  return `${fhirBase(module)}/subscription/sse?subscription=${encodeURIComponent(subscriptionId)}${token}`;
};
