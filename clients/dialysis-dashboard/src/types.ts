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

// Real-time SignalR messages
export interface ObservationRecordedMessage {
  sessionId: string
  observationId: string
  code: string
  value?: string
  unit?: string
  subId?: string
  channelName?: string
  _receivedAt?: number
}

export interface AlarmRecordedMessage {
  alarmId: string
  alarmType?: string
  eventPhase: string
  alarmState: string
  deviceId?: string
  sessionId?: string
  occurredAt: string
}

export interface SignalRTransportEnvelope {
  messageId?: string
  messageType?: string
  contentType?: string
  sentTime?: string
  body: number[] | string // byte array (UTF-8) as number[] or base64 string
}

// Context Layer types
export interface PatientContext {
  id: string
  medicalRecordNumber: string
  firstName: string
  lastName: string
  dateOfBirth?: string
  gender?: string
}

export interface ObservationDto {
  code: string
  value?: string
  unit?: string
  subId?: string
  observedAtUtc?: string
  effectiveTime?: string
  channelName?: string
}

export interface TreatmentSessionContext {
  sessionId: string
  patientMrn?: string
  deviceId?: string
  status: string
  startedAt?: string
  endedAt?: string
  signedAt?: string
  signedBy?: string
  preAssessment?: PreAssessmentContext
  therapyTimePrescribedMin?: number
  observations: ObservationDto[]
}

export interface PreAssessmentContext {
  preWeightKg?: number
  bpSystolic?: number
  bpDiastolic?: number
  accessTypeValue?: string
  prescriptionConfirmed: boolean
  painSymptomNotes?: string
  recordedAt: string
  recordedBy?: string
}

export interface AlarmContext {
  id: string
  alarmType?: string
  alarmState: string
  eventPhase: string
  sessionId?: string
  occurredAt: string
  priority?: string
}

/** Risk Layer: unified alert for display. */
export type AlertSeverity = 'critical' | 'warning' | 'info'

export interface Alert {
  id: string
  type: 'hypotension' | 'uf-exceeded' | 'missed-documentation' | 'prescription-mismatch' | 'device-alarm'
  severity: AlertSeverity
  title: string
  detail?: string
  actionLink?: string
  actionLabel?: string
  /** Payload for action handlers (e.g. patientMrn for prescription review). */
  actionPayload?: { patientMrn?: string }
  source: 'cds' | 'alarm' | 'derived'
  occurredAt?: string
  acknowledged?: boolean
}

/** Traceability Layer: timeline event for audit. */
export interface TimelineEvent {
  id: string
  type: 'state-transition' | 'audit' | 'alarm' | 'key-event'
  when: string
  who?: string
  what: string
  detail?: string
  resourceType?: string
  resourceId?: string
}
