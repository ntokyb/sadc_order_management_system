import type { OrderStatus } from '@/types';
import { Badge } from '@/components/ui/badge';
import { normalizeOrderStatus } from '@/lib/orderStatus';
import { cn } from '@/lib/utils';

const STATUS_STYLES: Record<OrderStatus, string> = {
  Pending: 'bg-yellow-100 text-yellow-800 hover:bg-yellow-100 border-yellow-200',
  Paid: 'bg-blue-100 text-blue-800 hover:bg-blue-100 border-blue-200',
  Fulfilled: 'bg-green-100 text-green-800 hover:bg-green-100 border-green-200',
  Cancelled: 'bg-red-100 text-red-800 hover:bg-red-100 border-red-200',
};

interface StatusBadgeProps {
  status: OrderStatus | number;
  className?: string;
  size?: 'default' | 'lg';
}

export function StatusBadge({ status, className, size = 'default' }: StatusBadgeProps) {
  const normalized = normalizeOrderStatus(status);

  return (
    <Badge
      variant="outline"
      className={cn(
        STATUS_STYLES[normalized],
        size === 'lg' && 'px-3 py-1 text-sm',
        className,
      )}
    >
      {normalized}
    </Badge>
  );
}
