"use client";

import { LoadingSkeleton, CardSkeleton, TableRowSkeleton } from "@/components/ui/LoadingSkeleton";

export default function DashboardLoading() {
  return (
    <div className="p-6 space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <CardSkeleton key={i} />
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-slate-800/80 rounded-xl p-6 border border-slate-700">
          <LoadingSkeleton className="h-6 w-1/3 mb-4" />
          <LoadingSkeleton className="h-48 w-full" />
        </div>
        <div className="bg-slate-800/80 rounded-xl p-6 border border-slate-700">
          <LoadingSkeleton className="h-6 w-1/3 mb-4" />
          <div className="space-y-3">
            {[1, 2, 3, 4, 5].map((i) => (
              <TableRowSkeleton key={i} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}