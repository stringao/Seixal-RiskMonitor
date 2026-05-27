import { test, expect } from "@playwright/test";

const AUTH_RESPONSE = {
  accessToken: "test-access-token",
  refreshToken: "test-refresh-token",
  user: { id: "1", email: "test@test.com", role: "Analyst" },
};

async function mockLoginApi(page: import("@playwright/test").Page) {
  await page.route("**/api/auth/login", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(AUTH_RESPONSE),
    })
  );
}

async function mockRegisterApi(page: import("@playwright/test").Page) {
  await page.route("**/api/auth/register", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(AUTH_RESPONSE),
    })
  );
}

async function mockMeApi(page: import("@playwright/test").Page) {
  await page.route("**/api/me", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(AUTH_RESPONSE.user),
    })
  );
}

test.describe("Landing page navigation", () => {
  test("shows SeixalRisk Monitor heading on landing page", async ({ page }) => {
    await page.goto("/");
    const heading = page.locator("h1");
    await expect(heading).toContainText("SeixalRisk");
    await expect(heading).toContainText("Monitor");
  });

  test("clicking Entrar navigates to /login", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("h1")).toContainText("SeixalRisk");

    const entrarLink = page.getByRole("link", { name: "Entrar" });
    await expect(entrarLink).toBeVisible();
    await entrarLink.click();
    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });
});

test.describe("Login flow", () => {
  test("shows Welcome back heading on login page", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("h1")).toHaveText("Welcome back");
  });

  test("login form has email and password inputs", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
  });

  test("submitting valid credentials redirects to /dashboard", async ({
    page,
  }) => {
    await mockLoginApi(page);
    await mockMeApi(page);
    await page.goto("/login");

    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("password123");
    await page.getByRole("button", { name: "Sign in" }).click();

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
  });

  test("clicking Create one link navigates to /register", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("h1")).toHaveText("Welcome back");

    const createLink = page.getByRole("link", { name: "Create one" });
    await expect(createLink).toBeVisible();
    await createLink.click();
    await expect(page).toHaveURL(/\/register/, { timeout: 10000 });
  });

  test("displays error message when login fails", async ({ page }) => {
    // Mock /me so AuthProvider doesn't clear localStorage on mount
    await page.route("**/api/me", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ id: "1", email: "test@test.com", role: "Analyst" }),
      })
    );
    // Mock refresh to fail so the interceptor clears tokens and redirects
    await page.route("**/api/auth/refresh", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ message: "Refresh failed" }),
      })
    );
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ message: "Invalid email or password" }),
      })
    );

    await page.goto("/login");
    await page.evaluate(() => {
      localStorage.setItem("access_token", "dummy-access-token");
      localStorage.setItem("refresh_token", "dummy-refresh-token");
    });
    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("wrongpassword");
    await page.getByRole("button", { name: "Sign in" }).click();

    // Stay on /login after login failure - form should still be visible
    await expect(page).toHaveURL(/\/login/, { timeout: 8000 });
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
  });
});

test.describe("Login validation", () => {
  test("empty form submission is blocked by HTML5 validation", async ({
    page,
  }) => {
    await page.goto("/login");
    await expect(page.locator("#email")).toBeVisible();

    // Submit without filling -- browser validation prevents submission
    await page.getByRole("button", { name: "Sign in" }).click();

    // Page stays on /login since HTML5 required validation blocks submit
    await expect(page).toHaveURL(/\/login/);
  });

  test("invalid email shows browser validation", async ({ page }) => {
    await page.goto("/login");
    await page.locator("#email").fill("not-an-email");
    await page.locator("#password").fill("password123");
    await page.getByRole("button", { name: "Sign in" }).click();

    // Browser validation keeps us on /login
    await expect(page).toHaveURL(/\/login/);
  });
});

test.describe("Registration flow", () => {
  test("shows Create account heading on register page", async ({ page }) => {
    await page.goto("/register");
    await expect(page.locator("h1")).toHaveText("Create account");
  });

  test("register form has email, password, and role selector", async ({
    page,
  }) => {
    await page.goto("/register");
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Viewer" })
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Analyst" })
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Admin" })
    ).toBeVisible();
  });

  test("submitting valid registration redirects to /dashboard", async ({
    page,
  }) => {
    await mockRegisterApi(page);
    await mockMeApi(page);
    await page.goto("/register");

    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("password123");
    await page.getByRole("button", { name: "Analyst" }).click();
    await page.getByRole("button", { name: "Create account" }).click();

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
  });

  test("clicking Sign in link navigates to /login", async ({ page }) => {
    await page.goto("/register");
    await expect(page.locator("h1")).toHaveText("Create account");

    const signInLink = page.getByRole("link", { name: "Sign in" });
    await expect(signInLink).toBeVisible();
    await signInLink.click();
    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });
});

test.describe("Registration validation", () => {
  test("empty form submission is blocked by HTML5 validation", async ({
    page,
  }) => {
    await page.goto("/register");
    await expect(page.locator("#email")).toBeVisible();

    // Submit without filling -- browser validation prevents submission
    await page.getByRole("button", { name: "Create account" }).click();

    // Browser validation keeps us on /register
    await expect(page).toHaveURL(/\/register/);
  });

  test("short password is blocked by minLength validation", async ({
    page,
  }) => {
    await page.goto("/register");
    await page.locator("#email").fill("test@test.com");
    // Password shorter than minLength=8
    await page.locator("#password").fill("short");
    await page.getByRole("button", { name: "Create account" }).click();

    // Browser validation keeps us on /register
    await expect(page).toHaveURL(/\/register/);
  });
});

test.describe("Protected route redirect", () => {
  test("visiting /dashboard without auth redirects to /login", async ({
    page,
  }) => {
    await page.goto("/dashboard");
    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });

  test("redirect to /login preserves callbackUrl parameter", async ({
    page,
  }) => {
    await page.goto("/dashboard");
    await expect(page).toHaveURL(/callbackUrl=%2Fdashboard/, {
      timeout: 10000,
    });
  });
});

test.describe("Auth error display", () => {
  test("displays error when server returns 401 on login", async ({ page }) => {
    // Mock /me so AuthProvider doesn't clear localStorage on mount
    await page.route("**/api/me", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ id: "1", email: "test@test.com", role: "Analyst" }),
      })
    );
    // Mock refresh to fail so the interceptor redirects without reading the error
    await page.route("**/api/auth/refresh", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ message: "Refresh failed" }),
      })
    );
    await page.route("**/api/auth/login", (route) =>
      route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ message: "Invalid credentials" }),
      })
    );

    await page.goto("/login");
    await page.evaluate(() => {
      localStorage.setItem("access_token", "dummy-access-token");
      localStorage.setItem("refresh_token", "dummy-refresh-token");
    });
    await page.locator("#email").fill("test@test.com");
    await page.locator("#password").fill("wrongpassword");
    await page.getByRole("button", { name: "Sign in" }).click();

    // Stay on /login after login failure - form should still be visible
    await expect(page).toHaveURL(/\/login/, { timeout: 8000 });
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
  });

  test("displays error when registration fails", async ({ page }) => {
    await page.route("**/api/auth/register", (route) =>
      route.fulfill({
        status: 400,
        contentType: "application/json",
        body: JSON.stringify({ message: "Email already exists" }),
      })
    );

    await page.goto("/register");
    await page.locator("#email").fill("existing@test.com");
    await page.locator("#password").fill("password123");
    await page.getByRole("button", { name: "Create account" }).click();

    // Form should still be visible after error
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
  });
});
