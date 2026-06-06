import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/after-visit-summaries";

// Demo provider id for the authoring clinician (mirrors care-coordination DEMO_PROVIDERS).
export const DEMO_PROVIDER_ID = "11111111-1111-1111-1111-111111111111";

export type AvsLineKind = 1 | 2 | 3; // Instruction | FollowUp | ResourceLink

/** Clinician starts a draft after-visit summary; returns the new summary id. */
export const createSummary = async (body: {
  patientId: string;
  encounterRef: string;
  visitDateUtc: string;
  authoringProviderId: string;
  narrative: string;
}): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(prefix, body);
  return response.data.id;
};

/** Appends a line to a draft (kind 3 = resource link, where text is the label and url the link). */
export const addLine = async (
  summaryId: string,
  body: { kind: AvsLineKind; text: string; url?: string },
): Promise<void> => {
  await apiClient.post(`${prefix}/${summaryId}/lines`, body);
};

/** Publishes a draft to the patient portal. */
export const publishSummary = async (summaryId: string): Promise<void> => {
  await apiClient.post(`${prefix}/${summaryId}/publish`);
};
