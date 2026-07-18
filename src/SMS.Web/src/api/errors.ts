import { isAxiosError } from 'axios'

/**
 * The API returns errors in one of three shapes depending on the failure path:
 * a bare JSON string (`BadRequest(result.Error)` from a handler), a FluentValidation
 * failure (`{ message, errors: string[] }` from ExceptionHandlingMiddleware), or
 * (rarely) nothing parseable. Normalize all of them to a single display string.
 */
export function getErrorMessage(err: unknown, fallback = 'Something went wrong'): string {
  if (isAxiosError(err)) {
    const data = err.response?.data
    if (typeof data === 'string' && data.trim()) return data
    if (data && typeof data === 'object') {
      if (Array.isArray((data as { errors?: unknown }).errors)) {
        const errors = (data as { errors: string[] }).errors
        if (errors.length > 0) return errors.join(' ')
      }
      if (typeof (data as { message?: unknown }).message === 'string') {
        return (data as { message: string }).message
      }
    }
    if (err.message) return err.message
  }
  return fallback
}

/**
 * True only for a genuine "no such record" response (HTTP 404). Any other failure -
 * a network blip, the dev API mid-restart, a 500 - is not the same thing and callers
 * should show a retryable error state instead of a permanent "not found".
 */
export function isNotFoundError(err: unknown): boolean {
  return isAxiosError(err) && err.response?.status === 404
}
