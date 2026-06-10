import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { fetchMyReminders } from "./remindersApi";

describe("remindersApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("fetches the signed-in patient's reminders through the portal _x/ehr aggregation path", async () => {
    const reminders = [
      { title: "Flu shot due", whatToDo: "Ask at your next session", resourceUrl: null },
    ];
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: reminders } as never);

    expect(await fetchMyReminders("p-1")).toEqual(reminders);
    expect(get).toHaveBeenCalledWith("/portal/api/_x/ehr/api/v1.0/portal/reminders/patients/p-1");
  });

  it("degrades a missing body to an empty list so the panel renders the empty state", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({ data: undefined } as never);
    expect(await fetchMyReminders("p-1")).toEqual([]);
  });
});
