import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/population";

export type CohortMeasureGap = {
  measureId: string;
  title: string;
  patientsWithGap: number;
};

export type QualityGap = {
  measureId: string;
  title: string;
  detail: string;
};

export type CohortPatientGaps = {
  patientId: string;
  medicalRecordNumber: string;
  name: string;
  gaps: QualityGap[];
};

export type CohortQualityResult = {
  patientsEvaluated: number;
  patientsWithAnyGap: number;
  measureGaps: CohortMeasureGap[];
  patientBreakdown: CohortPatientGaps[];
};

/** Population quality roll-up: open care gaps across the active panel, aggregated per measure. */
export const fetchCohortQuality = async (take = 100): Promise<CohortQualityResult> => {
  const response = await apiClient.get<CohortQualityResult>(`${prefix}/quality`, {
    params: { take },
  });
  return (
    response.data ?? {
      patientsEvaluated: 0,
      patientsWithAnyGap: 0,
      measureGaps: [],
      patientBreakdown: [],
    }
  );
};
