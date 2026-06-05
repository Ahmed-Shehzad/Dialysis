import { apiClient } from "@/lib/api/apiClient";

// HIS wraps successful responses in a HATEOAS envelope { data, links }.
type HateoasEnvelope<T> = { data: T; links: unknown[] };
const unwrap = <T>(envelope: HateoasEnvelope<T> | T): T =>
  envelope && typeof envelope === "object" && "data" in (envelope as Record<string, unknown>)
    ? (envelope as HateoasEnvelope<T>).data
    : (envelope as T);

const BASE = "/his/api/v1.0/integration/devices";

// HIS serialises enums as strings (JsonStringEnumConverter).
export type DeviceStatus = "Registered" | "Active" | "Suspended" | "Retired" | string;
export type DeviceStatusAction = "Suspend" | "Activate" | "Retire";

export type DeviceType = {
  code: string;
  display: string;
  category: string;
  unit?: string | null;
};

export type DeviceSummary = {
  id: string;
  deviceId: string;
  deviceTypeCode: string;
  status: DeviceStatus;
  patientId?: string | null;
  lastSeenAtUtc?: string | null;
};

export type RegisterDeviceInput = {
  deviceId: string;
  deviceTypeCode: string;
  manufacturer?: string | null;
  model?: string | null;
  serialNumber?: string | null;
  calibrationDueUtc?: string | null;
};

export const fetchDevices = async (take = 100): Promise<DeviceSummary[]> => {
  const response = await apiClient.get<HateoasEnvelope<DeviceSummary[]> | DeviceSummary[]>(BASE, {
    params: { take },
  });
  return unwrap(response.data);
};

export const fetchDeviceTypes = async (): Promise<DeviceType[]> => {
  const response = await apiClient.get<HateoasEnvelope<DeviceType[]> | DeviceType[]>(`${BASE}/types`);
  return unwrap(response.data);
};

export const registerDevice = async (input: RegisterDeviceInput): Promise<{ id: string }> => {
  const response = await apiClient.post<HateoasEnvelope<{ id: string }> | { id: string }>(BASE, input);
  return unwrap(response.data);
};

export const changeDeviceStatus = async (
  id: string,
  action: DeviceStatusAction,
): Promise<void> => {
  await apiClient.post(`${BASE}/${id}/status`, { action });
};

export const bindDevice = async (
  id: string,
  patientId: string,
  sessionId?: string | null,
): Promise<void> => {
  await apiClient.post(`${BASE}/${id}/bind`, { patientId, sessionId: sessionId ?? null });
};
