import { apiClient } from './client'
import type {
  CreateInvoiceRequest,
  FinancePage,
  FinanceSummaryDto,
  GenerateInvoicesRequest,
  InvoiceDetail,
  InvoiceDto,
  InvoiceGenerationResultDto,
  OnlinePaymentInitiationDto,
  PaymentDto,
  PaymentSettlementDto,
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

/** Starts a Paynow (EcoCash/OneMoney/card) collection against an invoice from the
 * cashier desk. Returns the poll URL and, for card/web, a redirect URL; for mobile money
 * Paynow pushes a prompt to the payer's phone and the cashier polls for confirmation. */
export async function initiateOnlinePayment(payload: {
  invoiceId: string
  amount?: number
  email?: string
  phone?: string
}) {
  const { data } = await apiClient.post<OnlinePaymentInitiationDto>('/payments/online/initiate', payload)
  return data
}

export async function checkPaymentStatus(paymentId: string) {
  const { data } = await apiClient.post<PaymentSettlementDto>(`/payments/${paymentId}/status`)
  return data
}

export async function downloadInvoicePdf(invoiceId: string, invoiceNumber: string) {
  const response = await apiClient.get(`/finance/reports/invoices/${invoiceId}`, { responseType: 'blob' })
  const safe = invoiceNumber.replace(/[^a-zA-Z0-9-_]/g, '_')
  const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `Invoice_${safe}.pdf`
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
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
