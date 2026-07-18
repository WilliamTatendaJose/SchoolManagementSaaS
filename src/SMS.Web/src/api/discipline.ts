import { apiClient } from './client'
import type { CreateDisciplineRecordRequest, DisciplineRecordDto, PaginatedList } from './types'

export interface DisciplineQuery {
  pageNumber?: number
  pageSize?: number
  studentId?: string
  incidentType?: string
}

export async function fetchDisciplineRecords(query: DisciplineQuery) {
  const { data } = await apiClient.get<PaginatedList<DisciplineRecordDto>>('/discipline', { params: query })
  return data
}

export async function createDisciplineRecord(payload: CreateDisciplineRecordRequest) {
  const { data } = await apiClient.post<{ id: string }>('/discipline', payload)
  return data
}
