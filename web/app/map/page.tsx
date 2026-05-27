'use client';

import { AppLayout } from '@/components/layout/AppLayout';
import dynamic from 'next/dynamic';

const MapContainer = dynamic(() => import('@/components/map/MapContainer'), {
  ssr: false,
  loading: () => (
    <div className="flex items-center justify-center bg-[#050a0f]" style={{ height: '100%' }}>
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-emerald-500" />
    </div>
  ),
});

export default function MapPage() {
  return (
    <AppLayout>
      <div className="h-full overflow-hidden">
        <MapContainer />
      </div>
    </AppLayout>
  );
}
