import { apiClient } from "@/lib/api/apiClient";

// HIS wraps successful responses in a HATEOAS envelope { data, links }.
type HateoasEnvelope<T> = { data: T; links: unknown[] };

const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

export type PatientSearchRow = {
  id: string;
  externalPatientId: string;
  searchText: string;
  indexedAtUtc: string;
};

export type ManagerDashboardSnapshot = {
  reportFocus?: string | null;
  queuedBillingExportJobsCount: number;
  openQualityWorkflowTasksCount: number;
  recentImportJobsCount: number;
  generatedAtUtc: string;
};

export type IntegrationOutboxRow = {
  id: string;
  assemblyQualifiedEventType: string;
  createdAtUtc: string;
  processedAtUtc?: string | null;
  correlationId?: string | null;
};

export const fetchManagerDashboard = async (focus?: string): Promise<ManagerDashboardSnapshot> => {
  const response = await apiClient.get<HateoasEnvelope<ManagerDashboardSnapshot>>(
    "/his/api/v1.0/data-management/manager-dashboard",
    { params: focus ? { reportFocus: focus } : {} },
  );
  return unwrap(response.data);
};

export const searchHisPatients = async (q: string, take = 25): Promise<PatientSearchRow[]> => {
  const response = await apiClient.get<HateoasEnvelope<PatientSearchRow[]>>(
    "/his/api/v1.0/data-management/patients/search",
    { params: { q, take } },
  );
  return unwrap(response.data);
};

export const fetchRecentIntegrationEvents = async (take = 25): Promise<IntegrationOutboxRow[]> => {
  const response = await apiClient.get<HateoasEnvelope<IntegrationOutboxRow[]>>(
    "/his/api/v1.0/data-management/integration/outbox-metadata",
    { params: { take } },
  );
  return unwrap(response.data);
};

export type AdmitPatientRequest = { patientId: string; wardCode: string };
export type BookAppointmentRequest = {
  patientId: string;
  providerId: string;
  slotStartUtc: string;
  slotEndUtc: string;
};
export type PlaceMedicationOrderRequest = {
  patientId: string;
  drugCode: string;
  dosage: string;
};

const unwrapPostId = (raw: unknown): string => {
  const body = raw as HateoasEnvelope<{ id: string } | string> | { id: string };
  const data =
    "data" in (body as Record<string, unknown>)
      ? (body as HateoasEnvelope<{ id: string } | string>).data
      : body;
  if (typeof data === "string") return data;
  return (data as { id: string }).id;
};

export const admitPatient = async (body: AdmitPatientRequest): Promise<string> => {
  const response = await apiClient.post("/his/api/v1.0/patient-flow/admissions", body);
  return unwrapPostId(response.data);
};

export const bookAppointment = async (body: BookAppointmentRequest): Promise<string> => {
  const response = await apiClient.post("/his/api/v1.0/scheduling/appointments", body);
  return unwrapPostId(response.data);
};

export const placeMedicationOrder = async (body: PlaceMedicationOrderRequest): Promise<string> => {
  const response = await apiClient.post("/his/api/v1.0/medication/orders", body);
  return unwrapPostId(response.data);
};
