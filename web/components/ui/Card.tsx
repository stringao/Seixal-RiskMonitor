import { type ReactNode } from "react";

interface CardProps {
  children: ReactNode;
  className?: string;
}

export function Card({ children, className = "" }: Readonly<CardProps>) {
  return (
    <div
      className={`w-full max-w-md rounded-xl border border-slate-700 bg-slate-800/50 p-8 backdrop-blur-sm ${className}`}
    >
      {children}
    </div>
  );
}
