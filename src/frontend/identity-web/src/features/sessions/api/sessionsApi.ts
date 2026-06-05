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
  /** When the session entered its current pause, or null while running / ended. */
  pausedAtUtc?: string | null;
  /** Total seconds spent paused so far (excluding any open pause) — lets the live timer exclude pauses. */
  accumulatedPausedSeconds?: number | null;
};

export const fetchActiveSessions = async (activeOnly = true): Promise<DialysisSessionSummary[]> => {
  const response = await apiClient.get<DialysisSessionSummary[]>("/admin/api/_x/pdms/api/v1.0/sessions", {
    params: { activeOnly, take: 100 },
  });
  return response.data ?? [];
};

/**
 * Patient-scoped recent treatments. Backs the patient-portal "Recent treatments" panel
 * and any clinician view that needs one patient's session history.
 */
export const fetchSessionsByPatient = async (
  patientId: string,
  lookbackDays = 90,
  take = 20,
): Promise<DialysisSessionSummary[]> => {
  const response = await apiClient.get<DialysisSessionSummary[]>(
    `/admin/api/_x/pdms/api/v1.0/sessions/by-patient/${patientId}`,
    { params: { lookbackDays, take } },
  );
  return response.data ?? [];
};

export type SessionPrescription = {
  dialyzerModel: string;
  prescribedDurationMinutes: number;
  bloodFlowRateMlPerMin: number;
  dialysateFlowRateMlPerMin: number;
  dialysatePotassiumMmolPerL: number;
  dialysateCalciumMmolPerL: number;
  dialysateSodiumMmolPerL: number;
  targetUfVolumeLiters: number;
  anticoagulationProtocolCode: string;
};

export type VascularAccessSummary = {
  kind: string;
  site: string;
  establishedOn: string;
};

export type ReadingStats = {
  count: number;
  systolicMin: number | null;
  systolicMax: number | null;
  systolicAvg: number | null;
  diastolicMin: number | null;
  diastolicMax: number | null;
  diastolicAvg: number | null;
  heartRateMin: number | null;
  heartRateMax: number | null;
  heartRateAvg: number | null;
  lastUltrafiltrationRateMlPerHour: number | null;
  firstObservedAtUtc: string | null;
  lastObservedAtUtc: string | null;
};

export type SessionSummary = {
  id: string;
  patientId: string;
  status: DialysisSessionSummary["status"];
  scheduledStartUtc: string;
  actualStartUtc: string | null;
  actualEndUtc: string | null;
  actualDurationMinutes: number | null;
  achievedUfVolumeLiters: number | null;
  ufAchievementPercent: number | null;
  abortReasonCode: string | null;
  machineId: string | null;
  pausedAtUtc: string | null;
  accumulatedPausedSeconds: number;
  prescription: SessionPrescription;
  access: VascularAccessSummary;
  readings: ReadingStats;
};

export const fetchSessionSummary = async (sessionId: string): Promise<SessionSummary> => {
  const response = await apiClient.get<SessionSummary>(
    `/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/summary`,
  );
  return response.data;
};

export const fetchSessionReadings = async (
  sessionId: string,
  limit = 200,
): Promise<VitalsReading[]> => {
  const response = await apiClient.get<VitalsReading[]>(
    `/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/readings`,
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
  const response = await apiClient.post<{ id: string }>("/admin/api/_x/pdms/api/v1.0/sessions", body);
  return response.data.id;
};

export const startSession = (sessionId: string) =>
  apiClient.post(`/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/start`);

export const pauseSession = (sessionId: string) =>
  apiClient.post(`/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/pause`);

export const resumeSession = (sessionId: string) =>
  apiClient.post(`/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/resume`);

export const completeSession = (sessionId: string, achievedUfVolumeLiters: number) =>
  apiClient.post(`/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/complete`, { achievedUfVolumeLiters });

export const abortSession = (sessionId: string, reasonCode: string) =>
  apiClient.post(`/admin/api/_x/pdms/api/v1.0/sessions/${sessionId}/abort`, { reasonCode });
