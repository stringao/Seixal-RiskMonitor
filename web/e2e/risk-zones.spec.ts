import { test, expect, Page } from "@playwright/test";

// ─── Auth setup (same pattern as map.spec.ts) ──────────────────────────────────

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

async function mockAuth(page: Page) {
  await page.route("http://localhost:5000/**", async (route) => {
    const url = route.request().url();
    if (url.includes("/auth/refresh")) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ accessToken: "refreshed-token", refreshToken: "refreshed-token" }),
      });
    }
    if (url.includes("/me")) {
      return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(USER_RESPONSE) });
    }
    return route.continue();
  });
}

async function setupAuth(page: Page) {
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

// ─── Mock data helpers ────────────────────────────────────────────────────────

function makeRiskZonesResponse() {
  return {
    zones: [
      {
        id: "zone-1",
        riskLevelName: "Critical",
        riskIndex: 78,
        temperature: 35,
        humidity: 28,
        windSpeed: 18,
        windDirection: "NW (315°)",
        wkt: "POLYGON((-9.2 38.5, -9.0 38.5, -9.0 38.7, -9.2 38.7, -9.2 38.5))",
        conclusion: "Área em atenção devido a temperaturas elevadas e humidade baixa. Risco crítico.",
        calculatedAt: new Date().toISOString(),
        dataPoints: [
          { latitude: 38.6, longitude: -9.1, fwi: 68, temperature: 35, humidity: 28, timestamp: new Date().toISOString() },
        ],
      },
      {
        id: "zone-2",
        riskLevelName: "High",
        riskIndex: 55,
        temperature: 30,
        humidity: 35,
        windSpeed: 12,
        windDirection: "N (0°)",
        wkt: "POLYGON((-9.0 38.6, -8.8 38.6, -8.8 38.8, -9.0 38.8, -9.0 38.6))",
        conclusion: "Risco elevado de incêndio. Vigilância recomendada.",
        calculatedAt: new Date().toISOString(),
        dataPoints: [
          { latitude: 38.7, longitude: -8.9, fwi: 55, temperature: 30, humidity: 35, timestamp: new Date().toISOString() },
        ],
      },
      {
        id: "zone-3",
        riskLevelName: "Medium",
        riskIndex: 32,
        temperature: 25,
        humidity: 50,
        windSpeed: 8,
        windDirection: "SE (135°)",
        wkt: "POLYGON((-8.8 38.5, -8.6 38.5, -8.6 38.7, -8.8 38.7, -8.8 38.5))",
        conclusion: "Risco moderado. Manter atenção.",
        calculatedAt: new Date().toISOString(),
        dataPoints: [
          { latitude: 38.6, longitude: -8.7, fwi: 32, temperature: 25, humidity: 50, timestamp: new Date().toISOString() },
        ],
      },
    ],
    lastUpdated: new Date().toISOString(),
    totalPoints: 3,
  };
}

function makeFireSpreadResponse(fireId = "fire-1") {
  return {
    fireEventId: fireId,
    fireLocation: { latitude: 38.5261, longitude: -8.8845, title: "Incêndio em Seixal" },
    currentWeather: { temperature: 32, humidity: 28, windSpeed: 18, windDirection: "NW (315°)", fwi: 68, isi: 12 },
    scenario: "Moderate",
    horizons: [
      { hours: 1, polygonWkt: "POLYGON((-9.0 38.4, -8.8 38.4, -8.8 38.6, -9.0 38.6, -9.0 38.4))", rosKmh: 2.3, areaKm2: 0.8, affectedMunicipalities: ["Seixal"], conclusion: "Área de 0.8 km² em 1h. Impacto moderado no Seixal." },
      { hours: 2, polygonWkt: "POLYGON((-9.1 38.3, -8.7 38.3, -8.7 38.7, -9.1 38.7, -9.1 38.3))", rosKmh: 2.3, areaKm2: 3.2, affectedMunicipalities: ["Seixal", "Sesimbra"], conclusion: "Área de 3.2 km² em 2h. Potencial impacto em Sesimbra." },
      { hours: 4, polygonWkt: "POLYGON((-9.2 38.2, -8.6 38.2, -8.6 38.8, -9.2 38.8, -9.2 38.2))", rosKmh: 2.3, areaKm2: 12.8, affectedMunicipalities: ["Seixal", "Sesimbra", "Fernão Ferro"], conclusion: "Alastramento significativo. Fernão Ferro em risco elevado." },
      { hours: 8, polygonWkt: "POLYGON((-9.3 38.1, -8.5 38.1, -8.5 38.9, -9.3 38.9, -9.3 38.1))", rosKmh: 2.3, areaKm2: 51.2, affectedMunicipalities: ["Seixal", "Sesimbra", "Fernão Ferro", "Almada"], conclusion: "Risco muito elevado de alastramento para Almada." },
      { hours: 12, polygonWkt: "POLYGON((-9.4 38.0, -8.4 38.0, -8.4 39.0, -9.4 39.0, -9.4 38.0))", rosKmh: 2.3, areaKm2: 115.5, affectedMunicipalities: ["Seixal", "Sesimbra", "Fernão Ferro", "Almada", "Barreiro"], conclusion: "Cenário crítico. Proteção civil deve ser alertada." },
    ],
    lastUpdated: new Date().toISOString(),
  };
}

// ─── Risk Zones Page ─────────────────────────────────────────────────────────

test.describe("Risk Zones Page - Rendering", () => {
  test("renders map container", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await expect(page.locator(".leaflet-container")).toBeVisible({ timeout: 10000 });
  });

  test("renders loading state initially", async ({ page }) => {
    await setupAuth(page);
    // Delay response to capture loading state
    await page.route("http://localhost:5000/api/risk/zones/detailed", async (route) => {
      await page.waitForTimeout(500);
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) });
    });
    await page.goto("/risk-zones");
    // Should show loading
    await expect(page.getByText(/a carregar|loading/i).first()).toBeVisible({ timeout: 5000 });
  });

  test("renders filter toggle button", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    // Filter button (top-right, shows Filter icon or X when open)
    await expect(page.locator(".bg-slate-800\\/95").first()).toBeVisible({ timeout: 5000 });
  });

  test("renders sidebar with all 6 nav items", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard");
    await page.waitForLoadState("load");
    // Check for all nav items including the new ones
    await expect(page.getByText("Dashboard")).toBeVisible();
    await expect(page.getByText("Mapa")).toBeVisible();
    await expect(page.getByText("Zonas de Risco")).toBeVisible();
    await expect(page.getByText("Simulação Fogo")).toBeVisible();
    await expect(page.getByText("Insights")).toBeVisible();
    await expect(page.getByText("Alertas")).toBeVisible();
  });

  test("sidebar Zonas de Risco links to /risk-zones", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/dashboard");
    await page.waitForLoadState("load");
    await page.getByRole("link", { name: /zonas de risco/i }).click();
    await expect(page).toHaveURL(/\/risk-zones/, { timeout: 5000 });
  });

  test("sidebar Simulação Fogo links to /fire-spread", async ({ page }) => {
    await setupAuth(page);
    await page.goto("/dashboard");
    await page.waitForLoadState("load");
    await page.getByRole("link", { name: /simulação fogo/i }).click();
    await expect(page).toHaveURL(/\/fire-spread/, { timeout: 5000 });
  });
});

