import { useQuery } from "@tanstack/react-query";
import { fetchEhrPatient } from "@/features/ehr/api/ehrApi";

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
 * Resolves a patient Guid into a display name via EHR's
 * `GET /api/ehr/api/v1.0/patients/{id}`. Cached aggressively because the name doesn't
 * change minute-to-minute — once we've fetched a name we reuse it across every tile,
 * chart heading, and consent row that references the same patient. Returns `null` for
 * the name while the request is in-flight or when the patient does not exist (404).
 *
 * Cross-module API call (EHR, not the calling module's own backend) is by design: EHR
 * owns demographics, and any patient surfaced anywhere in the SPA has an EHR
 * registration mirrored from HIS check-in/walk-in events (#31, #34).
 */
export const usePatientName = (patientId: string | null | undefined) => {
  const query = useQuery({
    queryKey: ["ehr", "patient-name", patientId],
    queryFn: () => fetchEhrPatient(patientId as string),
    enabled: Boolean(patientId),
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
  });
  const name = query.data ? formatPatientName(query.data) : null;
  return { name, isLoading: query.isLoading, error: query.error };
};
