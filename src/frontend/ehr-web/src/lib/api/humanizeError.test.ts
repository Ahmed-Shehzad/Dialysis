import { describe, expect, it } from "vitest";
import { humanizeError } from "./humanizeError";

const GENERIC = "Something went wrong. Your last action may not have completed.";

describe("humanizeError", () => {
  it("falls back to a generic sentence for null/undefined", () => {
    expect(humanizeError(null)).toBe(GENERIC);
    expect(humanizeError(undefined)).toBe(GENERIC);
  });

  it("prefers the ProblemDetails title, joining detail when present", () => {
    expect(humanizeError({ response: { data: { title: "Invalid dose" } } })).toBe("Invalid dose");
    expect(
      humanizeError({ response: { data: { title: "Invalid dose", detail: "Max 400mg" } } }),
    ).toBe("Invalid dose — Max 400mg");
  });

  it("maps auth statuses to a permission message without leaking the code", () => {
    expect(humanizeError({ response: { status: 401 } })).toBe(
      "You don't have permission to do that.",
    );
    expect(humanizeError({ response: { status: 403 } })).toBe(
      "You don't have permission to do that.",
    );
  });

  it("maps 404 / 409 / 5xx to user-readable sentences", () => {
    expect(humanizeError({ response: { status: 404 } })).toBe(
      "We couldn't find what you were looking for.",
    );
    expect(humanizeError({ response: { status: 409 } })).toBe(
      "Someone else changed this just now — refresh and try again.",
    );
    expect(humanizeError({ response: { status: 503 } })).toBe(
      "The system is unavailable right now. Please try again in a moment.",
    );
  });

  it("detects network errors from the message", () => {
    expect(humanizeError({ message: "Network Error" })).toBe(
      "We couldn't reach the server. Check your connection.",
    );
  });

  it("never exposes a raw status code", () => {
    const result = humanizeError({ response: { status: 418 } });
    expect(result).toBe(GENERIC);
    expect(result).not.toContain("418");
  });
});