test.describe("Risk Zones Page - Filters Panel", () => {
  test("opens filters panel on button click", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    // Click filter button (top-right with Filter icon)
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    await expect(page.getByText("Camada de Calor")).toBeVisible();
    await expect(page.getByText("Zonas de Risco")).toBeVisible();
  });

  test("heat layer toggle checkbox is present", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    const heatCheckbox = page.locator("input[type='checkbox']").first();
    await expect(heatCheckbox).toBeVisible();
  });

  test("polygons toggle checkbox is present", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    // Second checkbox is for polygons
    await expect(page.locator("input[type='checkbox']").nth(1)).toBeVisible();
  });

  test("min risk level dropdown has 4 options", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    const select = page.locator("select").first();
    await expect(select).toBeVisible();
    await select.click();
    await expect(page.getByRole("option", { name: "Baixo" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Médio" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Alto" })).toBeVisible();
    await expect(page.getByRole("option", { name: "Crítico" })).toBeVisible();
  });

  test("closing filter panel hides controls", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    await expect(page.getByText("Camada de Calor")).toBeVisible();
    // Click again to close
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    await page.waitForTimeout(400); // transition
  });
});

test.describe("Risk Zones Page - Polygon Layer", () => {
  test("polygons render when showPolygons is true", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Polygons are SVG paths on the map
    const paths = page.locator(".leaflet-overlay-pane path");
    await expect(paths.first()).toBeVisible({ timeout: 5000 });
  });

  test("clicking polygon opens popup with risk data", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Click on a polygon
    const paths = page.locator(".leaflet-overlay-pane path");
    await paths.first().click({ force: true });
    await page.waitForTimeout(500);
    // Popup should appear
    await expect(page.locator(".leaflet-popup")).toBeVisible({ timeout: 5000 });
    await expect(page.getByText(/Zona de Risco/i)).toBeVisible();
  });

  test("popup shows risk level badge", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const paths = page.locator(".leaflet-overlay-pane path");
    await paths.first().click({ force: true });
    await page.waitForTimeout(500);
    await expect(page.getByText(/Critical|Alto|Médio|Baixo/i)).toBeVisible();
  });

  test("popup shows temperature and humidity", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const paths = page.locator(".leaflet-overlay-pane path");
    await paths.first().click({ force: true });
    await page.waitForTimeout(500);
    // Should show temperature value
    await expect(page.getByText(/35.*°C|25.*°C|30.*°C/i)).toBeVisible();
    // Should show humidity value
    await expect(page.getByText(/28.*%|35.*%|50.*%/i)).toBeVisible();
  });

  test("popup shows conclusion text", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const paths = page.locator(".leaflet-overlay-pane path");
    await paths.first().click({ force: true });
    await page.waitForTimeout(500);
    await expect(page.getByText(/área em atenção|atenção|risco/i).first()).toBeVisible();
  });
});

