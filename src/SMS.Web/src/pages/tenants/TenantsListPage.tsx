import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Building2, Plus, Settings2 } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchTenants, updateTenantStatus } from '../../api/tenants'
import { getErrorMessage } from '../../api/errors'
import { TENANT_STATUSES, type TenantDto } from '../../api/types'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { CreateTenantDrawer } from './CreateTenantDrawer'
import { ManageTenantDrawer } from './ManageTenantDrawer'

const PAGE_SIZE = 15

const STATUS_TONE: Record<string, 'emerald' | 'amber' | 'slate'> = {
  Active: 'emerald',
  Pending: 'amber',
  Suspended: 'amber',
  Deactivated: 'slate',
}

export function TenantsListPage() {
  const queryClient = useQueryClient()
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)
  const [managingTenant, setManagingTenant] = useState<TenantDto | null>(null)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const page = Number(params.get('page') ?? '1')

  const { data, isLoading } = useQuery({
    queryKey: ['tenants', { page }],
    queryFn: () => fetchTenants(page, PAGE_SIZE),
    placeholderData: (prev) => prev,
  })

  async function handleStatusChange(id: string, status: string) {
    setActionError(null)
    setUpdatingId(id)
    try {
      await updateTenantStatus(id, { status })
      await queryClient.invalidateQueries({ queryKey: ['tenants'] })
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not update school status'))
    } finally {
      setUpdatingId(null)
    }
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1

  return (
    <div>
      <PageHeader
        title="Schools"
        description={data ? `${data.total} schools on the platform` : 'Manage tenant schools'}
        actions={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4" strokeWidth={2.5} />
            Add school
          </Button>
        }
      />

      {actionError && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {actionError}
        </p>
      )}

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : data && data.data.length > 0 ? (
          <>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">School</th>
                  <th className="px-4 py-3 font-medium">Contact</th>
                  <th className="px-4 py-3 font-medium">Plan</th>
                  <th className="px-4 py-3 font-medium">Seats</th>
                  <th className="px-4 py-3 font-medium">Renews</th>
                  <th className="px-4 py-3 font-medium">Modules</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="w-12 px-2 py-3" />
                </tr>
              </thead>
              <tbody>
                {data.data.map((t) => (
                  <tr key={t.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900 dark:text-white">{t.name}</p>
                      <p className="text-xs text-slate-400">{t.code}</p>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                      <p>{t.email || '—'}</p>
                      {t.phone && <p className="text-xs text-slate-400">{t.phone}</p>}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone="brand">{t.subscriptionPlan}</Badge>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{t.maxStudents}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                      {t.subscriptionEndDate ? new Date(t.subscriptionEndDate).toLocaleDateString() : '—'}
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-400">
                      {[
                        t.hasLmsModule && 'LMS',
                        t.hasTransportModule && 'Transport',
                        t.hasHostelModule && 'Boarding',
                        t.hasLibraryModule && 'Library',
                      ]
                        .filter(Boolean)
                        .join(', ') || 'None'}
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={t.status}
                        disabled={updatingId === t.id}
                        onChange={(e) => handleStatusChange(t.id, e.target.value)}
                        className={`rounded-full border-0 px-2.5 py-1 text-xs font-medium focus:outline-none focus:ring-2 focus:ring-brand-500/30 disabled:opacity-50 ${
                          STATUS_TONE[t.status] === 'emerald'
                            ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300'
                            : STATUS_TONE[t.status] === 'amber'
                              ? 'bg-amber-50 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'
                              : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
                        }`}
                      >
                        {TENANT_STATUSES.map((s) => (
                          <option key={s} value={s}>
                            {s}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-2 py-3">
                      <button
                        onClick={() => setManagingTenant(t)}
                        className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                        aria-label="Manage subscription"
                        title="Manage subscription"
                      >
                        <Settings2 className="h-3.5 w-3.5" strokeWidth={2} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination
              pageNumber={data.page}
              totalPages={totalPages}
              totalCount={data.total}
              pageSize={data.pageSize}
              hasPreviousPage={data.page > 1}
              hasNextPage={data.page * data.pageSize < data.total}
              onPageChange={(p) => setParams({ page: String(p) }, { replace: true })}
            />
          </>
        ) : (
          <EmptyState
            icon={Building2}
            title="No schools yet"
            description="Onboard the first school to get started."
            action={
              <Button onClick={() => setCreateOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add school
              </Button>
            }
          />
        )}
      </div>

      <CreateTenantDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
      <ManageTenantDrawer tenant={managingTenant} onClose={() => setManagingTenant(null)} />
    </div>
  )
}
