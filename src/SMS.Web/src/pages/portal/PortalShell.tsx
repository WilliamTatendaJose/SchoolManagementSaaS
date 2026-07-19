import { GraduationCap, LogOut } from 'lucide-react'
import { Outlet, useNavigate } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { useAuthStore } from '../../auth/authStore'
import { ThemeToggle } from '../../components/ui/ThemeToggle'

/** A deliberately minimal shell for the parent portal - no admin sidebar, no staff nav.
 * Parents land here on a phone from a WhatsApp-shared link, so it's a single scrollable
 * page rather than the multi-section staff app shell. */
export function PortalShell() {
  const navigate = useNavigate()
  const logout = useAuthStore((s) => s.logout)
  const user = useAuthStore((s) => s.user)
  const tenantName = useAuthStore((s) => s.tenantName)

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
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      <header className="sticky top-0 z-10 flex h-14 items-center justify-between border-b border-slate-200/70 bg-white/80 px-4 backdrop-blur-xl dark:border-white/10 dark:bg-slate-900/80">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-gradient-to-br from-brand-500 to-brand-700">
            <GraduationCap className="h-4 w-4 text-white" strokeWidth={2.25} />
          </div>
          <div className="leading-tight">
            <p className="text-sm font-semibold text-slate-900 dark:text-white">{tenantName || 'Parent portal'}</p>
            {user && <p className="text-[11px] text-slate-400">{user.firstName} {user.lastName}</p>}
          </div>
        </div>
        <div className="flex items-center gap-1.5">
          <ThemeToggle />
          <button
            onClick={handleLogout}
            title="Log out"
            className="flex h-8 w-8 items-center justify-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-white"
          >
            <LogOut className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-lg px-4 py-5 sm:max-w-2xl">
        <Outlet />
      </main>
    </div>
  )
}
