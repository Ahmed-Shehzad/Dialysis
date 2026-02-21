import axios, { type AxiosInstance } from "axios";
import { getAuthToken } from "./auth/auth-token";

const API_BASE = "/api";

function createApiClient(): AxiosInstance {
  const client = axios.create({
    baseURL: API_BASE,
    headers: {
      Accept: "application/json",
      "X-Tenant-Id": "default",
    },
  });

  client.interceptors.request.use((config) => {
    const token = getAuthToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  return client;
}

const api = createApiClient();

export async function getSessionsSummary(
  from?: string,
  to?: string
): Promise<import("./types").SessionsSummaryReport> {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const { data } = await api.get<import("./types").SessionsSummaryReport>(
    `/reports/sessions-summary?${params}`
  );
  return data;
}

export async function getAlarmsBySeverity(
  from?: string,
  to?: string
): Promise<import("./types").AlarmsBySeverityReport> {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const { data } = await api.get<import("./types").AlarmsBySeverityReport>(
    `/reports/alarms-by-severity?${params}`
  );
  return data;
}

export async function getPrescriptionCompliance(
  from?: string,
  to?: string
): Promise<import("./types").PrescriptionComplianceReport> {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const { data } = await api.get<import("./types").PrescriptionComplianceReport>(
    `/reports/prescription-compliance?${params}`
  );
  return data;
}

export interface PrescriptionByMrnResponse {
  orderId: string;
  therapyModality: string;
  bloodFlowRateMlMin?: number;
  ufTargetVolumeMl?: number;
  ufRateMlH?: number;
}

export async function getPrescriptionByMrn(
  mrn: string
): Promise<PrescriptionByMrnResponse | null> {
  try {
    const { data: raw } = await api.get(
      `/prescriptions/${encodeURIComponent(mrn)}`
    );
    return {
      orderId: raw.orderId ?? raw.OrderId,
      therapyModality: raw.therapyModality ?? raw.TherapyModality ?? "—",
      bloodFlowRateMlMin: raw.bloodFlowRateMlMin ?? raw.BloodFlowRateMlMin,
      ufTargetVolumeMl: raw.ufTargetVolumeMl ?? raw.UfTargetVolumeMl,
      ufRateMlH: raw.ufRateMlH ?? raw.UfRateMlH,
    };
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) return null;
    throw err;
  }
}

export interface TimeSeriesObservation {
  id: string;
  code: string;
  value?: string;
  unit?: string;
  subId?: string;
  observedAtUtc: string;
  effectiveTime?: string;
  channelName?: string;
}

export async function getObservationsInTimeRange(
  sessionId: string,
  start: string,
  end: string
): Promise<{ sessionId: string; observations: TimeSeriesObservation[] }> {
  const params = new URLSearchParams({ start, end });
  const { data } = await api.get<{
    sessionId: string;
    observations: TimeSeriesObservation[];
  }>(`/treatment-sessions/${encodeURIComponent(sessionId)}/observations?${params}`);
  return data;
}

export async function getPatientByMrn(
  mrn: string
): Promise<import("./types").PatientContext | null> {
  try {
    const { data } = await api.get<import("./types").PatientContext>(
      `/patients/mrn/${encodeURIComponent(mrn)}`
    );
    return data;
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) return null;
    throw err;
  }
}

export async function getTreatmentSession(
  sessionId: string
): Promise<import("./types").TreatmentSessionContext | null> {
  try {
    const { data: raw } = await api.get(
      `/treatment-sessions/${encodeURIComponent(sessionId)}`
    );
    return {
      sessionId: raw.sessionId,
      patientMrn: raw.patientMrn,
      deviceId: raw.deviceId,
      status: raw.status,
      startedAt: raw.startedAt,
      endedAt: raw.endedAt,
      signedAt: raw.signedAt,
      signedBy: raw.signedBy,
      preAssessment: raw.preAssessment
        ? {
            preWeightKg: raw.preAssessment.preWeightKg,
            bpSystolic: raw.preAssessment.bpSystolic,
            bpDiastolic: raw.preAssessment.bpDiastolic,
            accessTypeValue: raw.preAssessment.accessTypeValue,
            prescriptionConfirmed:
              raw.preAssessment.prescriptionConfirmed ?? false,
            painSymptomNotes: raw.preAssessment.painSymptomNotes,
            recordedAt: raw.preAssessment.recordedAt,
            recordedBy: raw.preAssessment.recordedBy,
          }
        : undefined,
      therapyTimePrescribedMin: raw.therapyTimePrescribedMin,
      observations: raw.observations ?? [],
    };
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) return null;
    throw err;
  }
}

