import { apiClient } from './client'

export async function downloadReportCard(studentId: string, termId: string, studentNumber: string, scheme?: string) {
  const response = await apiClient.get(`/academic/students/${studentId}/report-card`, {
    params: { termId, scheme },
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
