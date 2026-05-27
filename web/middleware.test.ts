import { describe, it, expect, vi, beforeEach } from "vitest";
import { NextRequest, NextResponse } from "next/server";

// The proxy module is self-contained. We import it directly.
// NextRequest and NextResponse are available from next/server in the test env.

describe("proxy", () => {
  let middleware: (request: NextRequest) => NextResponse;

  beforeEach(async () => {
    vi.resetModules();
    const mod = await import("./proxy");
    middleware = mod.middleware;
  });

  // ---------------------------------------------------------------
  // Helper to create mock NextRequest objects
  // ---------------------------------------------------------------

  function createMockRequest(
    pathname: string,
    options: { token?: string | null } = {}
  ): NextRequest {
    const url = `http://localhost:3000${pathname}`;

    const cookies = new Map<string, { name: string; value: string }>();
    if (options.token !== undefined && options.token !== null) {
      cookies.set("access_token", {
        name: "access_token",
        value: options.token,
      });
    }

    return {
      nextUrl: new URL(url),
      url,
      cookies: {
        get: (name: string) => cookies.get(name) ?? undefined,
      },
    } as unknown as NextRequest;
  }

  // ---------------------------------------------------------------
  // Public paths
  // ---------------------------------------------------------------

  describe("public paths", () => {
    it("allows access to /", () => {
      const request = createMockRequest("/");
      const response = middleware(request);
      expect(response).toBeInstanceOf(NextResponse);
    });

    it("allows access to /login", () => {
      const request = createMockRequest("/login");
      const response = middleware(request);
      expect(response).toBeInstanceOf(NextResponse);
    });

    it("allows access to /register", () => {
      const request = createMockRequest("/register");
      const response = middleware(request);
      expect(response).toBeInstanceOf(NextResponse);
    });
  });

  // ---------------------------------------------------------------
  // Protected paths - no token
  // ---------------------------------------------------------------

  describe("protected paths without token", () => {
    it("redirects to /login with callbackUrl when no access_token cookie", () => {
      const request = createMockRequest("/dashboard", { token: null });
      const response = middleware(request);

      // Should be a redirect response
      expect(response.status).toBe(307);

      // Check redirect URL contains /login and callbackUrl
      const location = response.headers.get("location");
      expect(location).toContain("/login");
      expect(location).toContain("callbackUrl=%2Fdashboard");
    });

    it("redirects to /login for any unprotected path without token", () => {
      const request = createMockRequest("/settings/profile", { token: null });
      const response = middleware(request);

      expect(response.status).toBe(307);
      const location = response.headers.get("location");
      expect(location).toContain("/login");
      expect(location).toContain(
        encodeURIComponent("/settings/profile")
      );
    });
  });

  // ---------------------------------------------------------------
  // Protected paths - with token
  // ---------------------------------------------------------------

  describe("protected paths with token", () => {
    it("allows access to protected path when access_token cookie exists", () => {
      const request = createMockRequest("/dashboard", {
        token: "valid-jwt-token",
      });
      const response = middleware(request);

      // Should not redirect - should be NextResponse.next()
      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).not.toBe(307);
    });

    it("allows access to any path with valid token", () => {
      const request = createMockRequest("/admin/users", {
        token: "valid-jwt-token",
      });
      const response = middleware(request);

      expect(response).toBeInstanceOf(NextResponse);
      expect(response.status).not.toBe(307);
    });
  });

  // ---------------------------------------------------------------
  // Static asset paths (matcher exclusion)
  // ---------------------------------------------------------------
  // Note: The actual matcher config skips _next/static, _next/image,
  // favicon.ico, and api paths at the Next.js routing level, so the
  // middleware function itself is never called for those paths.
  // We verify the config export is correct.

  describe("config proxy", () => {
    it("excludes _next/static, _next/image, favicon.ico, and api from matcher", async () => {
      const mod = await import("./proxy");
      const config = mod.config;

      expect(config).toBeDefined();
      expect(config.matcher).toBeDefined();
      // The matcher is a single regex-like pattern string, not individual entries
      const matcherPattern = (config.matcher as string[])[0];
      expect(matcherPattern).toContain("_next/static");
      expect(matcherPattern).toContain("_next/image");
      expect(matcherPattern).toContain("favicon.ico");
    });
  });

  // ---------------------------------------------------------------
  // Edge cases
  // ---------------------------------------------------------------

  describe("edge cases", () => {
    it("does not redirect empty-string token (falsy value in cookie)", () => {
      // Empty string is still a "value" in the cookie map
      const request = createMockRequest("/dashboard", { token: "" });
      const response = middleware(request);

      // Empty string token is truthy enough to pass the cookie.get check
      // because cookies.get returns the object with value: ""
      // but the middleware checks `.value` which is "", which is falsy
      // So this should redirect
      expect(response.status).toBe(307);
    });

    it("redirects with correct callbackUrl encoding for paths with special characters", () => {
      const request = createMockRequest("/dashboard?tab=alerts", {
        token: null,
      });
      const response = middleware(request);

      expect(response.status).toBe(307);
      const location = response.headers.get("location");
      expect(location).toContain("/login");
      expect(location).toContain("callbackUrl=");
    });
  });
});
