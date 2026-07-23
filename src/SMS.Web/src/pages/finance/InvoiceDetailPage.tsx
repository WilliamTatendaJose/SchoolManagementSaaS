import { useQuery } from '@tanstack/react-query'
import { CalendarDays, CreditCard, Download, FileText, Printer, Receipt } from 'lucide-react'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { downloadInvoicePdf, fetchInvoice } from '../../api/finance'
import { downloadReceipt } from '../../api/reports'
import { PAYMENT_METHOD_LABELS } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { RecordPaymentDrawer } from './RecordPaymentDrawer'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const canRecordPayment = useAuthStore((s) => s.hasPermission('payments.record'))
  const [paymentOpen, setPaymentOpen] = useState(false)
  const [downloadingPdf, setDownloadingPdf] = useState(false)

  const { data: invoice, isLoading } = useQuery({
    queryKey: ['invoice', id],
    queryFn: () => fetchInvoice(id!),
    enabled: !!id,
  })

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!invoice) {
    return <p className="text-sm text-slate-500">Invoice not found.</p>
  }

  const isPaid = invoice.balance <= 0

  return (
    <div>
      <PageHeader
        title={invoice.invoiceNumber}
        backTo="/finance"
        actions={
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              loading={downloadingPdf}
              onClick={async () => {
                setDownloadingPdf(true)
                try {
                  await downloadInvoicePdf(invoice.id, invoice.invoiceNumber)
                } finally {
                  setDownloadingPdf(false)
                }
              }}
            >
              <FileText className="h-4 w-4" strokeWidth={2} />
              PDF
            </Button>
            <Button
              variant="secondary"
              onClick={() => window.open(`/finance/invoices/${invoice.id}/print`, '_blank', 'noopener,noreferrer')}
            >
              <Printer className="h-4 w-4" strokeWidth={2} />
              Print
            </Button>
            {canRecordPayment && !isPaid && (
              <Button onClick={() => setPaymentOpen(true)}>
                <CreditCard className="h-4 w-4" strokeWidth={2} />
                Record payment
              </Button>
            )}
          </div>
        }
      />

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{invoice.student.fullName}</h2>
              <Badge tone={isPaid ? 'emerald' : 'amber'}>{isPaid ? 'Paid' : 'Outstanding'}</Badge>
            </div>
            <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
              {invoice.student.studentNumber} · {invoice.academicTerm.name}
            </p>
            <p className="mt-2 flex items-center gap-1.5 text-xs text-slate-400">
              <CalendarDays className="h-3.5 w-3.5" strokeWidth={2} />
              Issued {new Date(invoice.invoiceDate).toLocaleDateString()} · Due{' '}
              {new Date(invoice.dueDate).toLocaleDateString()}
            </p>
          </div>
          <div className="text-right">
            <p className="text-xs font-medium uppercase tracking-wide text-slate-400">Balance</p>
            <p className={`text-2xl font-semibold ${isPaid ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-900 dark:text-white'}`}>
              {currency.format(invoice.balance)}
            </p>
          </div>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Line items</h3>
          <table className="mt-3 w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="py-2 font-medium">Description</th>
                <th className="py-2 text-right font-medium">Qty</th>
                <th className="py-2 text-right font-medium">Amount</th>
                <th className="py-2 text-right font-medium">Total</th>
              </tr>
            </thead>
            <tbody>
              {invoice.items.map((item) => (
                <tr key={item.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                  <td className="py-2 text-slate-700 dark:text-slate-200">{item.description}</td>
                  <td className="py-2 text-right text-slate-500 dark:text-slate-400">{item.quantity}</td>
                  <td className="py-2 text-right text-slate-500 dark:text-slate-400">{currency.format(item.amount)}</td>
                  <td className="py-2 text-right font-medium text-slate-800 dark:text-slate-100">
                    {currency.format(item.amount * item.quantity)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="mt-4 space-y-1.5 border-t border-slate-200 pt-4 text-sm dark:border-slate-800">
            <div className="flex justify-between text-slate-500 dark:text-slate-400">
              <span>Subtotal</span>
              <span>{currency.format(invoice.totalAmount)}</span>
            </div>
            {invoice.discountAmount > 0 && (
              <div className="flex justify-between text-slate-500 dark:text-slate-400">
                <span>Discount</span>
                <span>-{currency.format(invoice.discountAmount)}</span>
              </div>
            )}
            <div className="flex justify-between text-slate-500 dark:text-slate-400">
              <span>Paid</span>
              <span>-{currency.format(invoice.paidAmount)}</span>
            </div>
            <div className="flex justify-between border-t border-slate-200 pt-1.5 font-semibold text-slate-900 dark:border-slate-800 dark:text-white">
              <span>Balance</span>
              <span>{currency.format(invoice.balance)}</span>
            </div>
          </div>

          {invoice.notes && (
            <p className="mt-4 rounded-lg bg-slate-50 p-3 text-sm text-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
              {invoice.notes}
            </p>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Payment history</h3>
          {invoice.payments.length === 0 ? (
            <p className="mt-3 text-sm text-slate-400">No payments recorded yet.</p>
          ) : (
            <ul className="mt-3 space-y-3">
              {invoice.payments.map((p) => (
                <li key={p.id} className="rounded-xl border border-slate-100 p-3 dark:border-slate-800">
                  <div className="flex items-center justify-between">
                    <p className="flex items-center gap-1.5 text-sm font-medium text-slate-800 dark:text-slate-100">
                      <Receipt className="h-3.5 w-3.5 text-slate-400" strokeWidth={2} />
                      {currency.format(p.amount)}
                    </p>
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-slate-400">
                        {PAYMENT_METHOD_LABELS[p.paymentMethod as keyof typeof PAYMENT_METHOD_LABELS] ?? p.paymentMethod}
                      </span>
                      <button
                        onClick={() => downloadReceipt(p.id, p.receiptNumber)}
                        className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                        aria-label="Download receipt"
                        title="Download receipt"
                      >
                        <Download className="h-3.5 w-3.5" strokeWidth={2} />
                      </button>
                    </div>
                  </div>
                  <p className="mt-1 text-xs text-slate-400">
                    {p.receiptNumber} · {new Date(p.paymentDate).toLocaleDateString()}
                  </p>
                  {p.transactionReference && (
                    <p className="text-xs text-slate-400">Ref: {p.transactionReference}</p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <RecordPaymentDrawer
        open={paymentOpen}
        onClose={() => setPaymentOpen(false)}
        invoiceId={invoice.id}
        studentId={invoice.student.id}
        studentName={invoice.student.fullName}
        balance={invoice.balance}
      />
    </div>
  )
}
