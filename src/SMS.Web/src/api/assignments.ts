import { apiClient } from './client'
import type { AssignmentDto, AssignmentSubmissionDto, GradeSubmissionRequest } from './types'

export interface AssignmentsQuery {
  classId?: string
  subjectId?: string
  termId?: string
}

export async function fetchAssignments(query: AssignmentsQuery = {}) {
  const { data } = await apiClient.get<AssignmentDto[]>('/assignments', { params: query })
  return data
}

export interface CreateAssignmentPayload {
  classId: string
  subjectId: string
  academicTermId: string
  title: string
  description?: string
  dueDate: string
  attachment?: File | null
}

export async function createAssignment(payload: CreateAssignmentPayload) {
  const form = new FormData()
  form.append('ClassId', payload.classId)
  form.append('SubjectId', payload.subjectId)
  form.append('AcademicTermId', payload.academicTermId)
  form.append('Title', payload.title)
  if (payload.description) form.append('Description', payload.description)
  form.append('DueDate', payload.dueDate)
  if (payload.attachment) form.append('Attachment', payload.attachment)

  const { data } = await apiClient.post<{ id: string }>('/assignments', form)
  return data
}

export async function fetchSubmissions(assignmentId: string) {
  const { data } = await apiClient.get<AssignmentSubmissionDto[]>(`/assignments/${assignmentId}/submissions`)
  return data
}

export interface RecordSubmissionPayload {
  assignmentId: string
  studentId: string
  comment?: string
  attachment?: File | null
}

export async function recordSubmission(payload: RecordSubmissionPayload) {
  const form = new FormData()
  form.append('StudentId', payload.studentId)
  if (payload.comment) form.append('Comment', payload.comment)
  if (payload.attachment) form.append('Attachment', payload.attachment)

  const { data } = await apiClient.post<{ id: string }>(`/assignments/${payload.assignmentId}/submissions`, form)
  return data
}

export async function gradeSubmission(payload: GradeSubmissionRequest) {
  await apiClient.post(`/assignments/submissions/${payload.submissionId}/grade`, payload)
}
