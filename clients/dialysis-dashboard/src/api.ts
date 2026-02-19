import { getAuthToken } from './auth/auth-token'

const API_BASE = '/api'

async function get<T>(path: string): Promise<T> {
  const token = getAuthToken()
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'X-Tenant-Id': 'default',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(API_BASE + path, { headers })
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
