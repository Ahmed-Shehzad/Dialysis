// Shared SmartConnect wire types. Enums match the .NET enum int values exactly
// (System.Text.Json default serialization on the backend is numeric).
// Mapping helpers live alongside each tab's API module.

// The SmartConnect module serves its operator/admin API under /api/v1/admin (aligned with the
// other modules' /api convention), reached through the gateway's /smartconnect/api/* → BFF route
// (the BFF strips the /smartconnect base, forwarding /api/v1/admin/* to the module). Inbound HL7
// (/smartconnect/v1/messages, /flows/{id}/messages) stays on its own external contract.
export const SMARTCONNECT_PREFIX = "/smartconnect/api/v1";
export const ADMIN_PREFIX = `${SMARTCONNECT_PREFIX}/admin`;

// --- Flows ---------------------------------------------------------------
export const FlowRuntimeState = {
  Stopped: 0,
  Started: 1,
  Paused: 2,
} as const;
export type FlowRuntimeStateValue = (typeof FlowRuntimeState)[keyof typeof FlowRuntimeState];

export const FlowRuntimeStateLabel: Record<FlowRuntimeStateValue, string> = {
  0: "Stopped",
  1: "Started",
  2: "Paused",
};

export type FlowScripts = {
  deploy?: string | null;
  undeploy?: string | null;
  preprocessor?: string | null;
  postprocessor?: string | null;
};

export type RouteFilterSlot = {
  kind: string;
  propertiesJson?: string | null;
};

export type TransformStageSlot = RouteFilterSlot;

export type OutboundRouteSlot = {
  ordinal: number;
  kind: string;
  propertiesJson?: string | null;
  filters?: RouteFilterSlot[];
  transforms?: TransformStageSlot[];
};

export type AttachmentHandlerSlot = RouteFilterSlot;

export type IntegrationFlowPipelineDefinition = {
  routeFilters: RouteFilterSlot[];
  attachmentHandler?: AttachmentHandlerSlot | null;
  sourceTransformStages: TransformStageSlot[];
  outboundRoutesSequential: boolean;
  outboundRoutes: OutboundRouteSlot[];
  scripts?: FlowScripts | null;
  linkedLibraryIds: string[];
};

export type ChannelAttachmentReference = {
  name: string;
  mimeType: string;
  /** Base64-encoded contents. Capped at 1 MiB (decoded) by the backend. */
  base64Bytes: string;
  description?: string | null;
};

/** Allowed values for {@link IntegrationFlow.dataTypes}. */
export const CHANNEL_DATA_TYPES = [
  "HL7v2",
  "FHIR",
  "NCPDP",
  "JSON",
  "XML",
  "Binary",
  "Other",
] as const;
export type ChannelDataType = (typeof CHANNEL_DATA_TYPES)[number];

export type IntegrationFlow = {
  id: string;
  name: string;
  runtimeState: FlowRuntimeStateValue;
  pipeline: IntegrationFlowPipelineDefinition;
  tags: string[];
  groupId?: string | null;
  description?: string | null;
  /** Declared accepted payload formats; surfaces in the channel list and dialog filter. */
  dataTypes: string[];
  /** Other flow ids this channel depends on; Start enforces they're all Started. */
  dependencies: string[];
  /** Channel-level reference docs (sample messages, profile JSON, vendor docs). */
  attachments: ChannelAttachmentReference[];
};

// --- Messages / Ledger ---------------------------------------------------
export const MessageLedgerStatus = {
  Received: 0,
  RouteFilterDropped: 1,
  OutboundSent: 2,
  OutboundFailed: 3,
  Completed: 4,
} as const;
export type MessageLedgerStatusValue =
  (typeof MessageLedgerStatus)[keyof typeof MessageLedgerStatus];

export const MessageLedgerStatusLabel: Record<MessageLedgerStatusValue, string> = {
  0: "Received",
  1: "RouteFilterDropped",
  2: "OutboundSent",
  3: "OutboundFailed",
  4: "Completed",
};

export type MessageLedgerEntry = {
  id: string;
  flowId: string;
  integrationMessageId: string;
  correlationId: string;
  status: MessageLedgerStatusValue;
  outboundRouteOrdinal?: number | null;
  detail?: string | null;
  /** Backend may return base64-encoded bytes or null. */
  payloadSnapshot?: string | null;
  /** Free-form metadata bag — `BATCH_METADATA_KEYS` for batch context (slice D2);
   * `LEDGER_SEARCH_KEYS` for the C2 indexed columns; arbitrary keys for source-transport
   * provenance. */
  metadata?: Record<string, string>;
  createdAtUtc: string;
};

export type MessageListResponse = {
  items: MessageLedgerEntry[];
  totalCount: number;
};

export type MessageListQuery = {
  flowId?: string;
  correlationIdPrefix?: string;
  from?: string;
  to?: string;
  status?: MessageLedgerStatusValue;
  /** Exact-match filter on the derived MSH-9 column (e.g. `ORU^R01`). */
  messageType?: string;
  /** Exact-match filter on the derived sender column (e.g. `MachineA@FACILITY`). */
  senderId?: string;
  /** Exact-match filter on the derived batch-id column — the inbound transport's source
   * identifier (e.g. a fully-qualified file path) shared by every record in a fan-out. */
  batchId?: string;
  skip?: number;
  take?: number;
};

/** Stable metadata keys for slice D2 batch context. Mirrors `BatchMetadataKeys` on the
 * backend so the dashboard can read batch info off a ledger entry without re-deriving it. */
