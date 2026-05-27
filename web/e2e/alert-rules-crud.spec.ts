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
  await page.context().addInitScript(({ accessToken, refreshToken, user }) => {
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
    localStorage.setItem("user", JSON.stringify(user));
  }, AUTH_RESPONSE);
  await page.context().addCookies([
    { name: "access_token", value: AUTH_RESPONSE.accessToken, domain: "localhost", path: "/" },
  ]);
}

// ─── Alert Rules CRUD Tests ───────────────────────────────────────────────────

test.describe("Alert Rules - Create", () => {
  test("submit button is disabled when name is empty", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Open create form
    await page.getByRole("button", { name: /nova regra|new rule/i }).click();

    // Submit button should be disabled when name is empty
    await expect(page.getByRole("button", { name: /criar regra|create/i })).toBeDisabled();
  });

  test("cancels creating new rule", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Open create form
    await page.getByRole("button", { name: /nova regra|new rule/i }).click();

    // Fill name field
    await page.locator('input[placeholder*="Alerta"]').fill("Test Rule");

    // Cancel
    await page.getByRole("button", { name: /cancelar|cancel/i }).click();

    // Form should be closed
    await expect(page.locator("text=Criar nova regra")).not.toBeVisible();
    // New rule button should be visible again
    await expect(page.getByRole("button", { name: /nova regra|new rule/i })).toBeVisible();
  });

  test("form fields are accessible and can be filled", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Open create form
    await page.getByRole("button", { name: /nova regra|new rule/i }).click();

    // Verify form elements exist
    await expect(page.locator('input[placeholder*="Alerta"]')).toBeVisible();
    await expect(page.locator("select").first()).toBeVisible();
    await expect(page.getByRole("button", { name: /criar regra|create/i })).toBeVisible();

    // Fill the form
    await page.locator('input[placeholder*="Alerta"]').fill("Test Rule Name");
    await page.locator("select").first().selectOption("Fire");

    // Verify values were set
    await expect(page.locator('input[placeholder*="Alerta"]')).toHaveValue("Test Rule Name");
  });
});

test.describe("Alert Rules - Update", () => {
  const existingRule = {
    id: "rule-1",
    name: "Original Rule",
    eventType: "Fire",
    severityThreshold: "Medium",
    areaWkt: null,
    isActive: true,
    createdAt: new Date().toISOString(),
  };

  test("opens edit form when clicking edit button", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [existingRule] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Click edit button
    await page.locator('button[title="Editar"]').click();

    // Edit form should appear with existing value
    await expect(page.locator("text=Editar regra")).toBeVisible();
    await expect(page.locator('input[value="Original Rule"]')).toBeVisible();
  });

  test("edit form has pre-filled values from rule", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [existingRule] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Click edit button
    await page.locator('button[title="Editar"]').click();

    // Verify the form is pre-filled with existing values
    await expect(page.locator('input[type="text"]')).toHaveValue("Original Rule");
  });

  test("cancels editing rule", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [existingRule] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    await page.locator('button[title="Editar"]').click();
    await page.locator('input[type="text"]').fill("Changed Name");
    await page.getByRole("button", { name: /cancelar|cancel/i }).click();

    // Original name should still be visible
    await expect(page.locator("text=Original Rule")).toBeVisible();
    // Edit form should be closed
    await expect(page.locator("text=Editar regra")).not.toBeVisible();
  });
});

test.describe("Alert Rules - Delete", () => {
  test("shows delete confirmation button exists", async ({ page }) => {
    const rule = {
      id: "rule-1",
      name: "Rule to Delete",
      eventType: null,
      severityThreshold: null,
      areaWkt: null,
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Delete button should be visible
    await expect(page.locator('button[title="Eliminar"]')).toBeVisible();
  });
});

test.describe("Alert Rules - Toggle Active", () => {
  test("toggle button exists and is clickable", async ({ page }) => {
    const rule = {
      id: "rule-1",
      name: "Toggleable Rule",
      eventType: null,
      severityThreshold: null,
      areaWkt: null,
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [rule] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // Toggle button should be visible (title is "Desativar" when active)
    await expect(page.locator('button[title="Desativar"]')).toBeVisible();
  });
});

test.describe("Alert Rules - Navigation", () => {
  test("navigates from alerts page to rules", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 20 }) })
    );
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );

    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");

    await page.getByRole("link", { name: /regras|rules/i }).click();
    await expect(page).toHaveURL(/\/alerts\/rules/, { timeout: 5000 });
  });

  test("navigates back to alerts from rules", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/alerts/rules", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ items: [] }) })
    );

    await page.goto("/dashboard/alerts/rules");
    await page.waitForLoadState("load");

    // The back arrow should navigate to alerts
    const backLink = page.locator("a").first();
    await backLink.click();
    await expect(page).toHaveURL(/\/dashboard\/alerts/, { timeout: 5000 });
  });
});

test.describe("Alerts Page Filters", () => {
  test("filters alerts by severity", async ({ page }) => {
    const alerts = [
      { id: "1", title: "Critical Alert", severity: "Critical", message: "Test", geoEventId: null, isRead: false, createdAt: new Date().toISOString() },
      { id: "2", title: "Info Alert", severity: "Info", message: "Test", geoEventId: null, isRead: false, createdAt: new Date().toISOString() },
    ];

    await setupAuth(page);

    await page.route("**/api/alerts", async (route) => {
      const url = route.request().url();
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: alerts, totalCount: 2, page: 1, pageSize: 20 }),
      });
    });

    await page.goto("/dashboard/alerts");
    await page.waitForLoadState("load");

    // Select Critical filter
    await page.locator("select").first().selectOption("Critical");

    // Filter should be applied (check that the select shows Critical)
    await expect(page.locator("select").first()).toHaveValue("Critical");
  });

  test("shows unread only filter", async ({ page }) => {
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

    // Check the unread only checkbox
    await page.locator('input[type="checkbox"]').check();

    // Checkbox should be checked
    await expect(page.locator('input[type="checkbox"]')).toBeChecked();
  });
});