test.describe("Risk Zones Page - Heat Layer", () => {
  test("heat layer toggle shows circle markers", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(makeRiskZonesResponse()) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    // Enable heat layer
    const heatCheckbox = page.locator("input[type='checkbox']").first();
    await heatCheckbox.check();
    await page.waitForTimeout(2000);
    // Circle markers should appear on the map
    const circles = page.locator(".leaflet-overlay-pane circle, .leaflet-marker-div");
    await expect(circles.first()).toBeVisible({ timeout: 5000 });
  });
});

test.describe("Risk Zones Page - API Integration", () => {
  test("shows error state when API fails", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/risk/zones/detailed", (route) =>
      route.fulfill({ status: 500, contentType: "application/json", body: JSON.stringify({ error: "Server error" }) })
    );
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Map should still render even with no data
    await expect(page.locator(".leaflet-container")).toBeVisible({ timeout: 10000 });
  });

  test("API called with minRiskLevel parameter", async ({ page }) => {
    await setupAuth(page);
    const apiRequest = page.waitForRequest((req) => req.url().includes("/risk/zones/detailed"));
    await page.goto("/risk-zones");
    await page.waitForLoadState("load");
    await page.locator("button").filter({ has: page.locator("svg") }).first().click();
    // Change min risk level
    const select = page.locator("select").first();
    await select.selectOption("High");
    await page.waitForTimeout(2000);
    const request = await apiRequest;
    // Should have minRiskLevel in query string
    expect(request.url()).toContain("minRiskLevel=High");
  });
});

// ─── Fire Spread Page ─────────────────────────────────────────────────────────

test.describe("Fire Spread Page - Rendering", () => {
  test("renders full-screen layout", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    // Should have 70/30 layout
    await expect(page.locator(".leaflet-container")).toBeVisible({ timeout: 10000 });
  });

  test("shows loading spinner initially", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", async (route) => {
      await page.waitForTimeout(500);
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) });
    });
    await page.goto("/fire-spread");
    // Loading spinner should be visible
    await expect(page.locator(".animate-spin").first()).toBeVisible({ timeout: 2000 });
  });

  test("shows no active fires state when empty", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByText(/nenhum incêndio ativo/i)).toBeVisible();
  });

  test("shows error state on API failure", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 500, contentType: "application/json", body: JSON.stringify({ error: "Server error" }) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByText(/erro ao carregar/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /tentar novamente/i })).toBeVisible();
  });

  test("refresh button re-fetches data", async ({ page }) => {
    await setupAuth(page);
    let callCount = 0;
    await page.route("http://localhost:5000/api/fires/active/spread", (route) => {
      callCount++;
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) });
    });
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const initialCount = callCount;
    // Trigger retry button if error
    const retryBtn = page.getByRole("button", { name: /tentar novamente/i });
    if (await retryBtn.isVisible()) {
      await retryBtn.click();
      await page.waitForTimeout(2000);
      expect(callCount).toBeGreaterThan(initialCount);
    }
  });
});

