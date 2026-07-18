import { apiClient } from './client'
import type { AcademicTermDto } from './types'

export async function fetchAcademicTerms(academicYearId?: string) {
  const { data } = await apiClient.get<AcademicTermDto[]>('/academic/terms', {
    params: academicYearId ? { academicYearId } : undefined,
  })
  return data
}
