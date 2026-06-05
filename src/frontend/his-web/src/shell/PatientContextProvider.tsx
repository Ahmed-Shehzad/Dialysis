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
 * Holds the patient the user is currently "working with" so that opening another app
 * (e.g. jumping from HIS check-in to the EHR chart) inherits the same patient without
 * a fresh selection. Renders no UI; pair it with `<PatientContextBar />` to surface it.
 */
export const PatientContextProvider = ({ children }: { children: ReactNode }) => {
  const [patient, setPatient] = useState<SelectedPatient | null>(readStoredPatient);

  useEffect(() => {
    try {
      if (patient) globalThis.localStorage?.setItem(STORAGE_KEY, JSON.stringify(patient));
      else globalThis.localStorage?.removeItem(STORAGE_KEY);
    } catch {
      // Storage unavailable (private mode / SSR) — fall back to in-memory only.
    }
  }, [patient]);

  // Keep apps in sync if the patient changes in another tab/app on the same origin.
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY) setPatient(readStoredPatient());
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
