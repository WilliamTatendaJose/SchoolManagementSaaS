import { apiClient } from './client'
import type { AcademicYearDto, CreateAcademicYearRequest, UpdateAcademicYearRequest } from './types'

export async function fetchAcademicYears() {
  const { data } = await apiClient.get<AcademicYearDto[]>('/academic/years')
  return data
}

export async function createAcademicYear(payload: CreateAcademicYearRequest) {
  const { data } = await apiClient.post<{ id: string }>('/academic/years', payload)
  return data
}

export async function updateAcademicYear(payload: UpdateAcademicYearRequest) {
  await apiClient.put(`/academic/years/${payload.id}`, payload)
}

export async function deleteAcademicYear(id: string) {
  await apiClient.delete(`/academic/years/${id}`)
}
