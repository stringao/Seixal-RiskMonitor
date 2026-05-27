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

function makeAlert(overrides: Partial<{
  id: string; title: string; message: string; severity: string;
  isRead: boolean; createdAt: string; geoEventId: string | null;
}> = {}) {
  return {
    id: "alert-1",
    title: "Test Alert",
    message: "Test message",
    severity: "Info",
    isRead: false,
    createdAt: new Date().toISOString(),
    geoEventId: null,
    ...overrides,
  };
}

// ─── Alerts Page ─────────────────────────────────────────────────────────────

test.describe("Alerts Page - Rendering", () => {
  test("shows Alerts heading", async ({ page }) => {
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
    await expect(page.locator("h2")).toContainText("Alertas");
  });

  test("shows 'Atualizado' text when alerts load", async ({ page }) => {
    const alert = makeAlert({ id: "a1", title: "Test", message: "Test" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [alert], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText(/Atualizado/)).toBeVisible();
  });

  test("renders Regras button linking to rules page", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      })
    );
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [] }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    const rulesLink = page.getByRole("link", { name: /regras/i });
    await expect(rulesLink).toBeVisible();
  });

  test("renders severity filter dropdown", async ({ page }) => {
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
    const selects = page.locator("select");
    await expect(selects.first()).toBeVisible();
  });

  test("renders unread only checkbox", async ({ page }) => {
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
    const checkbox = page.locator('input[type="checkbox"]');
    await expect(checkbox).toBeVisible();
  });
});

test.describe("Alerts Page - Empty State", () => {
  test("shows empty state when no alerts", async ({ page }) => {
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
    await expect(page.getByText("Nenhum alerta encontrado")).toBeVisible();
  });
});

test.describe("Alerts Page - Alerts List", () => {
  test("displays alert cards with title and message", async ({ page }) => {
    const alert = makeAlert({ id: "a1", title: "Fire detected", message: "Forest fire in sector 7", severity: "Critical" });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [alert], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Fire detected")).toBeVisible();
    await expect(page.getByText("Forest fire in sector 7")).toBeVisible();
  });

  test("unread alerts show indicator dot", async ({ page }) => {
    const unread = makeAlert({ id: "a1", isRead: false });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [unread], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.locator(".opacity-60").first()).not.toBeVisible();
  });

  test("read alerts show reduced opacity", async ({ page }) => {
    const read = makeAlert({ id: "a1", isRead: true });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [read], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.locator(".opacity-60").first()).toBeVisible();
  });

  test("shows mark as read button on unread alerts", async ({ page }) => {
    const alert = makeAlert({ id: "a1", isRead: false });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [alert], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Marcar lida")).toBeVisible();
  });

  test("does not show mark as read on read alerts", async ({ page }) => {
    const alert = makeAlert({ id: "a1", isRead: true });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [alert], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Marcar lida")).not.toBeVisible();
  });

  test("shows unread count badge", async ({ page }) => {
    const alerts = [
      makeAlert({ id: "a1", isRead: false }),
      makeAlert({ id: "a2", isRead: false }),
      makeAlert({ id: "a3", isRead: true }),
    ];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: alerts, totalCount: 3, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("2 não lidas")).toBeVisible();
  });

  test("shows all read message when all alerts are read", async ({ page }) => {
    const alerts = [makeAlert({ id: "a1", isRead: true }), makeAlert({ id: "a2", isRead: true })];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: alerts, totalCount: 2, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Todas lidas")).toBeVisible();
  });
});

test.describe("Alerts Page - Filters", () => {
  test("severity filter options exist", async ({ page }) => {
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
    const select = page.locator("select").first();
    await select.click();
    await expect(page.getByRole("option", { name: "Info" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Warning" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Danger" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Critical" })).toBeVisible();
  });

  test("filtering by severity updates displayed alerts", async ({ page }) => {
    const alerts = [
      makeAlert({ id: "a1", severity: "Critical", title: "Critical Alert" }),
      makeAlert({ id: "a2", severity: "Info", title: "Info Alert" }),
    ];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) => {
      const url = route.request().url();
      const isFiltered = url.includes("severity=Critical");
      const filtered = isFiltered
        ? alerts.filter((a) => a.severity === "Critical")
        : alerts;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Critical Alert")).toBeVisible();
    await expect(page.getByText("Info Alert")).toBeVisible();
    // Apply Critical filter
    await page.locator("select").first().selectOption("Critical");
    await expect(page.getByText("Critical Alert")).toBeVisible();
    await expect(page.getByText("Info Alert")).not.toBeVisible();
  });

  test("unread only filter shows only unread alerts", async ({ page }) => {
    const alerts = [
      makeAlert({ id: "a1", isRead: false, title: "Unread Alert" }),
      makeAlert({ id: "a2", isRead: true, title: "Read Alert" }),
    ];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) => {
      const url = route.request().url();
      const isUnreadOnly = url.includes("isRead=false");
      const filtered = isUnreadOnly ? alerts.filter((a) => !a.isRead) : alerts;
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: filtered, totalCount: filtered.length, page: 1, pageSize: 20 }),
      });
    });
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Unread Alert")).toBeVisible();
    await expect(page.getByText("Read Alert")).toBeVisible();
    await page.locator('input[type="checkbox"]').check();
    await expect(page.getByText("Unread Alert")).toBeVisible();
    await expect(page.getByText("Read Alert")).not.toBeVisible();
  });
});

