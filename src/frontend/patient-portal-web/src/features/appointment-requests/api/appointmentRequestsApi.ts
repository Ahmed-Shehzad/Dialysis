import { apiClient } from "@/lib/api/apiClient";

const prefix = "/portal/api/_x/ehr/api/v1.0/portal/appointment-requests";

export type AppointmentRequest = {
  id: string;
  patientId: string;
  reasonText: string;
  earliestPreferredUtc: string;
  latestPreferredUtc: string;
  status: string;
  createdAppointmentId?: string | null;
  staffNote?: string | null;
};

/** The patient's own appointment requests. */
export const fetchMyRequests = async (patientId: string): Promise<AppointmentRequest[]> => {
  const response = await apiClient.get<AppointmentRequest[]>(`${prefix}/patients/${patientId}`);
  return response.data ?? [];
};

/** Patient submits an appointment request. */
export const requestAppointment = async (
  patientId: string,
  body: { reasonText: string; earliestPreferredUtc: string; latestPreferredUtc: string },
): Promise<{ id: string }> => {
  const response = await apiClient.post<{ id: string }>(`${prefix}/patients/${patientId}`, body);
  return response.data;
};

/** Patient cancels their own still-pending request. */
export const cancelRequest = async (patientId: string, requestId: string): Promise<void> => {
  await apiClient.post(`${prefix}/patients/${patientId}/${requestId}/cancel`);
};
