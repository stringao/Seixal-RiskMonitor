"use client";

interface StaticMapProps {
  latitude: number;
  longitude: number;
  zoom?: number;
  markerColor?: string;
  className?: string;
}

export function StaticMap({
  latitude,
  longitude,
  zoom = 15,
  markerColor = "#ef4444",
  className = "w-full h-48 rounded-lg overflow-hidden",
}: StaticMapProps) {
  // Calculate tile coordinates from lat/lng
  const n = Math.pow(2, zoom);
  const x = Math.floor(((longitude + 180) / 360) * n);
  const latRad = (latitude * Math.PI) / 180;
  const y = Math.floor(
    ((1 - Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) / Math.PI) / 2) * n
  );

  // Single tile URL
  const tileUrl = `https://tile.openstreetmap.org/${zoom}/${x}/${y}.png`;

  return (
    <div className={className}>
      <div
        className="relative w-full h-full overflow-hidden bg-slate-900"
        style={{ background: "#1a1a2e" }}
      >
        <img
          src={tileUrl}
          alt={`Mapa ${latitude.toFixed(4)}, ${longitude.toFixed(4)}`}
          className="w-full h-full object-contain"
        />

        {/* Center marker - positioned over the map center */}
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
          <svg
            width="32"
            height="48"
            viewBox="0 0 24 36"
            fill="none"
            className="transform -translate-y-1/2"
            style={{ filter: "drop-shadow(0 2px 4px rgba(0,0,0,0.5))" }}
          >
            <path
              d="M12 0C5.373 0 0 5.373 0 12c0 9 12 24 12 24s12-15 12-24c0-6.627-5.373-12-12-12z"
              fill={markerColor}
              stroke="white"
              strokeWidth="1.5"
            />
            <circle cx="12" cy="12" r="4" fill="white" />
          </svg>
        </div>

        {/* Coordinates label */}
        <div className="absolute bottom-2 left-2 bg-black/70 text-white text-xs px-2 py-1 rounded">
          {latitude.toFixed(4)}, {longitude.toFixed(4)}
        </div>

        {/* Attribution */}
        <div className="absolute bottom-2 right-2 bg-black/70 text-white text-[10px] px-2 py-1 rounded">
          © OpenStreetMap
        </div>
      </div>
    </div>
  );
}