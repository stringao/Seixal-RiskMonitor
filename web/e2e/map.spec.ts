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

async function setupAuth(page: import("@playwright/test").Page) {
  await mockAuth(page);
  await page.context().addInitScript(
    ({ accessToken, refreshToken, user }) => {
      localStorage.setItem("access_token", accessToken);
      localStorage.setItem("refresh_token", refreshToken);
      localStorage.setItem("user", JSON.stringify(user));
    },
    AUTH_RESPONSE
  );
  await page.context().addCookies([
    { name: "access_token", value: AUTH_RESPONSE.accessToken, domain: "localhost", path: "/" },
  ]);
}

function makeEvent(overrides: Partial<{
  id: string; eventType: string; title: string; description: string;
  severity: string; latitude: number; longitude: number;
  source: string; occurredAt: string; aiClassification: string | null;
  createdAt: string;
}> = {}) {
  return {
    id: "evt-1",
    eventType: "Fire",
    title: "Fire in Seixal",
    description: "Forest fire reported",
    severity: "Critical",
    latitude: 38.6267,
    longitude: -9.1048,
    source: "ICNF",
    occurredAt: "2026-05-26T10:30:00Z",
    aiClassification: "High risk",
    createdAt: "2026-05-26T10:30:00Z",
    ...overrides,
  };
}

// ─── Map Page ─────────────────────────────────────────────────────────────────

test.describe("Map Page - Rendering", () => {
  test("renders map container", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await expect(page.locator(".leaflet-container")).toBeVisible({ timeout: 10000 });
  });

  test("renders filters sidebar with Filtros heading", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await expect(page.getByText("Filtros")).toBeVisible();
  });

  test("renders legend", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await expect(page.getByText("Legenda")).toBeVisible();
  });
});

test.describe("Map Page - Legend", () => {
  test("legend shows severity section with all 4 levels", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    // Legend section "Severidade" (first one — second is in filters sidebar)
    await expect(page.getByText("Severidade").first()).toBeVisible();
    await expect(page.getByText("Baixo")).toBeVisible();
    await expect(page.getByText("Médio")).toBeVisible();
    await expect(page.getByText("Alto")).toBeVisible();
    await expect(page.getByText("Crítico")).toBeVisible();
  });

  test("legend shows event type section with all types in Portuguese", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    // Legend type section (not the filter dropdown options)
    await expect(page.getByText("Tipo de Evento")).toBeVisible();
    // Legend items are in SVG + text format inside the legend panel
    // Verify the text content within the legend container
    const legend = page.locator(".bg-slate-800\\/95").last();
    await expect(legend.getByText("Incêndio")).toBeVisible();
    await expect(legend.getByText("Inundação")).toBeVisible();
    await expect(legend.getByText("Tempestade")).toBeVisible();
    await expect(legend.getByText("Deslizamento")).toBeVisible();
    await expect(legend.getByText("Industrial")).toBeVisible();
    await expect(legend.getByText("Onda de Calor")).toBeVisible();
    await expect(legend.getByText("Outro")).toBeVisible();
  });
});

