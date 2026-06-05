import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import {
  bindDevice,
  changeDeviceStatus,
  fetchDevices,
  registerDevice,
} from "./devicesApi";

describe("devicesApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists devices under the HIS integration path and unwraps the envelope", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({
      data: { data: [{ id: "d1", deviceId: "OX-1", status: "Active" }], links: [] },
    } as never);

    const rows = await fetchDevices();

    expect(get).toHaveBeenCalledWith(
      "/his/api/v1.0/integration/devices",
      expect.objectContaining({ params: { take: 100 } }),
    );
    expect(rows).toEqual([{ id: "d1", deviceId: "OX-1", status: "Active" }]);
  });

  it("registers a device via POST", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: { id: "new-1" } } as never);

    const result = await registerDevice({ deviceId: "OX-9", deviceTypeCode: "pulse-oximeter" });

    expect(post).toHaveBeenCalledWith("/his/api/v1.0/integration/devices", {
      deviceId: "OX-9",
      deviceTypeCode: "pulse-oximeter",
    });
    expect(result).toEqual({ id: "new-1" });
  });

  it("posts a status transition to the status endpoint", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: null } as never);

    await changeDeviceStatus("d1", "Retire");

    expect(post).toHaveBeenCalledWith("/his/api/v1.0/integration/devices/d1/status", {
      action: "Retire",
    });
  });

  it("posts a patient binding to the bind endpoint", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: null } as never);

    await bindDevice("d1", "patient-7");

    expect(post).toHaveBeenCalledWith("/his/api/v1.0/integration/devices/d1/bind", {
      patientId: "patient-7",
      sessionId: null,
    });
  });
});
