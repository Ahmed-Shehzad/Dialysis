import { usePatientContext } from "./PatientContextProvider";

/**
 * Slim strip surfaced under the header that shows the patient the user is currently
 * "working with" across modules. Renders nothing when no patient is selected so the layout
 * stays unchanged on screens that don't need it.
 */
export const PatientContextBar = () => {
  const { patient, clear } = usePatientContext();
  if (!patient) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="border-b border-clinic-700/40 bg-clinic-900/40"
    >
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-3 px-6 py-2 text-sm">
        <div className="flex min-w-0 items-center gap-3">
          <span className="text-xs uppercase tracking-wide text-slate-400">Patient</span>
          <span className="truncate font-medium text-clinic-50">{patient.displayName}</span>
          {patient.mrn && <span className="truncate text-xs text-slate-400">{patient.mrn}</span>}
        </div>
        <button
          type="button"
          onClick={clear}
          aria-label="Clear selected patient"
          className="rounded-md border border-slate-700 px-2 py-0.5 text-xs text-slate-300 transition hover:border-slate-500"
        >
          Clear
        </button>
      </div>
    </div>
  );
};
