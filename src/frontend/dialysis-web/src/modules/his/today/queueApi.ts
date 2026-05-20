import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from "@tanstack/react-query";

/**
 * The receptionist's day is divided into three buckets. A patient transitions
 *   Expected → Waiting (after check-in) → InTreatment (after a chair is assigned).
 */
export type QueueStatus = "expected" | "waiting" | "in-treatment";

export interface QueueEntry {
  /** Stable id of the queue row (not the patient id — a patient may appear once per visit). */
  id: string;
  patientId: string;
  patientName: string;
  /** Medical record number, surfaced beside the name in the UI. */
  mrn: string;
  /** Scheduled arrival time, ISO 8601. */
  scheduledFor: string;
  status: QueueStatus;
  /** Populated once a chair has been assigned. */
  chair?: string;
  /** True when insurance eligibility has been verified for today. */
  eligibilityVerified: boolean;
}

export interface CheckInRequest {
  entryId: string;
  /** ISO timestamp the patient arrived. Defaults to "now" in the UI. */
  arrivalTime: string;
  /**
   * Set when the receptionist has explicitly acknowledged that insurance eligibility was
   * confirmed at the counter (only meaningful for entries that were not pre-verified).
   */
  eligibilityAcknowledged: boolean;
}

export interface AssignChairRequest {
  entryId: string;
  chair: string;
}

export interface RegisterWalkInRequest {
  patientName: string;
  mrn: string;
  /** True when insurance eligibility was confirmed at the counter before submitting. */
  eligibilityVerified: boolean;
}

/** Fixed chair roster for the demo. Replace with `/api/his/.../chairs` once it lands. */
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

/** Computes which chairs are still free, given a snapshot of today's queue. */
export const freeChairs = (entries: readonly QueueEntry[]): readonly string[] => {
  const occupied = new Set(
    entries.filter((e) => e.status === "in-treatment" && e.chair).map((e) => e.chair),
  );
  return ALL_CHAIRS.filter((c) => !occupied.has(c));
};

const QUEUE_QUERY_KEY = ["his", "todays-queue"] as const;

// TODO(his): replace with a real endpoint once HIS exposes /todays-queue + /check-in. Until
// then this drives the UI from a small mutable store so the receptionist can step through
// the workflow end-to-end (expected → waiting). The store is module-private; swap both
// `fetchTodaysQueue` and `submitCheckIn` for `apiClient` calls and the rest of the file
// (hooks, query key, optimistic update) is unchanged.
const SEED_ENTRIES: readonly QueueEntry[] = [
  {
    id: "q-1",
    patientId: "p-001",
    patientName: "Anna Müller",
    mrn: "MRN-10421",
    scheduledFor: "2026-05-20T08:00:00Z",
    status: "expected",
    eligibilityVerified: true,
  },
  {
    id: "q-2",
    patientId: "p-002",
    patientName: "Erik Larsen",
    mrn: "MRN-10433",
    scheduledFor: "2026-05-20T08:30:00Z",
    status: "expected",
    eligibilityVerified: false,
  },
  {
    id: "q-3",
    patientId: "p-003",
    patientName: "Priya Shah",
    mrn: "MRN-10448",
    scheduledFor: "2026-05-20T08:45:00Z",
    status: "waiting",
    eligibilityVerified: true,
  },
  {
    id: "q-4",
    patientId: "p-004",
    patientName: "Liam O'Connor",
    mrn: "MRN-10455",
    scheduledFor: "2026-05-20T08:50:00Z",
    status: "waiting",
    eligibilityVerified: true,
  },
  {
    id: "q-5",
    patientId: "p-005",
    patientName: "Sofia Rossi",
    mrn: "MRN-10412",
    scheduledFor: "2026-05-20T07:30:00Z",
    status: "in-treatment",
    chair: "Chair 4",
    eligibilityVerified: true,
  },
  {
    id: "q-6",
    patientId: "p-006",
    patientName: "Henrik Berg",
    mrn: "MRN-10401",
    scheduledFor: "2026-05-20T07:30:00Z",
    status: "in-treatment",
    chair: "Chair 7",
    eligibilityVerified: true,
  },
];

const mockStore: { entries: QueueEntry[] } = {
  entries: SEED_ENTRIES.map((e) => ({ ...e })),
};

