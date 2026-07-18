import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowRightLeft, ClipboardList, LogOut, Plus, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchAcademicYears } from '../../api/academicYears'
import { fetchClasses } from '../../api/classes'
import { fetchEnrollments, withdrawEnrollment } from '../../api/enrollments'
import { getErrorMessage } from '../../api/errors'
import type { EnrollmentDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { EnrollStudentDrawer } from './EnrollStudentDrawer'
import { PromoteDrawer } from './PromoteDrawer'
import { TransferDrawer } from './TransferDrawer'

const PAGE_SIZE = 10

export function EnrollmentsListPage() {
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('enrollments.manage'))
  const canPromote = useAuthStore((s) => s.hasPermission('enrollments.promote'))
  const [params, setParams] = useSearchParams()
  const [enrollOpen, setEnrollOpen] = useState(false)
  const [promoteOpen, setPromoteOpen] = useState(false)
  const [transferTarget, setTransferTarget] = useState<EnrollmentDto | null>(null)
  const [withdrawingId, setWithdrawingId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const page = Number(params.get('page') ?? '1')
  const search = params.get('q') ?? ''
  const classId = params.get('classId') ?? ''
  const academicYearId = params.get('yearId') ?? ''
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
  const { data: years } = useQuery({ queryKey: ['academic-years'], queryFn: fetchAcademicYears })

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['enrollments', { page, debouncedSearch, classId, academicYearId, status }],
    queryFn: () =>
      fetchEnrollments({
        pageNumber: page,
        pageSize: PAGE_SIZE,
        searchTerm: debouncedSearch || undefined,
        classId: classId || undefined,
        academicYearId: academicYearId || undefined,
        isActive: status ? status === 'active' : undefined,
      }),
    placeholderData: (prev) => prev,
  })

  async function handleWithdraw(enrollment: EnrollmentDto) {
    if (!window.confirm(`Withdraw ${enrollment.studentName} from ${enrollment.className}?`)) return
    setActionError(null)
    setWithdrawingId(enrollment.id)
    try {
      await withdrawEnrollment(enrollment.id)
      await queryClient.invalidateQueries({ queryKey: ['enrollments'] })
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not withdraw enrollment'))
    } finally {
      setWithdrawingId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Enrollments"
        description={data ? `${data.totalCount} enrollments` : undefined}
        actions={
          <>
            {canPromote && (
              <Button variant="secondary" onClick={() => setPromoteOpen(true)}>
                <TrendingUp className="h-4 w-4" strokeWidth={2} />
                Promote students
              </Button>
            )}
            {canManage && (
              <Button onClick={() => setEnrollOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Enroll student
              </Button>
            )}
          </>
        }
      />

      {actionError && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {actionError}
        </p>
      )}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <SearchInput
          placeholder="Search by student name or number…"
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
          value={academicYearId}
          onChange={(e) => updateParam('yearId', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All years</option>
          {years?.map((y) => (
            <option key={y.id} value={y.id}>
              {y.name}
            </option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => updateParam('status', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All statuses</option>
          <option value="active">Active</option>
          <option value="withdrawn">Withdrawn</option>
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
                  <th className="px-4 py-3 font-medium">Student</th>
                  <th className="px-4 py-3 font-medium">Class</th>
                  <th className="px-4 py-3 font-medium">Academic year</th>
                  <th className="px-4 py-3 font-medium">Enrolled</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  {canManage && <th className="px-4 py-3 font-medium">Actions</th>}
                </tr>
              </thead>
              <tbody>
                {data.items.map((e) => (
                  <tr
                    key={e.id}
                    className="border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Avatar name={e.studentName} size="sm" />
                        <div>
                          <p className="font-medium text-slate-900 dark:text-white">{e.studentName}</p>
                          <p className="text-xs text-slate-400">{e.studentNumber}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                      {e.className}
                      {e.streamName ? ` · ${e.streamName}` : ''}
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{e.academicYearName}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                      {new Date(e.enrollmentDate).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={e.isActive ? 'emerald' : 'slate'}>{e.isActive ? 'Active' : 'Withdrawn'}</Badge>
                    </td>
                    {canManage && (
                      <td className="px-4 py-3">
                        {e.isActive && (
                          <div className="flex items-center gap-3">
                            <button
                              onClick={() => setTransferTarget(e)}
                              className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                            >
                              <ArrowRightLeft className="h-3.5 w-3.5" strokeWidth={2} />
                              Transfer
                            </button>
                            <button
                              onClick={() => handleWithdraw(e)}
                              disabled={withdrawingId === e.id}
                              className="flex items-center gap-1 text-xs font-medium text-red-600 hover:underline disabled:opacity-50 dark:text-red-400"
                            >
                              <LogOut className="h-3.5 w-3.5" strokeWidth={2} />
                              Withdraw
                            </button>
                          </div>
                        )}
                      </td>
                    )}
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
            icon={ClipboardList}
            title="No enrollments found"
            description={
              search || classId || academicYearId || status
                ? 'Try adjusting your filters.'
                : 'Enroll your first student to get started.'
            }
            action={
              canManage &&
              !search &&
              !classId &&
              !academicYearId &&
              !status && (
                <Button onClick={() => setEnrollOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Enroll student
                </Button>
              )
            }
          />
        )}
      </div>

      <EnrollStudentDrawer open={enrollOpen} onClose={() => setEnrollOpen(false)} />
      <TransferDrawer enrollment={transferTarget} onClose={() => setTransferTarget(null)} />
      <PromoteDrawer open={promoteOpen} onClose={() => setPromoteOpen(false)} />
    </div>
  )
}
