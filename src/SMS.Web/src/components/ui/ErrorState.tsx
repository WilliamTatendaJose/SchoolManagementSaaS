import { RefreshCw, TriangleAlert } from 'lucide-react'
import { Button } from './Button'

export function ErrorState({
  title = 'Couldn’t load this page',
  description,
  onRetry,
}: {
  title?: string
  description?: string
  onRetry?: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-red-50 dark:bg-red-950/40">
        <TriangleAlert className="h-6 w-6 text-red-500" strokeWidth={1.75} />
      </div>
      <h3 className="mt-4 text-sm font-semibold text-slate-900 dark:text-white">{title}</h3>
      {description && (
        <p className="mt-1 max-w-xs text-sm text-slate-500 dark:text-slate-400">{description}</p>
      )}
      {onRetry && (
        <Button variant="secondary" className="mt-5" onClick={onRetry}>
          <RefreshCw className="h-4 w-4" strokeWidth={2} />
          Retry
        </Button>
      )}
    </div>
  )
}