export const BATCH_METADATA_KEYS = {
  BatchId: "smartconnect.batch.id",
  Sequence: "smartconnect.batch.sequence",
  Total: "smartconnect.batch.total",
  Source: "smartconnect.batch.source",
} as const;

export type FlowStatusCount = {
  status: MessageLedgerStatusValue;
  count: number;
};

// --- Alerts --------------------------------------------------------------
export const AlertErrorType = {
  Any: 0,
  OutboundFailure: 1,
  RouteFilterError: 2,
  TransformError: 3,
  PreProcessorError: 4,
  PostProcessorError: 5,
  AttachmentExtractError: 6,
} as const;
export type AlertErrorTypeValue = (typeof AlertErrorType)[keyof typeof AlertErrorType];

export const AlertErrorTypeLabel: Record<AlertErrorTypeValue, string> = {
  0: "Any",
  1: "OutboundFailure",
  2: "RouteFilterError",
  3: "TransformError",
  4: "PreProcessorError",
  5: "PostProcessorError",
  6: "AttachmentExtractError",
};

export type AlertErrorPattern = {
  errorType: AlertErrorTypeValue;
  regex?: string | null;
};

export type AlertActionSlot = {
  kind: string;
  propertiesJson?: string | null;
};

export type AlertRule = {
  id: string;
  name: string;
  enabled: boolean;
  description?: string | null;
  enabledFlowIds?: string[] | null;
  errorPatterns: AlertErrorPattern[];
  actions: AlertActionSlot[];
  /** .NET TimeSpan serializes as e.g. "00:05:00". null = no throttling. */
  throttleWindow?: string | null;
  revision: number;
  lastModifiedUtc: string;
};

export type AlertActionOutcome = {
  kind: string;
  succeeded: boolean;
  errorDetail?: string | null;
  responseSummary?: string | null;
  attemptedAtUtc: string;
};

export type AlertEvent = {
  id: string;
  ruleId: string;
  flowId?: string | null;
  messageId?: string | null;
  correlationId?: string | null;
  errorType: AlertErrorTypeValue;
  errorDetail?: string | null;
  occurredAtUtc: string;
  actionOutcomes: AlertActionOutcome[];
};

export type TestAlertRequest = {
  flowId?: string;
  messageId?: string;
  correlationId?: string;
  errorType?: AlertErrorTypeValue;
  errorDetail?: string;
};

// --- Code Template Libraries --------------------------------------------
export const CodeTemplateType = {
  Function: 0,
  DragAndDropCodeBlock: 1,
  CompiledCodeBlock: 2,
} as const;
export type CodeTemplateTypeValue = (typeof CodeTemplateType)[keyof typeof CodeTemplateType];

export const CodeTemplateTypeLabel: Record<CodeTemplateTypeValue, string> = {
  0: "Function",
  1: "DragAndDropCodeBlock",
  2: "CompiledCodeBlock",
};

export type CodeTemplate = {
  id: string;
  libraryId: string;
  name: string;
  code: string;
  type: CodeTemplateTypeValue;
  /** Backend ships as int[] (CodeTemplateContext flag enum). */
  contexts: number[];
  jsDoc?: string | null;
  revision: number;
  lastModifiedUtc: string;
  position: number;
};

export type CodeTemplateLibrary = {
  id: string;
  name: string;
  description?: string | null;
  linkedFlowIds: string[];
  autoLinkNewFlows: boolean;
  revision: number;
  lastModifiedUtc: string;
  templates: CodeTemplate[];
};

// --- Configuration Map ---------------------------------------------------
export const VariableMapScope = {
  Channel: "Channel",
  GlobalChannel: "GlobalChannel",
  Global: "Global",
  Configuration: "Configuration",
} as const;
export type VariableMapScopeValue = (typeof VariableMapScope)[keyof typeof VariableMapScope];

// --- Audit Events --------------------------------------------------------
export const AuditEventCategory = {
  FlowDeployed: 0,
  FlowUndeployed: 1,
  FlowStarted: 2,
  FlowStopped: 3,
  FlowPaused: 4,
  ConfigChanged: 5,
  UserAction: 6,
  Error: 7,
} as const;
export type AuditEventCategoryValue = (typeof AuditEventCategory)[keyof typeof AuditEventCategory];

export const AuditEventCategoryLabel: Record<AuditEventCategoryValue, string> = {
  0: "FlowDeployed",
  1: "FlowUndeployed",
  2: "FlowStarted",
  3: "FlowStopped",
  4: "FlowPaused",
  5: "ConfigChanged",
  6: "UserAction",
  7: "Error",
};

export const AuditEventLevel = {
  Info: 0,
  Warning: 1,
  Error: 2,
} as const;
export type AuditEventLevelValue = (typeof AuditEventLevel)[keyof typeof AuditEventLevel];

export const AuditEventLevelLabel: Record<AuditEventLevelValue, string> = {
  0: "Info",
  1: "Warning",
  2: "Error",
};

export type AuditEvent = {
  id: string;
  timestamp: string;
  category: AuditEventCategoryValue;
  level: AuditEventLevelValue;
  flowId?: string | null;
  userId?: string | null;
  summary: string;
  attributesJson?: string | null;
};

// --- Groups --------------------------------------------------------------
export type FlowGroup = {
  id: string;
  name: string;
  description?: string | null;
};

// --- Pruner / Retention --------------------------------------------------
export type PrunerOptions = {
  /** .NET TimeSpan string, e.g. "01:00:00". */
  interval: string;
  intervalHours: number;
  /** .NET TimeSpan string, e.g. "30.00:00:00". */
  retentionPeriod: string;
  retentionDays: number;
};
