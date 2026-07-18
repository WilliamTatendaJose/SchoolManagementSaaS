import { apiClient } from './client'
import type {
  CreateInvoiceRequest,
  FinancePage,
  FinanceSummaryDto,
  GenerateInvoicesRequest,
  InvoiceDetail,
  InvoiceDto,
  InvoiceGenerationResultDto,
  PaymentDto,
  RecordPaymentRequest,
  RecordPaymentResult,
  StudentAccountBalanceDto,
  StudentAccountTransactionDto,
} from './types'

export interface InvoicesQuery {
  studentId?: string
  termId?: string
  unpaidOnly?: boolean
  page?: number
  pageSize?: number
}

export async function fetchInvoices(query: InvoicesQuery) {
  const { data } = await apiClient.get<FinancePage<InvoiceDto>>('/finance/invoices', { params: query })
  return data
}

export async function fetchInvoice(id: string) {
  const { data } = await apiClient.get<InvoiceDetail>(`/finance/invoices/${id}`)
  return data
}

export async function createInvoice(payload: CreateInvoiceRequest) {
  const { data } = await apiClient.post<{ id: string }>('/finance/invoices', payload)
  return data
}

export async function generateInvoices(payload: GenerateInvoicesRequest) {
  const { data } = await apiClient.post<InvoiceGenerationResultDto>('/fees/invoices/generate', payload)
  return data
}

export async function recordPayment(payload: RecordPaymentRequest) {
  const { data } = await apiClient.post<RecordPaymentResult>('/finance/payments', payload)
  return data
}

export async function fetchAccountBalance(studentId: string) {
  const { data } = await apiClient.get<StudentAccountBalanceDto>(`/finance/students/${studentId}/account-balance`)
  return data
}

export async function fetchAccountTransactions(studentId: string) {
  const { data } = await apiClient.get<StudentAccountTransactionDto[]>(
    `/finance/students/${studentId}/account-transactions`,
  )
  return data
}

export interface PaymentsQuery {
  studentId?: string
  fromDate?: string
  toDate?: string
  page?: number
  pageSize?: number
}

export async function fetchPayments(query: PaymentsQuery) {
  const { data } = await apiClient.get<FinancePage<PaymentDto>>('/finance/payments', { params: query })
  return data
}

export async function fetchFinanceSummary(termId?: string) {
  const { data } = await apiClient.get<FinanceSummaryDto>('/finance/summary', {
    params: termId ? { termId } : undefined,
  })
  return data
}
