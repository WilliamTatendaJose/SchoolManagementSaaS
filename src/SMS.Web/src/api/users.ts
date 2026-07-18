import { apiClient } from './client'
import type {
  AssignRolesRequest,
  PaginatedList,
  RegisterUserRequest,
  UpdateUserRequest,
  UserDetailDto,
  UserListDto,
} from './types'

export interface UsersQuery {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  role?: string
  isActive?: boolean
}

export async function fetchUsers(query: UsersQuery) {
  const { data } = await apiClient.get<PaginatedList<UserListDto>>('/users', { params: query })
  return data
}

export async function fetchUser(id: string) {
  const { data } = await apiClient.get<UserDetailDto>(`/users/${id}`)
  return data
}

export async function registerUser(payload: RegisterUserRequest) {
  const { data } = await apiClient.post<{ id: string }>('/users', payload)
  return data
}

export async function updateUser(payload: UpdateUserRequest) {
  await apiClient.put(`/users/${payload.id}`, payload)
}

export async function assignRoles(payload: AssignRolesRequest) {
  await apiClient.put(`/users/${payload.userId}/roles`, payload.roles)
}

export async function resetPassword(id: string) {
  const { data } = await apiClient.post<{ temporaryPassword: string }>(`/users/${id}/reset-password`)
  return data
}
