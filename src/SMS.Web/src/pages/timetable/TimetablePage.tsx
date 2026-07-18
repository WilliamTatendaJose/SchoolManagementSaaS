import { useQuery } from '@tanstack/react-query'
import { CalendarClock, DoorOpen, Pencil, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchClasses } from '../../api/classes'
import { fetchStaff } from '../../api/staff'
import { fetchClassrooms, fetchTimetable } from '../../api/timetable'
import { DAYS_OF_WEEK, type ClassroomDto, type TimetableSlotDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { ClassroomFormDrawer } from './ClassroomFormDrawer'
import { SlotFormDrawer } from './SlotFormDrawer'

function formatTime(t: string) {
  const [h, m] = t.split(':')
  const hour = Number(h)
  const period = hour >= 12 ? 'PM' : 'AM'
  const hour12 = hour % 12 === 0 ? 12 : hour % 12
  return `${hour12}:${m} ${period}`
}

export function TimetablePage() {
  const canManage = useAuthStore((s) => s.hasPermission('timetable.manage'))
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'classrooms' ? 'classrooms' : 'schedule'
  const [addSlotOpen, setAddSlotOpen] = useState(false)
  const [addSlotDay, setAddSlotDay] = useState<number | undefined>()
  const [editingSlot, setEditingSlot] = useState<TimetableSlotDto | null>(null)
  const [classroomOpen, setClassroomOpen] = useState(false)
  const [editingClassroom, setEditingClassroom] = useState<ClassroomDto | undefined>()

  const termId = params.get('termId') ?? ''
  const classId = params.get('classId') ?? ''
  const teacherId = params.get('teacherId') ?? ''

  function setTab(next: 'schedule' | 'classrooms') {
    const p = new URLSearchParams(params)
    if (next === 'schedule') p.delete('tab')
    else p.set('tab', next)
    setParams(p, { replace: true })
  }

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms() })
  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })
  const { data: teachers } = useQuery({
    queryKey: ['staff-teachers'],
    queryFn: () => fetchStaff({ isTeacher: true, isActive: true, pageSize: 100 }),
  })
  const { data: classrooms, isLoading: loadingClassrooms } = useQuery({
    queryKey: ['classrooms'],
    queryFn: () => fetchClassrooms(),
    enabled: tab === 'classrooms',
  })

  useEffect(() => {
    if (!termId && terms && terms.length > 0) {
      const current = terms.find((t) => t.isCurrent) ?? terms[0]
      updateParam('termId', current.id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [termId, terms])

  const { data: slots, isLoading: loadingSlots } = useQuery({
    queryKey: ['timetable', { termId, classId, teacherId }],
    queryFn: () => fetchTimetable({ academicTermId: termId, classId: classId || undefined, teacherId: teacherId || undefined }),
    enabled: tab === 'schedule' && !!termId,
  })

  const slotsByDay = new Map<number, TimetableSlotDto[]>()
  slots?.forEach((s) => {
    const dayValue = DAYS_OF_WEEK.find((d) => d.label === s.dayOfWeek)?.value ?? 1
    const list = slotsByDay.get(dayValue) ?? []
    list.push(s)
    slotsByDay.set(dayValue, list)
  })
  slotsByDay.forEach((list) => list.sort((a, b) => a.startTime.localeCompare(b.startTime)))

  function openAddSlot(day?: number) {
    setAddSlotDay(day)
    setAddSlotOpen(true)
  }

  return (
    <div>
      <PageHeader
        title="Timetable"
        description="Weekly lesson schedule and room bookings"
        actions={
          tab === 'schedule'
            ? canManage &&
              termId && (
                <Button onClick={() => openAddSlot()}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add lesson
                </Button>
              )
            : canManage && (
                <Button
                  onClick={() => {
                    setEditingClassroom(undefined)
                    setClassroomOpen(true)
                  }}
                >
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add classroom
                </Button>
              )
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('schedule')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'schedule'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <CalendarClock className="h-4 w-4" strokeWidth={2} />
          Schedule
        </button>
        <button
          onClick={() => setTab('classrooms')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'classrooms'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <DoorOpen className="h-4 w-4" strokeWidth={2} />
          Classrooms
        </button>
      </div>

      {tab === 'schedule' ? (
        <>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <select
              value={termId}
              onChange={(e) => updateParam('termId', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              <option value="">Select a term…</option>
              {terms?.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.academicYearName} · {t.name}
                  {t.isCurrent ? ' (current)' : ''}
                </option>
              ))}
            </select>
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
              value={teacherId}
              onChange={(e) => updateParam('teacherId', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              <option value="">All teachers</option>
              {teachers?.items.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName}
                </option>
              ))}
            </select>
          </div>

          {!termId ? (
            <div className="mt-4 rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
              <EmptyState
                icon={CalendarClock}
                title="No academic term available"
                description="Add an academic year with terms under Classes & subjects first."
              />
            </div>
          ) : loadingSlots ? (
            <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-7">
              {Array.from({ length: 7 }).map((_, i) => (
                <div key={i} className="h-32 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : (
            <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3 lg:grid-cols-7">
              {DAYS_OF_WEEK.map((day) => {
                const daySlots = slotsByDay.get(day.value) ?? []
                return (
                  <div
                    key={day.value}
                    className="rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900"
                  >
                    <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2.5 dark:border-slate-800">
                      <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                        {day.label}
                      </span>
                      {canManage && (
                        <button
                          onClick={() => openAddSlot(day.value)}
                          className="text-slate-400 hover:text-brand-600 dark:hover:text-brand-400"
                          aria-label={`Add lesson on ${day.label}`}
                        >
                          <Plus className="h-3.5 w-3.5" strokeWidth={2.5} />
                        </button>
                      )}
                    </div>
                    <div className="space-y-2 p-2.5">
                      {daySlots.length === 0 ? (
                        <p className="px-1 py-2 text-xs text-slate-400">No lessons</p>
                      ) : (
                        daySlots.map((s) => (
                          <button
                            key={s.id}
                            onClick={() => canManage && setEditingSlot(s)}
                            disabled={!canManage}
                            className="w-full rounded-lg border border-slate-100 p-2 text-left transition-colors hover:border-brand-200 hover:bg-brand-50/50 disabled:cursor-default disabled:hover:border-slate-100 disabled:hover:bg-transparent dark:border-slate-800 dark:hover:border-brand-900 dark:hover:bg-brand-950/30"
                          >
                            <p className="text-xs font-medium text-slate-400">
                              {formatTime(s.startTime)} – {formatTime(s.endTime)}
                            </p>
                            <p className="text-sm font-medium text-slate-900 dark:text-white">{s.subjectName}</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400">
                              {!classId ? `${s.className} · ` : ''}
                              {s.teacherName}
                            </p>
                            {s.classroomName && (
                              <p className="text-xs text-slate-400">{s.classroomName}</p>
                            )}
                          </button>
                        ))
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          {loadingClassrooms ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : classrooms && classrooms.length > 0 ? (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-3 font-medium">Room</th>
                  <th className="px-4 py-3 font-medium">Capacity</th>
                  <th className="px-4 py-3 font-medium">Features</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  {canManage && <th className="w-12 px-2 py-3" />}
                </tr>
              </thead>
              <tbody>
                {classrooms.map((c) => (
                  <tr key={c.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900 dark:text-white">{c.name}</p>
                      {c.building && <p className="text-xs text-slate-400">{c.building}</p>}
                    </td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{c.capacity}</td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1.5">
                        {c.hasProjector && <Badge tone="brand">Projector</Badge>}
                        {c.hasWhiteboard && <Badge tone="slate">Whiteboard</Badge>}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={c.isActive ? 'emerald' : 'red'}>{c.isActive ? 'Active' : 'Inactive'}</Badge>
                    </td>
                    {canManage && (
                      <td className="px-2 py-3">
                        <button
                          onClick={() => {
                            setEditingClassroom(c)
                            setClassroomOpen(true)
                          }}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                          aria-label="Edit"
                        >
                          <Pencil className="h-3.5 w-3.5" strokeWidth={2} />
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={DoorOpen}
              title="No classrooms yet"
              action={
                canManage && (
                  <Button
                    onClick={() => {
                      setEditingClassroom(undefined)
                      setClassroomOpen(true)
                    }}
                  >
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add classroom
                  </Button>
                )
              }
            />
          )}
        </div>
      )}

      {termId && (
        <SlotFormDrawer
          open={addSlotOpen}
          onClose={() => setAddSlotOpen(false)}
          academicTermId={termId}
          defaultClassId={classId || undefined}
          defaultDayOfWeek={addSlotDay}
        />
      )}
      {termId && editingSlot && (
        <SlotFormDrawer
          open={!!editingSlot}
          onClose={() => setEditingSlot(null)}
          academicTermId={termId}
          slot={editingSlot}
        />
      )}
      <ClassroomFormDrawer open={classroomOpen} onClose={() => setClassroomOpen(false)} classroom={editingClassroom} />
    </div>
  )
}
