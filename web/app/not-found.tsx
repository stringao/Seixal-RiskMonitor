"use client";

import { Map, LayoutDashboard, Shield, AlertTriangle } from "lucide-react";
import Link from "next/link";

export default function NotFound() {
  return (
    <div className="min-h-screen bg-slate-900 flex items-center justify-center px-6">
      <div className="text-center">
        <div className="inline-flex items-center justify-center w-24 h-24 rounded-full bg-slate-800/80 mb-8">
          <span className="text-5xl font-bold text-slate-500">404</span>
        </div>
        <h1 className="text-2xl font-bold text-slate-100 mb-4">
          Página não encontrada
        </h1>
        <p className="text-slate-400 mb-8 max-w-md">
          A página que procura não existe ou foi movida.
        </p>
        <nav className="flex flex-wrap justify-center gap-4">
          <Link
            href="/dashboard"
            className="flex items-center gap-2 px-4 py-2 bg-emerald-500/20 text-emerald-400 rounded-lg text-sm font-medium hover:bg-emerald-500/30 transition-colors"
          >
            <LayoutDashboard className="w-4 h-4" />
            Dashboard
          </Link>
          <Link
            href="/map"
            className="flex items-center gap-2 px-4 py-2 bg-slate-700/80 text-slate-300 rounded-lg text-sm font-medium hover:bg-slate-600/80 transition-colors"
          >
            <Map className="w-4 h-4" />
            Mapa
          </Link>
        </nav>
      </div>
    </div>
  );
}