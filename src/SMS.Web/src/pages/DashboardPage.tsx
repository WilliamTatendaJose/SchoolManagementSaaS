import { useQuery } from '@tanstack/react-query'
import {
  AlertTriangle,
  ArrowRight,
  BedDouble,
  BookOpen,
  Briefcase,
  CalendarCheck,
  Lock,
  Receipt,
  School,
  Users,
  Wallet,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../api/client'
import { fetchPayments } from '../api/finance'
import { fetchOverdueLoans } from '../api/library'
import { fetchDefaulters } from '../api/reports'
import type { DashboardDto } from '../api/types'
import { PAYMENT_METHOD_LABELS } from '../api/types'
import { useAuthStore } from '../auth/authStore'
import { navGroups } from '../components/layout/navConfig'
import { EmptyState } from '../components/ui/EmptyState'
import { StatCard } from '../components/ui/StatCard'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function greeting() {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 17) return 'Good afternoon'
  return 'Good evening'
}

function daysOverdue(dueDate: string) {
  const diff = Date.now() - new Date(dueDate).getTime()
  return Math.max(1, Math.round(diff / (1000 * 60 * 60 * 24)))
}

function Panel({
  title,
  icon: Icon,
  tone = 'slate',
  action,
  children,
}: {
  title: string
  icon: React.ComponentType<{ className?: string; strokeWidth?: number }>
  tone?: 'amber' | 'slate'
  action?: { label: string; onClick: () => void }
  children: React.ReactNode
}) {
  return (
    <div className="rounded-2xl border border-slate-200/70 bg-white/70 p-6 shadow-sm backdrop-blur-xl dark:border-white/10 dark:bg-slate-900/60">
      <div className="flex items-center justify-between">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
          <Icon
            className={`h-4 w-4 ${tone === 'amber' ? 'text-amber-500' : 'text-slate-400'}`}
            strokeWidth={2}
          />
          {title}
        </h3>
        {action && (
          <button
            onClick={action.onClick}
            className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-400"
          >
            {action.label}
            <ArrowRight className="h-3 w-3" strokeWidth={2} />
          </button>
        )}
      </div>
      <div className="mt-4">{children}</div>
    </div>
  )
}

