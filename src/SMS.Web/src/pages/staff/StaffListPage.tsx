import { useQuery } from '@tanstack/react-query'
import { Briefcase, Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchStaff } from '../../api/staff'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { StaffFormDrawer } from './StaffFormDrawer'

const PAGE_SIZE = 10

export function StaffListPage() {
  const navigate = useNavigate()
  const canCreate = useAuthStore((s) => s.hasPermission('staff.create'))
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const type = params.get('type') ?? ''
  const status = params.get('status') ?? ''
  const debouncedSearch = useDebouncedValue(search, 350)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['staff', { page, debouncedSearch, type, status }],
    queryFn: () =>
      fetchStaff({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        searchTerm: debouncedSearch || undefined,
        isTeacher: type ? type === 'teacher' : undefined,
        isActive: status ? status === 'active' : undefined,
      }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Staff"
        description={data ? `${data.totalCount} staff members` : undefined}
        actions={
          canCreate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add staff
            </Button>
          )
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <SearchInput
          placeholder="Search by name, staff number or email…"
          value={search}
          onChange={(e) => updateParam('q', e.target.value)}
          className="sm:max-w-xs"
        />
        <select
          value={type}
          onChange={(e) => updateParam('type', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All staff</option>
          <option value="teacher">Teaching</option>
          <option value="nonteaching">Non-teaching</option>
        </select>
        <select
          value={status}
          onChange={(e) => updateParam('status', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : data && data.items.length > 0 ? (
          <>
            <table className={`w-full text-left text-sm transition-opacity ${isFetching ? 'opacity-60' : ''}`}>
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Staff</th>
                  <th className="px-4 py-3 font-medium">Department</th>
                  <th className="px-4 py-3 font-medium">Job title</th>
                  <th className="px-4 py-3 font-medium">Type</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((s) => (
                  <tr
                    key={s.id}
                    onClick={() => navigate(`/staff/${s.id}`)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar name={s.fullName} size="sm" />
                        <div>
                          <p className="font-medium text-slate-900 dark:text-white">{s.fullName}</p>
                          <p className="text-xs text-slate-400">{s.staffNumber}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{s.department ?? '—'}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{s.jobTitle ?? '—'}</td>
                    <td className="px-4 py-3">
                      <Badge tone={s.isTeacher ? 'brand' : 'slate'}>{s.isTeacher ? 'Teaching' : 'Non-teaching'}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={s.isActive ? 'emerald' : 'red'}>{s.isActive ? 'Active' : 'Inactive'}</Badge>
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
            icon={Briefcase}
            title="No staff found"
            description={search || type || status ? 'Try adjusting your filters.' : 'Add your first staff member to get started.'}
            action={
              canCreate &&
              !search &&
              !type &&
              !status && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add staff
                </Button>
              )
            }
          />
        )}
      </div>

      <StaffFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