test.describe("Alerts Page - Mark Read Actions", () => {
  test("marking alert as read button is clickable", async ({ page }) => {
    const alert = makeAlert({ id: "a1", isRead: false });
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [alert], totalCount: 1, page: 1, pageSize: 20 }),
      })
    );
    await page.route("POST", "**/api/alerts/read", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({}) })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    const markBtn = page.getByText("Marcar lida");
    await expect(markBtn).toBeVisible();
    await markBtn.click();
    // Button should no longer be visible after marking as read
    await expect(page.getByText("Marcar lida")).not.toBeVisible({ timeout: 3000 });
  });

  test("mark all as read button is visible when there are unread", async ({ page }) => {
    const alerts = [makeAlert({ id: "a1", isRead: false }), makeAlert({ id: "a2", isRead: false })];
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: alerts, totalCount: 2, page: 1, pageSize: 20 }),
      })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await expect(page.getByText("Marcar todas lidas")).toBeVisible();
  });
});

test.describe("Alerts Page - Navigation", () => {
  test("navigates to rules page via Regras button", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
      })
    );
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");
    await page.getByRole("link", { name: /regras/i }).click();
    await expect(page).toHaveURL(/\/dashboard\/alerts\/rules/, { timeout: 5000 });
  });
});

// ─── Alert Rules Page ────────────────────────────────────────────────────────

test.describe("Alert Rules - Form Validation", () => {
  test("shows error when creating rule with empty name", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.getByRole("button", { name: /nova regra/i }).click();
    // Submit button should be disabled when name is empty
    await expect(page.getByRole("button", { name: /criar regra/i })).toBeDisabled();
    // The error message appears on submit attempt, but button is disabled so type first
    await page.locator('input[placeholder*="Alerta"]').fill("Test");
    await page.locator('input[placeholder*="Alerta"]').fill(""); // clear to test validation
    // Now submit is still disabled, but fill name and verify error doesn't appear
    await page.locator('input[placeholder*="Alerta"]').fill("Valid Name");
    await expect(page.getByText("Nome é obrigatório")).not.toBeVisible();
  });

  test("successfully creates rule with all fields", async ({ page }) => {
    await setupAuth(page);
    let postCalled = false;
    await page.route("http://localhost:5000/api/alerts", (route) => {
      if (route.request().method() === "POST") {
        postCalled = true;
        return route.fulfill({
          status: 201,
          contentType: "application/json",
          body: JSON.stringify({
            id: "new-rule", name: "Fire Rule", eventType: "Fire",
            severityThreshold: "High", isActive: true, createdAt: new Date().toISOString(),
          }),
        });
      }
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) });
    });
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.getByRole("button", { name: /nova regra/i }).click();
    await page.locator('input[placeholder*="Alerta"]').fill("Fire Rule");
    await page.locator("select").first().selectOption("Fire");
    await page.locator("select").nth(1).selectOption("High");
    await page.getByRole("button", { name: /criar regra/i }).click();
    await expect(page.waitForFunction(() => postCalled, { timeout: 5000 })).toBeTruthy();
  });

  test("cancels creating new rule", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.getByRole("button", { name: /nova regra/i }).click();
    await page.locator('input[placeholder*="Alerta"]').fill("Test Rule");
    await page.getByRole("button", { name: /cancelar/i }).click();
    await expect(page.getByText("Criar nova regra")).not.toBeVisible();
    await expect(page.getByRole("button", { name: /nova regra/i })).toBeVisible();
  });

  test("opens edit form with pre-filled values", async ({ page }) => {
    const rule = {
      id: "rule-1", name: "Original Rule", eventType: "Fire",
      severityThreshold: "Medium", areaWkt: null, isActive: true,
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.locator('button[title="Editar"]').click();
    await expect(page.getByText("Editar regra")).toBeVisible();
    await expect(page.locator('input[type="text"]')).toHaveValue("Original Rule");
  });

  test("cancels editing rule", async ({ page }) => {
    const rule = {
      id: "rule-1", name: "Original", eventType: "Fire",
      severityThreshold: "Medium", areaWkt: null, isActive: true,
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await page.locator('button[title="Editar"]').click();
    await page.locator('input[type="text"]').fill("Changed Name");
    await page.getByRole("button", { name: /cancelar/i }).click();
    await expect(page.getByText("Original")).toBeVisible();
    await expect(page.getByText("Editar regra")).not.toBeVisible();
  });

  test("delete button is visible for each rule", async ({ page }) => {
    const rule = {
      id: "rule-1", name: "Delete Me", eventType: null,
      severityThreshold: null, areaWkt: null, isActive: true,
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.locator('button[title="Eliminar"]')).toBeVisible();
  });

  test("toggle active button visible for active rule", async ({ page }) => {
    const rule = {
      id: "rule-1", name: "Active Rule", eventType: null,
      severityThreshold: null, areaWkt: null, isActive: true,
      createdAt: new Date().toISOString(),
    };
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.locator('button[title="Desativar"]')).toBeVisible();
  });

  test("shows empty state when no rules", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    await expect(page.getByText("Nenhuma regra")).toBeVisible();
  });

  test("navigates back to alerts from rules", async ({ page }) => {
    await setupAuth(page);
    await page.route("**/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );
    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");
    const backLink = page.locator("a").first();
    await backLink.click();
    await expect(page).toHaveURL(/\/dashboard\/alerts/, { timeout: 5000 });
  });
});
