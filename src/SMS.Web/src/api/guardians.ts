import { apiClient } from './client'
import type {
  CreateGuardianRequest,
  GuardianDetailDto,
  GuardianListDto,
  LinkGuardianToStudentRequest,
  PaginatedList,
  UpdateGuardianRequest,
} from './types'

export interface GuardiansQuery {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  studentId?: string
}

export async function fetchGuardians(query: GuardiansQuery) {
  const { data } = await apiClient.get<PaginatedList<GuardianListDto>>('/guardians', { params: query })
  return data
}

export async function fetchGuardian(id: string) {
  const { data } = await apiClient.get<GuardianDetailDto>(`/guardians/${id}`)
  return data
}

export async function createGuardian(payload: CreateGuardianRequest) {
  const { data } = await apiClient.post<{ id: string }>('/guardians', payload)
  return data
}

export async function updateGuardian(payload: UpdateGuardianRequest) {
  await apiClient.put(`/guardians/${payload.id}`, payload)
}

export async function deleteGuardian(id: string) {
  await apiClient.delete(`/guardians/${id}`)
}

export async function linkGuardianToStudent(payload: LinkGuardianToStudentRequest) {
  const { data } = await apiClient.post<{ id: string }>(`/guardians/${payload.guardianId}/students`, payload)
  return data
}

export async function unlinkGuardianFromStudent(guardianId: string, studentId: string) {
  await apiClient.delete(`/guardians/${guardianId}/students/${studentId}`)
}
