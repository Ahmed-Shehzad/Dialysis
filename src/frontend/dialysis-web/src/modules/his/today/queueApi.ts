import { useQuery, type UseQueryResult } from "@tanstack/react-query";

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

/**
 * Source of truth for the "Today" queue. There is no backend endpoint yet — the receptionist
 * workflow is being built UI-first so the contract can be reviewed with clinical staff before
 * the HIS PatientFlow + Scheduling slices commit to a query shape. Swap this for a real
 * `apiClient.get("/api/his/api/v1.0/patient-flow/todays-queue")` call once that endpoint lands.
 */
// TODO(his): replace with real endpoint once HIS exposes /todays-queue. Until then this drives
// the UI from a fixed set of sample patients so the screen can be demoed to receptionists.
const MOCK_QUEUE: readonly QueueEntry[] = [
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

const fetchTodaysQueue = (): Promise<readonly QueueEntry[]> => Promise.resolve(MOCK_QUEUE);

export const useTodaysQueue = (): UseQueryResult<readonly QueueEntry[]> =>
  useQuery({
    queryKey: ["his", "todays-queue"],
    queryFn: fetchTodaysQueue,
    staleTime: 15_000,
  });
