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
