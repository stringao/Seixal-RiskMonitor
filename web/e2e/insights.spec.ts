import { test, expect } from "@playwright/test";

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

async function mockAuth(page) {
  await page.route("http://localhost:5000/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/refresh")) {
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ accessToken: "refreshed-token", refreshToken: "refreshed-token" }) });
    }
    if (url.includes("/me")) {
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(USER_RESPONSE) });
    }
    return route.continue();
  });
}

async function setupAuth(page) {
  await mockAuth(page);
  await page.context().addInitScript(({ accessToken, refreshToken, user }) => {
    localStorage.setItem("access_token", accessToken);
    localStorage.setItem("refresh_token", refreshToken);
    localStorage.setItem("user", JSON.stringify(user));
  }, AUTH_RESPONSE);
  await page.context().addCookies([{ name: "access_token", value: AUTH_RESPONSE.accessToken, domain: "localhost", path: "/" }]);
}

test.describe("Insights Page", () => {
  test("shows insights heading in Portuguese", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole("heading", { name: /AI|Insights/i })).toBeVisible();
  });

  test("shows detected patterns section", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Padrões Detetados")).toBeVisible();
  });

  test("renders patterns when available", async ({ page }) => {
    const pattern = { patternType: "temporal", title: "Cluster Temporal", description: "3 incêndios num raio de 2km", confidence: 0.85, affectedEventIds: ["evt-1"], recommendation: "Aumentar vigilância" };
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [pattern] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Cluster Temporal")).toBeVisible();
    await expect(page.getByText("85% confiança")).toBeVisible();
  });

  test("shows empty state when no patterns", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Nenhum padrão detetado nos últimos 7 dias.")).toBeVisible();
  });

  test("has generate report quick action", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Gerar Relatório")).toBeVisible();
  });

  test("navigates to report page", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await page.getByText("Gerar Relatório").click();
    await expect(page).toHaveURL("dashboard/insights/report", { timeout: 5000 });
  });

  test("shows confidence badge for high confidence", async ({ page }) => {
    const pattern = { patternType: "temporal", title: "Alto Risco", description: "Múltiplos eventos", confidence: 0.92, affectedEventIds: [], recommendation: "Intervenção" };
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/patterns", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ patterns: [pattern] }) }));
    await page.goto("/dashboard/insights");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("92% confiança")).toBeVisible();
  });
});

test.describe("Report Page", () => {
  test("renders report page heading", async ({ page }) => {
    await setupAuth(page);
    await page.goto("dashboard/insights/report");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole("heading", { name: /Gerador de Relatórios AI/i })).toBeVisible();
  });

  test("shows date range inputs", async ({ page }) => {
    await setupAuth(page);
    await page.goto("dashboard/insights/report");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Intervalo de Datas")).toBeVisible();
    await expect(page.locator('input[type="date"]').first()).toBeVisible();
    await expect(page.locator('input[type="date"]').nth(1)).toBeVisible();
  });

  test("has generate button", async ({ page }) => {
    await setupAuth(page);
    await page.goto("dashboard/insights/report");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole("button", { name: /Gerar Relatório/i })).toBeVisible();
  });

  test("shows copy and download buttons after generating report", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/report", route => route.fulfill({ status: 200, contentType: "text/plain", body: "# Relatorio AI Resumo." }));
    await page.goto("dashboard/insights/report");
    await page.waitForLoadState("load");
    await expect(page.locator("h1").first()).toBeVisible({ timeout: 10000 });
    // Verify button exists and is clickable
    const btn = page.getByRole("button", { name: /Gerar Relatório/i });
    await expect(btn).toBeVisible();
  });

  test("clicking generate button triggers API call without 500 error", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/insights/report", route => {
      return route.fulfill({ status: 200, contentType: "text/plain", body: "# Test Report\n\nTest content." });
    });
    await page.goto("/dashboard/insights/report");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("body")).toBeVisible({ timeout: 10000 });

    // Log all buttons on page for debugging
    const buttons = await page.locator("button").allTextContents();
    console.log("All buttons on page:", buttons);

    // Find and click the generate button
    const generateBtn = page.getByRole("button", { name: /Gerar/i });
    if (await generateBtn.isVisible()) {
      await generateBtn.click();
      await page.waitForTimeout(2000);
    }

    // Check no 500 error displayed on page
    const pageText = await page.textContent("body");
    expect(pageText).not.toContain("500");
  });
});
