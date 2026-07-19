import { useMutation, useQuery } from '@tanstack/react-query'
import { CreditCard, Receipt, Wallet } from 'lucide-react'
import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { fetchChildFinance, initiateChildPayment } from '../../api/portal'
import type { MyChildInvoiceDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { StatCard } from '../../components/ui/StatCard'
import { savePendingPayment } from '../../lib/pendingPayment'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export function PortalFinanceTab({ studentId, studentName }: { studentId: string; studentName: string }) {
  const user = useAuthStore((s) => s.user)
  const [payingInvoiceId, setPayingInvoiceId] = useState<string | null>(null)
  const [payError, setPayError] = useState<string | null>(null)

  const { data: finance, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-finance', studentId],
    queryFn: () => fetchChildFinance(studentId),
  })

  const payMutation = useMutation({
    mutationFn: (invoice: MyChildInvoiceDto) => {
      setPayingInvoiceId(invoice.invoiceId)
      return initiateChildPayment(invoice.invoiceId, user?.email)
    },
    onSuccess: (result) => {
      if (!result.redirectUrl) {
        setPayError('Payment could not be started - no redirect was returned.')
        setPayingInvoiceId(null)
        return
      }
      savePendingPayment({ paymentId: result.paymentId, studentId, studentName })
      window.location.href = result.redirectUrl
    },
    onError: (err) => {
      setPayError(getErrorMessage(err, 'Could not start the online payment.'))
      setPayingInvoiceId(null)
    },
  })

  if (isLoading) {
    return <div className="h-40 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
  }

  if (isError) {
    return <ErrorState description="Could not load fees." onRetry={() => refetch()} />
  }

  if (!finance) return null

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-2">
        <StatCard label="Total billed" value={currency.format(finance.totalBilled)} icon={Receipt} tone="slate" />
        <StatCard
          label="Outstanding"
          value={currency.format(finance.outstandingBalance)}
          icon={Wallet}
          tone={finance.outstandingBalance > 0 ? 'amber' : 'emerald'}
        />
      </div>

      {payError && (
        <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">{payError}</p>
      )}

      {finance.invoices.length === 0 ? (
        <EmptyState icon={Receipt} title="No invoices yet" description="Invoices will appear here once the school issues them." />
      ) : (
        <div className="space-y-2">
          {finance.invoices.map((invoice) => (
            <div
              key={invoice.invoiceId}
              className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
            >
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-slate-900 dark:text-white">{invoice.invoiceNumber}</p>
                <p className="text-xs text-slate-400">
                  Due {new Date(invoice.dueDate).toLocaleDateString()} · {currency.format(invoice.totalAmount)} total
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-3">
                <p className={`text-sm font-semibold ${invoice.balance > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-emerald-600 dark:text-emerald-400'}`}>
                  {currency.format(invoice.balance)}
                </p>
                {invoice.payable && (
                  <Button
                    onClick={() => payMutation.mutate(invoice)}
                    loading={payMutation.isPending && payingInvoiceId === invoice.invoiceId}
                    disabled={payMutation.isPending}
                  >
                    <CreditCard className="h-4 w-4" strokeWidth={2.5} />
                    Pay
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
