import { fetchPatientsByIds, type PatientLabel } from "./patientDirectoryApi";

type Resolve = (value: PatientLabel | null) => void;

/**
 * DataLoader-style coalescing loader for patient labels. Every `load(id)` issued within a tick is
 * batched into ONE `POST /patients/by-ids`, so a list rendering N `<PatientLabel/>` rows makes a single
 * request instead of N. Wired as the TanStack Query `queryFn` for `["ehr","patient-name", id]`, so per-id
 * caching and cross-id HTTP batching combine — every `usePatientDemographics` call site is N+1-free.
 */
const createPatientLoader = () => {
  let queue = new Map<string, Resolve[]>();
  let scheduled = false;

  const flush = (): void => {
    scheduled = false;
    const batch = queue;
    queue = new Map();
    const ids = [...batch.keys()];

    void fetchPatientsByIds(ids).then(
      (rows) => {
        const byId = new Map(rows.map((r) => [r.id, r]));
        batch.forEach((waiters, id) => {
          const value = byId.get(id) ?? null;
          waiters.forEach((resolve) => resolve(value));
        });
      },
      // Degrade to "unresolved" (the UI shows the id placeholder) rather than rejecting — a missing label
      // must never break the page, and we never surface an error object that might carry PHI.
      () => batch.forEach((waiters) => waiters.forEach((resolve) => resolve(null))),
    );
  };

  return (id: string): Promise<PatientLabel | null> =>
    new Promise<PatientLabel | null>((resolve) => {
      const waiters = queue.get(id);
      if (waiters) waiters.push(resolve);
      else queue.set(id, [resolve]);
      if (!scheduled) {
        scheduled = true;
        // Defer to a macrotask so the whole synchronous render burst is coalesced before it flushes.
        setTimeout(flush, 0);
      }
    });
};

/** Shared singleton loader — all label lookups in the app coalesce through it. */
export const loadPatientLabel = createPatientLoader();
