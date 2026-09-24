import { useAuth } from "@clerk/nextjs";
import { vi } from "vitest";

export type AuthState = ReturnType<typeof useAuth>;
export function makeSignedInAuth(
  overrides: Partial<Extract<AuthState, { isSignedIn: true; orgId: null }>> = {},
): AuthState {
  return {
    isLoaded: true,
    isSignedIn: true,
    userId: "user_123",
    sessionId: "sess_123",
    sessionClaims: {
      __raw: "fake",
      iss: "clerk",
      sub: "user_123",
      sid: "sess_123",
      nbf: 0,
      exp: 9999999999,
      iat: 0,
    },
    actor: null,
    orgId: null,
    orgRole: null,
    orgSlug: null,
    has: vi.fn().mockReturnValue(true),
    signOut: vi.fn(),
    getToken: vi.fn().mockResolvedValue("fake_token"),
    ...overrides,
  };
}

export function makeSignedOutAuth(
  overrides: Partial<Extract<AuthState, { isSignedIn: false }>> = {},
): AuthState {
  return {
    isLoaded: true,
    isSignedIn: false,
    userId: null,
    sessionId: null,
    sessionClaims: null,
    actor: null,
    orgId: null,
    orgRole: null,
    orgSlug: null,
    has: vi.fn().mockReturnValue(false),
    signOut: vi.fn(),
    getToken: vi.fn().mockResolvedValue(null),
    ...overrides,
  };
}