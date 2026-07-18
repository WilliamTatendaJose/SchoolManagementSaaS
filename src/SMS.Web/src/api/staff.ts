import { apiClient } from './client'
import type {
  AssignSubjectRequest,
  CreateStaffRequest,
  PaginatedList,
  StaffDetailDto,
  StaffListDto,
  UpdateStaffRequest,
} from './types'

export interface StaffQuery {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  isTeacher?: boolean
  isActive?: boolean
  department?: string
}

export async function fetchStaff(query: StaffQuery) {
  const { data } = await apiClient.get<PaginatedList<StaffListDto>>('/staff', { params: query })
  return data
}

export async function fetchStaffMember(id: string) {
  const { data } = await apiClient.get<StaffDetailDto>(`/staff/${id}`)
  return data
}

export async function createStaff(payload: CreateStaffRequest) {
  const { data } = await apiClient.post<{ id: string }>('/staff', payload)
  return data
}

export async function updateStaff(payload: UpdateStaffRequest) {
  await apiClient.put(`/staff/${payload.id}`, payload)
}

export async function deleteStaff(id: string) {
  await apiClient.delete(`/staff/${id}`)
}

export async function assignSubjectToTeacher(payload: AssignSubjectRequest) {
  const { data } = await apiClient.post<{ id: string }>(`/staff/${payload.staffId}/subjects`, payload)
  return data
}

export async function removeSubjectFromTeacher(staffId: string, subjectId: string) {
  await apiClient.delete(`/staff/${staffId}/subjects/${subjectId}`)
}
