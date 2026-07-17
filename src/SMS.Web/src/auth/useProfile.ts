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
    queryKey: ['me'],
    enabled: !!accessToken,
    queryFn: async () => {
      const { data } = await apiClient.get<UserDetailDto>('/users/me')
      setProfile(data)
      return data
    },
    staleTime: 5 * 60 * 1000,
  })
}
