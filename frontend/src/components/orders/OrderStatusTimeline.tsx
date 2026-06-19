import { Check, Circle, X } from 'lucide-react';
import type { OrderStatus } from '@/types';
import { cn } from '@/lib/utils';

const STEPS: OrderStatus[] = ['Pending', 'Paid', 'Fulfilled'];

const STATUS_INDEX: Record<OrderStatus, number> = {
  Pending: 0,
  Paid: 1,
  Fulfilled: 2,
  Cancelled: -1,
};

interface OrderStatusTimelineProps {
  status: OrderStatus;
}

export function OrderStatusTimeline({ status }: OrderStatusTimelineProps) {
  const currentIndex = STATUS_INDEX[status];
  const isCancelled = status === 'Cancelled';

  return (
    <div className="rounded-xl border bg-white p-6 shadow-sm">
      <div className="mb-4 flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-slate-700">Order lifecycle</p>
        {isCancelled && (
          <span className="inline-flex items-center gap-1 rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-semibold text-red-700">
            <X className="h-3 w-3" />
            Cancelled
          </span>
        )}
      </div>

      <div className="relative flex items-start justify-between">
        <div
          className={cn(
            'absolute left-6 right-6 top-4 h-0.5 -translate-y-1/2',
            isCancelled ? 'bg-red-200' : 'bg-slate-200',
          )}
          aria-hidden
        />
        {STEPS.map((step, index) => {
          const isComplete = !isCancelled && currentIndex > index;
          const isCurrent = !isCancelled && currentIndex === index;
          const isUpcoming = !isCancelled && currentIndex < index;

          return (
            <div key={step} className="relative z-10 flex flex-1 flex-col items-center text-center">
              <div
                className={cn(
                  'flex h-8 w-8 items-center justify-center rounded-full border-2 bg-white transition-colors',
                  isComplete && 'border-emerald-500 bg-emerald-500 text-white',
                  isCurrent && 'border-blue-600 bg-blue-600 text-white ring-4 ring-blue-100',
                  isUpcoming && 'border-slate-300 text-slate-400',
                  isCancelled && 'border-slate-300 text-slate-400',
                )}
              >
                {isComplete ? (
                  <Check className="h-4 w-4" />
                ) : (
                  <Circle className={cn('h-3 w-3', isCurrent && 'fill-current')} />
                )}
              </div>
              <p
                className={cn(
                  'mt-2 text-xs font-semibold uppercase tracking-wide',
                  isCurrent && 'text-blue-700',
                  isComplete && 'text-emerald-700',
                  (isUpcoming || isCancelled) && 'text-slate-400',
                )}
              >
                {step}
              </p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
