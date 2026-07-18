import { apiClient } from './client'
import type { CreateRoleRequest, PermissionGroupDto, RoleDetailDto, UpdateRolePermissionsRequest } from './types'

export async function fetchRoles() {
  const { data } = await apiClient.get<RoleDetailDto[]>('/roles')
  return data
}

export async function fetchPermissions(module?: string) {
  const { data } = await apiClient.get<PermissionGroupDto[]>('/roles/permissions', {
    params: module ? { module } : undefined,
  })
  return data
}

export async function createRole(payload: CreateRoleRequest) {
  const { data } = await apiClient.post<{ id: string }>('/roles', payload)
  return data
}

export async function updateRolePermissions(payload: UpdateRolePermissionsRequest) {
  await apiClient.put(`/roles/${payload.roleId}/permissions`, payload.permissionCodes)
}
