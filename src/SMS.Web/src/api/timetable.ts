import { apiClient } from './client'
import type {
  ClassroomDto,
  CreateClassroomRequest,
  CreateTimetableSlotRequest,
  TimetableSlotDto,
  UpdateClassroomRequest,
  UpdateTimetableSlotRequest,
} from './types'

export async function fetchClassrooms(activeOnly?: boolean) {
  const { data } = await apiClient.get<ClassroomDto[]>('/timetable/classrooms', {
    params: activeOnly ? { activeOnly } : undefined,
  })
  return data
}

export async function createClassroom(payload: CreateClassroomRequest) {
  const { data } = await apiClient.post<{ id: string }>('/timetable/classrooms', payload)
  return data
}

export async function updateClassroom(payload: UpdateClassroomRequest) {
  await apiClient.put(`/timetable/classrooms/${payload.id}`, payload)
}

export interface TimetableQuery {
  academicTermId: string
  classId?: string
  teacherId?: string
  classroomId?: string
}

export async function fetchTimetable(query: TimetableQuery) {
  const { data } = await apiClient.get<TimetableSlotDto[]>('/timetable', { params: query })
  return data
}

export async function createTimetableSlot(payload: CreateTimetableSlotRequest) {
  const { data } = await apiClient.post<{ id: string }>('/timetable/slots', payload)
  return data
}

export async function updateTimetableSlot(payload: UpdateTimetableSlotRequest) {
  await apiClient.put(`/timetable/slots/${payload.id}`, payload)
}

export async function deleteTimetableSlot(id: string) {
  await apiClient.delete(`/timetable/slots/${id}`)
}
