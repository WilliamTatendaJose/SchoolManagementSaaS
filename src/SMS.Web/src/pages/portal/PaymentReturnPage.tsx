import { useQuery } from '@tanstack/react-query'
import { CheckCircle2, Clock, GraduationCap, XCircle } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { checkChildPaymentStatus } from '../../api/portal'
import { Button } from '../../components/ui/Button'
import { clearPendingPayment, readPendingPayment } from '../../lib/pendingPayment'

/** Landing page for Paynow's ReturnUrl. Paynow's redirect carries none of our own data,
 * so the payment we're waiting on was stashed in sessionStorage before we sent the
 * browser there; this page reads it back and polls once for a settled status. */
export function PaymentReturnPage() {
  const navigate = useNavigate()
  const [attempt, setAttempt] = useState(0)
  const pending = readPendingPayment()

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['payment-status', pending?.paymentId, attempt],
    queryFn: () => checkChildPaymentStatus(pending!.paymentId),
    enabled: !!pending,
    retry: false,
  })

  useEffect(() => {
    if (data?.settled) {
      clearPendingPayment()
    }
  }, [data])

  const stillProcessing = data && !data.settled && data.status !== 'Failed' && data.status !== 'Cancelled'

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-slate-50 px-6 dark:bg-slate-950">
      <div className="w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="mx-auto flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-brand-500 to-brand-700">
          <GraduationCap className="h-5 w-5 text-white" strokeWidth={2.25} />
        </div>

        {!pending ? (
          <>
            <h1 className="mt-4 text-lg font-semibold text-slate-900 dark:text-white">No payment in progress</h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              We couldn't find a pending payment for this browser session. If you just paid, check the fees tab in a moment.
            </p>
          </>
        ) : isLoading ? (
          <>
            <div className="mx-auto mt-6 h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-brand-600 dark:border-slate-700" />
            <p className="mt-4 text-sm text-slate-500 dark:text-slate-400">Checking your payment for {pending.studentName}…</p>
          </>
        ) : isError ? (
          <>
            <XCircle className="mx-auto mt-4 h-10 w-10 text-red-500" strokeWidth={1.75} />
            <h1 className="mt-3 text-lg font-semibold text-slate-900 dark:text-white">Could not confirm payment</h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{getErrorMessage(error, 'Something went wrong.')}</p>
          </>
        ) : data?.settled ? (
          <>
            <CheckCircle2 className="mx-auto mt-4 h-10 w-10 text-emerald-500" strokeWidth={1.75} />
            <h1 className="mt-3 text-lg font-semibold text-slate-900 dark:text-white">Payment received</h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              Thank you. {pending.studentName}'s remaining balance is{' '}
              {data.invoiceBalance.toLocaleString(undefined, { style: 'currency', currency: 'USD' })}.
            </p>
          </>
        ) : stillProcessing ? (
          <>
            <Clock className="mx-auto mt-4 h-10 w-10 text-amber-500" strokeWidth={1.75} />
            <h1 className="mt-3 text-lg font-semibold text-slate-900 dark:text-white">Still processing</h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              Paynow hasn't confirmed this payment yet. This can take a minute for mobile money - check again shortly.
            </p>
            <Button variant="secondary" className="mt-4" onClick={() => setAttempt((a) => a + 1)}>
              Check again
            </Button>
          </>
        ) : (
          <>
            <XCircle className="mx-auto mt-4 h-10 w-10 text-red-500" strokeWidth={1.75} />
            <h1 className="mt-3 text-lg font-semibold text-slate-900 dark:text-white">Payment not completed</h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              This payment was {data?.status?.toLowerCase() ?? 'not completed'}. No charge was made - you can try again from the fees tab.
            </p>
          </>
        )}

        <Button className="mt-6 w-full" onClick={() => navigate('/portal', { replace: true })}>
          Back to portal
        </Button>
      </div>
    </div>
  )
}
