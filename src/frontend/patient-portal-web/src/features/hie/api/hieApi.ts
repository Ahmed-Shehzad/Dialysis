import { apiClient } from "@/lib/api/apiClient";

type HateoasEnvelope<T> = { data: T; links: unknown[] };
const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

export type ConsentDirection = "Inbound" | "Outbound" | 0 | 1 | 2;

export type ConsentDto = {
  id: string;
  patientId: string;
  partnerId: string;
  scope: string;
  direction: ConsentDirection;
  effectiveFromUtc: string;
  effectiveToUtc?: string | null;
  revokedAtUtc?: string | null;
};

export const fetchConsentsForPatient = async (patientId: string): Promise<ConsentDto[]> => {
  const response = await apiClient.get<HateoasEnvelope<ConsentDto[]>>(
    `/portal/api/_x/hie/api/v1.0/hie/consents/patient/${patientId}`,
  );
  return unwrap(response.data);
};

export const revokeConsent = async (consentId: string): Promise<void> => {
  await apiClient.delete(`/portal/api/_x/hie/api/v1.0/hie/consents/${consentId}`);
};

export type PatientMatchQuery = {
  mrn?: string;
  family?: string;
  given?: string;
  birthdate?: string;
};

export const submitFhirBundle = async (bundleJson: string, partner: string): Promise<unknown> => {
  const response = await apiClient.post("/portal/api/_x/hie/api/v1.0/fhir/Bundle", bundleJson, {
    headers: {
      "Content-Type": "application/fhir+json",
      "X-HIE-Partner": partner,
    },
    transformRequest: [(data) => data],
  });
  return response.data;
};

export const patientMatch = async (query: PatientMatchQuery): Promise<unknown> => {
  const response = await apiClient.get("/portal/api/_x/hie/api/v1.0/fhir/Patient/$match", {
    params: query,
    headers: { Accept: "application/fhir+json" },
  });
  return response.data;
};

// ──────────────────────────────────────────────────────────────────────────────
// Operator dashboard reads (Phase 3b)
// ──────────────────────────────────────────────────────────────────────────────

export type OutboundBundleStatus = 1 | 2 | 3; // Pending | Delivered | Failed

export const outboundStatusLabel = (status: OutboundBundleStatus): string => {
  switch (status) {
    case 1:
      return "Pending";
    case 2:
      return "Delivered";
    case 3:
      return "Failed";
  }
};

export type OutboundBundleDto = {
  id: string;
  patientId: string;
  resourceType: string;
  logicalId: string;
  partnerId: string;
  status: OutboundBundleStatus;
  attempts: number;
  createdAtUtc: string;
  nextAttemptAtUtc: string;
  deliveredAtUtc?: string | null;
  lastFailureReason?: string | null;
};

export const fetchOutboundBundles = async (
  statusFilter: OutboundBundleStatus | null,
  take = 50,
): Promise<OutboundBundleDto[]> => {
  const params: Record<string, string | number> = { take };
  if (statusFilter !== null) params.status = statusFilter;
  const response = await apiClient.get<HateoasEnvelope<OutboundBundleDto[]>>(
    "/portal/api/_x/hie/api/v1.0/hie/ops/outbound",
    { params },
  );
  return unwrap(response.data);
};

export const retryOutboundBundle = async (bundleId: string): Promise<void> => {
  await apiClient.post(`/portal/api/_x/hie/api/v1.0/hie/ops/outbound/${bundleId}/retry`);
};

export type InboundResourceDto = {
  id: string;
  partnerId: string;
  resourceType: string;
  logicalId: string;
  receivedAtUtc: string;
  validationOutcome?: string | null;
};

export const fetchInboundResources = async (
  partnerId: string | null,
  take = 50,
): Promise<InboundResourceDto[]> => {
  const params: Record<string, string | number> = { take };
  if (partnerId) params.partnerId = partnerId;
  const response = await apiClient.get<HateoasEnvelope<InboundResourceDto[]>>(
    "/portal/api/_x/hie/api/v1.0/hie/ops/inbound",
    { params },
  );
  return unwrap(response.data);
};

export type PartnerStatusDto = {
  partnerId: string;
  baseUrl: string;
  hasBearerToken: boolean;
  timeoutSeconds: number;
  isConfigured: boolean;
};

export const fetchPartners = async (): Promise<PartnerStatusDto[]> => {
  const response = await apiClient.get<HateoasEnvelope<PartnerStatusDto[]>>(
    "/portal/api/_x/hie/api/v1.0/hie/ops/partners",
  );
  return unwrap(response.data);
};

// ──────────────────────────────────────────────────────────────────────────────
// Patient self-access — my Community Health Record (Phase 5d, IndividualAccessServices)
// ──────────────────────────────────────────────────────────────────────────────

export type InsightsCounts = {
  encounters: number;
  observations: number;
  documents: number;
  procedures: number;
  medications: number;
  allergies: number;
  problems: number;
  other: number;
  total: number;
};

export type InsightsItem = {
  resourceType: string;
  date?: string | null;
  sourceOrganization: string;
  display?: string | null;
};

export type AllergyConflictAlert = {
  medicationDisplay: string;
  allergyDisplay: string;
  sources: string[];
};

export type PatientInsightsSummary = {
  patientReference: string;
  sourceOrganizations: string[];
  lastUpdatedUtc?: string | null;
  counts: InsightsCounts;
  recent: InsightsItem[];
  medications: InsightsItem[];
  allergies: InsightsItem[];
  problems: InsightsItem[];
  allergyConflictAlerts: AllergyConflictAlert[];
};

/** The caller's own outside records — patient self-access, gated by the patient's identity claim. */
export const fetchMyInsights = async (patientId: string): Promise<PatientInsightsSummary> => {
  const response = await apiClient.get<HateoasEnvelope<PatientInsightsSummary>>(
    `/portal/api/_x/hie/api/v1.0/hie/patient-access/insights/patient/${encodeURIComponent(patientId)}`,
  );
  return unwrap(response.data);
};
