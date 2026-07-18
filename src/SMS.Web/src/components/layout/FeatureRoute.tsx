import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../auth/authStore'
import type { FeatureModule } from '../../api/types'
import { PaywallPage } from '../../pages/PaywallPage'

interface FeatureRouteProps {
  permission: string
  module: FeatureModule
}

/** Gates a route behind both a permission and a subscription feature module. A user
 * without the permission is bounced to the dashboard, same as PermissionRoute - they
 * can't use this feature regardless of the tenant's plan. A user WITH the permission
 * but whose tenant hasn't got the module switched on sees a Paywall instead of being
 * redirected away, so they can discover the feature and request an upgrade. */
export function FeatureRoute({ permission, module }: FeatureRouteProps) {
  const profile = useAuthStore((s) => s.profile)
  const features = useAuthStore((s) => s.features)
  const hasPermission = useAuthStore((s) => s.hasPermission)
  const hasModule = useAuthStore((s) => s.hasModule)
  const location = useLocation()

  // Profile/features haven't loaded yet (fresh login before they resolve) - wait
  // rather than flashing a Paywall or bouncing the user before we actually know.
  if (!profile || !features) {
    return (
      <div className="flex h-64 items-center justify-center">
        <div className="h-8 w-8 animate-pulse rounded-full bg-slate-200 dark:bg-slate-800" />
      </div>
    )
  }

  if (!hasPermission(permission)) {
    return <Navigate to="/dashboard" replace state={{ from: location }} />
  }

  if (!hasModule(module)) {
    return <PaywallPage module={module} />
  }

  return <Outlet />
}
