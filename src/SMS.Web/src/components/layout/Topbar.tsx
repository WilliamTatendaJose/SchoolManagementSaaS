import { LogOut } from 'lucide-react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { apiClient } from '../../api/client'
import { ThemeToggle } from '../ui/ThemeToggle'
import { navItems } from './navConfig'

function initials(firstName?: string, lastName?: string) {
  return `${firstName?.[0] ?? ''}${lastName?.[0] ?? ''}`.toUpperCase() || '?'
}

export function Topbar() {
  const navigate = useNavigate()
  const location = useLocation()
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)

  const currentLabel = navItems.find((i) => i.path === location.pathname)?.label ?? ''

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
    <header className="flex h-16 shrink-0 items-center justify-between border-b border-slate-200/70 bg-white/70 px-8 backdrop-blur-xl dark:border-white/10 dark:bg-slate-900/70">
      <h2 className="text-base font-semibold text-slate-900 dark:text-white">{currentLabel}</h2>

      <div className="flex items-center gap-3">
        <ThemeToggle />

        <div className="h-6 w-px bg-slate-200 dark:bg-slate-800" aria-hidden />

        {user && (
          <button
            onClick={() => navigate('/profile')}
            title="My profile"
            className="flex items-center gap-2.5 rounded-lg px-1.5 py-1 transition-colors hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-100 text-xs font-semibold text-brand-700 dark:bg-brand-900/50 dark:text-brand-300">
              {initials(user.firstName, user.lastName)}
            </div>
            <span className="hidden text-sm font-medium text-slate-700 sm:inline dark:text-slate-300">
              {user.firstName} {user.lastName}
            </span>
          </button>
        )}
        <button
          onClick={handleLogout}
          title="Log out"
          className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-white"
        >
          <LogOut className="h-4 w-4" strokeWidth={2} />
        </button>
      </div>
    </header>
  )
}
