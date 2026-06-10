import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, VariableMapScope } from "./types";
import { deleteConfigMapEntry, fetchConfigMap, upsertConfigMapEntry } from "./configMap";

describe("configMap api", () => {
  afterEach(() => vi.restoreAllMocks());

  it("maps the server dictionary to key-sorted entries", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({
      data: { zebra: "z", alpha: "a", middle: "m" },
    } as never);

    const entries = await fetchConfigMap(VariableMapScope.Global);

    expect(entries).toEqual([
      { key: "alpha", value: "a" },
      { key: "middle", value: "m" },
      { key: "zebra", value: "z" },
    ]);
  });

  it("omits the flowId param entirely for global scope but forwards it for channel scope", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: {} } as never);

    await fetchConfigMap(VariableMapScope.Global);
    expect(get).toHaveBeenLastCalledWith(`${ADMIN_PREFIX}/config-map/${VariableMapScope.Global}`, {
      params: undefined,
    });

    await fetchConfigMap(VariableMapScope.Channel, "flow-7");
    expect(get).toHaveBeenLastCalledWith(`${ADMIN_PREFIX}/config-map/${VariableMapScope.Channel}`, {
      params: { flowId: "flow-7" },
    });
  });

  it("tolerates an empty body", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({ data: undefined } as never);
    expect(await fetchConfigMap(VariableMapScope.Global)).toEqual([]);
  });

  it("URL-encodes the entry key on upsert so slashes / spaces can't break the route", async () => {
    const put = vi.spyOn(apiClient, "put").mockResolvedValue({ data: undefined } as never);

    await upsertConfigMapEntry(VariableMapScope.Global, { key: "lab/system url", value: "v" });

    expect(put).toHaveBeenCalledWith(
      `${ADMIN_PREFIX}/config-map/${VariableMapScope.Global}/lab%2Fsystem%20url`,
      { value: "v" },
      { params: undefined },
    );
  });

  it("URL-encodes the entry key on delete and forwards the channel flowId", async () => {
    const del = vi.spyOn(apiClient, "delete").mockResolvedValue({ data: undefined } as never);

    await deleteConfigMapEntry(VariableMapScope.Channel, "a&b", "flow-1");

    expect(del).toHaveBeenCalledWith(
      `${ADMIN_PREFIX}/config-map/${VariableMapScope.Channel}/a%26b`,
      { params: { flowId: "flow-1" } },
    );
  });
});
