import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { fetchMyThreads, fetchThreadMessages, markMessageRead, sendMessage } from "./messagesApi";

const prefix = "/portal/api/_x/ehr/api/v1.0/portal/messages";

describe("messagesApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("fetches the patient's own threads through the portal _x/ehr aggregation path", async () => {
    const threads = [{ threadId: "t-1", subject: "Refill", messageCount: 2 }];
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: threads } as never);

    expect(await fetchMyThreads("p-1")).toEqual(threads);
    expect(get).toHaveBeenCalledWith(`${prefix}/patients/p-1/threads`);
  });

  it("degrades a missing body to an empty inbox instead of crashing the panel", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({ data: undefined } as never);
    expect(await fetchMyThreads("p-1")).toEqual([]);
  });

  it("fetches one thread's messages scoped to both patient and thread id", async () => {
    const messages = [{ id: "m-1", threadId: "t-1", body: "hello" }];
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: messages } as never);

    expect(await fetchThreadMessages("p-1", "t-1")).toEqual(messages);
    expect(get).toHaveBeenCalledWith(`${prefix}/patients/p-1/threads/t-1`);
  });

  it("sends a new message (no threadId starts a new conversation) and returns the id", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: { id: "m-9" } } as never);

    const body = { subject: "Question", body: "When is my next visit?" };
    expect(await sendMessage("p-1", body)).toEqual({ id: "m-9" });
    expect(post).toHaveBeenCalledWith(`${prefix}/patients/p-1`, body);
  });

  it("marks a care-team message read via the per-message read endpoint", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: undefined } as never);

    await markMessageRead("p-1", "m-2");
    expect(post).toHaveBeenCalledWith(`${prefix}/patients/p-1/messages/m-2/read`);
  });
});
