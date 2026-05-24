import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { Card } from "../Card";

describe("Card", () => {
  it("renders children", () => {
    render(<Card>Card content</Card>);
    expect(screen.getByText("Card content")).toBeInTheDocument();
  });

  it("renders complex children", () => {
    render(
      <Card>
        <h1>Title</h1>
        <p>Description</p>
      </Card>
    );
    expect(screen.getByText("Title")).toBeInTheDocument();
    expect(screen.getByText("Description")).toBeInTheDocument();
  });

  it("applies default card styling classes", () => {
    const { container } = render(<Card data-testid="card">Content</Card>);
    const card = container.firstChild as HTMLElement;
    expect(card.className).toContain("rounded-xl");
    expect(card.className).toContain("bg-slate-800/50");
    expect(card.className).toContain("border-slate-700");
  });

  it("applies custom className alongside default classes", () => {
    const { container } = render(
      <Card className="my-custom-class">Content</Card>
    );
    const card = container.firstChild as HTMLElement;
    expect(card.className).toContain("my-custom-class");
    expect(card.className).toContain("rounded-xl");
  });

  it("renders as a div element", () => {
    const { container } = render(<Card data-testid="card">Content</Card>);
    const card = container.firstChild as HTMLElement;
    expect(card.tagName).toBe("DIV");
  });
});
