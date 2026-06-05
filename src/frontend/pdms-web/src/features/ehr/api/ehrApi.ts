import { apiClient } from "@/lib/api/apiClient";

export type EhrPatient = {
  id: string;
  medicalRecordNumber: string;
  familyName: string;
  givenName: string;
  dateOfBirth: string;
  sexAtBirthCode?: string | null;
  status: string;
};

export type ChartItem = {
  kind: "Allergy" | "Problem" | "Medication" | "Vital" | "Immunization";
  id: string;
  recordedAtUtc: string;
  code: string;
  display: string;
  value?: string | null;
  status?: string | null;
};

export type PatientChartView = {
  patientId: string;
  allergies: ChartItem[];
  problems: ChartItem[];
  medications: ChartItem[];
  vitals: ChartItem[];
  immunizations: ChartItem[];
};

export type PatientSearchFilters = {
  q?: string;
  familyName?: string;
  givenName?: string;
  mrn?: string;
  dobFrom?: string; // YYYY-MM-DD
  dobTo?: string; // YYYY-MM-DD
  sex?: string; // "male" | "female" | other code
  status?: "Active" | "Inactive" | "Deceased" | "Merged";
  skip?: number;
  take?: number;
};

export type PatientSearchPage = {
  items: EhrPatient[];
  totalCount: number;
  skip: number;
  take: number;
};

const stripEmpty = (filters: PatientSearchFilters): Record<string, string | number> => {
  const out: Record<string, string | number> = {};
  for (const [k, v] of Object.entries(filters)) {
    if (v === undefined || v === null) continue;
    if (typeof v === "string" && v.trim() === "") continue;
    out[k] = v;
  }
  return out;
};

export const searchEhrPatientsPage = async (
  filters: PatientSearchFilters = {},
): Promise<PatientSearchPage> => {
  const response = await apiClient.get<PatientSearchPage>("/pdms/api/v1.0/patients", {
    params: stripEmpty({ take: 25, skip: 0, ...filters }),
  });
  return response.data ?? { items: [], totalCount: 0, skip: 0, take: filters.take ?? 25 };
};

/** Convenience wrapper preserving the legacy bare-array shape for non-paginated callers. */
export const searchEhrPatients = async (q?: string, take = 25): Promise<EhrPatient[]> => {
  const page = await searchEhrPatientsPage({ q, take });
  return page.items;
};

export const fetchPatientChart = async (patientId: string): Promise<PatientChartView> => {
  const response = await apiClient.get<PatientChartView>(
    `/pdms/api/v1.0/patients/${patientId}/chart`,
  );
  return response.data;
};

/** ClinicalNote status int from `Dialysis.EHR.ClinicalNotes.Domain.ClinicalNoteStatus`. */
export type ClinicalNoteStatus = 1 | 2 | 3 | 4; // Draft | Signed | Amended | EnteredInError

export const clinicalNoteStatusLabel = (status: ClinicalNoteStatus): string => {
  switch (status) {
    case 1:
      return "Draft";
    case 2:
      return "Signed";
    case 3:
      return "Amended";
    case 4:
      return "Entered in error";
  }
};

export type ClinicalNoteListItem = {
  id: string;
  encounterId: string;
  authoringProviderId: string;
  status: ClinicalNoteStatus;
  createdAtUtc: string;
  signedAtUtc?: string | null;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
};

export const fetchPatientNotes = async (
  patientId: string,
  take = 20,
): Promise<ClinicalNoteListItem[]> => {
  const response = await apiClient.get<ClinicalNoteListItem[]>(
    `/pdms/api/v1.0/patients/${patientId}/notes`,
    { params: { take } },
  );
  return response.data ?? [];
};

/** LabAbnormalFlag enum from `Dialysis.EHR.ClinicalNotes.Domain.LabAbnormalFlag`. */
export type LabAbnormalFlag = 1 | 2 | 3 | 4 | 5; // Normal | Low | High | Critical | AbnormalNos

export const labAbnormalFlagLabel = (flag: LabAbnormalFlag): string => {
  switch (flag) {
    case 1:
      return "Normal";
    case 2:
      return "Low";
    case 3:
      return "High";
    case 4:
      return "Critical";
    case 5:
      return "Abnormal";
  }
};

export type LabResultListItem = {
  id: string;
  labOrderId: string;
  loincCode: string;
  valueText: string;
  unitCode?: string | null;
  referenceRangeText?: string | null;
  abnormalFlag: LabAbnormalFlag;
  observedAtUtc: string;
};

