import { apiClient } from "@/lib/api/apiClient";

const prefix = "/ehr/api/v1.0/care-team";

export type CareTeamRole =
  | "PrimaryNephrologist"
  | "AttendingPhysician"
  | "DialysisNurse"
  | "Dietitian"
  | "SocialWorker"
  | "CareCoordinator"
  | "Other";

export const CARE_TEAM_ROLES: CareTeamRole[] = [
  "PrimaryNephrologist",
  "AttendingPhysician",
  "DialysisNurse",
  "Dietitian",
  "SocialWorker",
  "CareCoordinator",
  "Other",
];

export const careTeamRoleLabel = (role: CareTeamRole): string =>
  role.replace(/([a-z])([A-Z])/g, "$1 $2");

export type CareTeamMember = {
  providerId: string;
  role: CareTeamRole;
  isPrimary: boolean;
};

export type CareTeamView = {
  id: string;
  patientId: string;
  members: CareTeamMember[];
};

/**
 * Demo providers surfaced in the care-team picker. Placeholder until a provider-directory endpoint
 * exists; the ids are stable so a care team survives restarts in the demo.
 */
export const DEMO_PROVIDERS: ReadonlyArray<{ id: string; display: string }> = [
  { id: "10000000-0000-0000-0000-000000000001", display: "Dr. Ada Okafor (Nephrology)" },
  { id: "10000000-0000-0000-0000-000000000002", display: "Dr. Ben Reyes (Internal Medicine)" },
  { id: "10000000-0000-0000-0000-000000000003", display: "Nurse Carla Devi" },
  { id: "10000000-0000-0000-0000-000000000004", display: "Dietitian Erik Sandel" },
  { id: "10000000-0000-0000-0000-000000000005", display: "Social Worker Priya Nair" },
];

export const fetchCareTeam = async (patientId: string): Promise<CareTeamView | null> => {
  const response = await apiClient.get<CareTeamView | "">(`${prefix}/patients/${patientId}`, {
    validateStatus: (s) => s === 200 || s === 204,
  });
  return response.status === 204 || !response.data ? null : (response.data as CareTeamView);
};

export const addCareTeamMember = async (
  patientId: string,
  body: { providerId: string; role: CareTeamRole; isPrimary: boolean },
): Promise<string> => {
  const response = await apiClient.post<{ id: string }>(
    `${prefix}/patients/${patientId}/members`,
    body,
  );
  return response.data.id;
};

export const removeCareTeamMember = async (
  patientId: string,
  providerId: string,
): Promise<void> => {
  await apiClient.delete(`${prefix}/patients/${patientId}/members/${providerId}`);
};

export const setPrimaryCareTeamMember = async (
  patientId: string,
  providerId: string,
): Promise<void> => {
  await apiClient.post(`${prefix}/patients/${patientId}/members/${providerId}/primary`);
};
