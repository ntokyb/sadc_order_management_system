import type { Order, OrderStatus } from '@/types';

const ORDER_STATUSES: OrderStatus[] = ['Pending', 'Paid', 'Fulfilled', 'Cancelled'];

const STATUS_BY_NUMBER: Record<number, OrderStatus> = {
  0: 'Pending',
  1: 'Paid',
  2: 'Fulfilled',
  3: 'Cancelled',
};

/** API may return enum as number (0–3) or string — normalize to OrderStatus. */
export function normalizeOrderStatus(status: unknown): OrderStatus {
  if (typeof status === 'string' && ORDER_STATUSES.includes(status as OrderStatus)) {
    return status as OrderStatus;
  }
  if (typeof status === 'number' && status in STATUS_BY_NUMBER) {
    return STATUS_BY_NUMBER[status];
  }
  return 'Pending';
}

export function normalizeOrder(order: Order): Order {
  return {
    ...order,
    status: normalizeOrderStatus(order.status),
    lineItems: order.lineItems ?? [],
  };
}
