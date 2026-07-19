import { useEffect, useState } from 'react'
import { getQueuedAttendance, QUEUE_CHANGED_EVENT, type QueuedAttendance } from './attendanceQueue'

/** Live count (+ list) of attendance submissions still waiting to sync, kept fresh via
 * the queue's change event - no polling needed. */
export function usePendingAttendance() {
  const [pending, setPending] = useState<QueuedAttendance[]>([])

  useEffect(() => {
    let cancelled = false
    function refresh() {
      void getQueuedAttendance().then((items) => {
        if (!cancelled) setPending(items)
      })
    }
    refresh()
    window.addEventListener(QUEUE_CHANGED_EVENT, refresh)
    window.addEventListener('online', refresh)
    return () => {
      cancelled = true
      window.removeEventListener(QUEUE_CHANGED_EVENT, refresh)
      window.removeEventListener('online', refresh)
    }
  }, [])

  return pending
}
