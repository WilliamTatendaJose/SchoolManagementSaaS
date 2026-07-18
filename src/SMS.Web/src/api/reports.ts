import { apiClient } from './client'
import type { DefaulterDto, ReconciliationDto } from './types'

export async function fetchDefaulters(classId?: string) {
  const { data } = await apiClient.get<DefaulterDto[]>('/finance/reports/defaulters', {
    params: classId ? { classId } : undefined,
  })
  return data
}

export async function fetchReconciliation(fromDate: string, toDate: string) {
  const { data } = await apiClient.get<ReconciliationDto>('/finance/reports/reconciliation', {
    params: { fromDate, toDate },
  })
  return data
}

export async function downloadReceipt(paymentId: string, receiptNumber: string) {
  const response = await apiClient.get(`/finance/reports/receipts/${paymentId}`, { responseType: 'blob' })
  const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `${receiptNumber}.pdf`
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}
