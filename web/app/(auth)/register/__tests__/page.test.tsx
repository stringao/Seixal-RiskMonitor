import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import React from "react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { AuthProvider } from "@/lib/contexts/AuthContext";

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------

const mockLogin = vi.fn();
const mockRegister = vi.fn();
const mockLogout = vi.fn();

vi.mock("@/lib/hooks/useAuth", () => ({
  useAuth: () => ({
    user: null,
    isLoading: false,
    login: mockLogin,
    register: mockRegister,
    logout: mockLogout,
  }),
}));

const mockReplace = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: mockReplace, push: vi.fn() }),
}));

// ---------------------------------------------------------------------------
// Import the component AFTER mocks are set up
// ---------------------------------------------------------------------------

import RegisterPage from "../page";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function renderRegisterPage() {
  return render(
    <AuthProvider>
      <RegisterPage />
    </AuthProvider>
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("RegisterPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders email, password, role selector, and submit button", () => {
    renderRegisterPage();

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /create account/i })).toBeInTheDocument();

    // Three role buttons
    expect(screen.getByRole("button", { name: /^viewer$/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^analyst$/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^admin$/i })).toBeInTheDocument();
  });

  it("selecting a role updates the selected state", async () => {
    const user = userEvent.setup();
    renderRegisterPage();

    const analystButton = screen.getByRole("button", { name: /^analyst$/i });
    await user.click(analystButton);

    // The Analyst button should now have the active styling class
    expect(analystButton.className).toContain("border-emerald-500");
  });

  it("calls register and redirects to /dashboard on success", async () => {
    const user = userEvent.setup();
    mockRegister.mockResolvedValueOnce(undefined);

    renderRegisterPage();

    await user.type(screen.getByLabelText(/email/i), "new@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /^analyst$/i }));
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        "new@example.com",
        "password123",
        "Analyst"
      );
    });
    expect(mockReplace).toHaveBeenCalledWith("/dashboard");
  });

  it("shows error message on failed registration", async () => {
    const user = userEvent.setup();
    mockRegister.mockRejectedValueOnce(new Error("Email already exists"));

    renderRegisterPage();

    await user.type(screen.getByLabelText(/email/i), "taken@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText("Email already exists")).toBeInTheDocument();
    });
  });

  it("shows generic error message when error is not an Error instance", async () => {
    const user = userEvent.setup();
    mockRegister.mockRejectedValueOnce("unknown error");

    renderRegisterPage();

    await user.type(screen.getByLabelText(/email/i), "taken@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText("Registration failed")).toBeInTheDocument();
    });
  });

  it("shows link to login page", () => {
    renderRegisterPage();

    const loginLink = screen.getByRole("link", { name: /sign in/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/login");
  });

  // -----------------------------------------------------------------------
  // Edge cases: role selection
  // -----------------------------------------------------------------------

  it("allows selecting Viewer role", async () => {
    const user = userEvent.setup();
    mockRegister.mockResolvedValueOnce(undefined);

    renderRegisterPage();

    const viewerButton = screen.getByRole("button", { name: /^viewer$/i });
    await user.click(viewerButton);

    await user.type(screen.getByLabelText(/email/i), "viewer@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        "viewer@example.com",
        "password123",
        "Viewer"
      );
    });
  });

  it("allows selecting Admin role", async () => {
    const user = userEvent.setup();
    mockRegister.mockResolvedValueOnce(undefined);

    renderRegisterPage();

    const adminButton = screen.getByRole("button", { name: /^admin$/i });
    await user.click(adminButton);

    await user.type(screen.getByLabelText(/email/i), "admin@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(mockRegister).toHaveBeenCalledWith(
        "admin@example.com",
        "password123",
        "Admin"
      );
    });
  });

  // -----------------------------------------------------------------------
  // Edge cases: password validation
  // -----------------------------------------------------------------------

  it("has minLength attribute on password input for 8 character minimum", () => {
    renderRegisterPage();

    const passwordInput = screen.getByLabelText(/password/i);
    expect(passwordInput).toHaveAttribute("minLength", "8");
  });

  // -----------------------------------------------------------------------
  // Edge cases: network error
  // -----------------------------------------------------------------------

  it("handles network error during registration", async () => {
    const user = userEvent.setup();
    mockRegister.mockRejectedValueOnce(new Error("Network Error"));

    renderRegisterPage();

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText("Network Error")).toBeInTheDocument();
    });
  });

  it("clears previous error on new submission attempt", async () => {
    const user = userEvent.setup();

    // First attempt fails
    mockRegister.mockRejectedValueOnce(new Error("Email already exists"));

    renderRegisterPage();

    await user.type(screen.getByLabelText(/email/i), "taken@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText("Email already exists")).toBeInTheDocument();
    });

    // Second attempt succeeds
    mockRegister.mockResolvedValueOnce(undefined);

    await user.clear(screen.getByLabelText(/email/i));
    await user.type(screen.getByLabelText(/email/i), "new@example.com");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.queryByText("Email already exists")).not.toBeInTheDocument();
    });
  });

  it("renders the create account heading", () => {
    renderRegisterPage();
    expect(screen.getByRole("heading", { name: /create account/i })).toBeInTheDocument();
  });
});
