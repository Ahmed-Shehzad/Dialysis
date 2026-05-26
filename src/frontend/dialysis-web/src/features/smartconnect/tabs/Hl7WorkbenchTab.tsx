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
            value={requiredSegments}
            onChange={(e) => setRequiredSegments(e.target.value)}
            className="rounded border border-slate-700 bg-slate-900 px-3 py-1.5 text-xs text-slate-100"
            placeholder="Required segments — e.g. MSH,PID,OBR,OBX"
          />
          <input
            type="text"
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
        {dispatchMutation.data && <DispatchResult data={dispatchMutation.data} />}
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

const DispatchResult = ({ data }: { data: WorkbenchDispatchResponse }) => (
  <div
    className={
      "rounded border p-3 text-xs space-y-2 " +
      (data.succeeded
        ? "border-emerald-700/60 bg-emerald-950/40"
        : "border-rose-700/60 bg-rose-950/40")
    }
  >
    <p className={data.succeeded ? "text-emerald-200" : "text-rose-200"}>
      {data.succeeded
        ? `✓ Dispatched (correlation ${data.correlationId}).`
        : `✗ ${data.error ?? "Dispatch failed."}`}
    </p>
    {data.responsePayload && (
      <details className="text-slate-300">
        <summary className="cursor-pointer">Response payload</summary>
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
