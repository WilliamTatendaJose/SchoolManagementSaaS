import { useQuery } from '@tanstack/react-query'
import { Plus, UserRound } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { fetchGuardians } from '../../api/guardians'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { GuardianFormDrawer } from './GuardianFormDrawer'

const PAGE_SIZE = 10

export function GuardiansListPage() {
  const navigate = useNavigate()
  const canCreate = useAuthStore((s) => s.hasPermission('guardians.create'))
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const debouncedSearch = useDebouncedValue(search, 350)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const {
    data,
    isLoading,
    isFetching,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['guardians', { page, debouncedSearch }],
    queryFn: () =>
      fetchGuardians({ pageNumber: page, pageSize: PAGE_SIZE, searchTerm: debouncedSearch || undefined }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Guardians"
        description={data ? `${data.totalCount} guardians` : undefined}
        actions={
          canCreate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add guardian
            </Button>
          )
        }
      />

      <SearchInput
        placeholder="Search by name, phone or email…"
        value={search}
        onChange={(e) => updateParam('q', e.target.value)}
        className="max-w-xs"
      />

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : isError ? (
          <ErrorState description={getErrorMessage(error, 'Could not load guardians.')} onRetry={() => refetch()} />
        ) : data && data.items.length > 0 ? (
          <>
            <table className={`w-full text-left text-sm transition-opacity ${isFetching ? 'opacity-60' : ''}`}>
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Guardian</th>
                  <th className="px-4 py-3 font-medium">Contact</th>
                  <th className="px-4 py-3 font-medium">Occupation</th>
                  <th className="px-4 py-3 font-medium">Students</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((g) => (
                  <tr
                    key={g.id}
                    onClick={() => navigate(`/guardians/${g.id}`)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar name={g.fullName} size="sm" />
                        <p className="font-medium text-slate-900 dark:text-white">{g.fullName}</p>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <p className="text-slate-700 dark:text-slate-200">{g.phone ?? '—'}</p>
                      <p className="text-xs text-slate-400">{g.email ?? ''}</p>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{g.occupation ?? '—'}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{g.studentCount}</td>
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
            icon={UserRound}
            title="No guardians found"
            description={search ? 'Try a different search.' : 'Add your first guardian to get started.'}
            action={
              canCreate &&
              !search && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add guardian
                </Button>
              )
            }
          />
        )}
      </div>

      <GuardianFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
