import { apiClient } from './client'
import type { CreateFeeStructureRequest, FeeStructureDto, UpdateFeeStructureRequest } from './types'

export interface FeeStructuresQuery {
  classId?: string
  academicYearId?: string
}

export async function fetchFeeStructures(query: FeeStructuresQuery = {}) {
  const { data } = await apiClient.get<FeeStructureDto[]>('/fees/structures', { params: query })
  return data
}

export async function createFeeStructure(payload: CreateFeeStructureRequest) {
  const { data } = await apiClient.post<{ id: string }>('/fees/structures', payload)
  return data
}

export async function updateFeeStructure(payload: UpdateFeeStructureRequest) {
  await apiClient.put(`/fees/structures/${payload.id}`, payload)
}

export async function deleteFeeStructure(id: string) {
  await apiClient.delete(`/fees/structures/${id}`)
}