export const fetchPatientLabResults = async (
  patientId: string,
  lookbackDays = 180,
  take = 50,
): Promise<LabResultListItem[]> => {
  const response = await apiClient.get<LabResultListItem[]>(
    `/pdms/api/v1.0/patients/${patientId}/lab-results`,
    { params: { lookbackDays, take } },
  );
  return response.data ?? [];
};

export type EhrPatientDetail = {
  id: string;
  medicalRecordNumber: string;
  familyName: string;
  givenName: string;
  middleName?: string | null;
  dateOfBirth: string; // YYYY-MM-DD
  sexAtBirthCode?: string | null;
  preferredLanguageCode?: string | null;
  status: string;
};

/** Returns identity / demographics for one patient. Returns null on 404. */
export const fetchEhrPatient = async (patientId: string): Promise<EhrPatientDetail | null> => {
  try {
    const response = await apiClient.get<EhrPatientDetail>(
      `/pdms/api/v1.0/patients/${patientId}`,
    );
    return response.data;
  } catch (error) {
    const status = (error as { response?: { status?: number } })?.response?.status;
    if (status === 404) return null;
    throw error;
  }
};

export type RegisterPatientRequest = {
  medicalRecordNumber: string;
  familyName: string;
  givenName: string;
  middleName?: string;
  dateOfBirth: string; // YYYY-MM-DD
  sexAtBirthCode?: string;
  preferredLanguageCode?: string;
};

export type StartEncounterRequest = {
  patientId: string;
  providerId: string;
  encounterClassCode: string;
  appointmentId?: string | null;
};

export type SignNoteRequest = {
  noteId: string;
  signingProviderId: string;
};

export type OrderLabTestRequest = {
  patientId: string;
  encounterId: string;
  orderingProviderId: string;
  labFacilityCode: string;
  loincPanelCodes: string[];
};

export const registerPatient = async (body: RegisterPatientRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/pdms/api/v1.0/clinical/patients",
    body,
  );
  return response.data.id;
};

export const startEncounter = async (body: StartEncounterRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/pdms/api/v1.0/clinical/encounters",
    body,
  );
  return response.data.id;
};

export const signClinicalNote = async (body: SignNoteRequest): Promise<void> => {
  await apiClient.post(`/pdms/api/v1.0/clinical/notes/${body.noteId}/sign`, {
    signingProviderId: body.signingProviderId,
  });
};

export const orderLabTest = async (body: OrderLabTestRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/pdms/api/v1.0/clinical/lab-orders",
    body,
  );
  return response.data.id;
};

export type DraftClinicalNoteRequest = {
  encounterId: string;
  patientId: string;
  authoringProviderId: string;
  subjective: string;
  objective: string;
  assessment: string;
  plan: string;
};

export const draftClinicalNote = async (body: DraftClinicalNoteRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/pdms/api/v1.0/clinical/notes/draft",
    body,
  );
  return response.data.id;
};

/**
 * Well-known demo provider id seeded by `EhrDemoSeeder` when `Ehr:Demo:Enabled=true`.
 * Surfaced to the SPA as the authoring provider for notes / encounters until real
 * auth-claim → provider-id mapping lands. Stable across restarts.
 */
export const DEMO_PROVIDER_ID = "00000000-0000-0000-0000-000000000001";

/**
 * Demo lab facility code passed to `orderLabTest`. Placeholder until a real lab
 * directory endpoint (`/clinical/lab-facilities`) exists.
 */
export const DEMO_LAB_FACILITY = "DEMO-LAB-01";

/**
 * Common dialysis-relevant LOINC panels surfaced in the Order Labs dialog. Hardcoded
 * until the FHIR Terminology service is wired through to the SPA — at that point this
 * list moves behind a real search.
 */
export const COMMON_LAB_PANELS: ReadonlyArray<{ loinc: string; display: string }> = [
  { loinc: "24323-8", display: "Comprehensive metabolic panel" },
  { loinc: "58410-2", display: "CBC panel — Blood" },
  { loinc: "718-7", display: "Hemoglobin" },
  { loinc: "4544-3", display: "Hematocrit" },
  { loinc: "2160-0", display: "Creatinine" },
  { loinc: "2823-3", display: "Potassium" },
  { loinc: "2951-2", display: "Sodium" },
  { loinc: "2885-2", display: "Total protein" },
];
