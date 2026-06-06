import { apiClient } from "@/lib/api/apiClient";

const prefix = "/portal/api/_x/ehr/api/v1.0/after-visit-summaries";

export type AvsResourceLink = { label: string; url: string };

export type AfterVisitSummary = {
  id: string;
  patientId: string;
  visitDateUtc: string;
  narrative: string;
  status: string;
  publishedAtUtc?: string | null;
  instructions: string[];
  followUps: string[];
  resourceLinks: AvsResourceLink[];
};

/** The patient's own published after-visit summaries, newest visit first. */
export const fetchMyAfterVisitSummaries = async (
  patientId: string,
): Promise<AfterVisitSummary[]> => {
  const response = await apiClient.get<AfterVisitSummary[]>(`${prefix}/patients/${patientId}`);
  return response.data ?? [];
};
