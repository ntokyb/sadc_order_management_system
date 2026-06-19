import { Pencil, Plus, Search, Trash2, Users } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { createCustomer, deleteCustomer, getCustomers, updateCustomer } from '@/api/customers';
import { getErrorMessage } from '@/api/client';
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { SADC_COUNTRIES } from '@/constants/sadcCountries';
import { useAuth } from '@/context/AuthContext';
import { COUNTRY_FLAGS, formatDisplayDate } from '@/lib/formatters';
import type { Customer } from '@/types';

const emptyForm = { name: '', email: '', countryCode: 'ZA' };

interface CustomerFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  submitLabel: string;
  initial: typeof emptyForm;
  onSubmit: (values: typeof emptyForm) => Promise<void>;
  loading: boolean;
}

function CustomerFormDialog({
  open,
  onOpenChange,
  title,
  submitLabel,
  initial,
  onSubmit,
  loading,
}: CustomerFormDialogProps) {
  const [form, setForm] = useState(initial);

  useEffect(() => {
    if (open) setForm(initial);
  }, [open, initial]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    await onSubmit(form);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <form onSubmit={(event) => void handleSubmit(event)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="customer-name">Name</Label>
            <Input
              id="customer-name"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="customer-email">Email</Label>
            <Input
              id="customer-email"
              type="email"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              required
            />
          </div>
          <div className="space-y-2">
            <Label>Country</Label>
            <Select
              value={form.countryCode}
              onValueChange={(value) => setForm({ ...form, countryCode: value })}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SADC_COUNTRIES.map((country) => (
                  <SelectItem key={country.code} value={country.code}>
                    {COUNTRY_FLAGS[country.code] ?? ''} {country.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function CustomersPage() {
  const { user } = useAuth();
  const canWrite = user?.isAdmin ?? false;
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editCustomer, setEditCustomer] = useState<Customer | null>(null);
  const [deleteCustomerTarget, setDeleteCustomerTarget] = useState<Customer | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [search]);

  useEffect(() => {
    void loadCustomers();
  }, [search, page, pageSize]);

  async function loadCustomers() {
    setLoading(true);
    setError(null);
    try {
      const result = await getCustomers(search, page, pageSize);
      setCustomers(result.items);
      setTotalPages(result.totalPages);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleCreate(values: typeof emptyForm) {
    setSaving(true);
    try {
      await createCustomer(values);
      setCreateOpen(false);
      setPage(1);
      toast.success('Customer created');
      await loadCustomers();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleUpdate(values: typeof emptyForm) {
    if (!editCustomer) return;
    setSaving(true);
    try {
      await updateCustomer(editCustomer.id, values);
      setEditCustomer(null);
      toast.success('Customer updated');
      await loadCustomers();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!deleteCustomerTarget) return;
    setSaving(true);
    try {
      await deleteCustomer(deleteCustomerTarget.id);
      setDeleteCustomerTarget(null);
      toast.success('Customer deleted');
      await loadCustomers();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <PageHeader
        title="Customers"
        description={`${totalCount} customer${totalCount === 1 ? '' : 's'} total`}
        action={
          canWrite ? (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" />
              Create Customer
            </Button>
          ) : undefined
        }
      />

      {!canWrite && (
        <p className="mb-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          Read-only access — you can browse customers but cannot create, edit, or delete.
        </p>
      )}

      {error && (
        <p role="alert" className="mb-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </p>
      )}

      <div className="relative mb-6 max-w-md">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          placeholder="Search by name or email"
        />
      </div>

      <div className="rounded-xl border bg-white shadow-sm">
        {loading ? (
          <div className="p-4">
            <DataTableSkeleton columns={canWrite ? 5 : 4} />
          </div>
        ) : customers.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-muted-foreground">
            <Users className="h-10 w-10" />
            <p>No customers found</p>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Country</TableHead>
                <TableHead>Created</TableHead>
                {canWrite && <TableHead className="text-right">Actions</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {customers.map((customer) => (
                <TableRow key={customer.id}>
                  <TableCell className="font-medium">{customer.name}</TableCell>
                  <TableCell>{customer.email}</TableCell>
                  <TableCell>
                    {COUNTRY_FLAGS[customer.countryCode] ?? ''} {customer.countryCode}
                  </TableCell>
                  <TableCell>{formatDisplayDate(customer.createdAt)}</TableCell>
                  {canWrite && (
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setEditCustomer(customer)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                          Edit
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-red-600 hover:text-red-700"
                          onClick={() => setDeleteCustomerTarget(customer)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                          Delete
                        </Button>
                      </div>
                    </TableCell>
                  )}
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

      <CustomerFormDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Create customer"
        submitLabel="Create customer"
        initial={emptyForm}
        onSubmit={handleCreate}
        loading={saving}
      />

      <CustomerFormDialog
        open={!!editCustomer}
        onOpenChange={(open) => !open && setEditCustomer(null)}
        title="Edit customer"
        submitLabel="Save changes"
        initial={
          editCustomer
            ? {
                name: editCustomer.name,
                email: editCustomer.email,
                countryCode: editCustomer.countryCode,
              }
            : emptyForm
        }
        onSubmit={handleUpdate}
        loading={saving}
      />

      <Dialog open={!!deleteCustomerTarget} onOpenChange={(open) => !open && setDeleteCustomerTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete customer</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Delete <strong>{deleteCustomerTarget?.name}</strong>? This only works if the customer has no orders.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteCustomerTarget(null)} disabled={saving}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={() => void handleDelete()} disabled={saving}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
