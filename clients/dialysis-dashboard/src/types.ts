export interface SessionsSummaryReport {
  sessionCount: number
  avgDurationMinutes: number
  from: string
  to: string
}

export interface AlarmsBySeverityReport {
  bySeverity: Record<string, number>
  from: string
  to: string
}

export interface PrescriptionComplianceReport {
  compliantCount: number
  totalEvaluated: number
  compliancePercent: number
  from: string
  to: string
}