export function DashboardPage() {
  const navigate = useNavigate()
  const profile = useAuthStore((s) => s.profile)
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const hasRole = useAuthStore((s) => s.hasRole)
  const hasModule = useAuthStore((s) => s.hasModule)
  const canViewDashboard = hasPermission('reports.dashboard')
  const canViewDefaulters = hasPermission('finance.report')
  const canViewPayments = hasPermission('finance.view')
  const canViewLibrary = hasPermission('library.view') && hasModule('library')

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    enabled: canViewDashboard,
    queryFn: async () => {
      const { data } = await apiClient.get<DashboardDto>('/dashboard')
      return data
    },
  })

  const { data: defaulters, isLoading: loadingDefaulters } = useQuery({
    queryKey: ['dashboard-defaulters'],
    queryFn: () => fetchDefaulters(),
    enabled: canViewDefaulters,
  })

  const { data: recentPayments, isLoading: loadingPayments } = useQuery({
    queryKey: ['dashboard-recent-payments'],
    queryFn: () => fetchPayments({ page: 1, pageSize: 5 }),
    enabled: canViewPayments,
  })

  const { data: overdueLoans, isLoading: loadingOverdue } = useQuery({
    queryKey: ['dashboard-overdue-loans'],
    queryFn: fetchOverdueLoans,
    enabled: canViewLibrary,
  })

  const quickLinks = navGroups
    .flatMap((g) => g.items)
    .filter((item) => item.path !== '/dashboard')
    .filter((item) => (!item.permission || hasPermission(item.permission)) && (!item.role || hasRole(item.role)))

  const hasNeedsAttention = canViewDefaulters || canViewLibrary
  const topDefaulters = defaulters?.slice().sort((a, b) => b.outstandingBalance - a.outstandingBalance).slice(0, 5) ?? []

  return (
    <div>
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-brand-950 via-brand-900 to-brand-700 px-8 py-7 text-white shadow-lg shadow-brand-900/20">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.06]"
          style={{
            backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
            backgroundSize: '24px 24px',
          }}
          aria-hidden
        />
        <div
          className="pointer-events-none absolute -right-24 -top-24 h-72 w-72 rounded-full bg-brand-400/20 blur-3xl"
          aria-hidden
        />
        <div className="relative">
          <h1 className="text-2xl font-semibold tracking-tight">
            {greeting()}
            {profile ? `, ${profile.firstName}` : ''}
          </h1>
          <p className="mt-1.5 text-sm text-brand-200/80">
            {new Date().toLocaleDateString(undefined, {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </p>
        </div>
      </div>

      {quickLinks.length > 0 && (
        <div className="mt-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          {quickLinks.map((item) => {
            const Icon = item.icon
            const locked = !!item.module && !hasModule(item.module)
            return (
              <button
                key={item.path}
                onClick={() => navigate(item.path)}
                className="group flex flex-col items-center gap-2 rounded-2xl border border-slate-200/70 bg-white/70 px-3 py-4 text-center shadow-sm backdrop-blur-xl transition-all duration-150 hover:-translate-y-0.5 hover:bg-white/90 hover:shadow-md dark:border-white/10 dark:bg-slate-900/60 dark:hover:bg-slate-900/80"
              >
                <span className="relative flex h-9 w-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600 transition-transform duration-150 group-hover:scale-105 dark:bg-brand-900/40 dark:text-brand-300">
                  <Icon className="h-[18px] w-[18px]" strokeWidth={2} />
                  {locked && (
                    <span className="absolute -right-1 -top-1 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-slate-700 ring-2 ring-white dark:ring-slate-900">
                      <Lock className="h-2 w-2 text-slate-300" strokeWidth={2.5} />
                    </span>
                  )}
                </span>
                <span className="text-xs font-medium text-slate-600 dark:text-slate-300">{item.label}</span>
              </button>
            )
          })}
        </div>
      )}

      {!canViewDashboard && !hasNeedsAttention && !canViewPayments && (
        <p className="mt-8 text-sm text-slate-500 dark:text-slate-400">
          Use the shortcuts above to get to your work.
        </p>
      )}

      {canViewDashboard && isLoading && (
        <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div
              key={i}
              className="h-[104px] animate-pulse rounded-2xl border border-slate-200 bg-slate-100 dark:border-slate-800 dark:bg-slate-800/50"
            />
          ))}
        </div>
      )}

      {canViewDashboard && data && (
        <>
          <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Active students" value={data.activeStudents.toLocaleString()} icon={Users} tone="brand" />
            <StatCard label="Boarding students" value={data.boardingStudents.toLocaleString()} icon={BedDouble} tone="amber" />
            <StatCard label="Staff" value={data.totalStaff.toLocaleString()} icon={Briefcase} tone="slate" />
            <StatCard label="Classes" value={data.totalClasses.toLocaleString()} icon={School} tone="emerald" />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="rounded-2xl border border-slate-200/70 bg-white/70 p-6 shadow-sm backdrop-blur-xl lg:col-span-2 dark:border-white/10 dark:bg-slate-900/60">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Fee collection</h3>
                <Wallet className="h-4 w-4 text-slate-400" strokeWidth={2} />
              </div>

              <div className="mt-4 flex items-baseline gap-2">
                <span className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">
                  {currency.format(data.totalCollected)}
                </span>
                <span className="text-sm text-slate-400">of {currency.format(data.totalBilled)} billed</span>
              </div>

              <div className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-brand-500 to-brand-600 transition-all"
                  style={{ width: `${Math.min(100, data.collectionRatePercent)}%` }}
                />
              </div>

              <div className="mt-3 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                <span>{data.collectionRatePercent.toFixed(1)}% collected</span>
                <span>{currency.format(data.totalOutstanding)} outstanding</span>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-200/70 bg-white/70 p-6 shadow-sm backdrop-blur-xl dark:border-white/10 dark:bg-slate-900/60">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Attendance rate</h3>
                <CalendarCheck className="h-4 w-4 text-slate-400" strokeWidth={2} />
              </div>
              <p className="mt-4 text-3xl font-semibold tracking-tight text-slate-900 dark:text-white">
                {data.attendanceRatePercent}%
              </p>
              <p className="mt-1 text-xs text-slate-400">last 30 days, school-wide</p>
            </div>
          </div>
        </>
      )}

      {hasNeedsAttention && (
        <div className="mt-6">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
            Needs attention
          </h2>
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            {canViewDefaulters && (
              <Panel title="Top fee defaulters" icon={AlertTriangle} tone="amber" action={{ label: 'View all', onClick: () => navigate('/reports') }}>
                {loadingDefaulters ? (
                  <div className="space-y-2">
                    {Array.from({ length: 3 }).map((_, i) => (
                      <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                    ))}
                  </div>
                ) : topDefaulters.length > 0 ? (
                  <ul className="space-y-1">
                    {topDefaulters.map((d) => (
                      <li
                        key={d.studentId}
                        className="flex items-center justify-between rounded-lg px-2 py-2 text-sm hover:bg-slate-50 dark:hover:bg-slate-800/40"
                      >
                        <div className="min-w-0">
                          <p className="truncate font-medium text-slate-800 dark:text-slate-100">{d.studentName}</p>
                          <p className="text-xs text-slate-400">{d.className || d.studentNumber}</p>
                        </div>
                        <span className="shrink-0 font-medium text-amber-700 dark:text-amber-400">
                          {currency.format(d.outstandingBalance)}
                        </span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="text-sm text-slate-400">No outstanding balances. Everyone's paid up.</p>
                )}
              </Panel>
            )}

            {canViewLibrary && (
              <Panel title="Overdue library loans" icon={BookOpen} tone="amber" action={{ label: 'View all', onClick: () => navigate('/library?tab=overdue') }}>
                {loadingOverdue ? (
                  <div className="space-y-2">
                    {Array.from({ length: 3 }).map((_, i) => (
                      <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                    ))}
                  </div>
                ) : overdueLoans && overdueLoans.length > 0 ? (
                  <ul className="space-y-1">
                    {overdueLoans.slice(0, 5).map((loan) => (
                      <li
                        key={loan.id}
                        className="flex items-center justify-between rounded-lg px-2 py-2 text-sm hover:bg-slate-50 dark:hover:bg-slate-800/40"
                      >
                        <div className="min-w-0">
                          <p className="truncate font-medium text-slate-800 dark:text-slate-100">{loan.bookTitle}</p>
                          <p className="text-xs text-slate-400">{loan.studentName}</p>
                        </div>
                        <span className="shrink-0 text-xs font-medium text-amber-700 dark:text-amber-400">
                          {daysOverdue(loan.dueDate)}d overdue
                        </span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="text-sm text-slate-400">No overdue loans right now.</p>
                )}
              </Panel>
            )}
          </div>
        </div>
      )}

      {canViewPayments && (
        <div className="mt-6">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">
            Recent activity
          </h2>
          <Panel title="Recent payments" icon={Receipt} action={{ label: 'View all', onClick: () => navigate('/finance?tab=payments') }}>
            {loadingPayments ? (
              <div className="space-y-2">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : recentPayments && recentPayments.data.length > 0 ? (
              <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
                {recentPayments.data.map((p) => (
                  <li key={p.id} className="flex items-center justify-between py-2.5 text-sm first:pt-0 last:pb-0">
                    <div className="min-w-0">
                      <p className="truncate font-medium text-slate-800 dark:text-slate-100">{p.studentName}</p>
                      <p className="text-xs text-slate-400">
                        {p.receiptNumber} ·{' '}
                        {PAYMENT_METHOD_LABELS[p.paymentMethod as keyof typeof PAYMENT_METHOD_LABELS] ?? p.paymentMethod} ·{' '}
                        {new Date(p.paymentDate).toLocaleDateString()}
                      </p>
                    </div>
                    <span className="shrink-0 font-medium text-slate-800 dark:text-slate-100">
                      {currency.format(p.amount)}
                    </span>
                  </li>
                ))}
              </ul>
            ) : (
              <EmptyState icon={Receipt} title="No payments yet" description="Recorded payments will show up here." />
            )}
          </Panel>
        </div>
      )}

      {canViewDashboard && data && data.enrollmentByClass.length > 0 && (
        <div className="mt-6 rounded-2xl border border-slate-200/70 bg-white/70 p-6 shadow-sm backdrop-blur-xl dark:border-white/10 dark:bg-slate-900/60">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Enrollment by class</h3>
          <div className="mt-5 space-y-3">
            {data.enrollmentByClass.map((c) => {
              const max = Math.max(...data.enrollmentByClass.map((x) => x.studentCount), 1)
              return (
                <div key={c.className} className="flex items-center gap-3">
                  <span className="w-28 shrink-0 truncate text-sm text-slate-600 dark:text-slate-300">
                    {c.className}
                  </span>
                  <div className="h-2 flex-1 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <div
                      className="h-full rounded-full bg-brand-500/80"
                      style={{ width: `${(c.studentCount / max) * 100}%` }}
                    />
                  </div>
                  <span className="w-8 shrink-0 text-right text-sm font-medium text-slate-700 dark:text-slate-300">
                    {c.studentCount}
                  </span>
                </div>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
