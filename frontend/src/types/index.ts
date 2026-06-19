export interface Customer {
  id: string;
  name: string;
  email: string;
  countryCode: string;
  createdAt: string;
}

export interface OrderLineItem {
  id: string;
  productSku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  customerId: string;
  customerName: string;
  status: 'Pending' | 'Paid' | 'Fulfilled' | 'Cancelled';
  currencyCode: string;
  totalAmount: number;
  createdAt: string;
  lineItems: OrderLineItem[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateCustomerInput {
  name: string;
  email: string;
  countryCode: string;
}

export interface CreateOrderLineItemInput {
  productSku: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateOrderInput {
  customerId: string;
  currencyCode: string;
  lineItems: CreateOrderLineItemInput[];
}

export type OrderStatus = Order['status'];

export interface OrderFilters {
  customerId?: string;
  status?: OrderStatus | '';
  page?: number;
  pageSize?: number;
  sort?: string;
}
