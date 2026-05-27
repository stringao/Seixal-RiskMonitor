"use client";

import dynamic from "next/dynamic";

const TerrainSlopeMap = dynamic(
  () => import("@/components/map/TerrainSlopeMap").then((m) => m.TerrainSlopeMap),
  {
    ssr: false,
    loading: () => (
      <div className="h-full w-full bg-slate-900 flex items-center justify-center">
        <div className="text-slate-400">A carregar mapa de relevo...</div>
      </div>
    ),
  }
);

export default function TerrainPage() {
  return (
    <div className="h-full w-full">
      <TerrainSlopeMap className="h-full w-full" />
    </div>
  );
}