import { apiClient } from "@/lib/api/apiClient";

/**
 * Wire shape of HIS's `GET /portal/api/v1.0/patient-access/patients/{id}/portal-summary`.
 * The endpoint is gated by `his.patientaccess.portal.read` and, when JWT auth is on, by
 * a `his_patient_id` (or `sub`) claim matching the route id — i.e. patients can only see
 * their own counts. In Dev with no authority the gate is satisfied automatically.
 */
export interface PatientPortalSummary {
  patientId: string;
  upcomingAppointmentCount: number;
  openMedicationOrderCount: number;
  openAdmissionCount: number;
}

interface ResourceEnvelope<T> {
  data: T;
}

export const fetchPortalSummary = async (patientId: string): Promise<PatientPortalSummary> => {
  const response = await apiClient.get<ResourceEnvelope<PatientPortalSummary>>(
    `/portal/api/v1.0/patient-access/patients/${patientId}/portal-summary`,
  );
  return response.data.data;
};
