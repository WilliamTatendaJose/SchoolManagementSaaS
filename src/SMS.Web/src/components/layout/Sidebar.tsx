import { GraduationCap, Lock, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { useLayoutStore } from '../../store/layoutStore'
import { navGroups } from './navConfig'

export function Sidebar() {
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const hasRole = useAuthStore((s) => s.hasRole)
  const hasModule = useAuthStore((s) => s.hasModule)
  const tenantName = useAuthStore((s) => s.tenantName)
  const collapsed = useLayoutStore((s) => s.sidebarCollapsed)
  const toggleSidebar = useLayoutStore((s) => s.toggleSidebar)

  return (
    <nav
      className={[
        'relative flex h-full shrink-0 flex-col border-r border-white/10 bg-slate-950/85 text-slate-300 backdrop-blur-xl transition-[width] duration-200 ease-in-out',
        collapsed ? 'w-[76px]' : 'w-64',
      ].join(' ')}
    >
      <div className="flex h-[68px] items-center gap-2.5 overflow-hidden px-5 py-5">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 shadow-lg shadow-brand-900/40">
          <GraduationCap className="h-5 w-5 text-white" strokeWidth={2.25} />
        </div>
        <div className={['min-w-0 overflow-hidden transition-all duration-200', collapsed ? 'w-0 opacity-0' : 'w-auto opacity-100'].join(' ')}>
          <p className="truncate whitespace-nowrap text-sm font-semibold text-white">{tenantName || 'School SMS'}</p>
          <p className="whitespace-nowrap text-[11px] font-medium uppercase tracking-wider text-slate-500">
            Management Suite
          </p>
        </div>
      </div>

      <div className="scroll-slim flex-1 space-y-6 overflow-x-hidden overflow-y-auto px-3 pb-6 pt-2">
        {navGroups.map((group) => {
          const visible = group.items.filter(
            (item) => (!item.permission || hasPermission(item.permission)) && (!item.role || hasRole(item.role)),
          )
          if (visible.length === 0) return null

          return (
            <div key={group.label}>
              <p
                className={[
                  'overflow-hidden whitespace-nowrap px-3 pb-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-600 transition-all duration-200',
                  collapsed ? 'h-0 opacity-0' : 'h-4 opacity-100',
                ].join(' ')}
              >
                {group.label}
              </p>
              <ul className="space-y-0.5">
                {visible.map((item) => {
                  const Icon = item.icon
                  const locked = !!item.module && !hasModule(item.module)
                  return (
                    <li key={item.path} className="relative">
                      <NavLink
                        to={item.path}
                        title={collapsed ? item.label : undefined}
                        className={({ isActive }) =>
                          [
                            'group relative flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-all duration-150',
                            collapsed ? 'justify-center' : '',
                            isActive
                              ? 'bg-gradient-to-r from-brand-600/25 to-brand-600/5 text-white shadow-[inset_0_1px_0_0_rgba(255,255,255,0.04)]'
                              : 'text-slate-400 hover:bg-white/5 hover:text-slate-100',
                          ].join(' ')
                        }
                      >
                        {({ isActive }) => (
                          <>
                            <span
                              className={[
                                'absolute -left-3 top-1/2 h-4 w-[3px] -translate-y-1/2 rounded-full bg-brand-400 transition-all duration-150',
                                isActive ? 'opacity-100' : 'opacity-0',
                              ].join(' ')}
                              aria-hidden
                            />
                            <span className="relative shrink-0">
                              <Icon
                                className={[
                                  'h-[18px] w-[18px] transition-colors',
                                  isActive ? 'text-brand-400' : 'text-slate-500 group-hover:text-slate-300',
                                ].join(' ')}
                                strokeWidth={2}
                              />
                              {locked && collapsed && (
                                <span
                                  className="absolute -right-1 -top-1 flex h-3 w-3 items-center justify-center rounded-full bg-slate-800 ring-1 ring-slate-950"
                                  aria-label="Requires upgrade"
                                >
                                  <Lock className="h-2 w-2 text-slate-400" strokeWidth={2.5} />
                                </span>
                              )}
                            </span>
                            <span
                              className={[
                                'flex-1 overflow-hidden whitespace-nowrap transition-all duration-200',
                                collapsed ? 'w-0 opacity-0' : 'w-auto opacity-100',
                              ].join(' ')}
                            >
                              {item.label}
                            </span>
                            {locked && !collapsed && (
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

      <div className="border-t border-white/10 p-3">
        <button
          type="button"
          onClick={toggleSidebar}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          className={[
            'flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-slate-400 transition-colors hover:bg-white/5 hover:text-slate-100',
            collapsed ? 'justify-center' : '',
          ].join(' ')}
        >
          {collapsed ? (
            <PanelLeftOpen className="h-[18px] w-[18px] shrink-0" strokeWidth={2} />
          ) : (
            <>
              <PanelLeftClose className="h-[18px] w-[18px] shrink-0" strokeWidth={2} />
              <span className="whitespace-nowrap">Collapse</span>
            </>
          )}
        </button>
      </div>
    </nav>
  )
}
