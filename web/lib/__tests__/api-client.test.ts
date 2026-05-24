import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import axios from "axios";
import MockAdapter from "axios-mock-adapter";

// We must mock localStorage before importing api-client,
// because the module attaches interceptors at import time.
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] ?? null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
    get length() {
      return Object.keys(store).length;
    },
    key: vi.fn((_index: number) => null),
    _store: () => store,
    _setStore: (s: Record<string, string>) => {
      store = s;
    },
  };
})();

Object.defineProperty(globalThis, "localStorage", { value: localStorageMock });

// Mock window.location.href setter so redirects do not actually navigate
const mockLocation = {
  href: "",
  assign: vi.fn(),
  reload: vi.fn(),
  replace: vi.fn(),
  toString: () => "",
  origin: "http://localhost:3000",
  protocol: "http:",
  host: "localhost:3000",
  hostname: "localhost",
  port: "3000",
  pathname: "/",
  search: "",
  hash: "",
};
Object.defineProperty(globalThis, "window", {
  value: { location: mockLocation },
  writable: true,
});

// Dynamic import so localStorage mock is in place first
// We re-import for each test to reset module-level state (isRefreshing, pendingRequests)
const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

describe("api-client", () => {
  let mockAxios: MockAdapter;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let apiClient: any;

  beforeEach(async () => {
    // Reset localStorage state
    localStorageMock._setStore({});
    mockLocation.href = "";

    // Reset module registry so module-level variables are re-initialized
    vi.resetModules();

    // Import fresh module
    const mod = await import("../../lib/api-client");
    apiClient = mod.default;

    // Attach mock adapter to the apiClient instance
    mockAxios = new MockAdapter(apiClient, { onNoMatch: "throwException" });
  });

  afterEach(() => {
    mockAxios.restore();
  });

  // ---------------------------------------------------------------
  // Client configuration
  // ---------------------------------------------------------------

  describe("client configuration", () => {
    it("is created with correct base URL defaulting to localhost:5000/api", () => {
      expect(apiClient.defaults.baseURL).toBe(`${API_BASE_URL}/api`);
    });

    it("sets Content-Type header to application/json", () => {
      // Axios stores per-method default headers; check that a GET request sends it
      const headerValue =
        apiClient.defaults.headers.common["Content-Type"] ??
        apiClient.defaults.headers["Content-Type"];
      expect(headerValue).toBe("application/json");
    });
  });

  // ---------------------------------------------------------------
  // Request interceptor
  // ---------------------------------------------------------------

  describe("request interceptor", () => {
    it("adds Authorization header when access_token exists in localStorage", async () => {
      localStorageMock._setStore({ access_token: "test-token-123" });
      mockAxios.onGet("/test").reply((config) => {
        return [200, { auth: config.headers?.Authorization }];
      });

      const response = await apiClient.get("/test");
      expect(response.data.auth).toBe("Bearer test-token-123");
    });

    it("does not add Authorization header when no token in localStorage", async () => {
      mockAxios.onGet("/test").reply((config) => {
        return [200, { auth: config.headers?.Authorization ?? null }];
      });

      const response = await apiClient.get("/test");
      expect(response.data.auth).toBeNull();
    });
  });

  // ---------------------------------------------------------------
  // Response interceptor - success path
  // ---------------------------------------------------------------

  describe("response interceptor - success", () => {
    it("returns successful responses unchanged", async () => {
      mockAxios.onGet("/data").reply(200, { message: "hello" });

      const response = await apiClient.get("/data");
      expect(response.status).toBe(200);
      expect(response.data).toEqual({ message: "hello" });
    });
  });

  // ---------------------------------------------------------------
  // Response interceptor - 401 handling
  // ---------------------------------------------------------------

  describe("response interceptor - 401 handling", () => {
    let plainAxiosMock: MockAdapter;

    beforeEach(() => {
      // Mock the plain axios instance used for refresh calls.
      // The refresh call in api-client.ts uses `axios.post(...)` not `apiClient.post(...)`.
      plainAxiosMock = new MockAdapter(axios);
    });

    afterEach(() => {
      plainAxiosMock.restore();
    });

    it("attempts to refresh tokens via POST /auth/refresh on 401", async () => {
      localStorageMock._setStore({
        access_token: "old-access",
        refresh_token: "old-refresh",
      });

      // First call to /protected returns 401
      mockAxios.onGet("/protected").replyOnce(401);

      // Refresh endpoint returns new tokens
      plainAxiosMock
        .onPost(`${API_BASE_URL}/api/auth/refresh`)
        .reply(200, {
          accessToken: "new-access",
          refreshToken: "new-refresh",
        });

      // Retry of /protected succeeds
      mockAxios.onGet("/protected").reply(200, { success: true });

      const response = await apiClient.get("/protected");
      expect(response.status).toBe(200);
      expect(response.data).toEqual({ success: true });

      // Verify tokens were stored
      expect(localStorageMock._store()).toEqual({
        access_token: "new-access",
        refresh_token: "new-refresh",
      });
    });

    it("retries the original request with the new token after refresh", async () => {
      localStorageMock._setStore({
        access_token: "old-access",
        refresh_token: "old-refresh",
      });

      let retryConfig: Record<string, unknown> = {};

      // First call: 401
      mockAxios.onGet("/protected").replyOnce(401);

      // Refresh endpoint
      plainAxiosMock
        .onPost(`${API_BASE_URL}/api/auth/refresh`)
        .reply(200, {
          accessToken: "refreshed-token",
          refreshToken: "refreshed-refresh",
        });

      // Capture the retry request headers
      mockAxios.onGet("/protected").reply((config) => {
        retryConfig = config.headers as Record<string, unknown>;
        return [200, { ok: true }];
      });

      await apiClient.get("/protected");
      expect(retryConfig.Authorization).toBe("Bearer refreshed-token");
    });

    it("clears tokens and redirects to /login on failed refresh", async () => {
      localStorageMock._setStore({
        access_token: "old-access",
        refresh_token: "old-refresh",
      });

      // 401 response
      mockAxios.onGet("/protected").replyOnce(401);

      // Refresh endpoint fails
      plainAxiosMock
        .onPost(`${API_BASE_URL}/api/auth/refresh`)
        .reply(403, { error: "invalid" });

      await expect(apiClient.get("/protected")).rejects.toBeTruthy();

      // Tokens should be cleared
      expect(localStorageMock._store()).toEqual({});

      // Should redirect to /login
      expect(mockLocation.href).toBe("/login");
    });

    it("clears tokens and redirects when no tokens are available", async () => {
      // No tokens in localStorage
      mockAxios.onGet("/protected").replyOnce(401);

      await expect(apiClient.get("/protected")).rejects.toBeTruthy();

      expect(mockLocation.href).toBe("/login");
    });

    it("does not retry if request has already been retried (_retry flag)", async () => {
      localStorageMock._setStore({
        access_token: "some-token",
        refresh_token: "some-refresh",
      });

      // Return 401 for every call (simulating a refresh that also fails)
      mockAxios.onGet("/protected").reply(401);

      // Make refresh succeed so we can observe the second 401 being rejected
      plainAxiosMock
        .onPost(`${API_BASE_URL}/api/auth/refresh`)
        .reply(200, {
          accessToken: "new-token",
          refreshToken: "new-refresh",
        });

      // The first 401 triggers refresh -> retry -> second 401 -> reject (no more retries)
      await expect(apiClient.get("/protected")).rejects.toBeTruthy();
    });
  });

  // ---------------------------------------------------------------
  // Concurrent request queuing during refresh
  // ---------------------------------------------------------------

  describe("concurrent request queuing", () => {
    let plainAxiosMock: MockAdapter;

    beforeEach(() => {
      plainAxiosMock = new MockAdapter(axios);
    });

    afterEach(() => {
      plainAxiosMock.restore();
    });

    it("queues concurrent requests during refresh and replays them with new token", async () => {
      localStorageMock._setStore({
        access_token: "old-access",
        refresh_token: "old-refresh",
      });

      // Both endpoints return 401 on first call
      mockAxios.onGet("/resource-a").replyOnce(401);
      mockAxios.onGet("/resource-b").replyOnce(401);

      // Delay the refresh slightly so the second request hits the queue
      let resolveRefresh: (value: unknown) => void;
      const refreshPromise = new Promise((resolve) => {
        resolveRefresh = resolve;
      });

      plainAxiosMock
        .onPost(`${API_BASE_URL}/api/auth/refresh`)
        .reply(async () => {
          await refreshPromise;
          return [200, { accessToken: "shared-token", refreshToken: "shared-refresh" }];
        });

      // After refresh succeeds, both retries succeed
      const headersA: Record<string, unknown>[] = [];
      const headersB: Record<string, unknown>[] = [];

      mockAxios.onGet("/resource-a").reply((config) => {
        headersA.push(config.headers as Record<string, unknown>);
        return [200, { resource: "a" }];
      });

      mockAxios.onGet("/resource-b").reply((config) => {
        headersB.push(config.headers as Record<string, unknown>);
        return [200, { resource: "b" }];
      });

      // Fire both requests concurrently
      const promiseA = apiClient.get("/resource-a");
      const promiseB = apiClient.get("/resource-b");

      // Allow the refresh to complete
      resolveRefresh!(undefined);

      const [resA, resB] = await Promise.all([promiseA, promiseB]);

      expect(resA.data).toEqual({ resource: "a" });
      expect(resB.data).toEqual({ resource: "b" });

      // Both retries should use the new shared token
      expect(headersA[0]?.Authorization).toBe("Bearer shared-token");
      expect(headersB[0]?.Authorization).toBe("Bearer shared-token");
    });
  });

  // ---------------------------------------------------------------
  // Non-401 errors pass through
  // ---------------------------------------------------------------

  describe("non-401 errors", () => {
    it("rejects without attempting refresh for non-401 errors", async () => {
      mockAxios.onGet("/fail").reply(500, { error: "Internal Server Error" });

      await expect(apiClient.get("/fail")).rejects.toHaveProperty(
        "response.status",
        500
      );
    });

    it("rejects without attempting refresh for network errors", async () => {
      mockAxios.onGet("/network-fail").networkError();

      await expect(apiClient.get("/network-fail")).rejects.toBeTruthy();
    });
  });
});
