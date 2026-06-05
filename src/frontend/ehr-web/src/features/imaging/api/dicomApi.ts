import { apiClient } from "@/lib/api/apiClient";

/** Study-level metadata distilled from a DICOMweb QIDO-RS (DICOM-JSON) study object. */
export type StudyMetadata = {
  studyInstanceUid: string;
  instanceCount: number;
  modality: string | null;
};

type DicomJsonElement = { vr: string; Value?: unknown[] };
type DicomJsonStudy = Record<string, DicomJsonElement>;

const firstValue = (study: DicomJsonStudy, tag: string): unknown => study[tag]?.Value?.[0];

/**
 * Fetches study metadata (modality + number of instances) for a linked DICOM study via the EHR BFF's
 * DICOMweb aggregation (`/ehr/api/_x/dicom/dicom-web/*` → SmartConnect QIDO-RS, filtered by the
 * study's UID — no reliance on the DICOM-level PatientID matching the EHR patient id). Returns null
 * when the study isn't in the store yet.
 */
export const fetchStudyMetadata = async (
  studyInstanceUid: string,
): Promise<StudyMetadata | null> => {
  const response = await apiClient.get<DicomJsonStudy[]>(
    `/ehr/api/_x/dicom/dicom-web/studies?StudyInstanceUID=${encodeURIComponent(studyInstanceUid)}`,
  );
  const study = response.data?.[0];
  if (!study) return null;

  const count = firstValue(study, "00201208");
  const modality = firstValue(study, "00080060");
  return {
    studyInstanceUid,
    instanceCount: typeof count === "number" ? count : Number(count ?? 0),
    modality: typeof modality === "string" && modality.length > 0 ? modality : null,
  };
};
