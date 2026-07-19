import { useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { useCallback, useEffect, useRef } from 'react'
import { markAttendance } from '../api/attendance'
import { getQueuedAttendance, removeQueuedAttendance } from './attendanceQueue'

/** True for a genuine connectivity failure (no response reached us at all) - as opposed
 * to a real HTTP error response, which means we ARE online and the server rejected the
 * request for some other reason. */
function isNetworkFailure(err: unknown) {
  return !navigator.onLine || (isAxiosError(err) && !err.response)
}

/**
 * Flushes the offline attendance queue: replays each queued class/date submission in
 * turn. Runs on mount, whenever the browser regains connectivity, and periodically while
 * online (in case a request silently failed without firing the `offline` event, e.g. a
 * flaky mobile connection). Mount this once, high in the tree (AppShell), so marks queued
 * on the Attendance page still sync even after the teacher navigates elsewhere.
 */
export function useAttendanceSync() {
  const queryClient = useQueryClient()
  const syncingRef = useRef(false)

  const flush = useCallback(async () => {
    if (syncingRef.current || !navigator.onLine) return
    syncingRef.current = true

    try {
      const queued = await getQueuedAttendance()
      for (const item of queued) {
        try {
          await markAttendance(item.payload)
          await removeQueuedAttendance(item.key)
          await queryClient.invalidateQueries({ queryKey: ['class-attendance', item.classId, item.date] })
        } catch (err) {
          if (isNetworkFailure(err)) {
            // Still offline (or just went offline mid-flush) - stop here and let the
            // next online event or timer tick pick up where we left off.
            return
          }
          // A real rejection from the server (not a connectivity issue). Leave it
          // queued rather than silently dropping the teacher's marks, and move on so
          // one bad entry doesn't block the rest of the queue.
        }
      }
    } finally {
      syncingRef.current = false
    }
  }, [queryClient])

  useEffect(() => {
    void flush()
    window.addEventListener('online', flush)
    const interval = window.setInterval(flush, 60_000)
    return () => {
      window.removeEventListener('online', flush)
      window.clearInterval(interval)
    }
  }, [flush])
}
