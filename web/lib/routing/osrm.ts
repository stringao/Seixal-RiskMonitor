/**
 * OSRM Routing Service
 * Self-hosted Open Source Routing Machine using OpenStreetMap data
 */

const OSRM_URL = process.env.NEXT_PUBLIC_OSRM_URL ?? "http://localhost:5001";

export interface RouteResult {
  geometry: [number, number][]; // [lng, lat] coordinates
  distance: number; // meters
  duration: number; // seconds
}

export interface RouteError {
  error: string;
  message: string;
}

/**
 * Get route between two points using OSRM
 */
export async function getRoute(
  fromLng: number,
  fromLat: number,
  toLng: number,
  toLat: number
): Promise<RouteResult> {
  const url = `${OSRM_URL}/route/v1/driving/${fromLng},${fromLat};${toLng},${toLat}?overview=full&geometries=geojson`;

  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`OSRM request failed: ${response.statusText}`);
  }

  const data = await response.json();

  if (data.code !== "Ok" || !data.routes?.[0]) {
    const error = data.message ?? "Unknown OSRM error";
    throw new Error(error);
  }

  const route = data.routes[0];

  // GeoJSON coordinates are [lng, lat], convert to Leaflet [lat, lng]
  const geometry = route.geometry.coordinates.map(
    (coord: [number, number]) => [coord[1], coord[0]] as [number, number]
  );

  return {
    geometry,
    distance: route.distance, // meters
    duration: route.duration, // seconds
  };
}

/**
 * Get routes from an event to multiple fire stations
 */
export async function getRoutesToStations(
  eventLng: number,
  eventLat: number,
  stations: Array<{ id: string; longitude: number; latitude: number }>
): Promise<Map<string, RouteResult>> {
  const routes = new Map<string, RouteResult>();

  // OSRM Route API supports up to waypoints, but we fetch one by one for simplicity
  // Could optimize with batch requests if needed
  const promises = stations.map(async (station) => {
    try {
      const route = await getRoute(eventLng, eventLat, station.longitude, station.latitude);
      routes.set(station.id, route);
    } catch (error) {
      console.warn(`Failed to get route to station ${station.id}:`, error);
    }
  });

  await Promise.all(promises);

  return routes;
}