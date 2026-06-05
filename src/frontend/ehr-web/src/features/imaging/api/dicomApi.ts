import { apiClient } from "@/lib/api/apiClient";

/** Study-level metadata distilled from a DICOMweb QIDO-RS (DICOM-JSON) study object. */
export type StudyMetadata = {
  studyInstanceUid: string;
  instanceCount: number;
  modality: string | null;
};

type DicomJsonElement = { vr: string; Value?: unknown[] };
type DicomJsonStudy = Record<string, DicomJsonElement>;

/** One instance under a study, for the viewer's paging. */
export type StudyInstance = {
  seriesInstanceUid: string;
  sopInstanceUid: string;
  instanceNumber: number;
};

const firstValue = (study: DicomJsonStudy, tag: string): unknown => study[tag]?.Value?.[0];

const stringTag = (study: DicomJsonStudy, tag: string): string => {
  const v = firstValue(study, tag);
  return typeof v === "string" ? v : "";
};

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

/**
 * Lists the instances under a study (series + SOP UIDs), ordered by series then SOP, so the viewer
 * can page through every frame via the per-instance rendered endpoint.
 */
export const fetchStudyInstances = async (studyInstanceUid: string): Promise<StudyInstance[]> => {
  const response = await apiClient.get<DicomJsonStudy[]>(
    `/ehr/api/_x/dicom/dicom-web/studies/${encodeURIComponent(studyInstanceUid)}/instances`,
  );
  return (response.data ?? []).map((instance, index) => {
    const number = firstValue(instance, "00200013");
    return {
      seriesInstanceUid: stringTag(instance, "0020000E"),
      sopInstanceUid: stringTag(instance, "00080018"),
      instanceNumber: typeof number === "number" ? number : Number(number ?? index + 1),
    };
  });
};
