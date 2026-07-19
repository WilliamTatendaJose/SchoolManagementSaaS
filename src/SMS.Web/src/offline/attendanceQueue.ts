import type { MarkAttendanceRequest } from '../api/types'

const DB_NAME = 'sms-offline'
const DB_VERSION = 1
const STORE = 'pendingAttendance'

/** Fired on window whenever the queue's contents change, so any number of UI badges can
 * stay in sync without polling. */
export const QUEUE_CHANGED_EVENT = 'sms:attendance-queue-changed'

export interface QueuedAttendance {
  /** classId:date - marking the same class/date again while still queued just replaces
   * the pending entry, since a resync only needs the latest state, not a history. */
  key: string
  classId: string
  date: string
  className: string
  payload: MarkAttendanceRequest
  queuedAt: string
}

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION)
    request.onupgradeneeded = () => {
      const db = request.result
      if (!db.objectStoreNames.contains(STORE)) {
        db.createObjectStore(STORE, { keyPath: 'key' })
      }
    }
    request.onsuccess = () => resolve(request.result)
    request.onerror = () => reject(request.error as Error)
  })
}

async function withStore<T>(mode: IDBTransactionMode, fn: (store: IDBObjectStore) => IDBRequest<T>): Promise<T> {
  const db = await openDb()
  try {
    return await new Promise<T>((resolve, reject) => {
      const tx = db.transaction(STORE, mode)
      const req = fn(tx.objectStore(STORE))
      req.onsuccess = () => resolve(req.result)
      req.onerror = () => reject(req.error as Error)
    })
  } finally {
    db.close()
  }
}

export async function queueAttendance(classId: string, date: string, className: string, payload: MarkAttendanceRequest) {
  const entry: QueuedAttendance = { key: `${classId}:${date}`, classId, date, className, payload, queuedAt: new Date().toISOString() }
  await withStore('readwrite', (store) => store.put(entry))
  window.dispatchEvent(new Event(QUEUE_CHANGED_EVENT))
}

export async function getQueuedAttendance(): Promise<QueuedAttendance[]> {
  return withStore('readonly', (store) => store.getAll())
}

export async function removeQueuedAttendance(key: string) {
  await withStore('readwrite', (store) => store.delete(key))
  window.dispatchEvent(new Event(QUEUE_CHANGED_EVENT))
}
