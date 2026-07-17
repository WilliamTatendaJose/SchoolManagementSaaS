import { useQuery } from '@tanstack/react-query'
import { Users, BedDouble, Briefcase, School, Wallet, CalendarCheck } from 'lucide-react'
import { apiClient } from '../api/client'
import type { DashboardDto } from '../api/types'
import { useAuthStore } from '../auth/authStore'
import { StatCard } from '../components/ui/StatCard'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function greeting() {
  const hour = new Date().getHours()
  if (hour < 12) return 'Good morning'
  if (hour < 17) return 'Good afternoon'
  return 'Good evening'
}

export function DashboardPage() {
  const profile = useAuthStore((s) => s.profile)
  const canViewDashboard = useAuthStore((s) => s.hasPermission('reports.dashboard'))

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard'],
    enabled: canViewDashboard,
    queryFn: async () => {
      const { data } = await apiClient.get<DashboardDto>('/dashboard')
      return data
    },
  })

  return (
    <div>
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">
          {greeting()}{profile ? `, ${profile.firstName}` : ''}
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          {new Date().toLocaleDateString(undefined, {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric',
          })}
        </p>
      </div>

      {!canViewDashboard && (
        <p className="mt-8 text-sm text-slate-500 dark:text-slate-400">
          You don't have access to school-wide analytics. Use the sidebar to get to your work.
        </p>
      )}

      {canViewDashboard && isLoading && (
        <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
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
          <div className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Active students" value={data.activeStudents.toLocaleString()} icon={Users} tone="brand" />
            <StatCard label="Boarding students" value={data.boardingStudents.toLocaleString()} icon={BedDouble} tone="amber" />
            <StatCard label="Staff" value={data.totalStaff.toLocaleString()} icon={Briefcase} tone="slate" />
            <StatCard label="Classes" value={data.totalClasses.toLocaleString()} icon={School} tone="emerald" />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
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

            <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
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

          {data.enrollmentByClass.length > 0 && (
            <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
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
        </>
      )}
    </div>
  )
}
