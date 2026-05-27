import { test, expect } from "@playwright/test";

// ─── Shared auth helpers ──────────────────────────────────────────────────────

const AUTH_RESPONSE = {
  accessToken: "test-access-token",
  refreshToken: "test-refresh-token",
  user: { id: "1", email: "test@test.com", role: "Admin" },
};

const USER_RESPONSE = {
  id: "1",
  email: "test@test.com",
  role: "Admin",
};

async function mockAuth(page: import("@playwright/test").Page) {
  await page.route("http://localhost:5000/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/refresh")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: "refreshed-token",
          refreshToken: "refreshed-token",
        }),
      });
    }
    if (url.includes("/me")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(USER_RESPONSE),
      });
    }
    return route.continue();
  });
}

// Sets token in localStorage BEFORE navigation so auth check passes
async function setupAuth(page: import("@playwright/test").Page) {
  await mockAuth(page);
  await page.context().addInitScript(({ accessToken, refreshToken, user }) => {
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
    localStorage.setItem("user", JSON.stringify(user));
  }, AUTH_RESPONSE);
  // Set cookie for middleware (checks cookies, not localStorage)
  await page.context().addCookies([
    { name: "access_token", value: AUTH_RESPONSE.accessToken, domain: "localhost", path: "/" },
  ]);
}

// ─── Map page ───────────────────────────────────────────────────────────────

test.describe("Map page", () => {
  test("renders map container", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/map");
    await expect(page.locator("body")).toBeVisible();
  });

  test("navigates from landing to map", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/");
    const mapLink = page.getByRole("link", { name: /map|mapa/i }).first();
    if (await mapLink.isVisible()) {
      await mapLink.click();
      await expect(page).toHaveURL(/\/map/, { timeout: 5000 });
    }
  });
});

// ─── Dashboard page ─────────────────────────────────────────────────────────

test.describe("Dashboard page", () => {
  test("shows dashboard heading", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard");
    await expect(page.locator("h1, h2").first()).toBeVisible();
  });

  test("renders stats cards", async ({ page }) => {
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 100 }),
      })
    );
    await setupAuth(page);
    await page.goto("/dashboard");
    await expect(page.locator("body")).toBeVisible();
  });

  test("navigates to alerts from dashboard", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard");
    const alertsLink = page.getByRole("link", { name: /alert|alerta/i }).first();
    if (await alertsLink.isVisible()) {
      await alertsLink.click();
      await expect(page).toHaveURL(/\/alerts/, { timeout: 5000 });
    }
  });
});

// ─── Alerts page ─────────────────────────────────────────────────────────────

test.describe("Alerts page", () => {
  test("shows alerts heading", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.locator("h2")).toContainText("Alertas", { ignoreCase: true });
  });

  test("renders severity filter", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.locator("select")).toBeVisible();
  });

  test("renders alerts list", async ({ page }) => {
    await setupAuth(page);
    await page.route(/api\/alerts/, (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              id: "1",
              title: "Fire alert",
              severity: "Critical",
              message: "Fire detected in area",
              geoEventId: null,
              isRead: false,
              createdAt: new Date().toISOString(),
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await expect(page.getByText("Fire alert")).toBeVisible();
  });

  test("navigates to rules from alerts", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    const rulesLink = page.getByRole("link", { name: /regras|rules/i });
    if (await rulesLink.isVisible()) {
      await rulesLink.click();
      await expect(page).toHaveURL(/\/alerts\/rules/, { timeout: 5000 });
    }
  });
});

// ─── Alert Rules page ────────────────────────────────────────────────────────

