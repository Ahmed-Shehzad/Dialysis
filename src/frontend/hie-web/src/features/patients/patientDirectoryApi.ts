import { apiClient } from "@/lib/api/apiClient";

/** Slim, label-only patient projection (name + MRN + DOB) returned by EHR's batch lookup. */
export type PatientLabel = {
  id: string;
  medicalRecordNumber: string;
  givenName: string;
  familyName: string;
  middleName?: string | null;
  dateOfBirth: string; // YYYY-MM-DD
};

// Must match the server cap on POST /patients/by-ids; the loader chunks to this size.
const MAX_PATIENT_BATCH = 200;

/**
 * Resolves many patient ids to their labels in as few round-trips as possible (one per <=200 ids),
 * through the HIE BFF's _x/ehr aggregation. Ids go in the request BODY, never the query string, to keep
 * patient identifiers out of gateway / proxy access logs. This is the N+1 killer behind the batched
 * patient-label resolver.
 */
export const fetchPatientsByIds = async (ids: string[]): Promise<PatientLabel[]> => {
  const distinct = [...new Set(ids)].filter(Boolean);
  if (distinct.length === 0) return [];
  const chunks: string[][] = [];
  for (let i = 0; i < distinct.length; i += MAX_PATIENT_BATCH) {
    chunks.push(distinct.slice(i, i + MAX_PATIENT_BATCH));
  }
  const pages = await Promise.all(
    chunks.map((chunk) =>
      apiClient
        .post<PatientLabel[]>(`/hie/api/_x/ehr/api/v1.0/patients/by-ids`, { ids: chunk })
        .then((r) => r.data ?? []),
    ),
  );
  return pages.flat();
};
