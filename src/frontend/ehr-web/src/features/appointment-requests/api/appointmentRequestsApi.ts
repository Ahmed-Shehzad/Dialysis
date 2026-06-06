import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/portal/appointment-requests";

// Demo provider id used when booking the approved appointment (mirrors care-coordination DEMO_PROVIDERS).
export const DEMO_PROVIDER_ID = "11111111-1111-1111-1111-111111111111";

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

/** Staff worklist of still-pending appointment requests. */
export const fetchPendingRequests = async (take = 100): Promise<AppointmentRequest[]> => {
  const response = await apiClient.get<AppointmentRequest[]>(`${prefix}/pending`, {
    params: { take },
  });
  return response.data ?? [];
};

/** Approve a request — books the appointment and links it. */
export const approveRequest = async (
  requestId: string,
  body: {
    patientId: string;
    providerId: string;
    startUtc: string;
    endUtc: string;
    encounterClassCode: string;
    visitReason?: string;
    staffNote?: string;
  },
): Promise<{ appointmentId: string }> => {
  const response = await apiClient.post<{ appointmentId: string }>(
    `${prefix}/${requestId}/approve`,
    body,
  );
  return response.data;
};

/** Decline a request with a note. */
export const declineRequest = async (requestId: string, staffNote: string): Promise<void> => {
  await apiClient.post(`${prefix}/${requestId}/decline`, { staffNote });
};