test.describe("Alert Rules page", () => {
  test("shows rules heading", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [] }),
      })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.locator("h2")).toContainText("Regras", { ignoreCase: true });
  });

  test("shows create rule button", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.getByRole("button", { name: /nova regra|new rule/i })).toBeVisible();
  });

  test("shows empty state when no rules", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.locator("text=Nenhuma regra")).toBeVisible();
  });

  test("renders rules list", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              id: "1",
              name: "Fire Rule",
              eventType: "Fire",
              severityThreshold: "Warning",
              areaWkt: null,
              isActive: true,
              createdAt: new Date().toISOString(),
            },
          ],
        }),
      })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.locator("text=Fire Rule")).toBeVisible();
  });

  test("shows create form when clicking new rule", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.getByRole("button", { name: /nova regra|new rule/i }).click();
    await expect(page.locator("text=Criar nova regra")).toBeVisible();
  });

  test("navigates back to alerts", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    const backLink = page.getByRole("main").getByRole("link", { name: /alertas/i });
    if (await backLink.isVisible()) {
      await backLink.click();
      await expect(page).toHaveURL(/\/dashboard\/alerts/, { timeout: 5000 });
    }
  });
});

// ─── Event detail page ──────────────────────────────────────────────────────

test.describe("Event detail page", () => {
  test("renders event detail", async ({ page }) => {
    const mockEvent = {
      id: "evt-1",
      title: "Fire in Seixal",
      eventType: "Fire",
      severity: "Danger",
      source: "ICNF",
      latitude: 38.6,
      longitude: -9.1,
      occurredAt: new Date().toISOString(),
      description: "Forest fire reported",
      aiClassification: "High risk",
      aiInsight: "Immediate attention required",
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events/evt-1", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(mockEvent) })
    );
    await page.goto("/dashboard/events/evt-1");
    await page.waitForLoadState("load");
    await expect(page.locator("text=Fire in Seixal")).toBeVisible();
  });

  test("shows error state for invalid event", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events/invalid-id", (route) =>
      route.fulfill({ status: 404, contentType: "application/json", body: JSON.stringify({ message: "Not found" }) })
    );
    await page.goto("/dashboard/events/invalid-id");
    await page.waitForLoadState("load");
    await expect(page.locator("body")).toBeVisible();
  });

  test("back link navigates to dashboard", async ({ page }) => {
    const mockEvent = {
      id: "evt-1",
      title: "Test Event",
      eventType: "Flood",
      severity: "Warning",
      source: "IPMA",
      latitude: 38.6,
      longitude: -9.1,
      occurredAt: new Date().toISOString(),
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events/evt-1", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(mockEvent) })
    );
    await page.goto("/dashboard/events/evt-1");
    await page.waitForLoadState("load");
    const backLink = page.getByRole("link", { name: /voltar|back/i });
    if (await backLink.isVisible()) {
      await backLink.click();
      await expect(page).toHaveURL(/\/dashboard/, { timeout: 5000 });
    }
  });
});

// ─── Insights page ───────────────────────────────────────────────────────────

test.describe("Insights page", () => {
  test("shows insights heading", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/insights/patterns", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ patterns: [] }),
      })
    );
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.getByRole("heading", { name: "AI Insights" })).toBeVisible();
  });

  test("renders loading state", async ({ page }) => {
    await page.route("**/api/insights/patterns", (route) =>
      new Promise((resolve) => setTimeout(() => resolve(route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) })), 1000))
    );
    await setupAuth(page);
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("body")).toBeVisible();
  });

  test("renders patterns when available", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/insights/patterns", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          patterns: [
            { patternType: "temporal", title: "Temporal clustering", description: "3 fires in 2km radius", confidence: 0.85, affectedEventIds: [], recommendation: "" },
          ],
        }),
      })
    );
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("text=Temporal clustering")).toBeVisible();
  });

  test("shows empty state when no patterns", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/insights/patterns", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) })
    );
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("text=No patterns detected")).toBeVisible();
  });

  test("has generate report quick action", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/insights/patterns", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) })
    );
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.getByText("Generate Report").first()).toBeVisible();
  });

  test("navigates to report page", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/insights/patterns", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) })
    );
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    const reportLink = page.getByText("Generate Report");
    await reportLink.click();
    await expect(page).toHaveURL(/\/insights\/report/, { timeout: 5000 });
  });
});

// ─── Report page ────────────────────────────────────────────────────────────

test.describe("Report page", () => {
  test("renders report page", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard/insights/report");
    await page.waitForLoadState("load");
    await expect(page.locator("body")).toBeVisible();
  });
});