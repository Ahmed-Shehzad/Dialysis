import { usePatientDemographics } from "./usePatientName";

interface PatientLabelProps {
  patientId: string | null | undefined;
  className?: string;
  /** Show the "· MRN xxxx" suffix (default true). */
  showMrn?: boolean;
}

/**
 * Renders a patient as a readable "Given Family · MRN" wherever a patientId appears. Resolution goes
 * through the batched loader (see patientLoader.ts), so a list of these costs ONE request per tick, not
 * one per row. While loading — or when the patient can't be resolved (not found / no permission) — it
 * shows a stable short-id placeholder, never an empty string or a layout jump.
 */
export const PatientLabel = ({ patientId, className, showMrn = true }: PatientLabelProps) => {
  const { patient, displayName } = usePatientDemographics(patientId);
  if (!patientId) return <span className={className}>—</span>;

  return (
    <span className={className}>
      {displayName ?? `Patient ${patientId.slice(0, 8)}…`}
      {showMrn && patient?.medicalRecordNumber && (
        <span className="ml-1 font-mono text-xs text-slate-500">
          · MRN {patient.medicalRecordNumber}
        </span>
      )}
    </span>
  );
};
