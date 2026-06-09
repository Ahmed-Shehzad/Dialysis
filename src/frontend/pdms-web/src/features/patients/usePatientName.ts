import { useQuery } from "@tanstack/react-query";
import { type EhrPatientDetail, fetchEhrPatient } from "@/features/ehr/api/ehrApi";

/**
 * Formats a `PatientDetailDto` into a display name. Falls back to the MRN when name
 * fields are missing — never returns an empty string for an existing patient.
 */
const formatPatientName = (patient: {
  familyName: string;
  givenName: string;
  middleName?: string | null;
  medicalRecordNumber: string;
}): string => {
  const parts = [patient.givenName, patient.middleName, patient.familyName].filter(
    (p): p is string => Boolean(p && p.trim().length > 0),
  );
  if (parts.length === 0) return `MRN ${patient.medicalRecordNumber}`;
  return parts.join(" ");
};

/**
 * Resolves a patient Guid into their EHR demographics (name, MRN, DOB) via EHR's
 * `GET /pdms/api/_x/ehr/api/v1.0/patients/{id}` (the PDMS BFF's EHR aggregation). Cached
 * aggressively because demographics don't change minute-to-minute — once fetched we reuse
 * it across every tile, chart heading, session header, and consent row that references the
 * same patient. `patient` is `null` while in-flight or when the patient does not exist (404).
 *
 * Cross-module API call (EHR, not PDMS's own backend) is by design: EHR owns demographics,
 * and any patient surfaced anywhere in the SPA has an EHR registration mirrored from HIS
 * check-in/walk-in events (#31, #34).
 */
export const usePatientDemographics = (patientId: string | null | undefined) => {
  const query = useQuery({
    queryKey: ["ehr", "patient-name", patientId],
    queryFn: () => fetchEhrPatient(patientId as string),
    enabled: Boolean(patientId),
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
  });
  const patient: EhrPatientDetail | null = query.data ?? null;
  const displayName = patient ? formatPatientName(patient) : null;
  return { patient, displayName, isLoading: query.isLoading, error: query.error };
};

/** Convenience wrapper that returns only the formatted display name. Shares the cache above. */
export const usePatientName = (patientId: string | null | undefined) => {
  const { displayName, isLoading, error } = usePatientDemographics(patientId);
  return { name: displayName, isLoading, error };
};