test.describe("Map Page - Filters", () => {
  test("renders event type filter dropdown", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    const selects = page.locator("select");
    await expect(selects.first()).toBeVisible();
  });

  test("renders severity filter dropdown", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("networkidle");
    const selects = page.locator("select");
    await expect(selects.nth(1)).toBeVisible();
  });

  test("renders two date range inputs (from and to)", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    const dateInputs = page.locator('input[type="date"]');
    await expect(dateInputs).toHaveCount(2);
    await expect(dateInputs.first()).toBeVisible();
    await expect(dateInputs.nth(1)).toBeVisible();
  });

  test("renders all five quick date range buttons", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await expect(page.getByRole("button", { name: "Hoje" })).toBeVisible();
    await expect(page.getByRole("button", { name: "7 dias" })).toBeVisible();
    await expect(page.getByRole("button", { name: "30 dias" })).toBeVisible();
    await expect(page.getByRole("button", { name: "90 dias" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Tudo" })).toBeVisible();
  });

  test("clicking Hoje sets both date inputs", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.getByRole("button", { name: "Hoje" }).click();
    const dateInputs = page.locator('input[type="date"]');
    const fromValue = await dateInputs.first().inputValue();
    const toValue = await dateInputs.nth(1).inputValue();
    expect(fromValue).not.toBe("");
    expect(toValue).not.toBe("");
  });

  test("clicking Tudo clears both date inputs", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    // Set a range first
    await page.getByRole("button", { name: "7 dias" }).click();
    // Then clear it
    await page.getByRole("button", { name: "Tudo" }).click();
    const dateInputs = page.locator('input[type="date"]');
    await expect(dateInputs.first()).toHaveValue("");
    await expect(dateInputs.nth(1)).toHaveValue("");
  });

  test("event type filter has all event type options in Portuguese", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    const typeSelect = page.locator("select").first();
    await typeSelect.click();
    await expect(page.getByRole("option", { name: "Incêndio" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Inundação" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Tempestade" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Deslizamento" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Industrial" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Onda de Calor" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Outro" })).toBeVisible();
  });

  test("severity filter has all severity options in Portuguese", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    const sevSelect = page.locator("select").nth(1);
    await sevSelect.click();
    await expect(page.getByRole("option", { name: "Baixo" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Médio" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Alto" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Crítico" })).toBeVisible();
  });

  test("Limpar Filtros resets type and severity dropdowns", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.locator("select").first().selectOption("Fire");
    await page.locator("select").nth(1).selectOption("Critical");
    await page.getByText("Limpar Filtros").click();
    await expect(page.locator("select").first()).toHaveValue("");
    await expect(page.locator("select").nth(1)).toHaveValue("");
  });
});

test.describe("Map Page - Event Markers", () => {
  test("displays markers on map when events exist", async ({ page }) => {
    const events = [
      makeEvent({ id: "e1", title: "Fire Event" }),
      makeEvent({ id: "e2", title: "Flood Event", latitude: 38.63, longitude: -9.1 }),
    ];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: events, totalCount: 2, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    const markers = page.locator(".leaflet-marker-icon");
    await expect(markers.first()).toBeVisible();
  });

  test("clicking marker opens popup with event title", async ({ page }) => {
    const event = makeEvent({ id: "e1", title: "Fire in Seixal" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    await expect(page.getByText("Fire in Seixal").first()).toBeVisible();
  });

  test("popup shows event type in Portuguese", async ({ page }) => {
    const event = makeEvent({ id: "e1", eventType: "Fire" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    await expect(page.getByText("Incêndio")).toBeVisible();
  });

  test("popup shows severity in Portuguese", async ({ page }) => {
    const event = makeEvent({ id: "e1", severity: "Critical" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    await expect(page.getByText("Crítico")).toBeVisible();
  });

  test("popup shows date in dd/MM/yyyy HH:mm format", async ({ page }) => {
    const event = makeEvent({ id: "e1", occurredAt: "2026-05-26T14:30:00Z" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    // Should show 26/05/2026 14:30
    await expect(page.getByText(/26\/05\/2026/)).toBeVisible();
    await expect(page.getByText(/14:30/)).toBeVisible();
  });

  test("popup shows AI classification when present", async ({ page }) => {
    const event = makeEvent({ id: "e1", aiClassification: "High risk fire" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    await expect(page.getByText(/High risk fire/i)).toBeVisible();
  });

  test("popup shows description when present", async ({ page }) => {
    const event = makeEvent({ id: "e1", description: "Forest fire in sector 5" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [event], totalCount: 1, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/map");
    await page.waitForLoadState("load");
    await page.waitForTimeout(2000);
    await page.locator(".leaflet-marker-icon").first().click();
    await expect(page.getByText("Forest fire in sector 5")).toBeVisible();
  });
});

test.describe("Map Page - Navigation", () => {
  test("navigates to map from dashboard sidebar", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/events", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 200 }),
      })
    );
    await page.goto("/dashboard");
    await page.waitForLoadState("load");
    const mapLink = page.getByRole("link", { name: /mapa|map/i }).first();
    await mapLink.click();
    await expect(page).toHaveURL(/\/map/, { timeout: 5000 });
  });
});
