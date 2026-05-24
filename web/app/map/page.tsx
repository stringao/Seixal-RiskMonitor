'use client';

import dynamic from 'next/dynamic';

const MapContainer = dynamic(() => import('@/components/map/MapContainer'), {
  ssr: false,
  loading: () => (
    <div className="flex items-center justify-center h-full">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[var(--color-accent)]" />
    </div>
  ),
});

export default function MapPage() {
  return <MapContainer />;
}