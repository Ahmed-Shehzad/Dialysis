import { expect, test } from "@playwright/test";
import { decodeJwt, isExpired } from "../src/lib/auth/token";

// Pure-node guard against the exact regression: Keycloak emits unpadded base64url and
// puts UTF-8 strings in claim values. The old decoder ran `atob` directly without
// padding and without decoding the binary string back to UTF-8, so JSON.parse threw on
// anything fancier than a single-byte payload, decodeJwt returned null, isExpired(null)
// returned true, and the apiClient silently treated every fresh token as expired.

// Build a JWT-like token: header.payload.signature, all base64url, payload unpadded,
// containing a multi-byte UTF-8 character so the UTF-8 decode path is exercised.
const toBase64Url = (s: string): string =>
  Buffer.from(s, "utf-8")
    .toString("base64")
    .replaceAll("+", "-")
    .replaceAll("/", "_")
    .replace(/=+$/u, "");

const buildToken = (payload: Record<string, unknown>): string => {
  const h = toBase64Url(JSON.stringify({ alg: "RS256", typ: "JWT" }));
  const p = toBase64Url(JSON.stringify(payload));
  const sig = toBase64Url("not-a-real-signature");
  return `${h}.${p}.${sig}`;
};

test.describe("decodeJwt", () => {
  const decode = (token: string) => {
    const claims = decodeJwt(token);
    if (claims === null) throw new Error("decodeJwt returned null");
    return claims;
  };

  test("decodes an unpadded base64url JWT with ASCII claims", () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    const token = buildToken({ sub: "alice", exp, roles: ["his-developer"] });
    const claims = decode(token);
    expect(claims.sub).toBe("alice");
    expect(claims.exp).toBe(exp);
    expect(claims.roles).toEqual(["his-developer"]);
  });

  test("decodes a payload containing multi-byte UTF-8 (German umlaut, emoji, Chinese)", () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    const token = buildToken({
      sub: "user-1",
      exp,
      preferred_username: "müller",
      name: "测试 用户 🩺",
      email: "Mueller@example.com",
    });
    const claims = decode(token);
    expect(claims.preferred_username).toBe("müller");
    expect(claims.name).toBe("测试 用户 🩺");
  });

  test("decodes payloads of every modulo-4 length (forces padding logic)", () => {
    for (let extraChars = 0; extraChars < 8; extraChars++) {
      const filler = "a".repeat(extraChars);
      const exp = Math.floor(Date.now() / 1000) + 3600;
      const token = buildToken({ sub: "u" + filler, exp });
      const claims = decode(token);
      expect(claims.sub, `failed at filler length ${extraChars}`).toBe("u" + filler);
    }
  });

  test("returns null on a structurally invalid token", () => {
    expect(decodeJwt("not.a.valid")).toBeNull(); // 3 segments but payload is not JSON
    expect(decodeJwt("only-one-segment")).toBeNull();
    expect(decodeJwt("")).toBeNull();
  });
});

test.describe("isExpired", () => {
  test("returns true when claims is null (defensive default)", () => {
    expect(isExpired(null)).toBe(true);
  });

  test("returns false for a token expiring in the future", () => {
    expect(isExpired({ exp: Math.floor(Date.now() / 1000) + 60 })).toBe(false);
  });

  test("returns true for a token already past exp", () => {
    expect(isExpired({ exp: Math.floor(Date.now() / 1000) - 60 })).toBe(true);
  });

  test("returns true when exp claim is missing", () => {
    expect(isExpired({ sub: "alice" })).toBe(true);
  });
});
