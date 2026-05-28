"use client";

import { useState, useCallback } from "react";
import dynamic from "next/dynamic";
import { RefreshCw, Wind, Flower2, MapPin } from "lucide-react";
import { useAirQuality, usePollen } from "@/lib/hooks/useAirQuality";
import { AirQualityCard } from "@/components/environment/AirQualityCard";
import { PollenCard } from "@/components/environment/PollenCard";
import { AirQualityChart } from "@/components/environment/AirQualityChart";
import { HealthRiskBanner } from "@/components/environment/HealthRiskBanner";
import { buildChartDataFromForecast } from "@/lib/types/environment";
import { MAP_CONFIG } from "@/lib/map/config";

const AirQualityMap = dynamic(
  () => import("@/components/environment/AirQualityMap").then((m) => m.AirQualityMap),
  { ssr: false, loading: () => <div className="h-[400px] bg-slate-800/60 rounded-2xl animate-pulse" /> }
);

// Use Seixal coordinates from map config
const SEIXAL_LAT = MAP_CONFIG.center.lat;
const SEIXAL_LNG = MAP_CONFIG.center.lng;

export default function AirQualityPage() {
  const [refreshKey, setRefreshKey] = useState(0);

  const { data: airQualityData, loading: airQualityLoading, error: airError, refetch: refetchAir } = useAirQuality(SEIXAL_LAT, SEIXAL_LNG);
  const { data: pollenData, loading: pollenLoading, error: pollenError, refetch: refetchPollen } = usePollen(SEIXAL_LAT, SEIXAL_LNG);

  const loading = airQualityLoading || pollenLoading;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetchAir();
    refetchPollen();
  }, [refetchAir, refetchPollen]);

  if (loading) {
    return (
      <div className="p-6 space-y-6">
        {/* Header skeleton */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-slate-800/60 rounded-xl animate-pulse" />
            <div className="space-y-2">
              <div className="h-6 w-48 bg-slate-800/60 rounded animate-pulse" />
              <div className="h-4 w-32 bg-slate-800/60 rounded animate-pulse" />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="h-80 bg-slate-800/60 rounded-2xl animate-pulse" />
          <div className="h-80 bg-slate-800/60 rounded-2xl animate-pulse" />
        </div>
        <div className="h-64 bg-slate-800/60 rounded-2xl animate-pulse" />
      </div>
    );
  }

  const hasError = airError && pollenError;

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-500/20 to-emerald-600/20 border border-emerald-500/30 flex items-center justify-center">
            <Wind className="w-5 h-5 text-emerald-400" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-white">Qualidade do Ar & Poluição</h1>
            <div className="flex items-center gap-2 text-sm text-slate-400">
              <MapPin className="w-3.5 h-3.5" />
              <span>Seixal ({SEIXAL_LAT.toFixed(4)}°N, {Math.abs(SEIXAL_LNG).toFixed(4)}°W)</span>
              {!loading && (airQualityData || pollenData) && (
                <>
                  <span className="text-slate-600">•</span>
                  <span>
                    Atualizado{" "}
                    {airQualityData?.timestamp
                      ? new Date(airQualityData.timestamp).toLocaleTimeString("pt-PT", {
                          hour: "2-digit",
                          minute: "2-digit",
                        })
                      : "recentemente"}
                  </span>
                </>
              )}
            </div>
          </div>
        </div>
        <button
          onClick={handleRefresh}
          className="flex items-center gap-2 px-3 py-2 rounded-xl text-sm font-medium text-slate-300 bg-slate-800/50 border border-slate-700/50 hover:bg-slate-700/50 hover:text-white transition-colors"
        >
          <RefreshCw className="w-4 h-4" />
          Atualizar
        </button>
      </div>

      {/* Error state */}
      {hasError && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-sm text-red-400">
          Erro ao carregar dados: {airError || pollenError}
        </div>
      )}

      {/* Health Risk Banner */}
      <HealthRiskBanner
        pollenLevel={pollenData?.level}
        pollenIndex={pollenData?.pollenIndex}
        airQualityLevel={airQualityData?.aqiLevel?.level}
        airQualityAqi={airQualityData?.aqi}
      />

      {/* AQI Summary Banner */}
      {airQualityData && (
        <div className="bg-gradient-to-r from-slate-800/40 to-slate-800/20 border border-slate-700/50 rounded-2xl p-5">
          <div className="flex items-center gap-6 flex-wrap">
            {/* AQI Level Indicator */}
            <div className="flex items-center gap-3">
              <Wind className="w-6 h-6 text-emerald-400" />
              <div>
                <div className="text-xs text-slate-400 uppercase tracking-wider">Índice Qualidade do Ar</div>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-2xl font-bold text-white">{airQualityData.aqiLevel.label}</span>
                  <span className="text-sm text-slate-400">({airQualityData.aqi}/5)</span>
                  <div className="flex gap-1 ml-2">
                    {Array.from({ length: 5 }, (_, i) => (
                      <div
                        key={i}
                        className="w-3 h-3 rounded-full"
                        style={{
                          backgroundColor: i < airQualityData.aqi ? airQualityData.aqiLevel.color : "#334155",
                        }}
                      />
                    ))}
                  </div>
                </div>
              </div>
            </div>

            {/* Divider */}
            <div className="w-px h-12 bg-slate-700/50 hidden md:block" />

            {/* Pollen Level Indicator */}
            {pollenData && (
              <div className="flex items-center gap-3">
                <Flower2 className="w-6 h-6 text-yellow-400" />
                <div>
                  <div className="text-xs text-slate-400 uppercase tracking-wider">Índice de Polén</div>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-2xl font-bold text-white">{pollenData.label}</span>
                    <span className="text-sm text-slate-400">({pollenData.pollenIndex}/5)</span>
                  </div>
                </div>
              </div>
            )}

            {/* Divider */}
            <div className="w-px h-12 bg-slate-700/50 hidden md:block" />

            {/* Main Pollutant */}
            {airQualityData.pollutants.length > 0 && (
              <div>
                <div className="text-xs text-slate-400 uppercase tracking-wider">Poluente Dominante</div>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-2xl font-bold text-white">
                    {airQualityData.pollutants[0].symbol}
                  </span>
                  <span className="text-sm text-slate-400">
                    {airQualityData.pollutants[0].value} {airQualityData.pollutants[0].unit}
                  </span>
                </div>
              </div>
            )}

            {/* Health Risk */}
            {airQualityData.healthRisk && (
              <>
                <div className="w-px h-12 bg-slate-700/50 hidden md:block" />
                <div>
                  <div className="text-xs text-slate-400 uppercase tracking-wider">Risco Saúde</div>
                  <div className="mt-1">
                    <span className="text-sm font-medium text-amber-400">{airQualityData.healthRisk}</span>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* Map — Seixal monitoring station */}
      <AirQualityMap
        airQualityData={airQualityData}
        pollenData={pollenData}
      />

      {/* Main Cards Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AirQualityCard data={airQualityData} loading={false} />
        <PollenCard data={pollenData} loading={false} />
      </div>

      {/* Chart Section — use real forecast data from API */}
      <AirQualityChart
        data={airQualityData?.raw ? buildChartDataFromForecast(airQualityData.raw.forecast) : undefined}
        loading={false}
      />

      {/* Pollutants Detail Table */}
      {airQualityData && airQualityData.pollutants.length > 0 && (
        <div className="bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/50 rounded-2xl p-5">
          <h3 className="text-sm font-semibold text-white mb-4 flex items-center gap-2">
            <Wind className="w-4 h-4 text-emerald-400" />
            Detalhes dos Poluentes
          </h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-700/50">
                  <th className="text-left text-slate-400 font-medium pb-3 pr-4">Poluente</th>
                  <th className="text-left text-slate-400 font-medium pb-3 pr-4">Concentração</th>
                  <th className="text-left text-slate-400 font-medium pb-3 pr-4">Nível</th>
                  <th className="text-left text-slate-400 font-medium pb-3 pr-4">Nível Visual</th>
                </tr>
              </thead>
              <tbody>
                {airQualityData.pollutants.map((pollutant) => (
                  <tr key={pollutant.symbol} className="border-b border-slate-800/50">
                    <td className="py-3 pr-4">
                      <span className="text-white font-medium">{pollutant.symbol}</span>
                      <span className="text-slate-400 ml-2 text-xs">({pollutant.name})</span>
                    </td>
                    <td className="py-3 pr-4 text-slate-300">
                      {pollutant.value} {pollutant.unit}
                    </td>
                    <td className="py-3 pr-4">
                      <span
                        className="text-xs font-medium px-2 py-0.5 rounded-full"
                        style={{ color: pollutant.color }}
                      >
                        {pollutant.level}
                      </span>
                    </td>
                    <td className="py-3 pr-4">
                      <div className="w-32 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all"
                          style={{
                            width: `${Math.min((pollutant.value / 200) * 100, 100)}%`,
                            backgroundColor: pollutant.color,
                          }}
                        />
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
