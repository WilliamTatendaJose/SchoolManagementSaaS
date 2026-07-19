import { useQuery } from '@tanstack/react-query'
import { Plus, Upload, Users } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import { STUDENT_STATUSES } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { StatusBadge } from '../../components/ui/Badge'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { StudentFormDrawer } from './StudentFormDrawer'

const PAGE_SIZE = 10

export function StudentsListPage() {
  const navigate = useNavigate()
  const canCreate = useAuthStore((s) => s.hasPermission('students.create'))
  const canImport = useAuthStore((s) => s.hasPermission('students.import'))
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const classId = params.get('classId') ?? ''
  const status = params.get('status') ?? ''
  const debouncedSearch = useDebouncedValue(search, 350)

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })

  const {
    data,
    isLoading,
    isFetching,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['students', { page, debouncedSearch, classId, status }],
    queryFn: () =>
      fetchStudents({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        searchTerm: debouncedSearch || undefined,
        classId: classId || undefined,
        status: status || undefined,
      }),
    placeholderData: (prev) => prev,
  })

  return (
    <div>
      <PageHeader
        title="Students"
        description={data ? `${data.totalCount} students` : undefined}
        actions={
          <div className="flex items-center gap-2">
            {canImport && (
              <Button variant="secondary" onClick={() => navigate('/students/import')}>
                <Upload className="h-4 w-4" strokeWidth={2.5} />
                Import
              </Button>
            )}
            {canCreate && (
              <Button onClick={() => setCreateOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add student
              </Button>
            )}
          </div>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <SearchInput
          placeholder="Search by name or student number…"
          value={search}
          onChange={(e) => updateParam('q', e.target.value)}
          className="sm:max-w-xs"
        />
        <select
          value={classId}
          onChange={(e) => updateParam('classId', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All classes</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => updateParam('status', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All statuses</option>
          {STUDENT_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : isError ? (
          <ErrorState description={getErrorMessage(error, 'Could not load students.')} onRetry={() => refetch()} />
        ) : data && data.items.length > 0 ? (
          <>
            <table className={`w-full text-left text-sm transition-opacity ${isFetching ? 'opacity-60' : ''}`}>
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Student</th>
                  <th className="px-4 py-3 font-medium">Class</th>
                  <th className="px-4 py-3 font-medium">Guardian</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((s) => (
                  <tr
                    key={s.id}
                    onClick={() => navigate(`/students/${s.id}`)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar name={s.fullName} size="sm" />
                        <div>
                          <p className="font-medium text-slate-900 dark:text-white">{s.fullName}</p>
                          <p className="text-xs text-slate-400">{s.studentNumber}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{s.className ?? '—'}</td>
                    <td className="px-4 py-3">
                      {s.primaryGuardianName ? (
                        <div>
                          <p className="text-slate-700 dark:text-slate-200">{s.primaryGuardianName}</p>
                          <p className="text-xs text-slate-400">{s.primaryGuardianPhone}</p>
                        </div>
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={s.status} />
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
            icon={Users}
            title="No students found"
            description={search || classId || status ? 'Try adjusting your filters.' : 'Add your first student to get started.'}
            action={
              canCreate &&
              !search &&
              !classId &&
              !status && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add student
                </Button>
              )
            }
          />
        )}
      </div>

      <StudentFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
