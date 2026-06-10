import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX } from "./types";
import { workbenchDispatch, workbenchParseHl7, workbenchValidateHl7 } from "./workbench";

const HL7 = "MSH|^~\\&|SENDA|FACA|RECB|FACB|20260101010101||ADT^A01|MSGID|P|2.5";

describe("workbench api", () => {
  afterEach(() => vi.restoreAllMocks());

  it("parses by POSTing the raw payload text in the body (stateless server, no canned data)", async () => {
    const parsed = {
      header: { messageType: "ADT^A01", version: "2.5" },
      segmentsJson: "{}",
      segmentNames: ["MSH"],
    };
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: parsed } as never);

    expect(await workbenchParseHl7(HL7)).toEqual(parsed);
    expect(post).toHaveBeenCalledWith(`${ADMIN_PREFIX}/workbench/parse-hl7`, {
      payloadText: HL7,
    });
  });

  it("forwards the full validate request (required segments + min version)", async () => {
    const verdict = { isValid: false, reason: "missing OBX", header: null, segmentsJson: null };
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: verdict } as never);

    const body = { payloadText: HL7, requiredSegments: ["MSH", "PID", "OBX"], minVersion: "2.5" };
    expect(await workbenchValidateHl7(body)).toEqual(verdict);
    expect(post).toHaveBeenCalledWith(`${ADMIN_PREFIX}/workbench/validate-hl7`, body);
  });

  it("dispatches the payload through a chosen flow and returns the ledger snapshot", async () => {
    const outcome = {
      dispatchedMessageId: "m-1",
      correlationId: "c-1",
      succeeded: true,
      error: null,
      outboundRoutesAttempted: [0],
      responsePayload: "ACK",
      ledgerSnapshot: [],
    };
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: outcome } as never);

    expect(await workbenchDispatch("flow-1", HL7)).toEqual(outcome);
    expect(post).toHaveBeenCalledWith(`${ADMIN_PREFIX}/workbench/dispatch`, {
      flowId: "flow-1",
      payloadText: HL7,
    });
  });
});
