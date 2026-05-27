export type FireStationType = "Voluntarios" | "Sapadores" | "Municipais";

export interface FireStation {
  id: string;
  name: string;
  code: string;
  type: FireStationType;
  address: string;
  postalCode: string;
  city: string;
  district: string;
  county: string;
  parish: string | null;
  latitude: number;
  longitude: number;
  phone: string | null;
  email: string | null;
  website: string | null;
  operationalZone: string | null;
  cim: string | null;
  personnelCount: number;
  vehicleCount: number;
  isActive: boolean;
}

export interface FireStationListResponse {
  items: FireStation[];
  totalCount: number;
}