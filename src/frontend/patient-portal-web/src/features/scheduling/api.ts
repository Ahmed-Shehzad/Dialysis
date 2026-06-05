import { apiClient } from "@/lib/api/apiClient";

/**
 * Demo provider used by the patient portal booking form. The HIS scheduling validator
 * only checks that ProviderId != Guid.Empty (no catalog lookup yet), so a hardcoded
 * well-known Guid is sufficient for the demo loop and consistent with the EHR demo
 * provider constant.
 */
export const DEMO_HIS_PROVIDER_ID = "00000000-0000-0000-0000-000000000001";

export interface BookAppointmentInput {
  patientId: string;
  providerId: string;
  slotStartUtc: string;
  slotEndUtc: string;
}

interface ResourceEnvelope<T> {
  data: T;
}

interface BookAppointmentResponse {
  id: string;
}

/**
 * Books an appointment via HIS `POST /portal/api/v1.0/scheduling/appointments`. Returns
 * the new appointment id. The portal mutation invalidates the portal summary on success
 * so the upcoming-appointments tile increments.
 */
export const bookAppointment = async (input: BookAppointmentInput): Promise<string> => {
  const response = await apiClient.post<ResourceEnvelope<BookAppointmentResponse>>(
    "/portal/api/v1.0/scheduling/appointments",
    {
      PatientId: input.patientId,
      ProviderId: input.providerId,
      SlotStartUtc: input.slotStartUtc,
      SlotEndUtc: input.slotEndUtc,
    },
  );
  return response.data.data.id;
};
