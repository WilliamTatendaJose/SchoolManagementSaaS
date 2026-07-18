import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'

interface RoleRouteProps {
  role: string
}

/** Gates nested routes behind role membership, for platform-level areas that the backend
 * itself checks by role rather than permission (e.g. TenantsController is
 * [Authorize(Roles = "SuperAdmin")], not permission-gated). */
export function RoleRoute({ role }: RoleRouteProps) {
  const profile = useAuthStore((s) => s.profile)
  const hasRole = useAuthStore((s) => s.hasRole)
  const location = useLocation()

  if (!profile) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="h-8 w-8 animate-pulse rounded-full bg-slate-200 dark:bg-slate-800" />
      </div>
    )
  }

  if (!hasRole(role)) {
    return <Navigate to="/dashboard" replace state={{ from: location }} />
  }

  return <Outlet />
}
