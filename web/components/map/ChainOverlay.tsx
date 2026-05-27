"use client";

import { useEffect, useRef } from "react";
import { useMap } from "react-leaflet";
import L from "leaflet";
import type { EventChain } from "@/lib/api/insights";

interface ChainOverlayProps {
  chains: EventChain[];
  onChainClick?: (chain: EventChain) => void;
}

const CHAIN_STYLES: Record<string, { color: string; dashArray?: string; weight: number }> = {
  Simultaneous: { color: "#3b82f6", weight: 2 },
  Sequential: { color: "#f59e0b", weight: 2 },
  ResourceContention: { color: "#ef4444", weight: 3 },
  EmberCast: { color: "#10b981", dashArray: "5, 5", weight: 2 },
};

function getConfidenceColor(confidence: number): string {
  if (confidence >= 0.8) return "#22c55e";
  if (confidence >= 0.5) return "#eab308";
  return "#ef4444";
}

function ChainOverlayInner({ chains, onChainClick }: ChainOverlayProps) {
  const map = useMap();
  const layerGroupRef = useRef<L.LayerGroup | null>(null);
  const polylinesRef = useRef<L.Polyline[]>([]);

  useEffect(() => {
    if (!map) return;

    // Clean up previous layers
    if (layerGroupRef.current) {
      map.removeLayer(layerGroupRef.current);
      layerGroupRef.current = null;
    }
    polylinesRef.current.forEach((polyline) => map.removeLayer(polyline));
    polylinesRef.current = [];

    const group = L.layerGroup();
    layerGroupRef.current = group;
    map.addLayer(group);

    chains.forEach((chain) => {
      // Extract event coordinates from findings if available
      const eventCoords: [number, number][] = [];

      if (chain.findings && typeof chain.findings === "object") {
        const findings = chain.findings as Record<string, unknown>;
        if (findings.eventIds && Array.isArray(findings.eventIds)) {
          // We need coordinates - this would come from the events themselves
          // For now, we use the chain's contributing factors to build a description
        }
      }

      // Use contributing factors to extract location hints
      // In a real implementation, you'd fetch event coordinates by ID
      const style = CHAIN_STYLES[chain.analysisType] ?? CHAIN_STYLES["Sequential"];
      const lineColor = getConfidenceColor(chain.confidenceScore);

      // For demo/placeholder - in production, chain events would have actual coordinates
      // This creates a visual indicator that there's a chain relationship
      const tooltipText = `${chain.analysisType} (${Math.round(chain.confidenceScore * 100)}% confiança)`;

      // Note: In a full implementation, you would:
      // 1. Fetch event coordinates from the API using the event IDs
      // 2. Draw polylines connecting related events
      // 3. Add markers at chain focal points

      // Placeholder: log chain for debugging
      console.log(`Chain ${chain.id}: ${chain.description}`, chain);
    });

    return () => {
      if (layerGroupRef.current) {
        map.removeLayer(layerGroupRef.current);
        layerGroupRef.current = null;
      }
      polylinesRef.current.forEach((polyline) => map.removeLayer(polyline));
      polylinesRef.current = [];
    };
  }, [map, chains, onChainClick]);

  return null;
}

export function ChainOverlay(props: ChainOverlayProps) {
  return <ChainOverlayInner {...props} />;
}

// Helper to draw chain lines between events
export function drawChainLines(
  map: L.Map,
  chains: EventChain[],
  eventCoordinates: Map<string, [number, number]>
): L.Polyline[] {
  const polylines: L.Polyline[] = [];
  const layerGroup = L.layerGroup().addTo(map);

  chains.forEach((chain) => {
    const style = CHAIN_STYLES[chain.analysisType] ?? CHAIN_STYLES["Sequential"];
    const lineColor = getConfidenceColor(chain.confidenceScore);

    // Extract coordinates from findings if available
    if (chain.findings && typeof chain.findings === "object") {
      const findings = chain.findings as Record<string, unknown>;

      if (findings.eventIds && Array.isArray(findings.eventIds)) {
        const coords: [number, number][] = [];

        for (const eventId of findings.eventIds) {
          const coord = eventCoordinates.get(String(eventId));
          if (coord) {
            coords.push([coord[0], coord[1]]);
          }
        }

        if (coords.length >= 2) {
          const polyline = L.polyline(coords, {
            color: lineColor,
            weight: style.weight,
            dashArray: style.dashArray,
            opacity: 0.8,
          });

          polyline.bindTooltip(
            `${chain.analysisType}: ${Math.round(chain.confidenceScore * 100)}% confiança`,
            { permanent: false, direction: "top" }
          );

          layerGroup.addLayer(polyline);
          polylines.push(polyline);
        }
      }
    }
  });

  return polylines;
}
