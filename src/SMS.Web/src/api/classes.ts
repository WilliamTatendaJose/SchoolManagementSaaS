import { apiClient } from './client'
import type { ClassDto, CreateClassRequest, UpdateClassRequest } from './types'

export async function fetchClasses() {
  const { data } = await apiClient.get<ClassDto[]>('/academic/classes')
  return data
}

export async function createClass(payload: CreateClassRequest) {
  const { data } = await apiClient.post<{ id: string }>('/academic/classes', payload)
  return data
}

export async function updateClass(payload: UpdateClassRequest) {
  await apiClient.put(`/academic/classes/${payload.id}`, payload)
}

export async function deleteClass(id: string) {
  await apiClient.delete(`/academic/classes/${id}`)
}
