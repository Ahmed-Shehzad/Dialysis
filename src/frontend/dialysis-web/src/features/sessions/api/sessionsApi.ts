import { apiClient } from "@/lib/api/apiClient";
import type { VitalsReading } from "@/features/vitals/api/vitalsApi";

export type DialysisSessionSummary = {
  id: string;
  patientId: string;
  status: "Scheduled" | "InProgress" | "Completed" | "Aborted" | "Cancelled" | "Paused";
  scheduledStartUtc: string;
  actualStartUtc?: string | null;
  actualEndUtc?: string | null;
  machineId?: string | null;
};

export const fetchActiveSessions = async (activeOnly = true): Promise<DialysisSessionSummary[]> => {
  const response = await apiClient.get<DialysisSessionSummary[]>(
    "/api/pdms/api/v1.0/sessions",
    { params: { activeOnly, take: 100 } },
  );
  return response.data ?? [];
};

export const fetchSessionReadings = async (
  sessionId: string,
  limit = 200,
): Promise<VitalsReading[]> => {
  const response = await apiClient.get<VitalsReading[]>(
    `/api/pdms/api/v1.0/sessions/${sessionId}/readings`,
    { params: { limit } },
  );
  return response.data ?? [];
};

export type VascularAccessKind =
  | "ArteriovenousFistula"
  | "ArteriovenousGraft"
  | "CentralVenousCatheter"
  | "PeritonealCatheter";

export type ScheduleSessionRequest = {
  patientId: string;
  scheduledStartUtc: string;
  dialyzerModel: string;
  prescribedDurationMinutes: number;
  bloodFlowRateMlPerMin: number;
  dialysateFlowRateMlPerMin: number;
  dialysatePotassiumMmolPerL: number;
  dialysateCalciumMmolPerL: number;
  dialysateSodiumMmolPerL: number;
  targetUfVolumeLiters: number;
  anticoagulationProtocolCode: string;
  accessKind: VascularAccessKind;
  accessSite: string;
  accessEstablishedOn: string; // YYYY-MM-DD
};

export const scheduleSession = async (body: ScheduleSessionRequest): Promise<string> => {
  const response = await apiClient.post<{ id: string }>("/api/pdms/api/v1.0/sessions", body);
  return response.data.id;
};

export const startSession = (sessionId: string) =>
  apiClient.post(`/api/pdms/api/v1.0/sessions/${sessionId}/start`);

export const completeSession = (sessionId: string, achievedUfVolumeLiters: number) =>
  apiClient.post(`/api/pdms/api/v1.0/sessions/${sessionId}/complete`, { achievedUfVolumeLiters });

export const abortSession = (sessionId: string, reasonCode: string) =>
  apiClient.post(`/api/pdms/api/v1.0/sessions/${sessionId}/abort`, { reasonCode });
