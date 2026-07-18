import { BookOpenCheck, Bus, Lock, Library as LibraryIcon, Mail, Tent } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../auth/authStore'
import type { FeatureModule } from '../api/types'
import { FEATURE_MODULE_LABELS } from '../api/types'
import { Button } from '../components/ui/Button'

const MODULE_INFO: Record<
  FeatureModule,
  { icon: typeof Bus; pitch: string; bullets: string[] }
> = {
  lms: {
    icon: BookOpenCheck,
    pitch: 'Set assignments, collect submissions and track grading in one place.',
    bullets: ['Assignment creation with due dates and attachments', 'Student submission tracking', 'Grading and feedback workflow'],
  },
  transport: {
    icon: Bus,
    pitch: 'Manage school transport routes, stops and rider assignments.',
    bullets: ['Routes with driver and vehicle assignment', 'Pickup/drop-off stop scheduling', 'Student-to-stop assignment'],
  },
  hostel: {
    icon: Tent,
    pitch: 'Run boarding operations: dormitory placement and weekend leave.',
    bullets: ['Dormitory and room assignment', 'Weekend leave requests and approvals', 'Departure/return tracking'],
  },
  library: {
    icon: LibraryIcon,
    pitch: 'Catalogue books and track loans and returns.',
    bullets: ['Book catalogue with copy tracking', 'Borrow/return workflow', 'Overdue loan tracking'],
  },
}

export function PaywallPage({ module }: { module: FeatureModule }) {
  const navigate = useNavigate()
  const tenantName = useAuthStore((s) => s.tenantName)
  const plan = useAuthStore((s) => s.features?.subscriptionPlan)
  const info = MODULE_INFO[module]
  const Icon = info.icon

  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-950/40 dark:text-brand-300">
          <Icon className="h-7 w-7" strokeWidth={1.75} />
        </div>
        <div className="mt-4 flex items-center justify-center gap-1.5 text-xs font-medium uppercase tracking-wide text-amber-600 dark:text-amber-400">
          <Lock className="h-3.5 w-3.5" strokeWidth={2} />
          Not included in your plan
        </div>
        <h1 className="mt-2 text-lg font-semibold text-slate-900 dark:text-white">
          {FEATURE_MODULE_LABELS[module]}
        </h1>
        <p className="mt-1.5 text-sm text-slate-500 dark:text-slate-400">{info.pitch}</p>

        <ul className="mt-5 space-y-2 text-left text-sm text-slate-600 dark:text-slate-300">
          {info.bullets.map((b) => (
            <li key={b} className="flex items-start gap-2">
              <span className="mt-1.5 h-1 w-1 shrink-0 rounded-full bg-slate-400" />
              {b}
            </li>
          ))}
        </ul>

        <div className="mt-6 rounded-xl bg-slate-50 p-4 text-left text-sm dark:bg-slate-800/60">
          <p className="text-slate-500 dark:text-slate-400">
            {tenantName ?? 'Your school'} is currently on the{' '}
            <span className="font-medium text-slate-700 dark:text-slate-200">{plan ?? '—'}</span> plan.
          </p>
          <p className="mt-1.5 text-slate-500 dark:text-slate-400">
            This module is switched on by your platform administrator, either by upgrading your
            subscription or enabling it individually.
          </p>
        </div>

        <div className="mt-6 flex flex-col gap-2">
          <Button
            onClick={() =>
              window.open(
                `mailto:?subject=${encodeURIComponent(`Upgrade request: ${FEATURE_MODULE_LABELS[module]}`)}&body=${encodeURIComponent(
                  `Hi,\n\nWe'd like to enable the ${FEATURE_MODULE_LABELS[module]} module for ${tenantName ?? 'our school'}.\n\nThanks.`,
                )}`,
              )
            }
          >
            <Mail className="h-4 w-4" strokeWidth={2} />
            Request upgrade
          </Button>
          <Button variant="secondary" className="w-full" onClick={() => navigate('/settings')}>
            View subscription details
          </Button>
        </div>
      </div>
    </div>
  )
}
