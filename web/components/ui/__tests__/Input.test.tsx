import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { Input } from "../Input";
import React from "react";

describe("Input", () => {
  it("renders label text", () => {
    render(<Input label="Email" />);
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
  });

  it("renders input with correct type", () => {
    render(<Input label="Password" type="password" />);
    const input = screen.getByLabelText("Password");
    expect(input).toHaveAttribute("type", "password");
  });

  it("displays error message when error prop is provided", () => {
    render(<Input label="Email" error="Email is required" />);
    expect(screen.getByText("Email is required")).toBeInTheDocument();
  });

  it("does not display error element when error prop is undefined", () => {
    render(<Input label="Email" />);
    const errorElements = screen.queryAllByText(/.+/).filter(
      (el) => el.classList.contains("text-red-400")
    );
    expect(errorElements).toHaveLength(0);
  });

  it("applies error border class when error is present", () => {
    render(<Input label="Email" error="Required" />);
    const input = screen.getByLabelText("Email");
    expect(input.className).toContain("border-red-500");
  });

  it("applies default border class when no error", () => {
    render(<Input label="Email" />);
    const input = screen.getByLabelText("Email");
    expect(input.className).toContain("border-slate-600");
  });

  it("generates id from label when id is not provided", () => {
    render(<Input label="First Name" />);
    const input = screen.getByLabelText("First Name");
    expect(input).toHaveAttribute("id", "first-name");
  });

  it("uses provided id when id prop is given", () => {
    render(<Input label="Email" id="custom-email-id" />);
    const input = screen.getByLabelText("Email");
    expect(input).toHaveAttribute("id", "custom-email-id");
  });

  it("forwards ref to the input element", () => {
    const ref = React.createRef<HTMLInputElement>();
    render(<Input label="Email" ref={ref} />);
    expect(ref.current).not.toBeNull();
    expect(ref.current?.tagName).toBe("INPUT");
  });

  it("applies custom className alongside default classes", () => {
    render(<Input label="Email" className="my-extra-class" />);
    const input = screen.getByLabelText("Email");
    expect(input.className).toContain("my-extra-class");
    expect(input.className).toContain("rounded-lg");
  });

  it("passes through placeholder attribute", () => {
    render(<Input label="Email" placeholder="you@example.com" />);
    const input = screen.getByLabelText("Email");
    expect(input).toHaveAttribute("placeholder", "you@example.com");
  });

  it("passes through required attribute", () => {
    render(<Input label="Email" required />);
    const input = screen.getByLabelText("Email");
    expect(input).toHaveAttribute("required");
  });

  it("associates label with input via htmlFor matching input id", () => {
    render(<Input label="Email" id="email-field" />);
    const label = screen.getByText("Email");
    expect(label).toHaveAttribute("for", "email-field");
    const input = screen.getByLabelText("Email");
    expect(input).toHaveAttribute("id", "email-field");
  });
});
