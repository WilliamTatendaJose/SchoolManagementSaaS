import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '../auth/authStore'
import type { LoginResponse } from './types'

export const apiClient = axios.create({ baseURL: '/api' })

// Plain client with no interceptors, used for the refresh call itself so it can't
// recursively trigger another refresh.
const refreshClient = axios.create({ baseURL: '/api' })

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState()
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const { accessToken, refreshToken, setSession, logout } = useAuthStore.getState()
  if (!refreshToken || !accessToken) {
    return null
  }

  try {
    const { data } = await refreshClient.post<LoginResponse>('/auth/refresh', {
      accessToken,
      refreshToken,
    })
    setSession(data.accessToken, data.refreshToken)
    return data.accessToken
  } catch {
    logout()
    return null
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined

    if (error.response?.status !== 401 || !original || original._retried) {
      return Promise.reject(error)
    }

    original._retried = true

    // Multiple requests can 401 at once; share a single in-flight refresh.
    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null
    })

    const newToken = await refreshPromise
    if (!newToken) {
      return Promise.reject(error)
    }

    original.headers.set('Authorization', `Bearer ${newToken}`)
    return apiClient(original)
  },
)
