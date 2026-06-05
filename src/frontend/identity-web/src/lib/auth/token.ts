const STORAGE_KEY = "dialysis.token";

export const tokenStore = {
  get(): string | null {
    try {
      return globalThis.sessionStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  },
  set(token: string | null) {
    try {
      if (token) globalThis.sessionStorage.setItem(STORAGE_KEY, token);
      else globalThis.sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // sessionStorage unavailable — ignore
    }
  },
};

export type JwtClaims = {
  sub?: string;
  preferred_username?: string;
  name?: string;
  email?: string;
  roles?: string[];
  realm_access?: { roles?: string[] };
  exp?: number;
};

export const decodeJwt = (token: string): JwtClaims | null => {
  try {
    const payload = token.split(".")[1];
    if (!payload) return null;
    // Keycloak emits base64url without padding; atob requires base64 with padding to a
    // multiple of 4. Also, JWT payloads are JSON encoded as UTF-8 — `atob` returns a
    // binary string where each char is one byte, so multi-byte UTF-8 sequences (common
    // in role names, emails, etc.) become mojibake that JSON.parse misreads. Decode
    // through the byte sequence properly so non-ASCII payloads round-trip.
    const base64 = payload.replaceAll("-", "+").replaceAll("_", "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const binary = atob(padded);
    const bytes = new Uint8Array(binary.length);
    // atob produces a binary string where every char.codePointAt(i) is in 0..255, so
    // codePointAt is equivalent to charCodeAt here but satisfies the lint preference.
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.codePointAt(i) ?? 0;
    const json = new TextDecoder("utf-8").decode(bytes);
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
};

export const isExpired = (claims: JwtClaims | null): boolean => {
  if (!claims?.exp) return true;
  return claims.exp * 1000 < Date.now();
};
