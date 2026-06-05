import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PermissionGate } from "./PermissionGate";
import { useAuth } from "@/features/auth/components/AuthProvider";
import type { AuthenticatedUser } from "@/features/auth/api/authApi";

vi.mock("@/features/auth/components/AuthProvider", () => ({
  useAuth: vi.fn(),
}));

const mockedUseAuth = vi.mocked(useAuth);

const authState = (
  status: "idle" | "loading" | "authenticated" | "anonymous",
  user: AuthenticatedUser | null,
) =>
  ({
    status,
    user,
    signIn: vi.fn(),
    signOut: vi.fn(),
    getAccessToken: () => null,
  }) as ReturnType<typeof useAuth>;

const userWith = (permissions: string[]): AuthenticatedUser => ({
  username: "nurse@example.com",
  roles: [],
  permissions,
  claims: {},
});

describe("PermissionGate", () => {
  beforeEach(() => {
    mockedUseAuth.mockReset();
  });

  it("renders the fallback when the user is not authenticated", () => {
    mockedUseAuth.mockReturnValue(authState("anonymous", null));
    render(
      <PermissionGate fallback={<span>locked</span>}>
        <span>secret</span>
      </PermissionGate>,
    );
    expect(screen.getByText("locked")).toBeInTheDocument();
    expect(screen.queryByText("secret")).not.toBeInTheDocument();
  });

  it("renders children for an authenticated user when no permission is required", () => {
    mockedUseAuth.mockReturnValue(authState("authenticated", userWith([])));
    render(
      <PermissionGate>
        <span>secret</span>
      </PermissionGate>,
    );
    expect(screen.getByText("secret")).toBeInTheDocument();
  });

  it("hides children when the required permission is absent", () => {
    mockedUseAuth.mockReturnValue(authState("authenticated", userWith(["his.queue.read"])));
    render(
      <PermissionGate permission="his.queue.manage" fallback={<span>locked</span>}>
        <span>secret</span>
      </PermissionGate>,
    );
    expect(screen.getByText("locked")).toBeInTheDocument();
    expect(screen.queryByText("secret")).not.toBeInTheDocument();
  });

  it("shows children when the required permission is present", () => {
    mockedUseAuth.mockReturnValue(authState("authenticated", userWith(["his.queue.manage"])));
    render(
      <PermissionGate permission="his.queue.manage">
        <span>secret</span>
      </PermissionGate>,
    );
    expect(screen.getByText("secret")).toBeInTheDocument();
  });
});
