import { NavLink } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import { navItems } from './navConfig'

export function Sidebar() {
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const profile = useAuthStore((s) => s.profile)

  const visibleItems = navItems.filter(
    (item) => !item.permission || hasPermission(item.permission),
  )

  return (
    <nav className="flex h-full w-60 shrink-0 flex-col border-r border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <div className="px-4 py-5">
        <p className="text-lg font-semibold text-slate-900 dark:text-white">
          {useAuthStore.getState().tenantName ?? 'School SMS'}
        </p>
        {profile && (
          <p className="mt-1 truncate text-xs text-slate-500 dark:text-slate-400">
            {profile.fullName}
          </p>
        )}
      </div>
      <ul className="flex-1 space-y-0.5 overflow-y-auto px-2 pb-4">
        {visibleItems.map((item) => (
          <li key={item.path}>
            <NavLink
              to={item.path}
              className={({ isActive }) =>
                [
                  'block rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-sky-100 text-sky-900 dark:bg-sky-900/40 dark:text-sky-100'
                    : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
                ].join(' ')
              }
            >
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
