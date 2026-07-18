import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'

interface PermissionRouteProps {
  permission: string
}

/** Gates nested routes behind a permission, mirroring the sidebar's own filtering
 * so a user can't reach a page directly by URL that they can't see in nav. */
export function PermissionRoute({ permission }: PermissionRouteProps) {
  const profile = useAuthStore((s) => s.profile)
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const location = useLocation()

  // Profile hasn't loaded yet (fresh login before /users/me resolves) — wait
  // rather than bouncing the user before we actually know their permissions.
  if (!profile) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="h-8 w-8 animate-pulse rounded-full bg-slate-200 dark:bg-slate-800" />
      </div>
    )
  }

  if (!hasPermission(permission)) {
    return <Navigate to="/dashboard" replace state={{ from: location }} />
  }

  return <Outlet />
}
