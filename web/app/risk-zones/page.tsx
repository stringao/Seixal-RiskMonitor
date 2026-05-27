"use client";

import dynamic from "next/dynamic";

const RiskZonesMap = dynamic(
  () => import("@/components/risk/RiskZonesMap").then((m) => m.RiskZonesMap),
  {
    ssr: false,
    loading: () => (
      <div className="h-full w-full bg-[#050a0f] flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-emerald-500" />
      </div>
    ),
  }
);

export default function RiskZonesPage() {
  return (
    <div className="h-full overflow-hidden">
      <RiskZonesMap />
    </div>
  );
}