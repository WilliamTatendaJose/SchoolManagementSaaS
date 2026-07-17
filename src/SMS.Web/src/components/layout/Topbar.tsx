import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { apiClient } from '../../api/client'

export function Topbar() {
  const navigate = useNavigate()
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)

  async function handleLogout() {
    try {
      await apiClient.post('/auth/logout')
    } catch {
      // Ignore network errors on logout; we're clearing local session regardless.
    }
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="flex h-14 shrink-0 items-center justify-end gap-4 border-b border-slate-200 bg-white px-6 dark:border-slate-800 dark:bg-slate-900">
      {user && (
        <span className="text-sm text-slate-600 dark:text-slate-300">
          {user.firstName} {user.lastName}
        </span>
      )}
      <button
        onClick={handleLogout}
        className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
      >
        Log out
      </button>
    </header>
  )
}
