import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/client'
import type { UserDetailDto } from '../api/types'
import { useAuthStore } from './authStore'

/**
 * Fetches the current user's profile (roles + permissions), keeping the auth
 * store in sync so permission checks and nav filtering stay current.
 */
export function useProfile() {
  const accessToken = useAuthStore((s) => s.accessToken)
  const setProfile = useAuthStore((s) => s.setProfile)

  return useQuery({
    // Scoped by accessToken (not just a static ['me']) - otherwise switching accounts
    // within the same tab (log out, log back in as someone else, no hard reload) would
    // serve the PREVIOUS user's cached profile for up to staleTime, since react-query
    // has no other way to know the "identity" behind the query changed. This showed up
    // as e.g. a freshly-logged-in SuperAdmin not seeing SuperAdmin-only nav/routes.
    queryKey: ['me', accessToken],
    enabled: !!accessToken,
    queryFn: async () => {
      const { data } = await apiClient.get<UserDetailDto>('/users/me')
      setProfile(data)
      return data
    },
    staleTime: 5 * 60 * 1000,
  })
}
