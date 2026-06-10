// Intentionally diverges from the shared copy (ehr/pdms/identity): hie-web has no full EHR API module, so it resolves labels through its own patientDirectoryApi (the HIE BFF's _x/ehr aggregation).
import { useQuery } from "@tanstack/react-query";
import { type PatientLabel } from "./patientDirectoryApi";
import { loadPatientLabel } from "./patientLoader";

/**
 * Formats a patient label into a display name. Falls back to the MRN when name fields are missing —
 * never returns an empty string for an existing patient.
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
 * Resolves a patient Guid into their EHR demographics (name, MRN, DOB) through the batched loader, so a
 * page of these costs one request, not one per row. `patient` is null while in-flight or unresolvable
 * (not found / no permission); the UI then shows an id placeholder. EHR owns demographics — this is a
 * cross-module read via the HIE BFF's _x/ehr aggregation by design.
 */
export const usePatientDemographics = (patientId: string | null | undefined) => {
  const query = useQuery({
    queryKey: ["ehr", "patient-name", patientId],
    queryFn: () => loadPatientLabel(patientId as string),
    enabled: Boolean(patientId),
    staleTime: 5 * 60_000,
    gcTime: 30 * 60_000,
    retry: false,
  });
  const patient: PatientLabel | null = query.data ?? null;
  const displayName = patient ? formatPatientName(patient) : null;
  return { patient, displayName, isLoading: query.isLoading, error: query.error };
};

/** Convenience wrapper that returns only the formatted display name. Shares the cache above. */
export const usePatientName = (patientId: string | null | undefined) => {
  const { displayName, isLoading, error } = usePatientDemographics(patientId);
  return { name: displayName, isLoading, error };
};
