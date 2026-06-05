import { apiClient } from "@/lib/api/apiClient";

export type ChannelTarget = { channel: string; address: string };
export type ChainLink = {
  clinicianSub: string;
  displayName: string;
  channels: ChannelTarget[];
};

export type OnCallRotation = {
  id: string;
  chairId: string;
  shiftCode: "morning" | "afternoon" | "night";
  effectiveFromUtc: string;
  effectiveUntilUtc: string;
  primary: ChainLink;
  backup: ChainLink;
  supervisor: ChainLink;
};

export type EscalationPolicy = {
  id: string;
  name: string;
  criticalPrimaryWindowSeconds: number;
  criticalBackupWindowSeconds: number;
  warningPrimaryWindowSeconds: number;
  warningBackupWindowSeconds: number;
  informationalPrimaryWindowSeconds: number;
  quietHoursSuppressNonCritical: boolean;
};

export type AlarmDispatchAttempt = {
  chainLinkIndex: number;
  channel: string;
  address: string;
  delivered: boolean;
  failureReason: string | null;
  attemptedAtUtc: string;
};

export type AlarmDispatch = {
  id: string;
  infusionId: string;
  sessionId: string;
  chairId: string;
  alarmCode: string;
  severity: string;
  startedAtUtc: string;
  resolvedAtUtc: string | null;
  status: string;
  currentLinkIndex: number;
  acknowledgedBySub: string | null;
  attempts: AlarmDispatchAttempt[];
};

export const fetchRotations = async (chairId?: string): Promise<OnCallRotation[]> => {
  const response = await apiClient.get<OnCallRotation[]>("/pdms/api/v1.0/oncall/rotations", {
    params: chairId ? { chairId } : undefined,
  });
  return response.data ?? [];
};

export const createRotation = async (
  request: Omit<OnCallRotation, "id">,
): Promise<OnCallRotation> => {
  const response = await apiClient.post<OnCallRotation>(
    "/pdms/api/v1.0/oncall/rotations",
    request,
  );
  return response.data;
};

export const replaceRotation = async (
  id: string,
  request: Omit<OnCallRotation, "id">,
): Promise<OnCallRotation> => {
  const response = await apiClient.put<OnCallRotation>(
    `/pdms/api/v1.0/oncall/rotations/${id}`,
    request,
  );
  return response.data;
};

export const fetchPolicies = async (): Promise<EscalationPolicy[]> => {
  const response = await apiClient.get<EscalationPolicy[]>("/pdms/api/v1.0/oncall/policies");
  return response.data ?? [];
};

export const replacePolicy = async (
  id: string,
  request: Omit<EscalationPolicy, "id">,
): Promise<EscalationPolicy> => {
  const response = await apiClient.put<EscalationPolicy>(
    `/pdms/api/v1.0/oncall/policies/${id}`,
    request,
  );
  return response.data;
};

export const fetchDispatches = async (params?: {
  from?: string;
  to?: string;
  chairId?: string;
}): Promise<AlarmDispatch[]> => {
  const response = await apiClient.get<AlarmDispatch[]>("/pdms/api/v1.0/oncall/dispatches", {
    params,
  });
  return response.data ?? [];
};
