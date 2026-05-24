"use client";

interface LoadingSkeletonProps {
  className?: string;
}

export function LoadingSkeleton({ className = "" }: LoadingSkeletonProps) {
  return (
    <div
      className={`animate-pulse bg-slate-700/50 rounded ${className}`}
      aria-hidden="true"
    />
  );
}

export function CardSkeleton() {
  return (
    <div className="bg-slate-800/80 rounded-xl p-6 border border-slate-700">
      <LoadingSkeleton className="h-6 w-1/3 mb-4" />
      <LoadingSkeleton className="h-10 w-full mb-3" />
      <LoadingSkeleton className="h-4 w-2/3" />
    </div>
  );
}

export function TableRowSkeleton() {
  return (
    <tr className="border-b border-slate-700">
      <td className="py-3 px-4"><LoadingSkeleton className="h-4 w-20" /></td>
      <td className="py-3 px-4"><LoadingSkeleton className="h-4 w-32" /></td>
      <td className="py-3 px-4"><LoadingSkeleton className="h-4 w-24" /></td>
      <td className="py-3 px-4"><LoadingSkeleton className="h-4 w-16" /></td>
    </tr>
  );
}

export function MapSkeleton() {
  return (
    <div className="h-full w-full bg-slate-800/50 flex items-center justify-center">
      <LoadingSkeleton className="h-8 w-32" />
    </div>
  );
}