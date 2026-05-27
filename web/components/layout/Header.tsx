"use client";

interface HeaderProps {
  title: string;
}

export function Header({ title }: HeaderProps) {
  return (
    <header className="h-12 bg-[#0a1118]/60 backdrop-blur-md border-b border-slate-800/60 flex items-center px-6 lg:px-8">
      <h1 className="text-sm lg:text-base font-semibold text-white tracking-tight">{title}</h1>
    </header>
  );
}
