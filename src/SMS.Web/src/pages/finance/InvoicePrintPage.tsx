import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { fetchInvoice } from '../../api/finance'
import { useAuthStore } from '../../auth/authStore'
import { PAYMENT_METHOD_LABELS } from '../../api/types'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export function InvoicePrintPage() {
  const { id } = useParams<{ id: string }>()
  const tenantName = useAuthStore((s) => s.tenantName)

  const { data: invoice, isLoading } = useQuery({
    queryKey: ['invoice', id],
    queryFn: () => fetchInvoice(id!),
    enabled: !!id,
  })

  useEffect(() => {
    if (invoice) {
      document.title = `Invoice ${invoice.invoiceNumber}`
    }
  }, [invoice])

  if (isLoading) {
    return <div className="p-10 text-sm text-slate-500">Loading…</div>
  }

  if (!invoice) {
    return <div className="p-10 text-sm text-slate-500">Invoice not found.</div>
  }

  const isPaid = invoice.balance <= 0

  return (
    <div className="mx-auto max-w-3xl bg-white p-10 text-slate-900 print:p-0">
      <div className="mb-6 flex justify-end print:hidden">
        <button
          onClick={() => window.print()}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-700"
        >
          Print / Save as PDF
        </button>
      </div>

      <div className="flex items-start justify-between border-b border-slate-200 pb-6">
        <div>
          <h1 className="text-xl font-bold">{tenantName ?? 'School'}</h1>
          <p className="mt-1 text-sm text-slate-500">Invoice</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-semibold">{invoice.invoiceNumber}</p>
          <p className="mt-1 text-sm text-slate-500">
            {isPaid ? <span className="font-medium text-emerald-600">PAID</span> : 'OUTSTANDING'}
          </p>
        </div>
      </div>

      <div className="mt-6 grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="font-medium uppercase tracking-wide text-slate-400">Billed to</p>
          <p className="mt-1 font-semibold">{invoice.student.fullName}</p>
          <p className="text-slate-500">{invoice.student.studentNumber}</p>
        </div>
        <div className="text-right">
          <p>
            <span className="text-slate-400">Term:</span> {invoice.academicTerm.name}
          </p>
          <p>
            <span className="text-slate-400">Issued:</span> {new Date(invoice.invoiceDate).toLocaleDateString()}
          </p>
          <p>
            <span className="text-slate-400">Due:</span> {new Date(invoice.dueDate).toLocaleDateString()}
          </p>
        </div>
      </div>

      <table className="mt-8 w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-300 text-xs uppercase tracking-wider text-slate-400">
            <th className="py-2 font-medium">Description</th>
            <th className="py-2 text-right font-medium">Qty</th>
            <th className="py-2 text-right font-medium">Amount</th>
            <th className="py-2 text-right font-medium">Total</th>
          </tr>
        </thead>
        <tbody>
          {invoice.items.map((item) => (
            <tr key={item.id} className="border-b border-slate-100">
              <td className="py-2">{item.description}</td>
              <td className="py-2 text-right text-slate-500">{item.quantity}</td>
              <td className="py-2 text-right text-slate-500">{currency.format(item.amount)}</td>
              <td className="py-2 text-right font-medium">{currency.format(item.amount * item.quantity)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="mt-4 flex justify-end">
        <div className="w-64 space-y-1.5 text-sm">
          <div className="flex justify-between text-slate-500">
            <span>Subtotal</span>
            <span>{currency.format(invoice.totalAmount)}</span>
          </div>
          {invoice.discountAmount > 0 && (
            <div className="flex justify-between text-slate-500">
              <span>Discount</span>
              <span>-{currency.format(invoice.discountAmount)}</span>
            </div>
          )}
          <div className="flex justify-between text-slate-500">
            <span>Paid</span>
            <span>-{currency.format(invoice.paidAmount)}</span>
          </div>
          <div className="flex justify-between border-t border-slate-300 pt-1.5 text-base font-bold">
            <span>Balance due</span>
            <span>{currency.format(invoice.balance)}</span>
          </div>
        </div>
      </div>

      {invoice.notes && (
        <div className="mt-8 border-t border-slate-200 pt-4 text-sm text-slate-600">
          <p className="font-medium uppercase tracking-wide text-slate-400">Notes</p>
          <p className="mt-1">{invoice.notes}</p>
        </div>
      )}

      {invoice.payments.length > 0 && (
        <div className="mt-8 border-t border-slate-200 pt-4">
          <p className="text-sm font-medium uppercase tracking-wide text-slate-400">Payment history</p>
          <table className="mt-2 w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400">
                <th className="py-1.5 font-medium">Receipt</th>
                <th className="py-1.5 font-medium">Date</th>
                <th className="py-1.5 font-medium">Method</th>
                <th className="py-1.5 text-right font-medium">Amount</th>
              </tr>
            </thead>
            <tbody>
              {invoice.payments.map((p) => (
                <tr key={p.id} className="border-b border-slate-100">
                  <td className="py-1.5 font-mono text-xs">{p.receiptNumber}</td>
                  <td className="py-1.5 text-slate-500">{new Date(p.paymentDate).toLocaleDateString()}</td>
                  <td className="py-1.5 text-slate-500">
                    {PAYMENT_METHOD_LABELS[p.paymentMethod as keyof typeof PAYMENT_METHOD_LABELS] ?? p.paymentMethod}
                  </td>
                  <td className="py-1.5 text-right font-medium">{currency.format(p.amount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="mt-10 text-center text-xs text-slate-400 print:mt-16">Thank you.</p>
    </div>
  )
}