export async function getAlarmsBySession(
  sessionId: string
): Promise<import("./types").AlarmContext[]> {
  const params = new URLSearchParams({ sessionId });
  const { data } = await api.get<{
    alarms: import("./types").AlarmContext[];
  }>(`/alarms?${params}`);
  return data.alarms ?? [];
}

/** CDS: FHIR DetectedIssue bundle. Returns empty entry[] when no issue. */
export interface DetectedIssueBundle {
  resourceType: string;
  type?: string;
  entry?: Array<{ resource?: DetectedIssueResource }>;
}

export interface DetectedIssueResource {
  resourceType: string;
  id?: string;
  code?: { coding?: Array<{ code?: string; display?: string }> };
  detail?: string;
  severity?: string;
  identified?: string;
  evidence?: unknown[];
}

export async function getHypotensionRisk(
  sessionId: string
): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId });
  const { data } = await api.get<DetectedIssueBundle>(
    `/cds/hypotension-risk?${params}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

export async function getPrescriptionComplianceCds(
  sessionId: string
): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId });
  const { data } = await api.get<DetectedIssueBundle>(
    `/cds/prescription-compliance?${params}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

export async function getVenousPressureRisk(
  sessionId: string
): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId });
  const { data } = await api.get<DetectedIssueBundle>(
    `/cds/venous-pressure-risk?${params}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

export async function getBloodLeakRisk(
  sessionId: string
): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId });
  const { data } = await api.get<DetectedIssueBundle>(
    `/cds/blood-leak-risk?${params}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

/** FHIR AuditEvent bundle for timeline/traceability. */
export interface AuditEventBundle {
  resourceType: string;
  entry?: Array<{ resource?: AuditEventResource }>;
}

export interface AuditEventResource {
  resourceType: string;
  type?: { code?: string };
  action?: string;
  recorded?: string;
  outcome?: string;
  outcomeDesc?: string;
  agent?: Array<{ altId?: string; name?: string }>;
  entity?: Array<{
    name?: string;
    description?: string;
    detail?: Array<{ type?: string; value?: string | { value?: string } }>;
  }>;
}

export async function getTreatmentAuditEvents(
  count = 100
): Promise<AuditEventBundle> {
  const { data } = await api.get<AuditEventBundle>(
    `/treatment-sessions/audit-events?count=${count}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

export async function getAlarmAuditEvents(
  count = 100
): Promise<AuditEventBundle> {
  const { data } = await api.get<AuditEventBundle>(
    `/alarms/audit-events?count=${count}`,
    { headers: { Accept: "application/fhir+json" } }
  );
  return data;
}

export async function recordPreAssessment(
  sessionId: string,
  data: {
    preWeightKg?: number;
    bpSystolic?: number;
    bpDiastolic?: number;
    accessTypeValue?: string;
    prescriptionConfirmed: boolean;
    painSymptomNotes?: string;
  }
): Promise<{ sessionId: string; recordedAt: string }> {
  try {
    const { data: result } = await api.post<{
      sessionId: string;
      recordedAt: string;
    }>(`/treatment-sessions/${encodeURIComponent(sessionId)}/pre-assessment`, data);
    return result;
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) {
      throw new Error("Session not found");
    }
    throw err;
  }
}

export async function signTreatmentSession(
  sessionId: string
): Promise<{ sessionId: string; signedAt: string; signedBy?: string }> {
  try {
    const { data } = await api.post<{
      sessionId: string;
      signedAt: string;
      signedBy?: string;
    }>(`/treatment-sessions/${encodeURIComponent(sessionId)}/sign`, {});
    return data;
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) {
      throw new Error("Session not found");
    }
    throw err;
  }
}

export async function completeTreatmentSession(
  sessionId: string
): Promise<{ sessionId: string; status: string }> {
  try {
    const { data } = await api.post<{ sessionId: string; status: string }>(
      `/treatment-sessions/${encodeURIComponent(sessionId)}/complete`
    );
    return data;
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) {
      throw new Error("Session not found");
    }
    throw err;
  }
}

export async function getTreatmentSessions(
  limit = 50
): Promise<string[]> {
  const from = new Date();
  from.setDate(from.getDate() - 7);
  const to = new Date();
  const params = new URLSearchParams();
  params.set("limit", String(limit));
  params.set("dateFrom", from.toISOString());
  params.set("dateTo", to.toISOString());
  const { data: bundle } = await api.get<{
    entry?: Array<{ resource?: { resourceType?: string; id?: string } }>;
  }>(`/treatment-sessions/fhir?${params}`, {
    headers: { Accept: "application/fhir+json" },
  });
  const ids: string[] = [];
  for (const e of bundle.entry ?? []) {
    const res = e.resource;
    if (res?.resourceType === "Procedure" && res.id?.startsWith("proc-")) {
      ids.push(res.id.replace(/^proc-/, ""));
    }
  }
  return ids;
}
