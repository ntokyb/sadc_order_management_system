import type { CreateCustomerInput, Customer, PagedResult } from '../types';
import { apiClient } from './client';

export async function createCustomer(data: CreateCustomerInput): Promise<Customer> {
  const response = await apiClient.post<Customer>('/api/customers', data);
  return response.data;
}

export async function updateCustomer(id: string, data: CreateCustomerInput): Promise<Customer> {
  const response = await apiClient.put<Customer>(`/api/customers/${id}`, data);
  return response.data;
}

export async function deleteCustomer(id: string): Promise<void> {
  await apiClient.delete(`/api/customers/${id}`);
}

export async function getCustomers(
  search?: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResult<Customer>> {
  const response = await apiClient.get<PagedResult<Customer>>('/api/customers', {
    params: {
      search: search || undefined,
      page,
      pageSize,
    },
  });
  return response.data;
}

export async function getCustomer(id: string): Promise<Customer> {
  const response = await apiClient.get<Customer>(`/api/customers/${id}`);
  return response.data;
}
