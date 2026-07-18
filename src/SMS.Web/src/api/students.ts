import { apiClient } from './client'
import type {
  CreateStudentRequest,
  PaginatedList,
  StudentDetailDto,
  StudentDto,
  UpdateStudentRequest,
} from './types'

export interface StudentsQuery {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  classId?: string
  status?: string
}

export async function fetchStudents(query: StudentsQuery) {
  const { data } = await apiClient.get<PaginatedList<StudentDto>>('/students', { params: query })
  return data
}

export async function fetchStudent(id: string) {
  const { data } = await apiClient.get<StudentDetailDto>(`/students/${id}`)
  return data
}

export async function createStudent(payload: CreateStudentRequest) {
  const { data } = await apiClient.post<{ id: string }>('/students', payload)
  return data
}

export async function updateStudent(payload: UpdateStudentRequest) {
  await apiClient.put(`/students/${payload.id}`, payload)
}

export async function deleteStudent(id: string) {
  await apiClient.delete(`/students/${id}`)
}
