import { useQuery } from '@tanstack/react-query'
import { Bus, Plus, Users } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchTransportRoutes } from '../../api/transport'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { CreateRouteDrawer } from './CreateRouteDrawer'

export function TransportPage() {
  const navigate = useNavigate()
  const canManage = useAuthStore((s) => s.hasPermission('transport.manage'))
  const [createOpen, setCreateOpen] = useState(false)

  const { data: routes, isLoading } = useQuery({ queryKey: ['transport-routes'], queryFn: fetchTransportRoutes })

  return (
    <div>
      <PageHeader
        title="Transport"
        description="Routes, stops and student assignments"
        actions={
          canManage && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add route
            </Button>
          )
        }
      />

      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-32 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
          ))}
        </div>
      ) : routes && routes.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {routes.map((r) => (
            <div
              key={r.id}
              onClick={() => navigate(`/transport/routes/${r.id}`)}
              className="cursor-pointer rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition-colors hover:border-brand-200 hover:bg-brand-50/40 dark:border-slate-800 dark:bg-slate-900 dark:hover:border-brand-900 dark:hover:bg-brand-950/20"
            >
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2">
                  <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-300">
                    <Bus className="h-4.5 w-4.5" strokeWidth={2} />
                  </div>
                  <div>
                    <p className="font-semibold text-slate-900 dark:text-white">{r.name}</p>
                    <p className="text-xs text-slate-400">{r.vehicleRegistration || 'No vehicle assigned'}</p>
                  </div>
                </div>
                <Badge tone={r.isActive ? 'emerald' : 'slate'}>{r.isActive ? 'Active' : 'Inactive'}</Badge>
              </div>
              <div className="mt-4 flex items-center justify-between text-sm">
                <span className="text-slate-500 dark:text-slate-400">{r.driverName ?? 'No driver'}</span>
                <span className="flex items-center gap-1 font-medium text-slate-700 dark:text-slate-200">
                  <Users className="h-3.5 w-3.5" strokeWidth={2} />
                  {r.riders}/{r.capacity}
                </span>
              </div>
              <p className="mt-1 text-xs text-slate-400">
                {r.stopCount} stop{r.stopCount === 1 ? '' : 's'} · {r.availableSeats} seats available
              </p>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={Bus}
          title="No transport routes yet"
          description="Add a route to start assigning student pickups and drop-offs."
          action={
            canManage && (
              <Button onClick={() => setCreateOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add route
              </Button>
            )
          }
        />
      )}

      <CreateRouteDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
