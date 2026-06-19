import { LogOut, Package, Users } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/context/AuthContext';
import { cn } from '@/lib/utils';

const navItems = [
  { to: '/customers', label: 'Customers', icon: Users },
  { to: '/orders', label: 'Orders', icon: Package },
];

export function AppShell() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-slate-50">
      <aside className="fixed inset-y-0 left-0 z-30 flex w-60 flex-col bg-slate-900 text-white">
        <div className="border-b border-slate-800 px-6 py-5">
          <p className="font-display text-lg font-bold tracking-tight">SADC Orders</p>
          <p className="mt-1 text-xs text-slate-400">Order management</p>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-4">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/customers'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-slate-700 text-white'
                    : 'text-slate-400 hover:bg-slate-800 hover:text-white',
                )
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-slate-800 p-4">
          {user && (
            <div className="mb-3 space-y-2">
              <p className="truncate text-sm font-medium text-white">{user.username}</p>
              <div className="flex flex-wrap gap-1">
                {user.roles.map((role) => (
                  <Badge
                    key={role}
                    variant="secondary"
                    className="bg-slate-800 text-slate-200 hover:bg-slate-800"
                  >
                    {role}
                  </Badge>
                ))}
              </div>
            </div>
          )}
          <Button
            variant="ghost"
            className="w-full justify-start gap-2 text-slate-300 hover:bg-slate-800 hover:text-white"
            onClick={logout}
          >
            <LogOut className="h-4 w-4" />
            Sign out
          </Button>
        </div>
      </aside>

      <div className="ml-60 min-h-screen">
        <main className="p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