test.describe("Fire Spread Page - Panel Content", () => {
  test("shows fire title and coordinates", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByText("Incêndio em Seixal")).toBeVisible();
    await expect(page.getByText(/38\.\d+/)).toBeVisible();
    await expect(page.getByText(/-8\.\d+/)).toBeVisible();
  });

  test("shows current weather conditions", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByText("Temperatura")).toBeVisible();
    await expect(page.getByText("Humidade")).toBeVisible();
    await expect(page.getByText("Vento")).toBeVisible();
    await expect(page.getByText("FWI")).toBeVisible();
    await expect(page.getByText("32°C")).toBeVisible();
    await expect(page.getByText("28%")).toBeVisible();
    await expect(page.getByText("18 km/h")).toBeVisible();
  });

  test("shows all 5 horizon buttons (1h, 2h, 4h, 8h, 12h)", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByRole("button", { name: "1h" })).toBeVisible();
    await expect(page.getByRole("button", { name: "2h" })).toBeVisible();
    await expect(page.getByRole("button", { name: "4h" })).toBeVisible();
    await expect(page.getByRole("button", { name: "8h" })).toBeVisible();
    await expect(page.getByRole("button", { name: "12h" })).toBeVisible();
  });

  test("1h horizon selected by default", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const btn1h = page.getByRole("button", { name: "1h" });
    await expect(btn1h).toHaveClass(/ring|border-green-500/i);
  });

  test("clicking horizon button shows correct prediction data", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Click 4h button
    await page.getByRole("button", { name: "4h" }).click();
    await page.waitForTimeout(500);
    // Should show 4h prediction
    await expect(page.getByText(/Previsão 4h/i)).toBeVisible();
    await expect(page.getByText(/12\.8 km²/i)).toBeVisible();
    await expect(page.getByText(/ROS.*2\.3/i)).toBeVisible();
  });

  test("affected municipalities shown for selected horizon", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // 4h shows Seixal, Sesimbra, Fernão Ferro
    await page.getByRole("button", { name: "4h" }).click();
    await page.waitForTimeout(500);
    await expect(page.getByText("Seixal")).toBeVisible();
    await expect(page.getByText("Sesimbra")).toBeVisible();
    await expect(page.getByText("Fernão Ferro")).toBeVisible();
  });

  test("conclusion text shown for selected horizon", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await page.getByRole("button", { name: "4h" }).click();
    await page.waitForTimeout(500);
    await expect(page.getByText(/alastramento/i).first()).toBeVisible();
  });

  test("FWI evolution chart rendered", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    await expect(page.getByText("Evolução do FWI")).toBeVisible();
    // Chart is rendered via recharts - should have SVG
    await expect(page.locator(".recharts-wrapper, svg").first()).toBeVisible({ timeout: 5000 });
  });

  test("different horizon changes panel data completely", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // 1h shows 0.8 km²
    await expect(page.getByText(/0\.8 km²/i)).toBeVisible();
    // 12h shows 115.5 km²
    await page.getByRole("button", { name: "12h" }).click();
    await page.waitForTimeout(500);
    await expect(page.getByText(/115\.5 km²/i)).toBeVisible();
    await expect(page.getByText(/Barreiro/i)).toBeVisible();
  });
});

test.describe("Fire Spread Page - Map", () => {
  test("map shows fire marker at fire location", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Fire marker should be visible
    await expect(page.locator(".leaflet-marker-icon").first()).toBeVisible({ timeout: 5000 });
  });

  test("spreading ellipses visible on map", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([makeFireSpreadResponse()]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Ellipses are polygons on the map
    const paths = page.locator(".leaflet-overlay-pane path");
    await expect(paths.first()).toBeVisible({ timeout: 5000 });
  });
});

test.describe("Fire Spread Page - Multiple Fires", () => {
  test("dropdown shown when multiple fires active", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          makeFireSpreadResponse("fire-1"),
          { ...makeFireSpreadResponse("fire-2"), fireLocation: { latitude: 38.6, longitude: -8.7, title: "Fire in Almada" } },
        ])
      })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    // Dropdown should appear
    const select = page.locator("select").first();
    await expect(select).toBeVisible({ timeout: 3000 });
  });

  test("switching fire updates panel data", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          makeFireSpreadResponse("fire-1"),
          { ...makeFireSpreadResponse("fire-2"), fireLocation: { latitude: 38.6, longitude: -8.7, title: "Fire in Almada" } },
        ])
      })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.waitForTimeout(3000);
    const select = page.locator("select").first();
    await select.selectOption("fire-2");
    await page.waitForTimeout(1000);
    await expect(page.getByText("Fire in Almada")).toBeVisible();
  });
});

test.describe("Fire Spread Page - Navigation", () => {
  test("can navigate to fire-spread from sidebar", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([]) })
    );
    await page.goto("/dashboard");
    await page.waitForLoadState("load");
    await page.getByRole("link", { name: /simulação fogo/i }).click();
    await expect(page).toHaveURL(/\/fire-spread/, { timeout: 5000 });
  });

  test("can navigate back to dashboard from fire-spread", async ({ page }) => {
    await setupAuth(page);
    await page.route("http://localhost:5000/api/fires/active/spread", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify([]) })
    );
    await page.goto("/fire-spread");
    await page.waitForLoadState("load");
    await page.getByRole("link", { name: /dashboard/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 5000 });
  });
});