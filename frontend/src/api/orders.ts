import type { CreateOrderInput, Order, OrderFilters, OrderStatus, PagedResult } from '../types';
import { normalizeOrder } from '@/lib/orderStatus';
import { apiClient } from './client';

export async function createOrder(data: CreateOrderInput): Promise<Order> {
  const response = await apiClient.post<Order>('/api/orders', data);
  return normalizeOrder(response.data);
}

export async function getOrders(filters: OrderFilters = {}): Promise<PagedResult<Order>> {
  const response = await apiClient.get<PagedResult<Order>>('/api/orders', {
    params: {
      customerId: filters.customerId || undefined,
      status: filters.status || undefined,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
      sort: filters.sort ?? 'createdAt_desc',
    },
  });
  return {
    ...response.data,
    items: (response.data.items ?? []).map(normalizeOrder),
  };
}

export async function getOrder(id: string): Promise<Order> {
  const response = await apiClient.get<Order>(`/api/orders/${id}`);
  return normalizeOrder(response.data);
}

export async function updateOrderStatus(
  id: string,
  status: OrderStatus,
  idempotencyKey: string,
): Promise<Order> {
  const response = await apiClient.put<Order>(
    `/api/orders/${id}/status`,
    { status },
    { headers: { 'Idempotency-Key': idempotencyKey } },
  );
  return normalizeOrder(response.data);
}

export async function deleteOrder(id: string): Promise<void> {
  await apiClient.delete(`/api/orders/${id}`);
}
