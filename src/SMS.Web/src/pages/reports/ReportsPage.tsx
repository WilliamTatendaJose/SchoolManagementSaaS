import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, Receipt, Users, Wallet } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { fetchClasses } from '../../api/classes'
import { fetchDefaulters, fetchReconciliation } from '../../api/reports'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatCard } from '../../components/ui/StatCard'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function isoDateDaysAgo(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString().slice(0, 10)
}

export function ReportsPage() {
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'reconciliation' ? 'reconciliation' : 'defaulters'
  const classId = params.get('classId') ?? ''
  const fromDate = params.get('from') ?? isoDateDaysAgo(7)
  const toDate = params.get('to') ?? isoDateDaysAgo(0)

  function setTab(next: 'defaulters' | 'reconciliation') {
    const p = new URLSearchParams(params)
    if (next === 'defaulters') p.delete('tab')
    else p.set('tab', next)
    setParams(p, { replace: true })
  }

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: tab === 'defaulters' })

  const { data: defaulters, isLoading: loadingDefaulters } = useQuery({
    queryKey: ['defaulters-report', classId],
    queryFn: () => fetchDefaulters(classId || undefined),
    enabled: tab === 'defaulters',
  })

  const { data: reconciliation, isLoading: loadingReconciliation } = useQuery({
    queryKey: ['reconciliation-report', fromDate, toDate],
    queryFn: () => fetchReconciliation(fromDate, toDate),
    enabled: tab === 'reconciliation' && !!fromDate && !!toDate,
  })

  const totalOutstanding = defaulters?.reduce((sum, d) => sum + d.outstandingBalance, 0) ?? 0

  return (
    <div>
      <PageHeader title="Reports" description="Fee collection and cashier reports" />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('defaulters')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'defaulters'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <AlertTriangle className="h-4 w-4" strokeWidth={2} />
          Defaulters
        </button>
        <button
          onClick={() => setTab('reconciliation')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'reconciliation'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <Receipt className="h-4 w-4" strokeWidth={2} />
          Cashier reconciliation
        </button>
      </div>

      {tab === 'defaulters' ? (
        <>
          <div className="mb-4 flex items-center gap-3">
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
          </div>

          {defaulters && defaulters.length > 0 && (
            <div className="mb-4 grid grid-cols-2 gap-4 lg:grid-cols-2">
              <StatCard label="Students with balances" value={String(defaulters.length)} icon={Users} tone="amber" />
              <StatCard label="Total outstanding" value={currency.format(totalOutstanding)} icon={Wallet} tone="amber" />
            </div>
          )}

          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingDefaulters ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : defaulters && defaulters.length > 0 ? (
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Student</th>
                    <th className="px-4 py-3 font-medium">Class</th>
                    <th className="px-4 py-3 font-medium">Guardian</th>
                    <th className="px-4 py-3 font-medium text-right">Outstanding</th>
                  </tr>
                </thead>
                <tbody>
                  {defaulters
                    .slice()
                    .sort((a, b) => b.outstandingBalance - a.outstandingBalance)
                    .map((d) => (
                      <tr key={d.studentId} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                        <td className="px-4 py-3">
                          <p className="font-medium text-slate-900 dark:text-white">{d.studentName}</p>
                          <p className="text-xs text-slate-400">{d.studentNumber}</p>
                        </td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{d.className || '—'}</td>
                        <td className="px-4 py-3">
                          <p className="text-slate-700 dark:text-slate-200">{d.guardianName || '—'}</p>
                          {d.guardianPhone && <p className="text-xs text-slate-400">{d.guardianPhone}</p>}
                        </td>
                        <td className="px-4 py-3 text-right font-medium text-amber-700 dark:text-amber-400">
                          {currency.format(d.outstandingBalance)}
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            ) : (
              <EmptyState icon={Users} title="No outstanding balances" description="Every student is fully paid up." />
            )}
          </div>
        </>
      ) : (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-3">
            <input
              type="date"
              value={fromDate}
              onChange={(e) => updateParam('from', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            />
            <span className="text-sm text-slate-400">to</span>
            <input
              type="date"
              value={toDate}
              onChange={(e) => updateParam('to', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            />
          </div>

          {loadingReconciliation ? (
            <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
          ) : reconciliation && reconciliation.byCurrency.length > 0 ? (
            <div className="space-y-4">
              <StatCard label="Payments in range" value={String(reconciliation.paymentCount)} icon={Receipt} tone="brand" />
              {reconciliation.byCurrency.map((c) => (
                <div
                  key={c.currency}
                  className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900"
                >
                  <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800">
                    <h3 className="text-sm font-semibold text-slate-900 dark:text-white">{c.currency}</h3>
                    <p className="text-sm font-semibold text-slate-900 dark:text-white">
                      {new Intl.NumberFormat('en-US', { style: 'currency', currency: c.currency }).format(c.totalCollected)}
                      <span className="ml-1.5 text-xs font-normal text-slate-400">{c.count} payments</span>
                    </p>
                  </div>
                  <table className="w-full text-left text-sm">
                    <thead>
                      <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                        <th className="px-4 py-2.5 font-medium">Method</th>
                        <th className="px-4 py-2.5 font-medium text-right">Count</th>
                        <th className="px-4 py-2.5 font-medium text-right">Amount</th>
                      </tr>
                    </thead>
                    <tbody>
                      {c.byMethod.map((m) => (
                        <tr key={m.paymentMethod} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                          <td className="px-4 py-2.5 text-slate-700 dark:text-slate-200">{m.paymentMethod}</td>
                          <td className="px-4 py-2.5 text-right text-slate-500 dark:text-slate-400">{m.count}</td>
                          <td className="px-4 py-2.5 text-right font-medium text-slate-800 dark:text-slate-100">
                            {new Intl.NumberFormat('en-US', { style: 'currency', currency: c.currency }).format(m.amount)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState icon={Receipt} title="No payments in this range" description="Try widening the date range." />
          )}
        </>
      )}
    </div>
  )
}
