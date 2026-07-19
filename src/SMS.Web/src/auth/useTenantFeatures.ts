import { useQuery } from '@tanstack/react-query'
import { fetchTenantFeatures } from '../api/settings'
import { useAuthStore } from './authStore'

/**
 * Fetches which subscription plan/feature modules are switched on for the current
 * tenant, keeping the auth store in sync so nav filtering and route gating stay current.
 * Every authenticated user needs this (not just settings.view holders) to know which
 * optional modules (Transport, Library, Boarding, LMS) they're allowed to see.
 */
export function useTenantFeatures() {
  const accessToken = useAuthStore((s) => s.accessToken)
  const setFeatures = useAuthStore((s) => s.setFeatures)

  return useQuery({
    // Scoped by accessToken - see the matching comment in useProfile.ts for why a
    // static key would leak a previous session's cached data across an account switch.
    queryKey: ['tenant-features', accessToken],
    enabled: !!accessToken,
    queryFn: async () => {
      const data = await fetchTenantFeatures()
      setFeatures(data)
      return data
    },
    staleTime: 5 * 60 * 1000,
  })
}
