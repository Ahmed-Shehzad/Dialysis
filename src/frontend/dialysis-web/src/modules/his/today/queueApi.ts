import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from "@tanstack/react-query";
import { apiClient } from "@/lib/api/apiClient";

/**
 * The receptionist's day is divided into three buckets. A patient transitions
 *   Expected -> Waiting (after check-in) -> InTreatment (after a chair is assigned).
 */
export type QueueStatus = "expected" | "waiting" | "in-treatment";

export interface QueueEntry {
  id: string;
  patientId: string;
  patientName: string;
  mrn: string;
  scheduledForUtc: string;
  status: QueueStatus;
  chair?: string;
  eligibilityVerified: boolean;
}

export interface CheckInRequest {
  entryId: string;
  arrivalTime: string;
  eligibilityAcknowledged: boolean;
}

export interface AssignChairRequest {
  entryId: string;
  chair: string;
}

export interface RegisterWalkInRequest {
  patientName: string;
  mrn: string;
  eligibilityVerified: boolean;
}

/** Fixed chair roster for the demo. Replace with a server-driven list when one exists. */
export const ALL_CHAIRS: readonly string[] = [
  "Chair 1",
  "Chair 2",
  "Chair 3",
  "Chair 4",
  "Chair 5",
  "Chair 6",
  "Chair 7",
  "Chair 8",
];

/** Chairs not currently occupied, computed from a snapshot of today's queue. */
export const freeChairs = (entries: readonly QueueEntry[]): readonly string[] => {
  const occupied = new Set(
    entries.filter((e) => e.status === "in-treatment" && e.chair).map((e) => e.chair),
  );
  return ALL_CHAIRS.filter((c) => !occupied.has(c));
};

const QUEUE_QUERY_KEY = ["his", "todays-queue"] as const;

// HIS wraps successful responses in the HATEOAS envelope { data, links }. Same helper as
// the rest of the HIS calls in `features/his/api/hisApi.ts` — kept inline here to avoid a
// cross-feature dependency just for one type alias.
type HateoasEnvelope<T> = { data: T; links: unknown[] };

const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

const fetchTodaysQueue = async (): Promise<readonly QueueEntry[]> => {
  const response = await apiClient.get<HateoasEnvelope<QueueEntry[]>>(
    "/api/his/api/v1.0/patient-flow/todays-queue",
  );
  return unwrap(response.data);
};

const submitCheckIn = async (req: CheckInRequest): Promise<{ id: string }> => {
  const response = await apiClient.post<HateoasEnvelope<{ id: string }>>(
    "/api/his/api/v1.0/patient-flow/queue/check-in",
    {
      entryId: req.entryId,
      arrivalTimeUtc: req.arrivalTime,
      eligibilityAcknowledged: req.eligibilityAcknowledged,
    },
  );
  return unwrap(response.data);
};

const submitAssignChair = async (req: AssignChairRequest): Promise<{ id: string }> => {
  const response = await apiClient.post<HateoasEnvelope<{ id: string }>>(
    "/api/his/api/v1.0/patient-flow/queue/assign-chair",
    req,
  );
  return unwrap(response.data);
};

const submitRegisterWalkIn = async (req: RegisterWalkInRequest): Promise<QueueEntry> => {
  const response = await apiClient.post<HateoasEnvelope<QueueEntry>>(
    "/api/his/api/v1.0/patient-flow/queue/walk-in",
    req,
  );
  return unwrap(response.data);
};

export const useTodaysQueue = (): UseQueryResult<readonly QueueEntry[]> =>
  useQuery({
    queryKey: QUEUE_QUERY_KEY,
    queryFn: fetchTodaysQueue,
    staleTime: 15_000,
  });

interface MutationContext {
  previous?: readonly QueueEntry[];
}

/**
 * Helper for the three queue mutations. Each one swaps in an optimistic version of the
 * cache, rolls back on error, and invalidates on settle — the same shape, only the
 * `applyOptimistic` step differs.
 */
const useQueueMutation = <TReq, TRes>(
  mutationFn: (req: TReq) => Promise<TRes>,
  applyOptimistic: (entries: readonly QueueEntry[], req: TReq) => readonly QueueEntry[],
): UseMutationResult<TRes, Error, TReq, MutationContext> => {
  const queryClient = useQueryClient();
  return useMutation<TRes, Error, TReq, MutationContext>({
    mutationFn,
    onMutate: async (req) => {
      await queryClient.cancelQueries({ queryKey: QUEUE_QUERY_KEY });
      const previous = queryClient.getQueryData<readonly QueueEntry[]>(QUEUE_QUERY_KEY);
      queryClient.setQueryData<readonly QueueEntry[]>(QUEUE_QUERY_KEY, (old) =>
        old ? applyOptimistic(old, req) : old,
      );
      return { previous };
    },
    onError: (_error, _req, context) => {
      if (context?.previous) {
        queryClient.setQueryData(QUEUE_QUERY_KEY, context.previous);
      }
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: QUEUE_QUERY_KEY });
    },
  });
};

/** Moves an Expected patient to Waiting. */
export const useCheckInPatient = () =>
  useQueueMutation<CheckInRequest, { id: string }>(submitCheckIn, (entries, req) =>
    entries.map((e) =>
      e.id === req.entryId
        ? {
            ...e,
            status: "waiting",
            eligibilityVerified: e.eligibilityVerified || req.eligibilityAcknowledged,
          }
        : e,
    ),
  );

/** Moves a Waiting patient to In treatment by assigning them a chair. */
export const useAssignChair = () =>
  useQueueMutation<AssignChairRequest, { id: string }>(submitAssignChair, (entries, req) =>
    entries.map((e) =>
      e.id === req.entryId ? { ...e, status: "in-treatment", chair: req.chair } : e,
    ),
  );

/** Appends a walk-in patient directly into the Waiting column. */
export const useRegisterWalkIn = () =>
  useQueueMutation<RegisterWalkInRequest, QueueEntry>(submitRegisterWalkIn, (entries, req) => [
    ...entries,
    {
      // Synthetic id used only for the optimistic placeholder. The real id arrives via
      // the mutation's resolved entry when `onSettled` invalidates the query.
      id: `walkin-pending-${Date.now()}`,
      patientId: `p-walkin-pending-${Date.now()}`,
      patientName: req.patientName.trim(),
      mrn: req.mrn.trim(),
      scheduledForUtc: new Date().toISOString(),
      status: "waiting",
      eligibilityVerified: req.eligibilityVerified,
    },
  ]);
