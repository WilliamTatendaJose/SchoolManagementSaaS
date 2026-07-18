import { GraduationCap, Lock } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { navGroups } from './navConfig'

export function Sidebar() {
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const hasRole = useAuthStore((s) => s.hasRole)
  const hasModule = useAuthStore((s) => s.hasModule)
  const tenantName = useAuthStore((s) => s.tenantName)

  return (
    <nav className="flex h-full w-64 shrink-0 flex-col bg-slate-950 text-slate-300">
      <div className="flex items-center gap-2.5 px-5 py-5">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 shadow-lg shadow-brand-900/40">
          <GraduationCap className="h-5 w-5 text-white" strokeWidth={2.25} />
        </div>
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-white">{tenantName || 'School SMS'}</p>
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-500">
            Management Suite
          </p>
        </div>
      </div>

      <div className="scroll-slim flex-1 space-y-6 overflow-y-auto px-3 pb-6 pt-2">
        {navGroups.map((group) => {
          const visible = group.items.filter(
            (item) => (!item.permission || hasPermission(item.permission)) && (!item.role || hasRole(item.role)),
          )
          if (visible.length === 0) return null

          return (
            <div key={group.label}>
              <p className="px-3 pb-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-600">
                {group.label}
              </p>
              <ul className="space-y-0.5">
                {visible.map((item) => {
                  const Icon = item.icon
                  const locked = !!item.module && !hasModule(item.module)
                  return (
                    <li key={item.path}>
                      <NavLink
                        to={item.path}
                        className={({ isActive }) =>
                          [
                            'group flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                            isActive
                              ? 'bg-brand-600/15 text-white'
                              : 'text-slate-400 hover:bg-white/5 hover:text-slate-100',
                          ].join(' ')
                        }
                      >
                        {({ isActive }) => (
                          <>
                            <Icon
                              className={[
                                'h-[18px] w-[18px] shrink-0 transition-colors',
                                isActive ? 'text-brand-400' : 'text-slate-500 group-hover:text-slate-300',
                              ].join(' ')}
                              strokeWidth={2}
                            />
                            <span className="flex-1 truncate">{item.label}</span>
                            {locked && (
                              <Lock className="h-3.5 w-3.5 shrink-0 text-slate-600" strokeWidth={2} aria-label="Requires upgrade" />
                            )}
                          </>
                        )}
                      </NavLink>
                    </li>
                  )
                })}
              </ul>
            </div>
          )
        })}
      </div>
    </nav>
  )
}
