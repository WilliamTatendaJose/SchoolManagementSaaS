import { apiClient } from './client'
import type {
  AssignStudentToRouteStopRequest,
  CreateRouteStopRequest,
  CreateTransportRouteRequest,
  RouteStopDto,
  RouteStopOccupantDto,
  TransportRouteDto,
} from './types'

export async function fetchTransportRoutes() {
  const { data } = await apiClient.get<TransportRouteDto[]>('/transport/routes')
  return data
}

export async function createTransportRoute(payload: CreateTransportRouteRequest) {
  const { data } = await apiClient.post<{ id: string }>('/transport/routes', payload)
  return data
}

export async function fetchRouteStops(routeId: string) {
  const { data } = await apiClient.get<RouteStopDto[]>(`/transport/routes/${routeId}/stops`)
  return data
}

export async function createRouteStop(payload: CreateRouteStopRequest) {
  const { data } = await apiClient.post<{ id: string }>('/transport/stops', payload)
  return data
}

export async function fetchStopOccupants(stopId: string) {
  const { data } = await apiClient.get<RouteStopOccupantDto[]>(`/transport/stops/${stopId}/occupants`)
  return data
}

export async function assignStudentToStop(payload: AssignStudentToRouteStopRequest) {
  await apiClient.post('/transport/stops/assign', payload)
}
