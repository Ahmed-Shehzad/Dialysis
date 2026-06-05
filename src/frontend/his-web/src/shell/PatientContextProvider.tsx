import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

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

/**
 * Holds the patient the user is currently "working with" so that opening another module
 * (e.g. jumping from HIS check-in to the EHR chart) inherits the same patient without
 * a fresh selection. Renders no UI; pair it with `<PatientContextBar />` to surface it.
 */
export const PatientContextProvider = ({ children }: { children: ReactNode }) => {
  const [patient, setPatient] = useState<SelectedPatient | null>(null);
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
