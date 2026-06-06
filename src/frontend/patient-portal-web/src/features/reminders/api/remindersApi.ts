import { apiClient } from "@/lib/api/apiClient";

const prefix = "/portal/api/_x/ehr/api/v1.0/portal/reminders";

export type PatientReminder = {
  title: string;
  whatToDo: string;
  resourceUrl?: string | null;
};

/** The signed-in patient's plain-language health reminders. */
export const fetchMyReminders = async (patientId: string): Promise<PatientReminder[]> => {
  const response = await apiClient.get<PatientReminder[]>(`${prefix}/patients/${patientId}`);
  return response.data ?? [];
};
