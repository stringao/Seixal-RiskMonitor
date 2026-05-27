"use client";

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";
import type { FireSpreadResponse } from "@/lib/types/riskZone";

const HORIZON_COLORS: Record<number, { bg: string; text: string; border: string; hex: string }> = {
  1:  { bg: "bg-green-500/20", text: "text-green-400", border: "border-green-500", hex: "#22c55e" },
  2:  { bg: "bg-yellow-500/20", text: "text-yellow-400", border: "border-yellow-500", hex: "#eab308" },
  4:  { bg: "bg-orange-500/20", text: "text-orange-400", border: "border-orange-500", hex: "#f97316" },
  8:  { bg: "bg-red-500/20", text: "text-red-400", border: "border-red-500", hex: "#ef4444" },
  12: { bg: "bg-red-800/20", text: "text-red-600", border: "border-red-800", hex: "#991b1b" },
};

const HORIZONS = [1, 2, 4, 8, 12];

interface SpreadPanelProps {
  fireSpread: FireSpreadResponse;
  selectedHorizon: number;
  onHorizonChange: (horizon: number) => void;
}

export function SpreadPanel({ fireSpread, selectedHorizon, onHorizonChange }: SpreadPanelProps) {
  const selectedPrediction = fireSpread.horizons.find((h) => h.hours === selectedHorizon);
  const hourLabels = ["1h", "2h", "4h", "8h", "12h"];

  // Build chart data from currentWeather - using FWI values over horizon times
  const chartData = fireSpread.horizons.map((h) => ({
    name: `${h.hours}h`,
    fwi: h.rosKmh * 10, // approximate FWI display
    ros: h.rosKmh,
  }));

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e] text-white p-4 overflow-y-auto">
      {/* Fire title and location */}
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white mb-1">{fireSpread.fireLocation.title}</h2>
        <p className="text-sm text-gray-400">
          {fireSpread.fireLocation.latitude.toFixed(4)}, {fireSpread.fireLocation.longitude.toFixed(4)}
        </p>
      </div>

      {/* Current weather conditions */}
      <div className="mb-6 p-4 bg-[#16213e] rounded-lg border border-gray-700">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Condições Atuais
        </h3>
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-gray-400">Temperatura</span>
            <p className="text-white font-medium">{fireSpread.currentWeather.temperature}°C</p>
          </div>
          <div>
            <span className="text-gray-400">Humidade</span>
            <p className="text-white font-medium">{fireSpread.currentWeather.humidity}%</p>
          </div>
          <div>
            <span className="text-gray-400">Vento</span>
            <p className="text-white font-medium">{fireSpread.currentWeather.windSpeed} km/h</p>
          </div>
          <div>
            <span className="text-gray-400">FWI</span>
            <p className="text-white font-medium">{fireSpread.currentWeather.fwi}</p>
          </div>
        </div>
      </div>

      {/* Timeline selector */}
      <div className="mb-6">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Temporalidade
        </h3>
        <div className="flex gap-2">
          {HORIZONS.map((hours, index) => {
            const colors = HORIZON_COLORS[hours];
            const isSelected = selectedHorizon === hours;
            return (
              <button
                key={hours}
                onClick={() => onHorizonChange(hours)}
                className={`
                  flex-1 py-2 px-1 rounded text-xs font-bold uppercase tracking-wide
                  transition-all duration-200 border
                  ${isSelected
                    ? `${colors.bg} ${colors.text} ${colors.border} ring-2 ring-offset-1 ring-offset-[#1a1a2e]`
                    : "bg-gray-800 text-gray-400 border-gray-700 hover:bg-gray-700"
                  }
                `}
              >
                {hourLabels[index]}
              </button>
            );
          })}
        </div>
      </div>

      {/* Selected horizon details */}
      {selectedPrediction && (
        <div className="mb-6 p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <h3 className="text-sm font-semibold mb-3 uppercase tracking-wide" style={{ color: HORIZON_COLORS[selectedHorizon].hex }}>
            Previsão {selectedHorizon}h
          </h3>
          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-400">ROS</span>
              <span className="text-white font-medium">{selectedPrediction.rosKmh} km/h</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">Área</span>
              <span className="text-white font-medium">{selectedPrediction.areaKm2} km²</span>
            </div>
            <div>
              <span className="text-gray-400 block mb-1">Municípios Afetados</span>
              <div className="flex flex-wrap gap-1">
                {selectedPrediction.affectedMunicipalities.map((m) => (
                  <span
                    key={m}
                    className="px-2 py-0.5 bg-gray-700 text-gray-200 rounded text-xs"
                  >
                    {m}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Conclusion text */}
      {selectedPrediction && (
        <div className="mb-6 p-4 bg-[#16213e] rounded-lg border border-gray-700">
          <h3 className="text-sm font-semibold text-gray-300 mb-2 uppercase tracking-wide">
            Conclusão
          </h3>
          <p className="text-sm text-gray-300 leading-relaxed">
            {selectedPrediction.conclusion}
          </p>
        </div>
      )}

      {/* FWI Evolution Chart */}
      <div className="flex-1 p-4 bg-[#16213e] rounded-lg border border-gray-700">
        <h3 className="text-sm font-semibold text-gray-300 mb-3 uppercase tracking-wide">
          Evolução do FWI
        </h3>
        <ResponsiveContainer width="100%" height={150}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
            <XAxis dataKey="name" tick={{ fill: "#9ca3af", fontSize: 10 }} />
            <YAxis tick={{ fill: "#9ca3af", fontSize: 10 }} />
            <Tooltip
              contentStyle={{
                backgroundColor: "#1f2937",
                border: "1px solid #374151",
                borderRadius: "6px",
                color: "#e5e7eb",
              }}
            />
            <Line
              type="monotone"
              dataKey="fwi"
              stroke="#ef4444"
              strokeWidth={2}
              dot={{ fill: "#ef4444", r: 3 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
