import { apiClient } from './client'
import type { BookDto, BookLoanDto, BorrowBookRequest, CreateBookRequest, ReturnBookRequest } from './types'

export async function fetchBooks(search?: string) {
  const { data } = await apiClient.get<BookDto[]>('/library/books', { params: search ? { search } : undefined })
  return data
}

export async function createBook(payload: CreateBookRequest) {
  const { data } = await apiClient.post<{ id: string }>('/library/books', payload)
  return data
}

export async function fetchOverdueLoans() {
  const { data } = await apiClient.get<BookLoanDto[]>('/library/loans/overdue')
  return data
}

export async function fetchStudentLoans(studentId: string) {
  const { data } = await apiClient.get<BookLoanDto[]>(`/library/students/${studentId}/loans`)
  return data
}

export async function borrowBook(payload: BorrowBookRequest) {
  const { data } = await apiClient.post<{ id: string }>('/library/loans/borrow', payload)
  return data
}

export async function returnBook(payload: ReturnBookRequest) {
  await apiClient.post(`/library/loans/${payload.loanId}/return`, payload)
}
