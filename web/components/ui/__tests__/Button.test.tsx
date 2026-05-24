import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, it, expect, vi } from "vitest";
import { Button } from "../Button";

describe("Button", () => {
  it("renders children as button text", () => {
    render(<Button>Click me</Button>);
    expect(
      screen.getByRole("button", { name: /click me/i })
    ).toBeInTheDocument();
  });

  it("applies primary variant classes by default", () => {
    render(<Button>Primary</Button>);
    const button = screen.getByRole("button");
    expect(button.className).toContain("bg-emerald-500");
  });

  it("applies secondary variant classes when variant is secondary", () => {
    render(<Button variant="secondary">Secondary</Button>);
    const button = screen.getByRole("button");
    expect(button.className).toContain("bg-slate-700");
  });

  it("shows spinner and disables button when isLoading is true", () => {
    render(<Button isLoading>Submit</Button>);
    const button = screen.getByRole("button");

    // Button should be disabled
    expect(button).toBeDisabled();

    // Children text should not be visible (replaced by spinner)
    expect(screen.queryByText("Submit")).not.toBeInTheDocument();
  });

  it("disables button when disabled prop is true", () => {
    render(<Button disabled>Disabled</Button>);
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("disables button when both isLoading and disabled are true", () => {
    render(
      <Button isLoading disabled>
        Both
      </Button>
    );
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("fires onClick handler when clicked", async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();

    render(<Button onClick={handleClick}>Click</Button>);
    await user.click(screen.getByRole("button"));

    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("does not fire onClick when disabled", async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();

    render(
      <Button onClick={handleClick} disabled>
        Click
      </Button>
    );
    await user.click(screen.getByRole("button"));

    expect(handleClick).not.toHaveBeenCalled();
  });

  it("does not fire onClick when loading", async () => {
    const handleClick = vi.fn();
    const user = userEvent.setup();

    render(
      <Button onClick={handleClick} isLoading>
        Click
      </Button>
    );
    await user.click(screen.getByRole("button"));

    expect(handleClick).not.toHaveBeenCalled();
  });

  it("applies custom className alongside base classes", () => {
    render(<Button className="my-custom-class">Custom</Button>);
    const button = screen.getByRole("button");
    expect(button.className).toContain("my-custom-class");
    expect(button.className).toContain("inline-flex");
  });

  it("passes through additional HTML button attributes", () => {
    render(
      <Button type="submit" form="my-form">
        Submit
      </Button>
    );
    const button = screen.getByRole("button");
    expect(button).toHaveAttribute("type", "submit");
    expect(button).toHaveAttribute("form", "my-form");
  });

  it("has correct base styling classes", () => {
    render(<Button>Styled</Button>);
    const button = screen.getByRole("button");
    const className = button.className;
    expect(className).toContain("inline-flex");
    expect(className).toContain("rounded-lg");
    expect(className).toContain("font-semibold");
    expect(className).toContain("disabled:opacity-50");
  });
});
