// Per-adapter parameter schemas the NewChannelDialog renders as structured form fields. Static
// here (one source of truth alongside the dialog) — eventually the dialog could fetch live JSON
// Schemas from /api/v1/admin/connectors/outbound/{kind}/schema, but only a handful of
// adapters publish a schema today (HTTP does; TCP/SMTP/etc. don't) and a fully schema-driven form
// adds a layer of indirection that isn't worth the cost for this slice.

export type AdapterFieldType = "string" | "number" | "boolean" | "json";

export type AdapterField = {
  key: string;
  label: string;
  type: AdapterFieldType;
  required?: boolean;
  placeholder?: string;
  hint?: string;
  defaultValue?: string | number | boolean;
};

export type AdapterSchema = {
  kind: string;
  description: string;
  fields: AdapterField[];
};

export const ADAPTER_SCHEMAS: Record<string, AdapterSchema> = {
  http: {
    kind: "http",
    description: "POST or PUT the route payload to a downstream REST endpoint.",
    fields: [
      {
        key: "url",
        label: "URL",
        type: "string",
        required: true,
        placeholder: "https://partner.example/inbound",
      },
      {
        key: "method",
        label: "Method",
        type: "string",
        defaultValue: "POST",
        hint: "POST / PUT / PATCH",
      },
      {
        key: "headers",
        label: "Headers (JSON)",
        type: "json",
        placeholder: '{"X-Foo":"bar"}',
        hint: "JSON object of static request headers.",
      },
      {
        key: "authentication",
        label: "Authentication (JSON)",
        type: "json",
        placeholder: '{"kind":"bearer","token":"..."}',
        hint: "{ kind: bearer | apiKey | basic | oauth2 }",
      },
    ],
  },
  tcp: {
    kind: "tcp",
    description: "Raw TCP send to a downstream HL7 listener or socket-based partner.",
    fields: [
      {
        key: "host",
        label: "Host",
        type: "string",
        required: true,
        placeholder: "downstream.example",
      },
      { key: "port", label: "Port", type: "number", required: true, defaultValue: 2575 },
      {
        key: "framing",
        label: "Framing",
        type: "string",
        defaultValue: "Mllp",
        hint: "Mllp / None / LineFeed / LengthPrefixed",
      },
    ],
  },
  file: {
    kind: "file",
    description: "Write the route payload to disk. Useful for archives and sftp-mounted drops.",
    fields: [
      {
        key: "directory",
        label: "Directory",
        type: "string",
        required: true,
        placeholder: "/var/spool/hl7-out",
      },
      {
        key: "fileNameTemplate",
        label: "File-name template",
        type: "string",
        defaultValue: "{flowId}-{messageId}.hl7",
        hint: "Tokens: {flowId} {messageId} {timestamp}",
      },
    ],
  },
  smtp: {
    kind: "smtp",
    description: "Email the route payload (or a derived summary) via SMTP.",
    fields: [
      {
        key: "host",
        label: "SMTP host",
        type: "string",
        required: true,
        placeholder: "smtp.example.com",
      },
      { key: "port", label: "Port", type: "number", defaultValue: 587 },
      { key: "to", label: "To", type: "string", required: true, placeholder: "ops@example.com" },
      { key: "subject", label: "Subject", type: "string", placeholder: "{flowName} dispatch" },
    ],
  },
  database: {
    kind: "database",
    description: "Execute a parameterised INSERT/UPDATE against a configured database connection.",
    fields: [
      {
        key: "connectionStringName",
        label: "Connection-string name",
        type: "string",
        required: true,
        placeholder: "OracleHL7",
      },
      {
        key: "statement",
        label: "SQL statement",
        type: "string",
        required: true,
        placeholder: "INSERT INTO hl7_inbox (payload, received) VALUES (@payload, @now)",
      },
      {
        key: "parameters",
        label: "Parameters (JSON)",
        type: "json",
        placeholder: '[{"name":"@payload","value":"$payload"}]',
      },
    ],
  },
  "channel-writer": {
    kind: "channel-writer",
    description:
      "Mirth-style in-process redirect — hands the payload to another SmartConnect flow.",
    fields: [
      {
        key: "targetFlowId",
        label: "Target flow id",
        type: "string",
        required: true,
        placeholder: "GUID of the receiving flow",
      },
      {
        key: "metadataPropagation",
        label: "Metadata propagation",
        type: "string",
        defaultValue: "All",
        hint: "All / None",
      },
    ],
  },
  "transponder-bus": {
    kind: "transponder-bus",
    description:
      "Publish the payload onto the Transponder bus so downstream modules can subscribe via SmartConnectRoutedPayloadIntegrationEvent.",
    fields: [
      {
        key: "routingHint",
        label: "Routing hint",
        type: "string",
        placeholder: "ORU^R01",
        hint: "Helps subscribers route without parsing the payload.",
      },
      {
        key: "headers",
        label: "Headers (JSON)",
        type: "json",
        placeholder: '{"sourceModule":"smartconnect"}',
      },
    ],
  },
  "pass-through": {
    kind: "pass-through",
    description: "No-op destination — useful when only the ledger row matters.",
    fields: [],
  },
};

export const getSchema = (kind: string): AdapterSchema | undefined => ADAPTER_SCHEMAS[kind];

// Route-filter and transform-stage schemas: parallel to ADAPTER_SCHEMAS but for the inbound side.
// Today only the verify-* plugins publish editable parameters; everything else is parameter-free
// and renders without a form.
export const ROUTE_FILTER_SCHEMAS: Record<string, AdapterSchema> = {
  "verify-hl7": {
    kind: "verify-hl7",
    description:
      "Drop messages that don't parse as HL7 v2, that miss any of the required segments, or that are below the minimum version. Pair with verify-hl7-strict when the channel should fail loudly instead of silently dropping.",
    fields: [
      {
        key: "requiredSegments",
        label: "Required segments (JSON array)",
        type: "json",
        placeholder: '["MSH","PID","PV1"]',
        hint: "Drop any HL7 v2 message that lacks one of these segments. Empty array = no segment check.",
      },
      {
        key: "minVersion",
        label: "Minimum HL7 version",
        type: "string",
        placeholder: "2.5",
        hint: "Compared component-wise against MSH.12. Leave blank to allow any version.",
      },
    ],
  },
  "verify-fhir": {
    kind: "verify-fhir",
    description:
      "Validate the message as FHIR R4 against the configured profile (US Core by default). Drops on any validation error.",
    fields: [
      {
        key: "profileUri",
        label: "Profile URI",
        type: "string",
        placeholder: "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient",
        hint: "Implementation-Guide profile URI. Blank = use the host's default validator profile.",
      },
    ],
  },
};

export const TRANSFORM_STAGE_SCHEMAS: Record<string, AdapterSchema> = {
  "verify-hl7-strict": {
    kind: "verify-hl7-strict",
    description: "Strict variant of verify-hl7 — throws (fails the route) instead of dropping.",
    fields: ROUTE_FILTER_SCHEMAS["verify-hl7"]!.fields,
  },
  "verify-fhir-strict": {
    kind: "verify-fhir-strict",
    description: "Strict variant of verify-fhir — throws (fails the route) on validation errors.",
    fields: ROUTE_FILTER_SCHEMAS["verify-fhir"]!.fields,
  },
};

export const getRouteFilterSchema = (kind: string): AdapterSchema | undefined =>
  ROUTE_FILTER_SCHEMAS[kind];

export const getTransformStageSchema = (kind: string): AdapterSchema | undefined =>
  TRANSFORM_STAGE_SCHEMAS[kind];
