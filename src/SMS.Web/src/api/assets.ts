import { apiClient } from './client'
import type { AssetDto, CreateAssetRequest, PaginatedList, UpdateAssetRequest } from './types'

export interface AssetsQuery {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  category?: string
  condition?: string
  isActive?: boolean
}

export async function fetchAssets(query: AssetsQuery) {
  const { data } = await apiClient.get<PaginatedList<AssetDto>>('/assets', { params: query })
  return data
}

export async function createAsset(payload: CreateAssetRequest) {
  const { data } = await apiClient.post<{ id: string }>('/assets', payload)
  return data
}

export async function updateAsset(payload: UpdateAssetRequest) {
  await apiClient.put(`/assets/${payload.id}`, payload)
}
