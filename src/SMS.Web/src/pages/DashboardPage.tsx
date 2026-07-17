import { useAuthStore } from '../auth/authStore'

export function DashboardPage() {
  const profile = useAuthStore((s) => s.profile)

  return (
    <div>
      <h1 className="text-2xl font-semibold text-slate-900 dark:text-white">
        Welcome{profile ? `, ${profile.firstName}` : ''}
      </h1>
      <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
        This is the dashboard shell. Feature screens land here next.
      </p>
    </div>
  )
}
