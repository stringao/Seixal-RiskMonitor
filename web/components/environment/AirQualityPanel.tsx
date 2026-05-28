"use client";

import { useState } from "react";
import { ChevronDown, ChevronUp, Wind, Flower2 } from "lucide-react";
import { AirQualityCard } from "./AirQualityCard";
import { PollenCard } from "./PollenCard";
import { AirQualityChart } from "./AirQualityChart";
import { HealthRiskBanner } from "./HealthRiskBanner";
import { buildChartDataFromForecast } from "@/lib/types/environment";
import type { AirQualityResponse, PollenResponse } from "@/lib/types/environment";

interface AirQualityPanelProps {
  airQualityData: AirQualityResponse | null;
  pollenData: PollenResponse | null;
  loading?: boolean;
  defaultVisible?: boolean;
}

export function AirQualityPanel({
  airQualityData,
  pollenData,
  loading = false,
  defaultVisible = true,
}: AirQualityPanelProps) {
  const [isVisible, setIsVisible] = useState(defaultVisible);

  return (
    <div className="bg-gradient-to-br from-slate-800/40 to-slate-800/20 border border-slate-700/50 rounded-2xl overflow-hidden">
      {/* Header - Always visible */}
      <button
        onClick={() => setIsVisible(!isVisible)}
        className="w-full px-5 py-4 flex items-center justify-between bg-slate-800/30 hover:bg-slate-800/50 transition-colors"
      >
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <Wind className="w-5 h-5 text-emerald-400" />
            <Flower2 className="w-5 h-5 text-yellow-400" />
          </div>
          <span className="font-semibold text-white">
            Qualidade do Ar & Polén
          </span>
          {!loading && (airQualityData || pollenData) && (
            <span className="text-xs text-slate-400 ml-2">
              Atualizado{" "}
              {airQualityData?.timestamp
                ? new Date(airQualityData.timestamp).toLocaleTimeString("pt-BR", {
                    hour: "2-digit",
                    minute: "2-digit",
                  })
                : "recentemente"}
            </span>
          )}
        </div>
        {isVisible ? (
          <ChevronUp className="w-5 h-5 text-slate-400" />
        ) : (
          <ChevronDown className="w-5 h-5 text-slate-400" />
        )}
      </button>

      {/* Content */}
      {isVisible && (
        <div className="p-5 space-y-5">
          {/* Health Risk Banner */}
          <HealthRiskBanner
            pollenLevel={pollenData?.level}
            pollenIndex={pollenData?.pollenIndex}
            airQualityLevel={airQualityData?.aqiLevel?.level}
            airQualityAqi={airQualityData?.aqi}
          />

          {/* Main cards grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <AirQualityCard data={airQualityData} loading={loading} />
            <PollenCard data={pollenData} loading={loading} />
          </div>

          {/* Chart */}
          <AirQualityChart
            data={airQualityData?.raw ? buildChartDataFromForecast(airQualityData.raw.forecast) : undefined}
            loading={loading}
          />
        </div>
      )}
    </div>
  );
}