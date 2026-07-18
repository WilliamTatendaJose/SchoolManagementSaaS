import { apiClient } from './client'
import type { AttendanceDto, AttendanceSummaryDto, MarkAttendanceRequest } from './types'

export async function fetchClassAttendance(classId: string, date: string) {
  const { data } = await apiClient.get<AttendanceDto[]>(`/attendance/class/${classId}`, { params: { date } })
  return data
}

export async function markAttendance(payload: MarkAttendanceRequest) {
  const { data } = await apiClient.post<{ message: string }>('/attendance', payload)
  return data
}

export async function fetchStudentAttendanceSummary(studentId: string, termId?: string) {
  const { data } = await apiClient.get<AttendanceSummaryDto>(`/attendance/student/${studentId}/summary`, {
    params: termId ? { termId } : undefined,
  })
  return data
}
