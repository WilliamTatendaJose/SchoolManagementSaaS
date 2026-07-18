import { apiClient } from './client'
import type {
  MessageDetailDto,
  MessageDispatchResultDto,
  MessageListDto,
  PaginatedList,
  SendFeeRemindersRequest,
  SendMessageRequest,
} from './types'

export interface MessagesQuery {
  pageNumber?: number
  pageSize?: number
  channel?: string
  messageType?: string
  status?: string
}

export async function fetchMessages(query: MessagesQuery) {
  const { data } = await apiClient.get<PaginatedList<MessageListDto>>('/messages', { params: query })
  return data
}

export async function fetchMessage(id: string) {
  const { data } = await apiClient.get<MessageDetailDto>(`/messages/${id}`)
  return data
}

export async function sendMessage(payload: SendMessageRequest) {
  const { data } = await apiClient.post<MessageDispatchResultDto>('/messages/send', payload)
  return data
}

export async function sendFeeReminders(payload: SendFeeRemindersRequest) {
  const { data } = await apiClient.post<MessageDispatchResultDto>('/messages/fee-reminders', payload)
  return data
}
