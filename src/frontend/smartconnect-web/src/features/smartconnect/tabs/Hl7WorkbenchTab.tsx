import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { fetchFlows } from "../api/flows";
import {
  workbenchDispatch,
  workbenchParseHl7,
  workbenchValidateHl7,
  type WorkbenchDispatchResponse,
  type WorkbenchParseResponse,
  type WorkbenchValidateResponse,
} from "../api/workbench";
import { humanizeError } from "@/lib/api/humanizeError";

// HL7 Workbench — paste-your-own HL7 v2 message, parse → validate → optionally dispatch through any
// existing HL7v2 channel. No sample payloads ship with the product; the empty state explicitly tells
// the operator to bring their own message. Supports HL7 v2.1 through v2.8+ uniformly because
// SmartConnect's parser is encoding-driven (it reads MSH-2 to pick separators) rather than
// version-gated.

const detectVersion = (text: string): string | null => {
  if (!text.startsWith("MSH")) return null;
  // Split MSH segment fields; MSH-12 is the version.
  const firstLine = text.split(/\r?\n|\r/, 1)[0] ?? "";
  const sep = firstLine[3] ?? "|";
  const fields = firstLine.split(sep);
  // MSH-12 is at index 11 (MSH-1 = sep, MSH-2 = encoding chars stored at fields[1], MSH-3 = fields[2], ...).
  return fields[11] ?? null;
};

// HL7 ACK code → human-readable label + tone (accept / error / reject).
// Covers both original ACKs (AA / AE / AR) and the enhanced commit-mode ACKs added in v2.4+
// (CA / CE / CR). Anything else falls through to "Unknown" tone-neutral.
const ACK_CODES: Record<string, { label: string; tone: "accept" | "error" | "reject" }> = {
  AA: { label: "Application Accept", tone: "accept" },
  AE: { label: "Application Error", tone: "error" },
  AR: { label: "Application Reject", tone: "reject" },
  CA: { label: "Commit Accept", tone: "accept" },
  CE: { label: "Commit Error", tone: "error" },
  CR: { label: "Commit Reject", tone: "reject" },
};

type ParsedAck = {
  ackCode: string;
  ackLabel: string;
  tone: "accept" | "error" | "reject" | "unknown";
  controlId: string | null;
  textMessage: string | null;
  errors: Array<{
    errorCode: string | null;
    severity: string | null;
    text: string | null;
  }>;
};

// Lightweight client-side ACK detector. Pure string-splitting — HL7's pipe / caret / tilde
// encoding is read straight off MSH-2 so any of the standard delimiter sets parses. Returns
// `null` for any payload that isn't an MSH-anchored ACK, leaving the caller to fall back to
// the raw-text display.
const parseAck = (text: string | null | undefined): ParsedAck | null => {
  if (!text || !text.startsWith("MSH")) return null;
  const lines = text.split(/\r?\n|\r/).filter((l) => l.length > 0);
  if (lines.length === 0) return null;
  const firstLine = lines[0]!;
  const fieldSep = firstLine[3] ?? "|";
  const componentSep = firstLine[4] ?? "^";
  const mshFields = firstLine.split(fieldSep);
  // MSH-9 lives at fields[8] (since MSH-1 = sep, MSH-2 = encoding chars stored at fields[1]).
  const msh9 = mshFields[8] ?? "";
  if (!msh9.startsWith("ACK")) return null;

  const msaLine = lines.find((l) => l.startsWith("MSA"));
  if (!msaLine) return null;
  const msaFields = msaLine.split(fieldSep);
  const ackCode = (msaFields[1] ?? "").trim();
  const controlId = (msaFields[2] ?? "").trim() || null;
  const textMessage = (msaFields[3] ?? "").trim() || null;
  const known = ACK_CODES[ackCode];
  const ackLabel = known?.label ?? "Unknown ACK code";
  const tone: ParsedAck["tone"] = known?.tone ?? "unknown";

  const errors = lines
    .filter((l) => l.startsWith("ERR"))
    .map((errLine) => {
      const errFields = errLine.split(fieldSep);
      // HL7 v2.4+: ERR-3 is the HL7 error code (composite). Older versions used ERR-1.
      // Take whichever field is non-empty as a best-effort code; surface the first component.
      const errorCodeField = errFields[3] || errFields[1] || "";
      const errorCode = errorCodeField.split(componentSep)[0]?.trim() || null;
      const severity = (errFields[4] ?? "").trim() || null;
      const text = (errFields[5] ?? "").trim() || null;
      return { errorCode, severity, text };
    });

  return { ackCode, ackLabel, tone, controlId, textMessage, errors };
};

