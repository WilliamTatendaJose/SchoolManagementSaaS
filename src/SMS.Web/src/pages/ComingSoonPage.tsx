import type { LucideIcon } from 'lucide-react'

export function ComingSoonPage({ title, icon: Icon }: { title: string; icon: LucideIcon }) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 dark:bg-slate-800">
        <Icon className="h-7 w-7 text-slate-400" strokeWidth={1.75} />
      </div>
      <h1 className="mt-5 text-lg font-semibold text-slate-900 dark:text-white">{title}</h1>
      <p className="mt-1.5 max-w-xs text-sm text-slate-500 dark:text-slate-400">
        This screen is next up on the roadmap and hasn't been built yet.
      </p>
    </div>
  )
}
