import { apiClient } from './client'
import type {
  EnrollStudentRequest,
  EnrollmentDto,
  PaginatedList,
  PromoteStudentsRequest,
  PromotionResultDto,
  TransferStudentRequest,
} from './types'

export interface EnrollmentsQuery {
  pageNumber?: number
  pageSize?: number
  academicYearId?: string
  classId?: string
  streamId?: string
  studentId?: string
  isActive?: boolean
  searchTerm?: string
}

export async function fetchEnrollments(query: EnrollmentsQuery) {
  const { data } = await apiClient.get<PaginatedList<EnrollmentDto>>('/enrollments', { params: query })
  return data
}

export async function fetchStudentEnrollments(studentId: string) {
  const { data } = await apiClient.get<EnrollmentDto[]>(`/enrollments/students/${studentId}`)
  return data
}

export async function enrollStudent(payload: EnrollStudentRequest) {
  const { data } = await apiClient.post<{ id: string }>('/enrollments', payload)
  return data
}

export async function transferStudent(payload: TransferStudentRequest) {
  await apiClient.post('/enrollments/transfer', payload)
}

export async function withdrawEnrollment(id: string) {
  await apiClient.post(`/enrollments/${id}/withdraw`)
}

export async function promoteStudents(payload: PromoteStudentsRequest) {
  const { data } = await apiClient.post<PromotionResultDto>('/enrollments/promote', payload)
  return data
}
