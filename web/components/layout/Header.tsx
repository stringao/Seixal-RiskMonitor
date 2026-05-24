"use client";

import { Bell } from "lucide-react";
import Link from "next/link";
import { AlertBadge } from "@/components/alerts/AlertBadge";

interface HeaderProps {
  title: string;
  unreadAlertCount?: number;
}

export function Header({ title, unreadAlertCount = 0 }: HeaderProps) {
  return (
    <header className="h-16 bg-slate-800/80 border-b border-slate-700 flex items-center justify-between px-6">
      <h1 className="text-lg font-semibold text-white">{title}</h1>
      <div className="flex items-center gap-4">
        <Link
          href="/dashboard/alerts"
          className="relative p-2 text-slate-400 hover:text-white transition-colors"
        >
          <AlertBadge count={unreadAlertCount} />
        </Link>
        <div className="w-8 h-8 rounded-full bg-emerald-500/20 flex items-center justify-center">
          <span className="text-sm font-semibold text-emerald-400">U</span>
        </div>
      </div>
    </header>
  );
}