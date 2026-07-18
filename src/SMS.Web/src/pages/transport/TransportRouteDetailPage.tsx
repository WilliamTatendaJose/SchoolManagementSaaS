import { useQuery, useQueryClient } from '@tanstack/react-query'
import { MapPin, Plus, UserMinus, Users } from 'lucide-react'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { assignStudentToStop, fetchRouteStops, fetchStopOccupants, fetchTransportRoutes } from '../../api/transport'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { CreateStopDrawer } from './CreateStopDrawer'

export function TransportRouteDetailPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('transport.manage'))
  const [createStopOpen, setCreateStopOpen] = useState(false)
  const [selectedStopId, setSelectedStopId] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [assignError, setAssignError] = useState<string | null>(null)
  const [assigningId, setAssigningId] = useState<string | null>(null)
  const debouncedSearch = useDebouncedValue(search, 300)

  // No single-route-fetch endpoint exists — reuse the list and find by id.
  const { data: routes } = useQuery({ queryKey: ['transport-routes'], queryFn: fetchTransportRoutes })
  const route = routes?.find((r) => r.id === id)

  const { data: stops, isLoading: loadingStops } = useQuery({
    queryKey: ['route-stops', id],
    queryFn: () => fetchRouteStops(id!),
    enabled: !!id,
  })

  const { data: occupants, isLoading: loadingOccupants } = useQuery({
    queryKey: ['stop-occupants', selectedStopId],
    queryFn: () => fetchStopOccupants(selectedStopId!),
    enabled: !!selectedStopId,
  })

  const { data: searchResults, isFetching: searching } = useQuery({
    queryKey: ['students-search', debouncedSearch],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 6, searchTerm: debouncedSearch, status: 'Active' }),
    enabled: debouncedSearch.length >= 2,
  })

  async function handleAssign(studentId: string) {
    if (!selectedStopId) return
    setAssignError(null)
    setAssigningId(studentId)
    try {
      await assignStudentToStop({ studentId, routeStopId: selectedStopId })
      setSearch('')
      await queryClient.invalidateQueries({ queryKey: ['stop-occupants', selectedStopId] })
      await queryClient.invalidateQueries({ queryKey: ['route-stops', id] })
      await queryClient.invalidateQueries({ queryKey: ['transport-routes'] })
    } catch (err) {
      setAssignError(getErrorMessage(err, 'Could not assign student'))
    } finally {
      setAssigningId(null)
    }
  }

  async function handleUnassign(studentId: string) {
    if (!selectedStopId) return
    setAssignError(null)
    setAssigningId(studentId)
    try {
      await assignStudentToStop({ studentId, routeStopId: null })
      await queryClient.invalidateQueries({ queryKey: ['stop-occupants', selectedStopId] })
      await queryClient.invalidateQueries({ queryKey: ['route-stops', id] })
      await queryClient.invalidateQueries({ queryKey: ['transport-routes'] })
    } catch (err) {
      setAssignError(getErrorMessage(err, 'Could not unassign student'))
    } finally {
      setAssigningId(null)
    }
  }

  const selectedStop = stops?.find((s) => s.id === selectedStopId)
  const occupantIds = new Set((occupants ?? []).map((o) => o.studentId))

  return (
    <div>
      <PageHeader
        title={route?.name ?? 'Route'}
        backTo="/transport"
        description={route ? `${route.driverName ?? 'No driver'} · ${route.vehicleRegistration ?? 'No vehicle'}` : undefined}
        actions={
          canManage && (
            <Button onClick={() => setCreateStopOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add stop
            </Button>
          )
        }
      />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <h3 className="mb-2 px-1 text-sm font-semibold text-slate-900 dark:text-white">Stops</h3>
          {loadingStops ? (
            <div className="space-y-2 p-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : stops && stops.length > 0 ? (
            <ul className="space-y-1.5">
              {stops
                .slice()
                .sort((a, b) => a.sequenceNumber - b.sequenceNumber)
                .map((s) => (
                  <li key={s.id}>
                    <button
                      type="button"
                      onClick={() => setSelectedStopId(s.id)}
                      className={`flex w-full items-center justify-between rounded-xl border px-3 py-2.5 text-left transition-colors ${
                        selectedStopId === s.id
                          ? 'border-brand-300 bg-brand-50 dark:border-brand-800 dark:bg-brand-950/30'
                          : 'border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/40'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-slate-400" strokeWidth={2} />
                        <div>
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.name}</p>
                          <p className="text-xs text-slate-400">
                            {s.pickupTime ? `Pickup ${s.pickupTime}` : ''}
                            {s.pickupTime && s.dropoffTime ? ' · ' : ''}
                            {s.dropoffTime ? `Drop-off ${s.dropoffTime}` : ''}
                          </p>
                        </div>
                      </div>
                      <span className="flex items-center gap-1 text-xs font-medium text-slate-500 dark:text-slate-400">
                        <Users className="h-3.5 w-3.5" strokeWidth={2} />
                        {s.riderCount}
                      </span>
                    </button>
                  </li>
                ))}
            </ul>
          ) : (
            <EmptyState icon={MapPin} title="No stops yet" description="Add stops to build out this route." />
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          {!selectedStop ? (
            <EmptyState icon={Users} title="Select a stop" description="Choose a stop on the left to manage its riders." />
          ) : (
            <>
              <h3 className="mb-3 px-1 text-sm font-semibold text-slate-900 dark:text-white">
                Riders at {selectedStop.name}
              </h3>

              {assignError && (
                <p className="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
                  {assignError}
                </p>
              )}

              {loadingOccupants ? (
                <div className="space-y-2">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                  ))}
                </div>
              ) : occupants && occupants.length > 0 ? (
                <ul className="mb-4 space-y-1.5">
                  {occupants.map((o) => (
                    <li
                      key={o.studentId}
                      className="flex items-center justify-between rounded-xl border border-slate-100 px-3 py-2 dark:border-slate-800"
                    >
                      <div className="flex items-center gap-2.5">
                        <Avatar name={o.fullName} size="sm" />
                        <div>
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{o.fullName}</p>
                          <p className="text-xs text-slate-400">
                            {o.studentNumber}
                            {o.className ? ` · ${o.className}` : ''}
                          </p>
                        </div>
                      </div>
                      {canManage && (
                        <button
                          onClick={() => handleUnassign(o.studentId)}
                          disabled={assigningId === o.studentId}
                          className="flex items-center gap-1 text-xs font-medium text-red-600 hover:underline disabled:opacity-50 dark:text-red-400"
                        >
                          <UserMinus className="h-3.5 w-3.5" strokeWidth={2} />
                          Remove
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="mb-4 text-sm text-slate-400">No riders assigned to this stop yet.</p>
              )}

              {canManage && (
                <div>
                  <SearchInput
                    placeholder="Search students to assign…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                  {debouncedSearch.length >= 2 && (
                    <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                      {searching ? (
                        <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                      ) : searchResults && searchResults.items.length > 0 ? (
                        searchResults.items.map((s) => (
                          <button
                            type="button"
                            key={s.id}
                            disabled={occupantIds.has(s.id) || assigningId === s.id}
                            onClick={() => handleAssign(s.id)}
                            className="flex w-full items-center justify-between border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-800 dark:hover:bg-slate-800"
                          >
                            <div className="flex items-center gap-3">
                              <Avatar name={s.fullName} size="sm" />
                              <div>
                                <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.fullName}</p>
                                <p className="text-xs text-slate-400">{s.studentNumber}</p>
                              </div>
                            </div>
                            {occupantIds.has(s.id) && <span className="text-xs text-slate-400">Already here</span>}
                          </button>
                        ))
                      ) : (
                        <p className="px-3 py-3 text-sm text-slate-400">No students found.</p>
                      )}
                    </div>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {id && (
        <CreateStopDrawer
          open={createStopOpen}
          onClose={() => setCreateStopOpen(false)}
          routeId={id}
          nextSequence={(stops?.length ?? 0) + 1}
        />
      )}
    </div>
  )
}
