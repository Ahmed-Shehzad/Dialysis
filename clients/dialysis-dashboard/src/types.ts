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
