import type { LucideIcon } from 'lucide-react'

interface StatCardProps {
  label: string
  value: string
  icon: LucideIcon
  tone?: 'brand' | 'emerald' | 'amber' | 'slate'
  hint?: string
}

const toneClasses: Record<NonNullable<StatCardProps['tone']>, string> = {
  brand: 'bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-300',
  emerald: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-900/40 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-600 dark:bg-amber-900/40 dark:text-amber-300',
  slate: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
}

export function StatCard({ label, value, icon: Icon, tone = 'brand', hint }: StatCardProps) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm font-medium text-slate-500 dark:text-slate-400">{label}</p>
          <p className="mt-2 text-2xl font-semibold tracking-tight text-slate-900 dark:text-white">
            {value}
          </p>
          {hint && <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">{hint}</p>}
        </div>
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${toneClasses[tone]}`}>
          <Icon className="h-5 w-5" strokeWidth={2} />
        </div>
      </div>
    </div>
  )
}
