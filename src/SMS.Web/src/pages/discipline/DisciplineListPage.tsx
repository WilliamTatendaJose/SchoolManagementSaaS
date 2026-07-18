import { useQuery } from '@tanstack/react-query'
import { Award, Plus, ShieldAlert, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchDisciplineRecords } from '../../api/discipline'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { CreateDisciplineDrawer } from './CreateDisciplineDrawer'

const PAGE_SIZE = 15

export function DisciplineListPage() {
  const canManage = useAuthStore((s) => s.hasPermission('discipline.manage'))
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)
  const page = Number(params.get('page') ?? '1')

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data, isLoading } = useQuery({
    queryKey: ['discipline', { page }],
    queryFn: () => fetchDisciplineRecords({ pageNumber: page, pageSize: PAGE_SIZE }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Discipline"
        description={data ? `${data.totalCount} records` : 'Incidents and merit awards'}
        actions={
          canManage && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Record incident
            </Button>
          )
        }
      />

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : data && data.items.length > 0 ? (
          <>
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Student</th>
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 font-medium">Merits/Demerits</th>
                  <th className="px-4 py-3 font-medium">Guardian</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((d) => (
                  <tr key={d.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{d.studentName}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                      {new Date(d.incidentDate).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone="slate">{d.incidentType}</Badge>
                    </td>
                    <td className="px-4 py-3 max-w-sm text-slate-600 dark:text-slate-300">
                      <p className="line-clamp-2">{d.description}</p>
                      {d.actionTaken && <p className="mt-0.5 text-xs text-slate-400">Action: {d.actionTaken}</p>}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {!!d.meritsAwarded && (
                          <span className="flex items-center gap-1 text-xs font-medium text-emerald-600 dark:text-emerald-400">
                            <Award className="h-3.5 w-3.5" strokeWidth={2} />+{d.meritsAwarded}
                          </span>
                        )}
                        {!!d.demeritsAwarded && (
                          <span className="flex items-center gap-1 text-xs font-medium text-red-600 dark:text-red-400">
                            <ShieldAlert className="h-3.5 w-3.5" strokeWidth={2} />-{d.demeritsAwarded}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      {d.guardianNotified ? (
                        <span className="flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400">
                          <ShieldCheck className="h-3.5 w-3.5" strokeWidth={2} />
                          Notified
                        </span>
                      ) : (
                        <span className="text-xs text-slate-400">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination
              pageNumber={data.pageNumber}
              totalPages={data.totalPages}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              hasPreviousPage={data.hasPreviousPage}
              hasNextPage={data.hasNextPage}
              onPageChange={(p) => updateParam('page', String(p))}
            />
          </>
        ) : (
          <EmptyState
            icon={ShieldAlert}
            title="No discipline records"
            description="Incidents and merit awards will appear here."
            action={
              canManage && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Record incident
                </Button>
              )
            }
          />
        )}
      </div>

      <CreateDisciplineDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
