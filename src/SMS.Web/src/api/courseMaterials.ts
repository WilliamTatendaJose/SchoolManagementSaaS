import { apiClient } from './client'
import type { CourseMaterialDto } from './types'

export interface CourseMaterialsQuery {
  classId?: string
  subjectId?: string
  termId?: string
}

export async function fetchCourseMaterials(query: CourseMaterialsQuery = {}) {
  const { data } = await apiClient.get<CourseMaterialDto[]>('/coursematerials', { params: query })
  return data
}

export interface CreateCourseMaterialPayload {
  classId: string
  subjectId: string
  academicTermId?: string
  title: string
  description?: string
  url?: string
  attachment?: File | null
}

export async function createCourseMaterial(payload: CreateCourseMaterialPayload) {
  const form = new FormData()
  form.append('ClassId', payload.classId)
  form.append('SubjectId', payload.subjectId)
  if (payload.academicTermId) form.append('AcademicTermId', payload.academicTermId)
  form.append('Title', payload.title)
  if (payload.description) form.append('Description', payload.description)
  if (payload.url) form.append('Url', payload.url)
  if (payload.attachment) form.append('Attachment', payload.attachment)

  const { data } = await apiClient.post<{ id: string }>('/coursematerials', form)
  return data
}

export async function deleteCourseMaterial(id: string) {
  await apiClient.delete(`/coursematerials/${id}`)
}
