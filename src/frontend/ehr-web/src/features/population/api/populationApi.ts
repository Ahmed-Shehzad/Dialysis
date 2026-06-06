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

export type PatientControlBreakdown = {
  patientId: string;
  medicalRecordNumber: string;
  name: string;
  outcome: string;
  value?: number | null;
};

export type PopulationControlResult = {
  measureId: string;
  title: string;
  inCohort: number;
  controlled: number;
  uncontrolled: number;
  noData: number;
  controlRatePercent: number;
  breakdown: PatientControlBreakdown[];
};

export type OutreachTarget = {
  patientId: string;
  medicalRecordNumber: string;
  name: string;
  contactResolved: boolean;
};

export type OutreachResult = {
  measureId: string;
  targeted: number;
  dispatched: boolean;
  targets: OutreachTarget[];
};

/** Condition-control rate for a configured measure (e.g. % of hypertensives with BP controlled). */
export const fetchPopulationControl = async (
  measureId: string,
  take = 100,
): Promise<PopulationControlResult> => {
  const response = await apiClient.get<PopulationControlResult>(`${prefix}/control`, {
    params: { measureId, take },
  });
  return response.data;
};

/** Reaches out to the uncontrolled patients for a measure; returns the audited target list. */
export const triggerOutreach = async (measureId: string, take = 100): Promise<OutreachResult> => {
  const response = await apiClient.post<OutreachResult>(`${prefix}/outreach`, { measureId, take });
  return response.data;
};
