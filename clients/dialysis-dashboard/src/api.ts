import { getAuthToken } from './auth/auth-token'

const API_BASE = '/api'

async function get<T>(path: string, accept?: string): Promise<T> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    Accept: accept ?? 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + path, { headers, cache: 'no-store' })
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

export async function getSessionsSummary(from?: string, to?: string): Promise<import('./types').SessionsSummaryReport> {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  return get(`/reports/sessions-summary?${params}`)
}

export async function getAlarmsBySeverity(from?: string, to?: string): Promise<import('./types').AlarmsBySeverityReport> {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  return get(`/reports/alarms-by-severity?${params}`)
}

export async function getPrescriptionCompliance(from?: string, to?: string): Promise<import('./types').PrescriptionComplianceReport> {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  return get(`/reports/prescription-compliance?${params}`)
}

export interface TimeSeriesObservation {
  id: string
  code: string
  value?: string
  unit?: string
  subId?: string
  observedAtUtc: string
  effectiveTime?: string
  channelName?: string
}

export async function getObservationsInTimeRange(
  sessionId: string,
  start: string,
  end: string
): Promise<{ sessionId: string; observations: TimeSeriesObservation[] }> {
  const params = new URLSearchParams({ start, end })
  return get(`/treatment-sessions/${encodeURIComponent(sessionId)}/observations?${params}`)
}

export async function getPatientByMrn(mrn: string): Promise<import('./types').PatientContext | null> {
  const res = await fetch(API_BASE + `/patients/mrn/${encodeURIComponent(mrn)}`, {
    headers: buildHeaders(),
    cache: 'no-store',
  })
  if (res.status === 404) return null
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

export async function getTreatmentSession(sessionId: string): Promise<import('./types').TreatmentSessionContext | null> {
  const res = await fetch(API_BASE + `/treatment-sessions/${encodeURIComponent(sessionId)}`, {
    headers: buildHeaders(),
    cache: 'no-store',
  })
  if (res.status === 404) return null
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  const raw = await res.json()
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
          prescriptionConfirmed: raw.preAssessment.prescriptionConfirmed ?? false,
          painSymptomNotes: raw.preAssessment.painSymptomNotes,
          recordedAt: raw.preAssessment.recordedAt,
          recordedBy: raw.preAssessment.recordedBy,
        }
      : undefined,
    therapyTimePrescribedMin: raw.therapyTimePrescribedMin,
    observations: raw.observations ?? [],
  }
}

export async function getAlarmsBySession(sessionId: string): Promise<import('./types').AlarmContext[]> {
  const params = new URLSearchParams({ sessionId })
  const res = await get<{ alarms: import('./types').AlarmContext[] }>(`/alarms?${params}`)
  return res.alarms ?? []
}

/** CDS: FHIR DetectedIssue bundle. Returns empty entry[] when no issue. */
export interface DetectedIssueBundle {
  resourceType: string
  type?: string
  entry?: Array<{ resource?: DetectedIssueResource }>
}

export interface DetectedIssueResource {
  resourceType: string
  id?: string
  code?: { coding?: Array<{ code?: string; display?: string }> }
  detail?: string
  severity?: string
  identified?: string
  evidence?: unknown[]
}

export async function getHypotensionRisk(sessionId: string): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId })
  return get<DetectedIssueBundle>(`/cds/hypotension-risk?${params}`, 'application/fhir+json')
}

export async function getPrescriptionComplianceCds(sessionId: string): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId })
  return get<DetectedIssueBundle>(`/cds/prescription-compliance?${params}`, 'application/fhir+json')
}

export async function getVenousPressureRisk(sessionId: string): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId })
  return get<DetectedIssueBundle>(`/cds/venous-pressure-risk?${params}`, 'application/fhir+json')
}

export async function getBloodLeakRisk(sessionId: string): Promise<DetectedIssueBundle> {
  const params = new URLSearchParams({ sessionId })
  return get<DetectedIssueBundle>(`/cds/blood-leak-risk?${params}`, 'application/fhir+json')
}

/** FHIR AuditEvent bundle for timeline/traceability. */
export interface AuditEventBundle {
  resourceType: string
  entry?: Array<{ resource?: AuditEventResource }>
}

export interface AuditEventResource {
  resourceType: string
  type?: { code?: string }
  action?: string
  recorded?: string
  outcome?: string
  outcomeDesc?: string
  agent?: Array<{ altId?: string; name?: string }>
  entity?: Array<{ name?: string; description?: string; detail?: Array<{ type?: string; value?: string | { value?: string } }> }>
}

export async function getTreatmentAuditEvents(count = 100): Promise<AuditEventBundle> {
  return get<AuditEventBundle>(`/treatment-sessions/audit-events?count=${count}`, 'application/fhir+json')
}

export async function getAlarmAuditEvents(count = 100): Promise<AuditEventBundle> {
  return get<AuditEventBundle>(`/alarms/audit-events?count=${count}`, 'application/fhir+json')
}

export async function recordPreAssessment(
  sessionId: string,
  data: {
    preWeightKg?: number
    bpSystolic?: number
    bpDiastolic?: number
    accessTypeValue?: string
    prescriptionConfirmed: boolean
    painSymptomNotes?: string
  }
): Promise<{ sessionId: string; recordedAt: string }> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + `/treatment-sessions/${encodeURIComponent(sessionId)}/pre-assessment`, {
    method: 'POST',
    headers,
    body: JSON.stringify(data),
    cache: 'no-store',
  })
  if (res.status === 404) throw new Error('Session not found')
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

export async function signTreatmentSession(sessionId: string): Promise<{ sessionId: string; signedAt: string; signedBy?: string }> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + `/treatment-sessions/${encodeURIComponent(sessionId)}/sign`, {
    method: 'POST',
    headers,
    body: JSON.stringify({}),
    cache: 'no-store',
  })
  if (res.status === 404) throw new Error('Session not found')
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

export async function completeTreatmentSession(sessionId: string): Promise<{ sessionId: string; status: string }> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Accept: 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + `/treatment-sessions/${encodeURIComponent(sessionId)}/complete`, {
    method: 'POST',
    headers,
    cache: 'no-store',
  })
  if (res.status === 404) throw new Error('Session not found')
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

function buildHeaders(): Record<string, string> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`
  return headers
}

export async function getTreatmentSessions(limit = 50): Promise<string[]> {
  const from = new Date()
  from.setDate(from.getDate() - 7)
  const to = new Date()
  const params = new URLSearchParams()
  params.set('limit', String(limit))
  params.set('dateFrom', from.toISOString())
  params.set('dateTo', to.toISOString())
  const bundle = await get<{
    entry?: Array<{ resource?: { resourceType?: string; id?: string } }>
  }>(`/treatment-sessions/fhir?${params}`, 'application/fhir+json')
  const ids: string[] = []
  for (const e of bundle.entry ?? []) {
    const res = e.resource
    if (res?.resourceType === 'Procedure' && res.id?.startsWith('proc-')) {
      ids.push(res.id.replace(/^proc-/, ''))
    }
  }
  return ids
}
