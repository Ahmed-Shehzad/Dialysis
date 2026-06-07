import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

/**
 * Minimal patient identity carried across modules. We deliberately keep this small so the
 * shell never holds stale clinical data — anything richer comes from each module's own
 * TanStack queries keyed by `id`.
 */
export interface SelectedPatient {
  id: string;
  displayName: string;
  mrn?: string;
}

interface PatientContextValue {
  patient: SelectedPatient | null;
  select: (patient: SelectedPatient) => void;
  clear: () => void;
}

const PatientContextCtx = createContext<PatientContextValue | undefined>(undefined);

// The per-context apps (his/ehr/pdms/…) are separate Vite builds served on the SAME gateway
// origin, so they share one `localStorage`. Persisting the selected patient here lets a
// full-page hop from the Front Desk app to the Chart app inherit the same patient without a
// fresh selection — the cross-app replacement for the old in-memory shell context.
const STORAGE_KEY = "dialysis.patient";

const serialize = (patient: SelectedPatient | null): string | null =>
  patient ? JSON.stringify(patient) : null;

const readStoredPatient = (): SelectedPatient | null => {
  try {
    const raw = globalThis.localStorage?.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as SelectedPatient;
    return parsed && typeof parsed.id === "string" ? parsed : null;
  } catch {
    return null;
  }
};

/**
 * Value equality. `readStoredPatient()` returns a fresh object every call, so without this an
 * identical cross-tab broadcast would look like a new value and churn state.
 */
const samePatient = (a: SelectedPatient | null, b: SelectedPatient | null): boolean =>
  a === b || (!!a && !!b && a.id === b.id && a.displayName === b.displayName && a.mrn === b.mrn);

/**
 * Holds the patient the user is currently "working with" so that opening another app
 * (e.g. jumping from HIS check-in to the EHR chart) inherits the same patient without
 * a fresh selection. Renders no UI; pair it with `<PatientContextBar />` to surface it.
 */
export const PatientContextProvider = ({ children }: { children: ReactNode }) => {
  const [patient, setPatient] = useState<SelectedPatient | null>(readStoredPatient);

  // Persist to localStorage — but only when the stored value actually changes. Writing the same
  // value still dispatches a `storage` event to every OTHER same-origin app, whose listener would
  // call setPatient() with a fresh object, re-run this effect, and write again: an infinite
  // cross-tab ping-pong that floods the browser (ERR_INSUFFICIENT_RESOURCES). Skipping no-op
  // writes breaks the cycle.
  useEffect(() => {
    try {
      const next = serialize(patient);
      const current = globalThis.localStorage?.getItem(STORAGE_KEY) ?? null;
      if (next === current) return;
      if (next === null) globalThis.localStorage?.removeItem(STORAGE_KEY);
      else globalThis.localStorage?.setItem(STORAGE_KEY, next);
    } catch {
      // Storage unavailable (private mode / SSR) — fall back to in-memory only.
    }
  }, [patient]);

  // Keep apps in sync if the patient changes in another tab/app on the same origin. Adopt only a
  // genuinely different value: returning the previous reference makes React bail out of the update,
  // so an identical re-broadcast neither re-renders nor re-triggers the write effect above.
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key !== STORAGE_KEY) return;
      const next = readStoredPatient();
      setPatient((prev) => (samePatient(prev, next) ? prev : next));
    };
    globalThis.addEventListener?.("storage", onStorage);
    return () => globalThis.removeEventListener?.("storage", onStorage);
  }, []);

  const value = useMemo<PatientContextValue>(
    () => ({
      patient,
      select: setPatient,
      clear: () => setPatient(null),
    }),
    [patient],
  );
  return <PatientContextCtx.Provider value={value}>{children}</PatientContextCtx.Provider>;
};

export const usePatientContext = (): PatientContextValue => {
  const ctx = useContext(PatientContextCtx);
  if (!ctx) {
    throw new Error("usePatientContext must be used within <PatientContextProvider>");
  }
  return ctx;
};
