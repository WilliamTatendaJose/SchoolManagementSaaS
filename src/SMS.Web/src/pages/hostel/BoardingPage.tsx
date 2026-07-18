import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BedDouble, CalendarClock, Plus, Shield } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import {
  fetchDormitories,
  fetchHouses,
  fetchWeekendLeaves,
  recordLeaveReturn,
} from '../../api/hostel'
import type { DormitoryDto, WeekendLeaveDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { AssignBoardingDrawer } from './AssignBoardingDrawer'
import { DormitoryFormDrawer } from './DormitoryFormDrawer'
import { DormitoryOccupantsDrawer } from './DormitoryOccupantsDrawer'
import { HouseFormDrawer } from './HouseFormDrawer'
import { RecordDepartureDrawer } from './RecordDepartureDrawer'
import { RequestLeaveDrawer } from './RequestLeaveDrawer'
import { ReviewLeaveDrawer } from './ReviewLeaveDrawer'

const leaveStatusTone: Record<string, 'emerald' | 'amber' | 'red' | 'slate' | 'brand'> = {
  Pending: 'amber',
  Approved: 'brand',
  Rejected: 'red',
  Departed: 'slate',
  Returned: 'emerald',
}

const PAGE_SIZE = 15

export function BoardingPage() {
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('hostel.manage'))
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'houses' ? 'houses' : params.get('tab') === 'leaves' ? 'leaves' : 'dormitories'

  const [dormFormOpen, setDormFormOpen] = useState(false)
  const [houseFormOpen, setHouseFormOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [viewingDorm, setViewingDorm] = useState<DormitoryDto | null>(null)
  const [requestLeaveOpen, setRequestLeaveOpen] = useState(false)
  const [reviewingLeave, setReviewingLeave] = useState<WeekendLeaveDto | null>(null)
  const [departingLeave, setDepartingLeave] = useState<WeekendLeaveDto | null>(null)
  const [returningId, setReturningId] = useState<string | null>(null)
  const [leaveError, setLeaveError] = useState<string | null>(null)

  const page = Number(params.get('page') ?? '1')
  const statusFilter = params.get('status') ?? ''

  function setTab(next: 'dormitories' | 'houses' | 'leaves') {
    const p = new URLSearchParams(params)
    if (next === 'dormitories') p.delete('tab')
    else p.set('tab', next)
    p.delete('page')
    setParams(p, { replace: true })
  }

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data: dormitories, isLoading: loadingDorms } = useQuery({
    queryKey: ['dormitories'],
    queryFn: fetchDormitories,
    enabled: tab === 'dormitories',
  })
  const { data: houses, isLoading: loadingHouses } = useQuery({
    queryKey: ['houses'],
    queryFn: fetchHouses,
    enabled: tab === 'houses',
  })
  const { data: leaves, isLoading: loadingLeaves } = useQuery({
    queryKey: ['weekend-leaves', { page, statusFilter }],
    queryFn: () => fetchWeekendLeaves({ pageNumber: page, pageSize: PAGE_SIZE, status: statusFilter || undefined }),
    enabled: tab === 'leaves',
    placeholderData: (prev) => prev,
  })

  async function handleReturn(leave: WeekendLeaveDto) {
    if (!window.confirm(`Mark ${leave.studentName} as returned?`)) return
    setLeaveError(null)
    setReturningId(leave.id)
    try {
      await recordLeaveReturn({ leaveId: leave.id })
      await queryClient.invalidateQueries({ queryKey: ['weekend-leaves'] })
    } catch (err) {
      setLeaveError(getErrorMessage(err, 'Could not record return'))
    } finally {
      setReturningId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Boarding"
        description="Dormitories, houses and weekend leave"
        actions={
          canManage &&
          (tab === 'dormitories' ? (
            <>
              <Button variant="secondary" onClick={() => setAssignOpen(true)}>
                Assign boarding
              </Button>
              <Button onClick={() => setDormFormOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add dormitory
              </Button>
            </>
          ) : tab === 'houses' ? (
            <>
              <Button variant="secondary" onClick={() => setAssignOpen(true)}>
                Assign boarding
              </Button>
              <Button onClick={() => setHouseFormOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add house
              </Button>
            </>
          ) : (
            <Button onClick={() => setRequestLeaveOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Request leave
            </Button>
          ))
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('dormitories')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'dormitories'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <BedDouble className="h-4 w-4" strokeWidth={2} />
          Dormitories
        </button>
        <button
          onClick={() => setTab('houses')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'houses'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <Shield className="h-4 w-4" strokeWidth={2} />
          Houses
        </button>
        <button
          onClick={() => setTab('leaves')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'leaves'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <CalendarClock className="h-4 w-4" strokeWidth={2} />
          Weekend leave
        </button>
      </div>

      {tab === 'dormitories' && (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          {loadingDorms ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : dormitories && dormitories.length > 0 ? (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Dormitory</th>
                  <th className="px-4 py-3 font-medium">Warden</th>
                  <th className="px-4 py-3 font-medium">Gender</th>
                  <th className="px-4 py-3 font-medium text-right">Occupancy</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {dormitories.map((d) => (
                  <tr
                    key={d.id}
                    onClick={() => setViewingDorm(d)}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                  >
                    <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{d.name}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{d.wardenName ?? '—'}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{d.gender ?? 'Mixed'}</td>
                    <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">
                      {d.occupants}/{d.capacity}
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={d.isActive ? 'emerald' : 'red'}>{d.isActive ? 'Active' : 'Inactive'}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={BedDouble}
              title="No dormitories yet"
              action={
                canManage && (
                  <Button onClick={() => setDormFormOpen(true)}>
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add dormitory
                  </Button>
                )
              }
            />
          )}
        </div>
      )}

      {tab === 'houses' && (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          {loadingHouses ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : houses && houses.length > 0 ? (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">House</th>
                  <th className="px-4 py-3 font-medium">House master</th>
                  <th className="px-4 py-3 font-medium text-right">Members</th>
                </tr>
              </thead>
              <tbody>
                {houses.map((h) => (
                  <tr key={h.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {h.color && (
                          <span
                            className="h-3 w-3 rounded-full border border-slate-200 dark:border-slate-700"
                            style={{ backgroundColor: h.color }}
                          />
                        )}
                        <span className="font-medium text-slate-900 dark:text-white">{h.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{h.houseMasterName ?? '—'}</td>
                    <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">{h.memberCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={Shield}
              title="No houses yet"
              action={
                canManage && (
                  <Button onClick={() => setHouseFormOpen(true)}>
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add house
                  </Button>
                )
              }
            />
          )}
        </div>
      )}

      {tab === 'leaves' && (
        <>
          <select
            value={statusFilter}
            onChange={(e) => updateParam('status', e.target.value)}
            className="mb-4 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
          >
            <option value="">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
            <option value="Departed">Departed</option>
            <option value="Returned">Returned</option>
          </select>

          {leaveError && (
            <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
              {leaveError}
            </p>
          )}

          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingLeaves ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : leaves && leaves.items.length > 0 ? (
              <>
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                      <th className="px-4 py-3 font-medium">Student</th>
                      <th className="px-4 py-3 font-medium">Destination</th>
                      <th className="px-4 py-3 font-medium">Departs</th>
                      <th className="px-4 py-3 font-medium">Returns</th>
                      <th className="px-4 py-3 font-medium">Status</th>
                      {canManage && <th className="px-4 py-3 font-medium">Actions</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {leaves.items.map((l) => (
                      <tr key={l.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                        <td className="px-4 py-3">
                          <p className="font-medium text-slate-900 dark:text-white">{l.studentName}</p>
                          {l.dormitoryName && <p className="text-xs text-slate-400">{l.dormitoryName}</p>}
                        </td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{l.destination}</td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                          {new Date(l.departureDate).toLocaleDateString()}
                        </td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                          {new Date(l.expectedReturnDate).toLocaleDateString()}
                        </td>
                        <td className="px-4 py-3">
                          <Badge tone={leaveStatusTone[l.status] ?? 'slate'}>{l.status}</Badge>
                        </td>
                        {canManage && (
                          <td className="px-4 py-3">
                            {l.status === 'Pending' && (
                              <button
                                onClick={() => setReviewingLeave(l)}
                                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                              >
                                Review
                              </button>
                            )}
                            {l.status === 'Approved' && (
                              <button
                                onClick={() => setDepartingLeave(l)}
                                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                              >
                                Record departure
                              </button>
                            )}
                            {l.status === 'Departed' && (
                              <button
                                onClick={() => handleReturn(l)}
                                disabled={returningId === l.id}
                                className="text-xs font-medium text-brand-600 hover:underline disabled:opacity-50 dark:text-brand-300"
                              >
                                Record return
                              </button>
                            )}
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination
                  pageNumber={leaves.pageNumber}
                  totalPages={leaves.totalPages}
                  totalCount={leaves.totalCount}
                  pageSize={leaves.pageSize}
                  hasPreviousPage={leaves.hasPreviousPage}
                  hasNextPage={leaves.hasNextPage}
                  onPageChange={(p) => updateParam('page', String(p))}
                />
              </>
            ) : (
              <EmptyState
                icon={CalendarClock}
                title="No weekend leave requests"
                action={
                  canManage && (
                    <Button onClick={() => setRequestLeaveOpen(true)}>
                      <Plus className="h-4 w-4" strokeWidth={2.5} />
                      Request leave
                    </Button>
                  )
                }
              />
            )}
          </div>
        </>
      )}

      <DormitoryFormDrawer open={dormFormOpen} onClose={() => setDormFormOpen(false)} />
      <HouseFormDrawer open={houseFormOpen} onClose={() => setHouseFormOpen(false)} />
      <AssignBoardingDrawer open={assignOpen} onClose={() => setAssignOpen(false)} />
      <DormitoryOccupantsDrawer dormitory={viewingDorm} onClose={() => setViewingDorm(null)} />
      <RequestLeaveDrawer open={requestLeaveOpen} onClose={() => setRequestLeaveOpen(false)} />
      <ReviewLeaveDrawer leave={reviewingLeave} onClose={() => setReviewingLeave(null)} />
      <RecordDepartureDrawer leave={departingLeave} onClose={() => setDepartingLeave(null)} />
    </div>
  )
}
