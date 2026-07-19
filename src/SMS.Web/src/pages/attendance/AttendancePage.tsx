import { useQuery, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { CalendarCheck, CheckCircle2, Clock, CloudOff, Send, ShieldQuestion, XCircle } from 'lucide-react'
import { useEffect, useState } from 'react'
import { fetchClassAttendance, markAttendance } from '../../api/attendance'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import type { AttendanceStatus } from '../../api/types'
import { ATTENDANCE_STATUSES } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatCard } from '../../components/ui/StatCard'
import { queueAttendance } from '../../offline/attendanceQueue'
import { usePendingAttendance } from '../../offline/usePendingAttendanceCount'

/** True for a genuine connectivity failure - as opposed to a real HTTP error response,
 * which means we reached the server and it rejected the request for some other reason
 * (validation, permissions, ...) that queuing and silently retrying would never fix. */
function isNetworkFailure(err: unknown) {
  return !navigator.onLine || (isAxiosError(err) && !err.response)
}

interface RowState {
  studentId: string
  studentName: string
  studentNumber: string
  status: AttendanceStatus
  timeIn: string
  reason: string
}

const STATUS_STYLES: Record<AttendanceStatus, { icon: typeof CheckCircle2; active: string }> = {
  Present: { icon: CheckCircle2, active: 'border-emerald-500 bg-emerald-50 text-emerald-700 dark:border-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300' },
  Absent: { icon: XCircle, active: 'border-red-500 bg-red-50 text-red-700 dark:border-red-600 dark:bg-red-950/40 dark:text-red-300' },
  Late: { icon: Clock, active: 'border-amber-500 bg-amber-50 text-amber-700 dark:border-amber-600 dark:bg-amber-950/40 dark:text-amber-300' },
  Excused: { icon: ShieldQuestion, active: 'border-slate-500 bg-slate-100 text-slate-700 dark:border-slate-500 dark:bg-slate-800 dark:text-slate-200' },
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

export function AttendancePage() {
  const queryClient = useQueryClient()
  const canMark = useAuthStore((s) => s.hasPermission('attendance.mark'))
  const [classId, setClassId] = useState('')
  const [date, setDate] = useState(todayIso())
  const [sendNotifications, setSendNotifications] = useState(true)
  const [rows, setRows] = useState<RowState[]>([])
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })
  const pending = usePendingAttendance()
  const pendingForThisView = pending.find((p) => p.classId === classId && p.date === date)

  useEffect(() => {
    if (!classId && classes && classes.length > 0) {
      setClassId(classes[0].id)
    }
  }, [classId, classes])

  const { data: roster, isLoading: loadingRoster } = useQuery({
    queryKey: ['class-roster', classId],
    queryFn: () => fetchStudents({ classId, status: 'Active', pageSize: 200 }),
    enabled: !!classId,
  })

  const { data: existing, isLoading: loadingAttendance } = useQuery({
    queryKey: ['class-attendance', classId, date],
    queryFn: () => fetchClassAttendance(classId, date),
    enabled: !!classId && !!date,
  })

  useEffect(() => {
    if (!roster) return
    // A reload while still offline can't re-fetch `existing` from the server, so prefer
    // whatever's still sitting in the offline queue for this exact class/date - otherwise
    // marks entered before the reload would appear to have vanished (they haven't; they're
    // just not visible until the queue syncs).
    const queuedByStudent = new Map(
      (pendingForThisView?.payload.records ?? []).map((r) => [r.studentId, r]),
    )
    const existingByStudent = new Map((existing ?? []).map((a) => [a.studentId, a]))
    setRows(
      roster.items.map((s) => {
        const queued = queuedByStudent.get(s.id)
        const a = existingByStudent.get(s.id)
        return {
          studentId: s.id,
          studentName: s.fullName,
          studentNumber: s.studentNumber,
          status: queued?.status ?? (a?.status as AttendanceStatus) ?? 'Present',
          timeIn: queued?.timeIn ?? a?.timeIn ?? '',
          reason: queued?.reason ?? a?.reason ?? '',
        }
      }),
    )
    // Intentionally does not touch savedMessage here - a successful save invalidates
    // `existing`, which re-runs this effect to reflect the persisted rows. Clearing the
    // banner on every re-run of this effect would wipe it the instant it appears.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roster, existing])

  // Switching class or date starts a fresh marking session, so any leftover
  // success/error message from the previous one is no longer relevant.
  useEffect(() => {
    setSavedMessage(null)
    setError(null)
  }, [classId, date])

  function updateRow(studentId: string, patch: Partial<RowState>) {
    setSavedMessage(null)
    setRows((prev) => prev.map((r) => (r.studentId === studentId ? { ...r, ...patch } : r)))
  }

  function markAll(status: AttendanceStatus) {
    setSavedMessage(null)
    setRows((prev) => prev.map((r) => ({ ...r, status })))
  }

  async function handleSave() {
    if (!classId || rows.length === 0) return
    setError(null)
    setSavedMessage(null)
    setSaving(true)

    const payload = {
      classId,
      date,
      sendNotifications,
      records: rows.map((r) => ({
        studentId: r.studentId,
        status: r.status,
        timeIn: r.timeIn || undefined,
        reason: r.reason || undefined,
      })),
    }

    try {
      const result = await markAttendance(payload)
      setSavedMessage(result.message)
      await queryClient.invalidateQueries({ queryKey: ['class-attendance', classId, date] })
    } catch (err) {
      if (isNetworkFailure(err)) {
        // No connectivity - queue it. The same idempotent payload replays automatically
        // once the connection returns (AppShell runs the sync loop app-wide), so the
        // teacher doesn't need to remember to retry or stay on this page.
        const className = classes?.find((c) => c.id === classId)?.name ?? 'this class'
        await queueAttendance(classId, date, className, payload)
        setSavedMessage(`Saved offline - will sync automatically once you're back online.`)
      } else {
        setError(getErrorMessage(err, 'Could not save attendance'))
      }
    } finally {
      setSaving(false)
    }
  }

  const counts = ATTENDANCE_STATUSES.reduce(
    (acc, s) => {
      acc[s] = rows.filter((r) => r.status === s).length
      return acc
    },
    {} as Record<AttendanceStatus, number>,
  )

  const loading = loadingRoster || loadingAttendance

  return (
    <div>
      <PageHeader
        title="Attendance"
        description="Mark daily attendance per class"
        actions={
          canMark && (
            <Button onClick={handleSave} loading={saving} disabled={!classId || rows.length === 0}>
              <CalendarCheck className="h-4 w-4" strokeWidth={2} />
              Save attendance
            </Button>
          )
        }
      />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <select
          value={classId}
          onChange={(e) => setClassId(e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          {!classes?.length && <option value="">No classes</option>}
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <input
          type="date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        />
        {canMark && (
          <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
            <input
              type="checkbox"
              checked={sendNotifications}
              onChange={(e) => setSendNotifications(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            <Send className="h-3.5 w-3.5" strokeWidth={2} />
            SMS guardians on absence
          </label>
        )}
      </div>

      {error && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {error}
        </p>
      )}
      {savedMessage && (
        <p className="mb-4 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
          {savedMessage}
        </p>
      )}

      {pending.length > 0 && (
        <div className="mb-4 flex items-start gap-2 rounded-lg bg-amber-50 px-3 py-2.5 text-sm text-amber-800 dark:bg-amber-950/40 dark:text-amber-300">
          <CloudOff className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={2} />
          <span>
            {pending.length} attendance {pending.length === 1 ? 'submission' : 'submissions'} waiting to sync
            {pending.length <= 3 && (
              <>: {pending.map((p) => `${p.className} (${p.date})`).join(', ')}</>
            )}
            . This will happen automatically once you're back online.
          </span>
        </div>
      )}

      {rows.length > 0 && (
        <div className="mb-4 grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard label="Present" value={String(counts.Present)} icon={CheckCircle2} tone="emerald" />
          <StatCard label="Absent" value={String(counts.Absent)} icon={XCircle} tone="amber" />
          <StatCard label="Late" value={String(counts.Late)} icon={Clock} tone="brand" />
          <StatCard label="Excused" value={String(counts.Excused)} icon={ShieldQuestion} tone="slate" />
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {loading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={CalendarCheck}
            title="No students in this class"
            description="Assign students to this class to mark their attendance."
          />
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="px-4 py-3 font-medium">Student</th>
                <th className="px-4 py-3 font-medium">
                  <div className="flex items-center gap-1.5">
                    Status
                    {canMark && (
                      <button
                        type="button"
                        onClick={() => markAll('Present')}
                        className="rounded px-1.5 py-0.5 text-[10px] font-medium normal-case text-brand-600 hover:bg-brand-50 dark:text-brand-400 dark:hover:bg-brand-950/40"
                      >
                        mark all present
                      </button>
                    )}
                  </div>
                </th>
                <th className="px-4 py-3 font-medium">Time in</th>
                <th className="px-4 py-3 font-medium">Reason</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.studentId} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                  <td className="px-4 py-2.5">
                    <p className="font-medium text-slate-900 dark:text-white">{r.studentName}</p>
                    <p className="text-xs text-slate-400">{r.studentNumber}</p>
                  </td>
                  <td className="px-4 py-2.5">
                    <div className="flex gap-1.5">
                      {ATTENDANCE_STATUSES.map((s) => {
                        const { icon: Icon, active } = STATUS_STYLES[s]
                        const isActive = r.status === s
                        return (
                          <button
                            key={s}
                            type="button"
                            disabled={!canMark}
                            onClick={() => updateRow(r.studentId, { status: s })}
                            title={s}
                            className={`flex h-7 w-7 items-center justify-center rounded-lg border transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                              isActive ? active : 'border-slate-200 text-slate-300 hover:border-slate-300 dark:border-slate-700 dark:text-slate-600'
                            }`}
                          >
                            <Icon className="h-4 w-4" strokeWidth={2} />
                          </button>
                        )
                      })}
                    </div>
                  </td>
                  <td className="px-4 py-2.5">
                    {(r.status === 'Present' || r.status === 'Late') && canMark ? (
                      <input
                        type="time"
                        value={r.timeIn}
                        onChange={(e) => updateRow(r.studentId, { timeIn: e.target.value })}
                        className="rounded-lg border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                      />
                    ) : (
                      <span className="text-slate-400">{r.timeIn || '—'}</span>
                    )}
                  </td>
                  <td className="px-4 py-2.5">
                    {(r.status === 'Absent' || r.status === 'Late' || r.status === 'Excused') && canMark ? (
                      <input
                        value={r.reason}
                        onChange={(e) => updateRow(r.studentId, { reason: e.target.value })}
                        placeholder="Optional reason"
                        className="w-full rounded-lg border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                      />
                    ) : (
                      <span className="text-slate-400">{r.reason || '—'}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
