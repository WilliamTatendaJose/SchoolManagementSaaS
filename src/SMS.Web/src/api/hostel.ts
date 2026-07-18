import { apiClient } from './client'
import type {
  AssignDormitoryRequest,
  AssignHouseRequest,
  CreateDormitoryRequest,
  CreateHouseRequest,
  DormitoryDto,
  DormitoryOccupantDto,
  HouseDto,
  PaginatedList,
  RecordLeaveDepartureRequest,
  RecordLeaveReturnRequest,
  RequestWeekendLeaveRequest,
  ReviewWeekendLeaveRequest,
  WeekendLeaveDto,
} from './types'

export async function fetchDormitories() {
  const { data } = await apiClient.get<DormitoryDto[]>('/hostel/dormitories')
  return data
}

export async function createDormitory(payload: CreateDormitoryRequest) {
  const { data } = await apiClient.post<{ id: string }>('/hostel/dormitories', payload)
  return data
}

export async function fetchDormitoryOccupants(dormitoryId: string) {
  const { data } = await apiClient.get<DormitoryOccupantDto[]>(`/hostel/dormitories/${dormitoryId}/occupants`)
  return data
}

export async function assignDormitory(payload: AssignDormitoryRequest) {
  await apiClient.post('/hostel/dormitories/assign', payload)
}

export async function fetchHouses() {
  const { data } = await apiClient.get<HouseDto[]>('/hostel/houses')
  return data
}

export async function createHouse(payload: CreateHouseRequest) {
  const { data } = await apiClient.post<{ id: string }>('/hostel/houses', payload)
  return data
}

export async function assignHouse(payload: AssignHouseRequest) {
  await apiClient.post('/hostel/houses/assign', payload)
}

export interface WeekendLeavesQuery {
  studentId?: string
  status?: string
  fromDate?: string
  toDate?: string
  pageNumber?: number
  pageSize?: number
}

export async function fetchWeekendLeaves(query: WeekendLeavesQuery) {
  const { data } = await apiClient.get<PaginatedList<WeekendLeaveDto>>('/hostel/leaves', { params: query })
  return data
}

export async function requestWeekendLeave(payload: RequestWeekendLeaveRequest) {
  const { data } = await apiClient.post<{ id: string }>('/hostel/leaves', payload)
  return data
}

export async function reviewWeekendLeave(payload: ReviewWeekendLeaveRequest) {
  await apiClient.post(`/hostel/leaves/${payload.leaveId}/review`, payload)
}

export async function recordLeaveDeparture(payload: RecordLeaveDepartureRequest) {
  await apiClient.post(`/hostel/leaves/${payload.leaveId}/depart`, payload)
}

export async function recordLeaveReturn(payload: RecordLeaveReturnRequest) {
  await apiClient.post(`/hostel/leaves/${payload.leaveId}/return`, payload)
}
