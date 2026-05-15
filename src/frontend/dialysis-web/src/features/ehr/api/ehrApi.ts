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
  const response = await apiClient.get<PatientSearchPage>("/api/ehr/api/v1.0/patients", {
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
    `/api/ehr/api/v1.0/patients/${patientId}/chart`,
  );
  return response.data;
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
    "/api/ehr/api/v1.0/clinical/patients",
    body,
  );
  return response.data.id;
};

export const startEncounter = async (body: StartEncounterRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/api/ehr/api/v1.0/clinical/encounters",
    body,
  );
  return response.data.id;
};

export const signClinicalNote = async (body: SignNoteRequest): Promise<void> => {
  await apiClient.post(`/api/ehr/api/v1.0/clinical/notes/${body.noteId}/sign`, {
    signingProviderId: body.signingProviderId,
  });
};

export const orderLabTest = async (body: OrderLabTestRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    "/api/ehr/api/v1.0/clinical/lab-orders",
    body,
  );
  return response.data.id;
};
