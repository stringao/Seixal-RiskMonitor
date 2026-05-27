"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, useRef, useEffect } from "react";
import {
  Map,
  LayoutDashboard,
  Shield,
  Bell,
  Settings,
  LogOut,
  ChevronLeft,
  ChevronRight,
  User,
  ChevronDown,
  Flame,
  Activity,
  Mountain,
} from "lucide-react";
import { useAuth } from "@/lib/hooks/useAuth";
import { AlertBadge } from "@/components/alerts/AlertBadge";

const NAV_ITEMS = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/map", label: "Mapa", icon: Map },
  { href: "/terrain", label: "Terreno", icon: Mountain },
  { href: "/risk-zones", label: "Zonas de Risco", icon: Flame },
  { href: "/fire-spread", label: "Simulação Fogo", icon: Activity },
  { href: "/insights", label: "Insights", icon: Shield },
  { href: "/alerts", label: "Alertas", icon: Bell },
];

const ROLE_COLORS: Record<string, string> = {
  Admin: "bg-purple-500/20 text-purple-400 border-purple-500/30",
  Analyst: "bg-blue-500/20 text-blue-400 border-blue-500/30",
  Viewer: "bg-slate-500/20 text-slate-400 border-slate-500/30",
};

const ROLE_LABELS: Record<string, string> = {
  Admin: "Administrador",
  Analyst: "Analista",
  Viewer: "Visualizador",
};

export function Sidebar({ unreadAlertCount = 0 }: { unreadAlertCount?: number }) {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const userRole = user?.role || "Viewer";
  const roleColor = ROLE_COLORS[userRole] || ROLE_COLORS.Viewer;
  const roleLabel = ROLE_LABELS[userRole] || userRole;
  const displayName = user?.email?.split("@")[0] || "Utilizador";
  const initials = displayName.slice(0, 2).toUpperCase();

  // Close menu when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleLogout = () => {
    logout();
    window.location.href = "/login";
  };

  return (
    <aside
      className={`h-screen bg-[#080d14]/95 backdrop-blur-md border-r border-slate-800/70 flex flex-col transition-all duration-300 shrink-0 ${
        collapsed ? "w-20" : "w-64"
      }`}
    >
      {/* Logo section */}
      <div className="h-14 px-4 border-b border-slate-800/60 flex items-center justify-between">
        <Link href="/dashboard" className="flex items-center gap-2.5 group">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-emerald-500/20 to-emerald-600/20 border border-emerald-500/30 flex items-center justify-center">
            <Shield className="w-4 h-4 text-emerald-400" />
          </div>
          {!collapsed && (
            <span className="text-sm font-bold text-white group-hover:text-emerald-400 transition-colors">
              SeixalRisk
            </span>
          )}
        </Link>
        <button
          onClick={() => setCollapsed(!collapsed)}
          className="p-1.5 rounded-lg text-slate-500 hover:text-white hover:bg-slate-700/50 transition-colors"
          title={collapsed ? "Expandir" : "Recolher"}
        >
          {collapsed ? (
            <ChevronRight className="w-4 h-4" />
          ) : (
            <ChevronLeft className="w-4 h-4" />
          )}
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-3 space-y-1">
        {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
          const active =
            pathname === href ||
            (href !== "/dashboard" && pathname.startsWith(href));
          return (
            <Link
              key={href}
              href={href}
              className={`relative flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-all duration-200 ${
                active
                  ? "bg-emerald-500/15 text-emerald-400 border border-emerald-500/20"
                  : "text-slate-400 hover:text-white hover:bg-slate-800/50 border border-transparent"
              } ${collapsed ? "justify-center" : ""}`}
              title={collapsed ? label : undefined}
            >
              <Icon className="w-5 h-5 shrink-0" />
              {!collapsed && <span>{label}</span>}
              {label === "Alertas" && unreadAlertCount > 0 && (
                <span className="absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center">
                  {unreadAlertCount > 9 ? "9+" : unreadAlertCount}
                </span>
              )}
            </Link>
          );
        })}
      </nav>

      {/* User profile section */}
      <div className="p-3 border-t border-slate-800/60 space-y-2" ref={menuRef}>
        {/* User menu toggle */}
        <div className="relative">
          <button
            onClick={() => setUserMenuOpen(!userMenuOpen)}
            className={`w-full flex items-center gap-3 p-3 rounded-xl bg-slate-800/40 border border-slate-700/30 hover:bg-slate-700/40 transition-colors ${
              collapsed ? "justify-center" : ""
            }`}
          >
            <div className="relative shrink-0">
              <div className="w-9 h-9 rounded-full bg-gradient-to-br from-emerald-500/30 to-emerald-600/30 border border-emerald-500/40 flex items-center justify-center">
                <span className="text-xs font-semibold text-emerald-400">
                  {initials}
                </span>
              </div>
              {/* Online indicator */}
              <span className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-emerald-500 rounded-full border-2 border-[#080d14]" />
            </div>
            {!collapsed && (
              <div className="flex-1 min-w-0 text-left">
                <div className="text-sm font-medium text-white truncate flex items-center gap-2">
                  {displayName}
                  <ChevronDown
                    className={`w-3 h-3 text-slate-400 transition-transform ${
                      userMenuOpen ? "rotate-180" : ""
                    }`}
                  />
                </div>
                <span
                  className={`inline-block text-[10px] px-1.5 py-0.5 rounded border mt-1 ${roleColor}`}
                >
                  {roleLabel}
                </span>
              </div>
            )}
          </button>

          {/* Dropdown menu */}
          {userMenuOpen && !collapsed && (
            <div className="absolute bottom-full left-0 right-0 mb-2 bg-slate-800/95 backdrop-blur-md border border-slate-700 rounded-xl overflow-hidden shadow-xl animate-in fade-in slide-in-from-bottom-2 duration-200">
              <div className="px-3 py-2 border-b border-slate-700/50">
                <div className="text-xs text-slate-400 truncate">{user?.email}</div>
              </div>
              <Link
                href="/settings"
                onClick={() => setUserMenuOpen(false)}
                className="flex items-center gap-3 px-3 py-2.5 text-sm text-slate-300 hover:bg-slate-700/50 hover:text-white transition-colors"
              >
                <User className="w-4 h-4" />
                <span>Perfil</span>
              </Link>
              <Link
                href="/settings"
                onClick={() => setUserMenuOpen(false)}
                className="flex items-center gap-3 px-3 py-2.5 text-sm text-slate-300 hover:bg-slate-700/50 hover:text-white transition-colors"
              >
                <Settings className="w-4 h-4" />
                <span>Definições</span>
              </Link>
              <div className="border-t border-slate-700/50">
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-3 px-3 py-2.5 text-sm text-red-400 hover:bg-red-500/10 transition-colors"
                >
                  <LogOut className="w-4 h-4" />
                  <span>Terminar Sessão</span>
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Logout button when collapsed */}
        {collapsed && (
          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center p-2.5 rounded-xl text-slate-400 hover:text-red-400 hover:bg-slate-800/50 transition-colors"
            title="Terminar Sessão"
          >
            <LogOut className="w-5 h-5" />
          </button>
        )}
      </div>
    </aside>
  );
}
