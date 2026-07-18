import type { ReactNode } from 'react'

type Tone = 'emerald' | 'amber' | 'red' | 'slate' | 'brand'

const toneClasses: Record<Tone, string> = {
  emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
  red: 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300',
  slate: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  brand: 'bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300',
}

/** Maps a StudentStatus/generic status string to a semantic color. */
const statusTone: Record<string, Tone> = {
  Active: 'emerald',
  Graduated: 'brand',
  Alumni: 'slate',
  Transferred: 'amber',
  Suspended: 'amber',
  Expelled: 'red',
}

export function Badge({ children, tone = 'slate' }: { children: ReactNode; tone?: Tone }) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${toneClasses[tone]}`}
    >
      {children}
    </span>
  )
}

export function StatusBadge({ status }: { status: string }) {
  return <Badge tone={statusTone[status] ?? 'slate'}>{status}</Badge>
}
