import { apiClient } from './client'

/**
 * Download a DB-stored file from an authenticated, tenant-scoped endpoint.
 *
 * Backend URLs for stored files look like `/api/files/{id}` and require the JWT, so a plain
 * `<a href>` won't work (anchors don't carry the Authorization header). We fetch the bytes as
 * a blob through `apiClient` (which injects the token) and trigger a client-side download.
 * The `baseURL: ''` override is because the URL already includes the `/api` prefix.
 */
export async function downloadStoredFile(url: string, filename?: string) {
  const response = await apiClient.get(url, { baseURL: '', responseType: 'blob' })
  const objectUrl = window.URL.createObjectURL(response.data as Blob)
  const link = document.createElement('a')
  link.href = objectUrl
  if (filename) link.download = filename
  link.target = '_blank'
  link.rel = 'noreferrer'
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(objectUrl)
}
