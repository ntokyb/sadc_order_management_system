import {
  ArrowLeft,
  Calendar,
  Copy,
  LoaderCircle,
  Package,
  Receipt,
  User,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { getErrorMessage } from '@/api/client';
import { deleteOrder, getOrder, updateOrderStatus } from '@/api/orders';
import { OrderStatusTimeline } from '@/components/orders/OrderStatusTimeline';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Skeleton } from '@/components/ui/skeleton';
import { StatusBadge } from '@/components/ui/StatusBadge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { useAuth } from '@/context/AuthContext';
import { formatDisplayDate, formatMoney, truncateId } from '@/lib/formatters';
import { cn } from '@/lib/utils';
import type { Order, OrderStatus } from '@/types';
import { getAllowedTransitions } from '@/utils/orderTransitions';

const HERO_STYLES: Record<OrderStatus, string> = {
  Pending: 'from-amber-500 via-orange-500 to-yellow-600',
  Paid: 'from-blue-600 via-indigo-600 to-violet-600',
  Fulfilled: 'from-emerald-500 via-green-600 to-teal-600',
  Cancelled: 'from-slate-500 via-slate-600 to-slate-700',
};

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const canWrite = user?.isAdmin ?? false;
  const [order, setOrder] = useState<Order | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [updatingStatus, setUpdatingStatus] = useState<OrderStatus | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setIsLoading(true);
    setError(null);
    getOrder(id)
      .then(setOrder)
      .catch((err) => setError(getErrorMessage(err)))
      .finally(() => setIsLoading(false));
  }, [id]);

  async function onUpdateStatus(nextStatus: OrderStatus) {
    if (!order) return;
    const idempotencyKey = crypto.randomUUID();
    setUpdatingStatus(nextStatus);
    setError(null);
    try {
      const updated = await updateOrderStatus(order.id, nextStatus, idempotencyKey);
      setOrder(updated);
      toast.success(`Order marked as ${nextStatus}`);
    } catch (err) {
      setError(getErrorMessage(err));
      toast.error(getErrorMessage(err));
    } finally {
      setUpdatingStatus(null);
    }
  }

  async function onDelete() {
    if (!order) return;
    setDeleting(true);
    setError(null);
    try {
      await deleteOrder(order.id);
      toast.success('Order deleted');
      navigate('/orders', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
      toast.error(getErrorMessage(err));
      setDeleting(false);
    }
  }

  function copyOrderId() {
    if (!order) return;
    void navigator.clipboard.writeText(order.id);
    toast.success('Order ID copied');
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-40 w-full rounded-2xl" />
        <Skeleton className="h-24 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  if (!order) {
    return (
      <div className="space-y-4">
        <p role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error ?? 'Order not found.'}
        </p>
        <Button variant="outline" asChild>
          <Link to="/orders">
            <ArrowLeft className="h-4 w-4" />
            Back to orders
          </Link>
        </Button>
      </div>
    );
  }

  const lineItems = order.lineItems ?? [];
  const allowedTransitions = getAllowedTransitions(order.status);
  const itemCount = lineItems.reduce((sum, item) => sum + item.quantity, 0);

  return (
    <div className="space-y-6">
      <Button variant="ghost" className="-ml-2 w-fit text-slate-600" asChild>
        <Link to="/orders">
          <ArrowLeft className="h-4 w-4" />
          Back to orders
        </Link>
      </Button>

      <section
        className={cn(
          'relative overflow-hidden rounded-2xl bg-gradient-to-br p-8 text-white shadow-lg',
          HERO_STYLES[order.status],
        )}
      >
        <div className="absolute -right-8 -top-8 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -bottom-10 left-1/3 h-32 w-32 rounded-full bg-black/10 blur-2xl" />

        <div className="relative flex flex-wrap items-start justify-between gap-6">
          <div className="space-y-3">
            <p className="text-sm font-medium uppercase tracking-widest text-white/80">Order detail</p>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="font-display text-3xl font-bold tracking-tight">
                #{truncateId(order.id, 12)}
              </h1>
              <Button
                type="button"
                size="sm"
                variant="secondary"
                className="h-8 bg-white/15 text-white hover:bg-white/25"
                onClick={copyOrderId}
              >
                <Copy className="h-3.5 w-3.5" />
                Copy ID
              </Button>
            </div>
            <p className="max-w-xl text-sm text-white/85">
              Placed by <span className="font-semibold">{order.customerName}</span> on{' '}
              {formatDisplayDate(order.createdAt)}
            </p>
          </div>

          <div className="text-right">
            <StatusBadge
              status={order.status}
              size="lg"
              className="border-white/30 bg-white/15 text-white hover:bg-white/15"
            />
            <p className="mt-4 text-sm text-white/80">Order total</p>
            <p className="text-4xl font-bold tracking-tight">
              {formatMoney(order.totalAmount, order.currencyCode)}
            </p>
          </div>
        </div>
      </section>

      {!canWrite && (
        <p className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Read-only access — status changes and deletes are disabled.
        </p>
      )}

      {error && (
        <p role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      <OrderStatusTimeline status={order.status} />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { icon: User, label: 'Customer', value: order.customerName },
          { icon: Calendar, label: 'Created', value: formatDisplayDate(order.createdAt) },
          { icon: Package, label: 'Line items', value: `${itemCount} unit${itemCount === 1 ? '' : 's'}` },
          { icon: Receipt, label: 'Currency', value: order.currencyCode },
        ].map(({ icon: Icon, label, value }) => (
          <Card key={label} className="border-slate-200 shadow-sm">
            <CardContent className="flex items-center gap-4 p-5">
              <div className="rounded-lg bg-slate-100 p-2.5 text-slate-700">
                <Icon className="h-5 w-5" />
              </div>
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
                <p className="font-semibold text-slate-900">{value}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="overflow-hidden border-slate-200 shadow-sm">
        <CardHeader className="border-b bg-slate-50/80">
          <CardTitle>Line items</CardTitle>
          <CardDescription>{lineItems.length} product{lineItems.length === 1 ? '' : 's'} on this order</CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead>SKU</TableHead>
                <TableHead>Qty</TableHead>
                <TableHead>Unit price</TableHead>
                <TableHead className="text-right">Line total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {lineItems.map((lineItem) => (
                <TableRow key={lineItem.id}>
                  <TableCell className="font-medium">{lineItem.productSku}</TableCell>
                  <TableCell>{lineItem.quantity}</TableCell>
                  <TableCell>{formatMoney(lineItem.unitPrice, order.currencyCode)}</TableCell>
                  <TableCell className="text-right font-medium">
                    {formatMoney(lineItem.lineTotal, order.currencyCode)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <div className="flex items-center justify-between border-t bg-slate-50/50 px-6 py-4">
            <span className="text-sm text-muted-foreground">{lineItems.length} lines</span>
            <div>
              <span className="text-sm text-muted-foreground">Total </span>
              <span className="text-xl font-bold">{formatMoney(order.totalAmount, order.currencyCode)}</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {canWrite && allowedTransitions.length > 0 && (
        <Card className="border-blue-100 shadow-sm">
          <CardHeader>
            <CardTitle>Next actions</CardTitle>
            <CardDescription>Valid transitions from {order.status}</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-3">
            {allowedTransitions.map((nextStatus) => {
              const isPaid = nextStatus === 'Paid';
              const isFulfilled = nextStatus === 'Fulfilled';
              const isCancelled = nextStatus === 'Cancelled';

              return (
                <Button
                  key={nextStatus}
                  variant={isCancelled ? 'outline' : 'default'}
                  className={
                    isCancelled
                      ? 'border-red-300 text-red-700 hover:bg-red-50'
                      : isFulfilled
                        ? 'bg-green-600 hover:bg-green-700'
                        : isPaid
                          ? 'bg-blue-600 hover:bg-blue-700'
                          : undefined
                  }
                  disabled={updatingStatus !== null}
                  onClick={() => void onUpdateStatus(nextStatus)}
                >
                  {updatingStatus === nextStatus ? (
                    <>
                      <LoaderCircle className="animate-spin" />
                      Updating…
                    </>
                  ) : (
                    `Mark as ${nextStatus}`
                  )}
                </Button>
              );
            })}
          </CardContent>
        </Card>
      )}

      {order.status === 'Fulfilled' && (
        <Card className="border-emerald-200 bg-emerald-50/40">
          <CardContent className="py-6">
            <StatusBadge status="Fulfilled" size="lg" />
            <p className="mt-2 text-sm text-emerald-800">Order complete — no further actions.</p>
          </CardContent>
        </Card>
      )}

      {order.status === 'Cancelled' && (
        <Card className="border-red-200 bg-red-50/40">
          <CardContent className="py-6">
            <StatusBadge status="Cancelled" size="lg" />
            <p className="mt-2 text-sm text-red-800">Order cancelled — no further actions.</p>
          </CardContent>
        </Card>
      )}

      {canWrite && order.status === 'Pending' && (
        <Card className="border-red-200 bg-red-50/50">
          <CardHeader>
            <CardTitle className="text-red-800">Danger zone</CardTitle>
            <CardDescription className="text-red-700">
              Permanently delete this pending order. This cannot be undone.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" className="border-red-300 text-red-700" onClick={() => setDeleteOpen(true)}>
              Delete order
            </Button>
          </CardContent>
        </Card>
      )}

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete order?</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            This will permanently delete order <span className="font-mono">{order.id}</span>.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)} disabled={deleting}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={() => void onDelete()} disabled={deleting}>
              {deleting ? 'Deleting…' : 'Delete order'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
