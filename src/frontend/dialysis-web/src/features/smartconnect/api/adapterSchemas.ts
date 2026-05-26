// Per-adapter parameter schemas the NewChannelDialog renders as structured form fields. Static
// here (one source of truth alongside the dialog) — eventually the dialog could fetch live JSON
// Schemas from /smartconnect/v1/admin/connectors/outbound/{kind}/schema, but only a handful of
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