export const Hl7WorkbenchTab = () => {
  const [payload, setPayload] = useState("");
  const [requiredSegments, setRequiredSegments] = useState("MSH,PID");
  const [minVersion, setMinVersion] = useState("");
  const [selectedFlowId, setSelectedFlowId] = useState<string>("");

  const detectedVersion = useMemo(() => detectVersion(payload), [payload]);

  const flowsQuery = useQuery({
    queryKey: ["smartconnect", "flows"],
    queryFn: fetchFlows,
    staleTime: 30_000,
  });
  const hl7Flows = useMemo(
    () => (flowsQuery.data ?? []).filter((f) => f.dataTypes?.includes("HL7v2")),
    [flowsQuery.data],
  );

  const parseMutation = useMutation({
    mutationFn: () => workbenchParseHl7(payload),
  });
  const validateMutation = useMutation({
    mutationFn: () => {
      const reqSegs = requiredSegments
        .split(/[,\s]+/)
        .map((s) => s.trim())
        .filter(Boolean);
      return workbenchValidateHl7({
        payloadText: payload,
        requiredSegments: reqSegs.length > 0 ? reqSegs : undefined,
        minVersion: minVersion.trim() || undefined,
      });
    },
  });
  const dispatchMutation = useMutation({
    mutationFn: () => {
      if (!selectedFlowId) {
        return Promise.reject(new Error("Select a target HL7v2 channel before dispatching."));
      }
      return workbenchDispatch(selectedFlowId, payload);
    },
  });

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h3 className="text-base font-semibold text-clinic-50">HL7 Workbench</h3>
        <p className="text-sm text-slate-400">
          Paste a real HL7 v2 message from your sender to begin. The Workbench parses, runs the same
          <code className="mx-1 rounded bg-slate-800 px-1">verify-hl7</code> validation your route
          filters use, and (optionally) dispatches the payload through any started HL7v2 channel. No
          sample messages ship with the product — bring your own.
        </p>
      </header>

      <section className="space-y-2">
        <label className="block text-sm text-slate-300">1 · Paste</label>
        <textarea
          aria-label="HL7 v2 message payload"
          value={payload}
          onChange={(e) => setPayload(e.target.value)}
          spellCheck={false}
          className="block w-full min-h-[160px] resize-y rounded border border-slate-700 bg-slate-900 px-3 py-2 font-mono text-xs text-slate-100 placeholder:text-slate-600"
          placeholder="MSH|^~\&|SENDA|FACA|RECB|FACB|20260101010101||ADT^A01|MSGID|P|2.5..."
        />
        <p className="text-xs text-slate-500">
          Detected version (MSH.12):{" "}
          <span className="text-slate-300">{detectedVersion ?? "—"}</span>
        </p>
      </section>

      <section className="space-y-2">
        <div className="flex items-center justify-between">
          <label className="block text-sm text-slate-300">2 · Parse</label>
          <button
            type="button"
            disabled={!payload || parseMutation.isPending}
            onClick={() => parseMutation.mutate()}
            className="rounded bg-clinic-600 px-3 py-1 text-xs text-clinic-50 disabled:bg-slate-800 disabled:text-slate-500"
          >
            {parseMutation.isPending ? "Parsing…" : "Parse"}
          </button>
        </div>
        {parseMutation.isError && (
          <p className="text-xs text-rose-300">{humanizeError(parseMutation.error)}</p>
        )}
        {parseMutation.data && <ParseResult data={parseMutation.data} />}
      </section>

      <section className="space-y-2">
        <div className="flex items-center justify-between gap-3">
          <label className="block text-sm text-slate-300">3 · Validate</label>
          <button
            type="button"
            disabled={!payload || validateMutation.isPending}
            onClick={() => validateMutation.mutate()}
            className="rounded bg-clinic-600 px-3 py-1 text-xs text-clinic-50 disabled:bg-slate-800 disabled:text-slate-500"
          >
            {validateMutation.isPending ? "Validating…" : "Validate"}
          </button>
        </div>
        <div className="grid grid-cols-1 gap-2 md:grid-cols-2">
          <input
            type="text"
            aria-label="Required segments"
            value={requiredSegments}
            onChange={(e) => setRequiredSegments(e.target.value)}
            className="rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-100"
            placeholder="Required segments — e.g. MSH,PID,OBR,OBX"
          />
          <input
            type="text"
            aria-label="Minimum version"
            value={minVersion}
            onChange={(e) => setMinVersion(e.target.value)}
            className="rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-100"
            placeholder="Minimum version — e.g. 2.5"
          />
        </div>
        {validateMutation.isError && (
          <p className="text-xs text-rose-300">{humanizeError(validateMutation.error)}</p>
        )}
        {validateMutation.data && <ValidateResult data={validateMutation.data} />}
      </section>

      <section className="space-y-2">
        <div className="flex items-center justify-between gap-3">
          <label className="block text-sm text-slate-300">4 · Send</label>
          <button
            type="button"
            disabled={!payload || !selectedFlowId || dispatchMutation.isPending}
            onClick={() => dispatchMutation.mutate()}
            className="rounded bg-emerald-700 px-3 py-1 text-xs text-emerald-50 disabled:bg-slate-800 disabled:text-slate-500"
          >
            {dispatchMutation.isPending ? "Dispatching…" : "Send"}
          </button>
        </div>
        <select
          aria-label="Target HL7v2 channel"
          value={selectedFlowId}
          onChange={(e) => setSelectedFlowId(e.target.value)}
          className="block w-full rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-100"
        >
          <option value="">— Pick an HL7v2 channel —</option>
          {hl7Flows.map((f) => (
            <option key={f.id} value={f.id}>
              {f.name}
            </option>
          ))}
        </select>
        {dispatchMutation.isError && (
          <p className="text-xs text-rose-300">{humanizeError(dispatchMutation.error)}</p>
        )}
        {dispatchMutation.data && (
          <DispatchResult
            data={dispatchMutation.data}
            onRetry={() => dispatchMutation.mutate()}
            isRetrying={dispatchMutation.isPending}
          />
        )}
      </section>
    </div>
  );
};

