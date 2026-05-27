export interface FireHotspot {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  gridCellId: string;
  fireCount: number;
  totalAreaBurned: number;
  averageSeverity: string;
  peakMonth: number;
  peakHour: number;
  commonWindDirection: string | null;
  averageFwi: number;
  riskLevel: string;
  lastUpdated: string;
}

export interface HotspotDetail extends FireHotspot {
  alerts: HotspotAlert[];
  seasonalPattern: FiresByMonth;
  recentEvents: HotspotEvent[];
}

export interface HotspotAlert {
  id: string;
  alertType: string;
  message: string;
  createdAt: string;
  isRead: boolean;
}

export interface FiresByMonth {
  [month: number]: number;
}

export interface HotspotEvent {
  id: string;
  title: string;
  severity: string;
  occurredAt: string;
  latitude: number;
  longitude: number;
}

export interface HotspotNear {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  gridCellId: string;
  fireCount: number;
  totalAreaBurned: number;
  riskLevel: string;
  distanceKm: number;
}