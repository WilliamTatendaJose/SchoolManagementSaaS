import { apiClient } from './client'
import type { CreateSubjectRequest, SubjectDto, UpdateSubjectRequest } from './types'

export async function fetchSubjects(activeOnly?: boolean) {
  const { data } = await apiClient.get<SubjectDto[]>('/academic/subjects', {
    params: activeOnly ? { activeOnly } : undefined,
  })
  return data
}

export async function createSubject(payload: CreateSubjectRequest) {
  const { data } = await apiClient.post<{ id: string }>('/academic/subjects', payload)
  return data
}

export async function updateSubject(payload: UpdateSubjectRequest) {
  await apiClient.put(`/academic/subjects/${payload.id}`, payload)
}

export async function deleteSubject(id: string) {
  await apiClient.delete(`/academic/subjects/${id}`)
}
