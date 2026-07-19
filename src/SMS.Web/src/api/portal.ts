import { apiClient } from './client'
import type {
  AcademicTermDto,
  AttendanceSummaryDto,
  MyChildDto,
  MyChildFinanceDto,
  OnlinePaymentInitiationDto,
  PaymentSettlementDto,
  StudentAcademicResultsDto,
} from './types'

export async function fetchMyChildren() {
  const { data } = await apiClient.get<MyChildDto[]>('/portal/children')
  return data
}

export async function fetchPortalTerms() {
  const { data } = await apiClient.get<AcademicTermDto[]>('/portal/terms')
  return data
}

export async function fetchChildFinance(studentId: string) {
  const { data } = await apiClient.get<MyChildFinanceDto>(`/portal/children/${studentId}/finance`)
  return data
}

export async function fetchChildResults(studentId: string, termId?: string) {
  const { data } = await apiClient.get<StudentAcademicResultsDto>(`/portal/children/${studentId}/results`, {
    params: termId ? { termId } : undefined,
  })
  return data
}

export async function fetchChildAttendance(studentId: string, termId?: string) {
  const { data } = await apiClient.get<AttendanceSummaryDto>(`/portal/children/${studentId}/attendance`, {
    params: termId ? { termId } : undefined,
  })
  return data
}

export async function downloadChildReportCard(studentId: string, termId: string, studentNumber: string) {
  const response = await apiClient.get(`/portal/children/${studentId}/report-card`, {
    params: { termId },
    responseType: 'blob',
  })
  const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `ReportCard_${studentNumber}.pdf`
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}

export async function initiateChildPayment(invoiceId: string, email?: string, phone?: string) {
  const { data } = await apiClient.post<OnlinePaymentInitiationDto>('/portal/payments/online/initiate', {
    invoiceId,
    email,
    phone,
  })
  return data
}

export async function checkChildPaymentStatus(paymentId: string) {
  const { data } = await apiClient.post<PaymentSettlementDto>(`/portal/payments/${paymentId}/status`)
  return data
}