const ParseResult = ({ data }: { data: WorkbenchParseResponse }) => (
  <div className="rounded border border-slate-800 bg-slate-900/60 p-3 space-y-2">
    <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-xs text-slate-300">
      <span>
        <span className="text-slate-500">Trigger: </span>
        {data.header.messageType ?? "—"}
      </span>
      <span>
        <span className="text-slate-500">Version: </span>
        {data.header.version ?? "—"}
      </span>
      <span>
        <span className="text-slate-500">Sender: </span>
        {data.header.sendingApp ?? "—"}@{data.header.sendingFacility ?? "—"}
      </span>
      <span>
        <span className="text-slate-500">Receiver: </span>
        {data.header.receivingApp ?? "—"}@{data.header.receivingFacility ?? "—"}
      </span>
      <span>
        <span className="text-slate-500">Control ID: </span>
        {data.header.controlId ?? "—"}
      </span>
      <span>
        <span className="text-slate-500">Timestamp: </span>
        {data.header.timestamp ?? "—"}
      </span>
    </div>
    <div className="text-xs text-slate-300">
      <span className="text-slate-500">Segments: </span>
      {data.segmentNames.join(", ")}
    </div>
    <details className="text-xs text-slate-400">
      <summary className="cursor-pointer text-slate-300">Structured JSON</summary>
      <pre className="mt-1 max-h-80 overflow-auto rounded bg-slate-950 p-2 font-mono text-[11px]">
        {JSON.stringify(JSON.parse(data.segmentsJson), null, 2)}
      </pre>
    </details>
  </div>
);

const ValidateResult = ({ data }: { data: WorkbenchValidateResponse }) => (
  <div
    className={
      "rounded border p-3 text-xs " +
      (data.isValid
        ? "border-emerald-700/60 bg-emerald-950/40 text-emerald-200"
        : "border-rose-700/60 bg-rose-950/40 text-rose-200")
    }
  >
    {data.isValid
      ? "✓ Valid against the configured rules."
      : `✗ ${data.reason ?? "Validation failed."}`}
    {data.header && (
      <p className="mt-1 text-slate-400">
        Trigger {data.header.trigger ?? "—"} · v{data.header.version ?? "—"} · ctrl{" "}
        {data.header.controlId ?? "—"}
      </p>
    )}
  </div>
);

