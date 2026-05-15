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

export const searchEhrPatients = async (q?: string, take = 25): Promise<EhrPatient[]> => {
  const response = await apiClient.get<EhrPatient[]>("/api/ehr/api/v1.0/patients", {
    params: { q, take },
  });
  return response.data ?? [];
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
  const response = await apiClient.post<{ id: string }>("/api/ehr/api/v1.0/clinical/patients", body);
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
