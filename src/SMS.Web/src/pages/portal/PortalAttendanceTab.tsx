import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchChildAttendance, fetchPortalTerms } from '../../api/portal'
import { ErrorState } from '../../components/ui/ErrorState'
import { StatCard } from '../../components/ui/StatCard'
import { CalendarCheck, CalendarX, Clock, FileWarning } from 'lucide-react'

export function PortalAttendanceTab({ studentId }: { studentId: string }) {
  const [termId, setTermId] = useState('')

  const { data: terms } = useQuery({ queryKey: ['portal-terms'], queryFn: fetchPortalTerms })

  const { data: summary, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-attendance', studentId, termId],
    queryFn: () => fetchChildAttendance(studentId, termId || undefined),
  })

  return (
    <div className="space-y-3">
      <select
        value={termId}
        onChange={(e) => setTermId(e.target.value)}
        className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
      >
        <option value="">All terms</option>
        {terms?.map((t) => (
          <option key={t.id} value={t.id}>
            {t.academicYearName} · {t.name}
          </option>
        ))}
      </select>

      {isLoading ? (
        <div className="h-40 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
      ) : isError ? (
        <ErrorState description="Could not load attendance." onRetry={() => refetch()} />
      ) : summary ? (
        <div className="space-y-3">
          <div className="rounded-2xl border border-slate-200 bg-white p-4 text-center shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <p className="text-xs text-slate-400">Attendance rate</p>
            <p className="mt-1 text-3xl font-semibold text-slate-900 dark:text-white">{summary.attendancePercentage}%</p>
            <p className="mt-1 text-xs text-slate-400">{summary.totalDays} school days recorded</p>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <StatCard label="Present" value={String(summary.presentDays)} icon={CalendarCheck} tone="emerald" />
            <StatCard label="Absent" value={String(summary.absentDays)} icon={CalendarX} tone="amber" />
            <StatCard label="Late" value={String(summary.lateDays)} icon={Clock} tone="slate" />
          </div>
          {summary.excusedDays > 0 && (
            <StatCard label="Excused" value={String(summary.excusedDays)} icon={FileWarning} tone="slate" />
          )}
        </div>
      ) : null}
    </div>
  )
}
