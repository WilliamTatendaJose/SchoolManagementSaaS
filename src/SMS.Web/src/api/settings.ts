import { apiClient } from './client'
import type { SchoolSettingsDto, TenantFeaturesDto, UpdateSchoolSettingsRequest } from './types'

export async function fetchSettings() {
  const { data } = await apiClient.get<SchoolSettingsDto>('/settings')
  return data
}

export async function updateSettings(payload: UpdateSchoolSettingsRequest) {
  await apiClient.put('/settings', payload)
}

export async function fetchTenantFeatures() {
  const { data } = await apiClient.get<TenantFeaturesDto>('/settings/features')
  return data
}
