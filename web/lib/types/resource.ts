export type ResourceType = "Tanker" | "Pump" | "Helicopter" | "Team" | "CommandUnit";
export type ResourceStatus = "Available" | "Dispatched" | "EnRoute" | "OnScene" | "Returning" | "Maintenance";

export interface FireResource {
  id: string;
  fireStationId: string;
  resourceType: ResourceType;
  name: string;
  personnelCount: number;
  waterCapacityLiters: number;
  isAvailable: boolean;
  deployedToEventId: string | null;
  deployedAt: string | null;
  expectedReturnAt: string | null;
  status: ResourceStatus;
}

export interface AvailableResource {
  resourceId: string;
  name: string;
  resourceType: ResourceType;
  stationId: string;
  stationName: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  travelTimeMinutes: number;
  personnelCount: number;
  waterCapacityLiters: number;
  status: ResourceStatus;
}

export interface DispatchOption {
  resourceId: string;
  name: string;
  resourceType: ResourceType;
  stationId: string;
  stationName: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  travelTimeMinutes: number;
  personnelCount: number;
  waterCapacityLiters: number;
  priorityScore: number;
}

export interface OptimalDispatch {
  eventId: string;
  eventTitle: string;
  eventLatitude: number;
  eventLongitude: number;
  requiredResourceType: string;
  options: DispatchOption[];
}

export interface ResourceAllocation {
  resourceId: string;
  name: string;
  resourceType: ResourceType;
  stationName: string;
  distanceKm: number;
  travelTimeMinutes: number;
  personnelCount: number;
}

export interface FireAllocation {
  eventId: string;
  eventTitle: string;
  latitude: number;
  longitude: number;
  severity: string;
  assignedResources: ResourceAllocation[];
}

export interface MultiFireAllocation {
  fireAllocations: FireAllocation[];
  totalResourcesAssigned: number;
}

export interface EtaResult {
  resourceId: string;
  resourceName: string;
  eventId: string;
  eventTitle: string;
  distanceKm: number;
  etaMinutes: number;
  estimatedArrival: string;
  routeGeometry: [number, number][] | null;
}

export interface EventDispatch {
  dispatchId: string;
  resourceId: string;
  resourceName: string;
  resourceType: ResourceType;
  stationName: string;
  dispatchedAt: string;
  arrivedAt: string | null;
  distanceKm: number;
  travelTimeMinutes: number;
  status: ResourceStatus;
  routePolyline: string;
}

export interface StationResourceStatus {
  stationId: string;
  stationName: string;
  latitude: number;
  longitude: number;
  availableCount: number;
  deployedCount: number;
  totalResources: number;
}

export interface ResourceTypeStatus {
  resourceType: ResourceType;
  available: number;
  enRoute: number;
  onScene: number;
  total: number;
}

export interface ResourceStatusSummary {
  totalResources: number;
  available: number;
  enRoute: number;
  onScene: number;
  byType: Record<string, ResourceTypeStatus>;
  byStation: StationResourceStatus[];
}

export interface StationWithResources {
  id: string;
  name: string;
  code: string;
  type: string;
  address?: string;
  city?: string;
  postalCode?: string;
  latitude: number;
  longitude: number;
  isActive?: boolean;
  personnelCount?: number;
  vehicleCount?: number;
  operationalZone?: string | null;
  availableCount: number;
  totalResources: number;
  resources: ResourceSummary[];
}

export interface ResourceSummary {
  id: string;
  name: string;
  resourceType: ResourceType;
  status: ResourceStatus;
  isAvailable: boolean;
}

export interface AvailableResourcesResponse {
  resources: AvailableResource[];
}

export interface EventDispatchesResponse {
  dispatches: EventDispatch[];
}