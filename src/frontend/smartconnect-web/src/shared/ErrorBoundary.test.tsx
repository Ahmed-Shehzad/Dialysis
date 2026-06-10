import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ReactNode } from "react";
import { ErrorBoundary } from "./ErrorBoundary";

const Bomb = (): ReactNode => {
  throw new Error("boom: secret diagnostic detail");
};

describe("ErrorBoundary", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders children when nothing throws", () => {
    render(
      <ErrorBoundary>
        <span>all good</span>
      </ErrorBoundary>,
    );
    expect(screen.getByText("all good")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows the calm fallback and logs to console.error when a child throws", () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => {});

    render(
      <ErrorBoundary>
        <Bomb />
      </ErrorBoundary>,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reload" })).toBeInTheDocument();
    // Raw error text must never reach the user — same philosophy as humanizeError.
    expect(screen.queryByText(/secret diagnostic detail/)).not.toBeInTheDocument();
    expect(consoleError).toHaveBeenCalled();
  });
});
