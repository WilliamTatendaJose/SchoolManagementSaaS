import { Outlet, useLocation } from 'react-router-dom'
import { useTenantFeatures } from '../../auth/useTenantFeatures'
import { useAttendanceSync } from '../../offline/useAttendanceSync'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'

export function AppShell() {
  // The profile itself is fetched higher up (AuthenticatedRoot in App.tsx), since
  // route guards above this shell (StaffAreaGuard, RoleRoute) need it too. Tenant
  // feature flags are only relevant once inside the staff shell.
  useTenantFeatures()
  // Mounted here (not on the Attendance page itself) so marks queued while offline
  // still sync even after the teacher navigates away before connectivity returns.
  useAttendanceSync()

  const location = useLocation()

  return (
    <div className="relative flex h-screen overflow-hidden bg-slate-50 dark:bg-slate-950">
      {/* Ambient background blobs - the whole point of the glass chrome (Sidebar,
          Topbar, Drawer) is to show a soft blur of *something*; a flat bg color
          behind them just reads as a slightly-dimmed flat color. Fixed so they don't
          move with scroll, low opacity so they stay a background detail, not a distraction. */}
      <div className="pointer-events-none fixed inset-0 z-0" aria-hidden>
        <div className="absolute -left-40 -top-40 h-[32rem] w-[32rem] rounded-full bg-brand-400/20 blur-[110px] dark:bg-brand-600/20" />
        <div className="absolute right-[-10rem] top-1/3 h-[28rem] w-[28rem] rounded-full bg-emerald-300/15 blur-[110px] dark:bg-emerald-500/10" />
        <div className="absolute bottom-[-12rem] left-1/3 h-[30rem] w-[30rem] rounded-full bg-amber-200/15 blur-[110px] dark:bg-amber-500/10" />
      </div>

      <div className="relative z-10 flex h-full w-full">
        <Sidebar />
        <div className="flex flex-1 flex-col overflow-hidden">
          <Topbar />
          <main className="scroll-slim flex-1 overflow-y-auto p-8">
            {/* Keyed on pathname so the fade-in replays on every route change. */}
            <div key={location.pathname} className="route-fade-in">
              <Outlet />
            </div>
          </main>
        </div>
      </div>
    </div>
  )
}