const DispatchResult = ({
  data,
  onRetry,
  isRetrying,
}: {
  data: WorkbenchDispatchResponse;
  onRetry: () => void;
  isRetrying: boolean;
}) => {
  const ack = parseAck(data.responsePayload);
  return (
    <div
      className={
        "rounded border p-3 text-xs space-y-2 " +
        (data.succeeded
          ? "border-emerald-700/60 bg-emerald-950/40"
          : "border-rose-700/60 bg-rose-950/40")
      }
    >
      <div className="flex items-start justify-between gap-2">
        <p className={data.succeeded ? "text-emerald-200" : "text-rose-200"}>
          {data.succeeded
            ? `✓ Dispatched (correlation ${data.correlationId}).`
            : `✗ ${data.error ?? "Dispatch failed."}`}
        </p>
        <button
          type="button"
          onClick={onRetry}
          disabled={isRetrying}
          title="Re-send the same payload through the same channel"
          className="shrink-0 rounded border border-slate-700 px-2 py-0.5 text-[11px] text-slate-200 hover:bg-slate-800 disabled:bg-slate-900 disabled:text-slate-500"
        >
          {isRetrying ? "Retrying…" : "Retry"}
        </button>
      </div>
      {ack && <AckPanel ack={ack} />}
      {data.responsePayload && (
        <details className="text-slate-300">
          <summary className="cursor-pointer">
            {ack ? "Raw ACK payload" : "Response payload"}
          </summary>
          <pre className="mt-1 max-h-40 overflow-auto rounded bg-slate-950 p-2 font-mono text-[11px]">
            {data.responsePayload}
          </pre>
        </details>
      )}
      {data.ledgerSnapshot.length > 0 && (
        <div className="overflow-x-auto">
          <table className="min-w-full text-[11px]">
            <thead className="text-slate-400">
              <tr>
                <th className="px-2 py-1 text-left">Status</th>
                <th className="px-2 py-1 text-left">Route</th>
                <th className="px-2 py-1 text-left">Detail</th>
                <th className="px-2 py-1 text-left">At</th>
              </tr>
            </thead>
            <tbody className="text-slate-200">
              {data.ledgerSnapshot.map((row) => (
                <tr key={row.id} className="border-t border-slate-800/60">
                  <td className="px-2 py-1">{row.status}</td>
                  <td className="px-2 py-1">{row.outboundRouteOrdinal ?? "—"}</td>
                  <td className="px-2 py-1">{row.detail ?? "—"}</td>
                  <td className="px-2 py-1">{new Date(row.createdAtUtc).toLocaleTimeString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

const ackToneClass = (tone: ParsedAck["tone"]): string => {
  if (tone === "accept") return "border-emerald-700/60 bg-emerald-950/30 text-emerald-200";
  if (tone === "error") return "border-amber-700/60 bg-amber-950/30 text-amber-200";
  if (tone === "reject") return "border-rose-700/60 bg-rose-950/30 text-rose-200";
  return "border-slate-700/60 bg-slate-900/40 text-slate-200";
};

const AckPanel = ({ ack }: { ack: ParsedAck }) => {
  const toneClass = ackToneClass(ack.tone);
  return (
    <div className={`rounded border p-2 ${toneClass}`}>
      <p className="text-[11px] font-semibold">
        ACK · {ack.ackCode} — {ack.ackLabel}
      </p>
      <div className="mt-1 grid grid-cols-2 gap-x-3 gap-y-0.5 text-[11px] text-slate-300">
        {ack.controlId && (
          <span>
            <span className="text-slate-500">Control ID:</span> {ack.controlId}
          </span>
        )}
        {ack.textMessage && (
          <span className="col-span-2">
            <span className="text-slate-500">Text:</span> {ack.textMessage}
          </span>
        )}
      </div>
      {ack.errors.length > 0 && (
        <table className="mt-1 w-full text-[11px]">
          <thead className="text-slate-400">
            <tr>
              <th className="px-1 py-0.5 text-left">Error</th>
              <th className="px-1 py-0.5 text-left">Severity</th>
              <th className="px-1 py-0.5 text-left">Text</th>
            </tr>
          </thead>
          <tbody className="text-slate-200">
            {ack.errors.map((err, i) => (
              <tr key={i} className="border-t border-slate-800/60">
                <td className="px-1 py-0.5">{err.errorCode ?? "—"}</td>
                <td className="px-1 py-0.5">{err.severity ?? "—"}</td>
                <td className="px-1 py-0.5">{err.text ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};
