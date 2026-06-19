import { Plus, ShoppingCart, Trash2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { getErrorMessage } from '@/api/client';
import { getCustomers } from '@/api/customers';
import { createOrder, getOrders } from '@/api/orders';
import { Button } from '@/components/ui/button';
import { DataTableSkeleton } from '@/components/ui/DataTableSkeleton';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PageHeader } from '@/components/ui/PageHeader';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { StatusBadge } from '@/components/ui/StatusBadge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { COUNTRY_CURRENCIES } from '@/constants/sadcCountries';
import { useAuth } from '@/context/AuthContext';
import { formatDisplayDate, truncateId } from '@/lib/formatters';
import type { Customer, Order, OrderStatus } from '@/types';

const STATUS_OPTIONS: Array<OrderStatus | 'all'> = [
  'all',
  'Pending',
  'Paid',
  'Fulfilled',
  'Cancelled',
];

type LineItemForm = { productSku: string; quantity: number; unitPrice: number };

const defaultLineItem = (): LineItemForm => ({
  productSku: 'SKU-001',
  quantity: 1,
  unitPrice: 100,
});

export function OrdersPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const canWrite = user?.isAdmin ?? false;
  const [orders, setOrders] = useState<Order[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [customerId, setCustomerId] = useState('all');
  const [status, setStatus] = useState<OrderStatus | 'all'>('all');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [formCustomerId, setFormCustomerId] = useState('');
  const [currencyCode, setCurrencyCode] = useState('ZAR');
  const [lineItems, setLineItems] = useState<LineItemForm[]>([defaultLineItem()]);

  useEffect(() => {
    void loadCustomers();
  }, []);

  useEffect(() => {
    void loadOrders();
  }, [customerId, status, page, pageSize]);

  const runningTotal = useMemo(
    () => lineItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0),
    [lineItems],
  );

  async function loadCustomers() {
    try {
      const result = await getCustomers(undefined, 1, 100);
      setCustomers(result.items);
      if (result.items[0]) {
        setFormCustomerId(result.items[0].id);
        setCurrencyCode(COUNTRY_CURRENCIES[result.items[0].countryCode]?.[0] ?? 'ZAR');
      }
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function loadOrders() {
    setLoading(true);
    setError(null);
    try {
      const result = await getOrders({
        customerId: customerId === 'all' ? undefined : customerId,
        status: status === 'all' ? '' : status,
        page,
        pageSize,
      });
      setOrders(result.items);
      setTotalPages(result.totalPages);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  function onFormCustomerChange(nextCustomerId: string) {
    const customer = customers.find((item) => item.id === nextCustomerId);
    setFormCustomerId(nextCustomerId);
    if (customer) {
      setCurrencyCode(COUNTRY_CURRENCIES[customer.countryCode]?.[0] ?? 'ZAR');
    }
  }

  function resetCreateForm() {
    const first = customers[0];
    setFormCustomerId(first?.id ?? '');
    setCurrencyCode(first ? (COUNTRY_CURRENCIES[first.countryCode]?.[0] ?? 'ZAR') : 'ZAR');
    setLineItems([defaultLineItem()]);
  }

  async function handleCreateOrder(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);
    try {
      await createOrder({
        customerId: formCustomerId,
        currencyCode,
        lineItems,
      });
      setCreateOpen(false);
      resetCreateForm();
      setPage(1);
      toast.success('Order created');
      await loadOrders();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  function updateLineItem(index: number, patch: Partial<LineItemForm>) {
    setLineItems((items) =>
      items.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    );
  }

  return (
    <div>
      <PageHeader
        title="Orders"
        description={`${totalCount} order${totalCount === 1 ? '' : 's'} total`}
        action={
          canWrite ? (
            <Button
              onClick={() => {
                resetCreateForm();
                setCreateOpen(true);
              }}
            >
              <Plus className="h-4 w-4" />
              Create Order
            </Button>
          ) : undefined
        }
      />

      {!canWrite && (
        <p className="mb-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Read-only access — you can view orders but cannot create or change them.
        </p>
      )}

      {error && (
        <p role="alert" className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      <div className="mb-6 flex flex-wrap items-end gap-4">
        <div className="space-y-2">
          <Label>Customer</Label>
          <Select value={customerId} onValueChange={(value) => { setCustomerId(value); setPage(1); }}>
            <SelectTrigger className="w-[220px]">
              <SelectValue placeholder="All customers" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All customers</SelectItem>
              {customers.map((customer) => (
                <SelectItem key={customer.id} value={customer.id}>
                  {customer.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label>Status</Label>
          <Select
            value={status}
            onValueChange={(value) => {
              setStatus(value as OrderStatus | 'all');
              setPage(1);
            }}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {STATUS_OPTIONS.map((option) => (
                <SelectItem key={option} value={option}>
                  {option === 'all' ? 'All statuses' : option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {(customerId !== 'all' || status !== 'all') && (
          <Button
            variant="link"
            className="px-0"
            onClick={() => {
              setCustomerId('all');
              setStatus('all');
              setPage(1);
            }}
          >
            Clear filters
          </Button>
        )}
      </div>

      <div className="rounded-xl border bg-white shadow-sm">
        {loading ? (
          <div className="p-4">
            <DataTableSkeleton columns={6} />
          </div>
        ) : orders.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-muted-foreground">
            <ShoppingCart className="h-10 w-10" />
            <p>No orders found</p>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Order ID</TableHead>
                <TableHead>Customer</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Currency</TableHead>
                <TableHead>Total</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {orders.map((order) => (
                <TableRow
                  key={order.id}
                  className="cursor-pointer"
                  onClick={() => navigate(`/orders/${order.id}`)}
                >
                  <TableCell className="font-mono text-sm text-primary">
                    {truncateId(order.id)}
                  </TableCell>
                  <TableCell>{order.customerName}</TableCell>
                  <TableCell>
                    <StatusBadge status={order.status} />
                  </TableCell>
                  <TableCell>{order.currencyCode}</TableCell>
                  <TableCell>
                    {order.currencyCode} {order.totalAmount.toFixed(2)}
                  </TableCell>
                  <TableCell>{formatDisplayDate(order.createdAt)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>

      <div className="mt-6 flex items-center justify-between">
        <Button variant="outline" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>
          Previous
        </Button>
        <span className="text-sm text-muted-foreground">
          Page {page} of {totalPages || 1}
        </span>
        <Button
          variant="outline"
          disabled={page >= totalPages || loading}
          onClick={() => setPage(page + 1)}
        >
          Next
        </Button>
      </div>

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Create order</DialogTitle>
          </DialogHeader>
          <form onSubmit={(event) => void handleCreateOrder(event)} className="space-y-4">
            <div className="space-y-2">
              <Label>Customer</Label>
              <Select value={formCustomerId} onValueChange={onFormCustomerChange} required>
                <SelectTrigger>
                  <SelectValue placeholder="Select customer" />
                </SelectTrigger>
                <SelectContent>
                  {customers.map((customer) => (
                    <SelectItem key={customer.id} value={customer.id}>
                      {customer.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Currency</Label>
              <Select value={currencyCode} onValueChange={setCurrencyCode}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(COUNTRY_CURRENCIES[
                    customers.find((c) => c.id === formCustomerId)?.countryCode ?? 'ZA'
                  ] ?? ['ZAR']).map((code) => (
                    <SelectItem key={code} value={code}>
                      {code}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <Label>Line items</Label>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setLineItems((items) => [...items, defaultLineItem()])}
                >
                  <Plus className="h-3.5 w-3.5" />
                  Add row
                </Button>
              </div>

              {lineItems.map((item, index) => (
                <div key={index} className="grid grid-cols-12 gap-2 rounded-lg border p-3">
                  <div className="col-span-5 space-y-1">
                    <Label className="text-xs">SKU</Label>
                    <Input
                      value={item.productSku}
                      onChange={(e) => updateLineItem(index, { productSku: e.target.value })}
                      required
                    />
                  </div>
                  <div className="col-span-3 space-y-1">
                    <Label className="text-xs">Qty</Label>
                    <Input
                      type="number"
                      min={1}
                      value={item.quantity}
                      onChange={(e) =>
                        updateLineItem(index, { quantity: Number(e.target.value) })
                      }
                      required
                    />
                  </div>
                  <div className="col-span-3 space-y-1">
                    <Label className="text-xs">Price</Label>
                    <Input
                      type="number"
                      min={0}
                      step="0.01"
                      value={item.unitPrice}
                      onChange={(e) =>
                        updateLineItem(index, { unitPrice: Number(e.target.value) })
                      }
                      required
                    />
                  </div>
                  <div className="col-span-1 flex items-end">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      disabled={lineItems.length === 1}
                      onClick={() =>
                        setLineItems((items) => items.filter((_, i) => i !== index))
                      }
                    >
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                </div>
              ))}

              <p className="text-right text-sm font-semibold">
                Total: {currencyCode} {runningTotal.toFixed(2)}
              </p>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)} disabled={saving}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving || !formCustomerId}>
                Create order
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
