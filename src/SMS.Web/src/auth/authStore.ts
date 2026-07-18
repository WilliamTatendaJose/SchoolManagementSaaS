import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { FeatureModule, TenantFeaturesDto, UserDetailDto, UserInfo } from '../api/types'

const MODULE_FLAG_KEYS: Record<FeatureModule, keyof TenantFeaturesDto> = {
  lms: 'hasLmsModule',
  transport: 'hasTransportModule',
  hostel: 'hasHostelModule',
  library: 'hasLibraryModule',
}

interface AuthState {
  tenantId: string | null
  tenantName: string | null
  accessToken: string | null
  refreshToken: string | null
  user: UserInfo | null
  profile: UserDetailDto | null
  features: TenantFeaturesDto | null
  setTenant: (id: string, name: string) => void
  setSession: (accessToken: string, refreshToken: string, user?: UserInfo) => void
  setProfile: (profile: UserDetailDto) => void
  setFeatures: (features: TenantFeaturesDto) => void
  logout: () => void
  hasPermission: (permission: string) => boolean
  hasRole: (role: string) => boolean
  hasModule: (module: FeatureModule) => boolean
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
      features: null,

      setTenant: (id, name) => set({ tenantId: id, tenantName: name }),

      setSession: (accessToken, refreshToken, user) =>
        set((state) => ({
          accessToken,
          refreshToken,
          user: user ?? state.user,
        })),

      setProfile: (profile) => set({ profile }),

      setFeatures: (features) => set({ features }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          user: null,
          profile: null,
          features: null,
        }),

      hasPermission: (permission) => {
        const { profile } = get()
        return profile?.permissions.includes(permission) ?? false
      },

      hasRole: (role) => {
        const { profile } = get()
        return profile?.roles.some((r) => r.name === role) ?? false
      },

      hasModule: (module) => {
        const { features } = get()
        if (!features) return false
        return features[MODULE_FLAG_KEYS[module]] === true
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
        features: state.features,
      }),
    },
  ),
)
