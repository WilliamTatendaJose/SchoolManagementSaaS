import { useQuery } from '@tanstack/react-query'
import { BookOpenCheck, CalendarCheck, GraduationCap, IdCard, Users, Wallet } from 'lucide-react'
import { useState } from 'react'
import { fetchMyChildren } from '../../api/portal'
import { Avatar } from '../../components/ui/Avatar'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PortalAssignmentsTab } from './PortalAssignmentsTab'
import { PortalAttendanceTab } from './PortalAttendanceTab'
import { PortalDetailsTab } from './PortalDetailsTab'
import { PortalFinanceTab } from './PortalFinanceTab'
import { PortalResultsTab } from './PortalResultsTab'

type Tab = 'results' | 'assignments' | 'attendance' | 'fees' | 'details'

const TABS: { key: Tab; label: string; icon: typeof GraduationCap }[] = [
  { key: 'results', label: 'Results', icon: GraduationCap },
  { key: 'assignments', label: 'Work', icon: BookOpenCheck },
  { key: 'attendance', label: 'Attendance', icon: CalendarCheck },
  { key: 'fees', label: 'Fees', icon: Wallet },
  { key: 'details', label: 'Details', icon: IdCard },
]

export function PortalHomePage() {
  const [tab, setTab] = useState<Tab>('results')
  const [selectedChildId, setSelectedChildId] = useState<string | null>(null)

  const { data: children, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-children'],
    queryFn: fetchMyChildren,
  })

  const activeChildId = selectedChildId ?? children?.[0]?.studentId ?? null
  const activeChild = children?.find((c) => c.studentId === activeChildId)

  if (isLoading) {
    return (
      <div className="space-y-3">
        <div className="h-16 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
        <div className="h-48 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
      </div>
    )
  }

  if (isError) {
    return <ErrorState description="Could not load your children." onRetry={() => refetch()} />
  }

  if (!children || children.length === 0) {
    return (
      <EmptyState
        icon={Users}
        title="No children linked to this account"
        description="If this doesn't look right, contact the school office to check your guardian record is linked to your child."
      />
    )
  }

  return (
    <div className="space-y-4">
      {children.length > 1 && (
        <div className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
          {children.map((child) => (
            <button
              key={child.studentId}
              onClick={() => setSelectedChildId(child.studentId)}
              className={`flex shrink-0 items-center gap-2 rounded-full border px-3 py-1.5 text-sm font-medium transition-colors ${
                child.studentId === activeChildId
                  ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-400 dark:bg-brand-900/30 dark:text-brand-300'
                  : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
              }`}
            >
              <Avatar name={child.fullName} size="sm" />
              {child.fullName.split(' ')[0]}
            </button>
          ))}
        </div>
      )}

      {activeChild && (
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-3">
            <Avatar name={activeChild.fullName} size="lg" />
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold text-slate-900 dark:text-white">{activeChild.fullName}</p>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {activeChild.className ?? 'No class assigned'} · {activeChild.studentNumber}
              </p>
            </div>
            {activeChild.outstandingBalance > 0 && (
              <div className="text-right">
                <p className="text-xs text-slate-400">Owing</p>
                <p className="text-sm font-semibold text-amber-600 dark:text-amber-400">
                  {activeChild.outstandingBalance.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}
                </p>
              </div>
            )}
          </div>
        </div>
      )}

      <div className="flex gap-1 rounded-xl bg-slate-100 p-1 dark:bg-slate-800/60">
        {TABS.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`flex flex-1 flex-col items-center justify-center gap-0.5 rounded-lg px-1 py-1.5 text-xs font-medium transition-colors sm:flex-row sm:gap-1.5 sm:text-sm ${
              tab === key
                ? 'bg-white text-slate-900 shadow-sm dark:bg-slate-900 dark:text-white'
                : 'text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
            }`}
          >
            <Icon className="h-4 w-4 shrink-0" strokeWidth={2} />
            {label}
          </button>
        ))}
      </div>

      {activeChildId && (
        <div>
          {tab === 'results' && <PortalResultsTab studentId={activeChildId} />}
          {tab === 'assignments' && <PortalAssignmentsTab studentId={activeChildId} />}
          {tab === 'attendance' && <PortalAttendanceTab studentId={activeChildId} />}
          {tab === 'fees' && <PortalFinanceTab studentId={activeChildId} studentName={activeChild?.fullName ?? ''} />}
          {tab === 'details' && <PortalDetailsTab studentId={activeChildId} />}
        </div>
      )}
    </div>
  )
}
