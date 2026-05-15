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
    `/api/hie/api/v1.0/hie/consents/patient/${patientId}`,
  );
  return unwrap(response.data);
};

export const revokeConsent = async (consentId: string): Promise<void> => {
  await apiClient.delete(`/api/hie/api/v1.0/hie/consents/${consentId}`);
};

export type PatientMatchQuery = {
  mrn?: string;
  family?: string;
  given?: string;
  birthdate?: string;
};

export const submitFhirBundle = async (bundleJson: string, partner: string): Promise<unknown> => {
  const response = await apiClient.post("/api/hie/api/v1.0/fhir/Bundle", bundleJson, {
    headers: {
      "Content-Type": "application/fhir+json",
      "X-HIE-Partner": partner,
    },
    transformRequest: [(data) => data],
  });
  return response.data;
};

export const patientMatch = async (query: PatientMatchQuery): Promise<unknown> => {
  const response = await apiClient.get("/api/hie/api/v1.0/fhir/Patient/$match", {
    params: query,
    headers: { Accept: "application/fhir+json" },
  });
  return response.data;
};
