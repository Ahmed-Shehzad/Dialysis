import { apiClient } from "@/lib/api/apiClient";

type HateoasEnvelope<T> = { data: T; links: unknown[] };
const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

const BASE = "/hie/api/v1.0/hie/mpi";

export type PatientLinkReview = {
  id: string;
  sourceEntryId: string;
  sourcePartnerId: string;
  sourceLabel: string;
  candidateEntryId: string;
  candidatePartnerId: string;
  candidateLabel: string;
  score: number;
  grade: string;
  createdAtUtc: string;
};

export const fetchPendingReviews = async (take = 100): Promise<PatientLinkReview[]> => {
  const response = await apiClient.get<HateoasEnvelope<PatientLinkReview[]> | PatientLinkReview[]>(
    `${BASE}/reviews`,
    { params: { take } },
  );
  return unwrap(response.data);
};

/** Steward adjudication: link = same person, otherwise the records are distinct. */
export const resolveReview = async (
  reviewId: string,
  link: boolean,
  note?: string,
): Promise<void> => {
  await apiClient.post(`${BASE}/reviews/${reviewId}/resolve`, { link, note: note ?? null });
};
