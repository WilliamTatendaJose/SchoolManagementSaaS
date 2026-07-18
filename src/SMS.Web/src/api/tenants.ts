import { apiClient } from './client'
import type {
  CreateTenantRequest,
  TenantDto,
  UpdateTenantModulesRequest,
  UpdateTenantStatusRequest,
  UpdateTenantSubscriptionRequest,
} from './types'

export interface TenantsPage {
  data: TenantDto[]
  total: number
  page: number
  pageSize: number
}

export async function fetchTenants(page: number, pageSize: number) {
  const { data } = await apiClient.get<TenantsPage>('/tenants', { params: { page, pageSize } })
  return data
}

export async function createTenant(payload: CreateTenantRequest) {
  const { data } = await apiClient.post<{ id: string }>('/tenants', payload)
  return data
}

export async function updateTenantStatus(id: string, payload: UpdateTenantStatusRequest) {
  await apiClient.patch(`/tenants/${id}/status`, payload)
}

export async function updateTenantSubscription(id: string, payload: UpdateTenantSubscriptionRequest) {
  await apiClient.put(`/tenants/${id}/subscription`, payload)
}

export async function updateTenantModules(id: string, payload: UpdateTenantModulesRequest) {
  await apiClient.put(`/tenants/${id}/modules`, payload)
}
