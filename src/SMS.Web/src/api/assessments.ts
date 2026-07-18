import { apiClient } from './client'
import type {
  AssessmentDto,
  AssessmentResultsDto,
  CreateAssessmentRequest,
  StudentResultInput,
} from './types'

export interface AssessmentsQuery {
  classId?: string
  subjectId?: string
  termId?: string
  assessmentType?: string
}

export async function fetchAssessments(query: AssessmentsQuery = {}) {
  const { data } = await apiClient.get<AssessmentDto[]>('/academic/assessments', { params: query })
  return data
}

export async function createAssessment(payload: CreateAssessmentRequest) {
  const { data } = await apiClient.post<{ id: string }>('/academic/assessments', payload)
  return data
}

export async function fetchAssessmentResults(id: string) {
  const { data } = await apiClient.get<AssessmentResultsDto>(`/academic/assessments/${id}/results`)
  return data
}

export async function recordResults(assessmentId: string, results: StudentResultInput[]) {
  const { data } = await apiClient.post<{ recordedCount: number }>(
    `/academic/assessments/${assessmentId}/results`,
    results,
  )
  return data
}

export async function publishResults(assessmentId: string) {
  const { data } = await apiClient.post<{ message: string }>(`/academic/assessments/${assessmentId}/publish`)
  return data
}
