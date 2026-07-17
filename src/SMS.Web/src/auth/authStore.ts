import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserDetailDto, UserInfo } from '../api/types'

interface AuthState {
  tenantId: string | null
  tenantName: string | null
  accessToken: string | null
  refreshToken: string | null
  user: UserInfo | null
  profile: UserDetailDto | null
  setTenant: (id: string, name: string) => void
  setSession: (accessToken: string, refreshToken: string, user?: UserInfo) => void
  setProfile: (profile: UserDetailDto) => void
  logout: () => void
  hasPermission: (permission: string) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      tenantId: null,
      tenantName: null,
      accessToken: null,
      refreshToken: null,
      user: null,
      profile: null,

      setTenant: (id, name) => set({ tenantId: id, tenantName: name }),

      setSession: (accessToken, refreshToken, user) =>
        set((state) => ({
          accessToken,
          refreshToken,
          user: user ?? state.user,
        })),

      setProfile: (profile) => set({ profile }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          user: null,
          profile: null,
        }),

      hasPermission: (permission) => {
        const { profile } = get()
        return profile?.permissions.includes(permission) ?? false
      },
    }),
    {
      name: 'sms-auth',
      partialize: (state) => ({
        tenantId: state.tenantId,
        tenantName: state.tenantName,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
        profile: state.profile,
      }),
    },
  ),
)