const fetchTodaysQueue = (): Promise<readonly QueueEntry[]> =>
  // Return a shallow copy so callers can't mutate the store directly via the query cache.
  Promise.resolve(mockStore.entries.map((e) => ({ ...e })));

const submitCheckIn = (req: CheckInRequest): Promise<QueueEntry> =>
  // Tiny simulated latency so the optimistic update + pending state are observable.
  new Promise((resolve, reject) => {
    globalThis.setTimeout(() => {
      const entry = mockStore.entries.find((e) => e.id === req.entryId);
      if (!entry) {
        reject(new Error("Queue entry not found"));
        return;
      }
      if (entry.status !== "expected") {
        reject(new Error("Patient is no longer expected — refresh and try again."));
        return;
      }
      entry.status = "waiting";
      entry.eligibilityVerified = entry.eligibilityVerified || req.eligibilityAcknowledged;
      resolve({ ...entry });
    }, 250);
  });

const submitAssignChair = (req: AssignChairRequest): Promise<QueueEntry> =>
  new Promise((resolve, reject) => {
    globalThis.setTimeout(() => {
      const entry = mockStore.entries.find((e) => e.id === req.entryId);
      if (!entry) {
        reject(new Error("Queue entry not found"));
        return;
      }
      if (entry.status !== "waiting") {
        reject(new Error("Patient is not waiting for a chair right now."));
        return;
      }
      const occupied = mockStore.entries.some(
        (e) => e.status === "in-treatment" && e.chair === req.chair,
      );
      if (occupied) {
        reject(new Error(`${req.chair} is already in use.`));
        return;
      }
      entry.status = "in-treatment";
      entry.chair = req.chair;
      resolve({ ...entry });
    }, 250);
  });

let nextWalkInSeq = 1;

const submitRegisterWalkIn = (req: RegisterWalkInRequest): Promise<QueueEntry> =>
  new Promise((resolve, reject) => {
    globalThis.setTimeout(() => {
      const name = req.patientName.trim();
      const mrn = req.mrn.trim();
      if (!name || !mrn) {
        reject(new Error("Patient name and MRN are required."));
        return;
      }
      const id = `walkin-${nextWalkInSeq++}`;
      const entry: QueueEntry = {
        id,
        patientId: `p-walkin-${id}`,
        patientName: name,
        mrn,
        scheduledFor: new Date().toISOString(),
        // Walk-ins skip the Expected column — they arrived without an appointment.
        status: "waiting",
        eligibilityVerified: req.eligibilityVerified,
      };
      mockStore.entries.push(entry);
      resolve({ ...entry });
    }, 250);
  });

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
 * `applyOptimistic` step differs. Pulling it out keeps the call sites declarative and
 * guarantees rollback semantics stay in lockstep across mutations.
 */
const useQueueMutation = <TReq>(
  mutationFn: (req: TReq) => Promise<QueueEntry>,
  applyOptimistic: (entries: readonly QueueEntry[], req: TReq) => readonly QueueEntry[],
): UseMutationResult<QueueEntry, Error, TReq, MutationContext> => {
  const queryClient = useQueryClient();
  return useMutation<QueueEntry, Error, TReq, MutationContext>({
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
  useQueueMutation<CheckInRequest>(submitCheckIn, (entries, req) =>
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
  useQueueMutation<AssignChairRequest>(submitAssignChair, (entries, req) =>
    entries.map((e) =>
      e.id === req.entryId ? { ...e, status: "in-treatment", chair: req.chair } : e,
    ),
  );

/** Appends a walk-in patient directly into the Waiting column. */
export const useRegisterWalkIn = () =>
  useQueueMutation<RegisterWalkInRequest>(submitRegisterWalkIn, (entries, req) => [
    ...entries,
    {
      // Synthetic id used only for the optimistic placeholder — the real id arrives via
      // the mutation's resolved entry when `onSettled` invalidates the query.
      id: `walkin-pending-${Date.now()}`,
      patientId: `p-walkin-pending-${Date.now()}`,
      patientName: req.patientName.trim(),
      mrn: req.mrn.trim(),
      scheduledFor: new Date().toISOString(),
      status: "waiting",
      eligibilityVerified: req.eligibilityVerified,
    },
  ]);
