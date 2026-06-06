import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/care-coordination";

export type HospitalEvent = {
  id: string;
  patientId: string | null;
  kind: "Admitted" | "Discharged" | "ExternalEncounter";
  source: string;
  occurredAtUtc: string;
  detail?: string | null;
  externalPatientRef?: string | null;
  followedUp: boolean;
};

export const hospitalEventKindLabel = (kind: HospitalEvent["kind"]): string => {
  switch (kind) {
    case "Admitted":
      return "Admitted";
    case "Discharged":
      return "Discharged";
    case "ExternalEncounter":
      return "Seen elsewhere";
  }
};

/** Facility-wide worklist of hospital events still needing follow-up. */
export const fetchNeedsFollowUp = async (take = 100): Promise<HospitalEvent[]> => {
  const response = await apiClient.get<HospitalEvent[]>(`${prefix}/worklist/needs-follow-up`, {
    params: { take },
  });
  return response.data ?? [];
};

/** A patient's hospital events for the chart card. */
export const fetchPatientHospitalEvents = async (
  patientId: string,
  take = 25,
): Promise<HospitalEvent[]> => {
  const response = await apiClient.get<HospitalEvent[]>(
    `${prefix}/patients/${patientId}/hospital-events`,
    { params: { take } },
  );
  return response.data ?? [];
};

/** Marks an event followed-up so it drops off the worklist. */
export const markHospitalEventFollowedUp = async (id: string): Promise<void> => {
  await apiClient.post(`${prefix}/hospital-events/${id}/follow-up`);
};
