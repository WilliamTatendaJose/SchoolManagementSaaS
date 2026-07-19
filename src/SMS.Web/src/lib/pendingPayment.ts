const KEY = 'sms:pendingPayment'

export interface PendingPayment {
  paymentId: string
  studentId: string
  studentName: string
}

/** Paynow's ReturnUrl carries no data of ours, so we stash which payment we're waiting
 * on before redirecting there and read it back when the browser returns. */
export function savePendingPayment(payment: PendingPayment) {
  sessionStorage.setItem(KEY, JSON.stringify(payment))
}

export function readPendingPayment(): PendingPayment | null {
  const raw = sessionStorage.getItem(KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as PendingPayment
  } catch {
    return null
  }
}

export function clearPendingPayment() {
  sessionStorage.removeItem(KEY)
}
