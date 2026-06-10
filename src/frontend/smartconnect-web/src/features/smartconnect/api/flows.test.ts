import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type IntegrationFlow } from "./types";
import {
  deleteFlow,
  exportFlow,
  fetchFlow,
  fetchFlows,
  fetchFlowStatistics,
  importFlow,
  pauseFlow,
  startFlow,
  stopFlow,
  updateFlow,
} from "./flows";

const flow = { id: "f-1", name: "ADT inbound" } as IntegrationFlow;

describe("flows api", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists flows from the admin endpoint and tolerates an empty body", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: undefined } as never);

    expect(await fetchFlows()).toEqual([]);
    expect(get).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows`);
  });

  it("fetches a single flow by id", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: flow } as never);

    expect(await fetchFlow("f-1")).toEqual(flow);
    expect(get).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/f-1`);
  });

  it("updates a flow via PUT keyed on the flow's own id", async () => {
    const put = vi.spyOn(apiClient, "put").mockResolvedValue({ data: undefined } as never);

    await updateFlow(flow);
    expect(put).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/f-1`, flow);
  });

  it("deletes a flow by id", async () => {
    const del = vi.spyOn(apiClient, "delete").mockResolvedValue({ data: undefined } as never);

    await deleteFlow("f-1");
    expect(del).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/f-1`);
  });

  it("drives the start / stop / pause lifecycle through the per-flow action endpoints", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: undefined } as never);

    await startFlow("f-1");
    await stopFlow("f-1");
    await pauseFlow("f-1");

    expect(post).toHaveBeenNthCalledWith(1, `${ADMIN_PREFIX}/flows/f-1/start`);
    expect(post).toHaveBeenNthCalledWith(2, `${ADMIN_PREFIX}/flows/f-1/stop`);
    expect(post).toHaveBeenNthCalledWith(3, `${ADMIN_PREFIX}/flows/f-1/pause`);
  });

  it("imports via POST /flows/import and exports via GET /flows/{id}/export", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: flow } as never);
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: flow } as never);

    expect(await importFlow(flow)).toEqual(flow);
    expect(post).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/import`, flow);

    expect(await exportFlow("f-1")).toEqual(flow);
    expect(get).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/f-1/export`);
  });

  it("fetches per-flow ledger statistics and tolerates an empty body", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: null } as never);

    expect(await fetchFlowStatistics("f-1")).toEqual([]);
    expect(get).toHaveBeenCalledWith(`${ADMIN_PREFIX}/flows/f-1/statistics`);
  });
});
