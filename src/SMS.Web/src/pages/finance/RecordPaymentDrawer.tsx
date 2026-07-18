import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAccountBalance, recordPayment } from '../../api/finance'
import { getErrorMessage } from '../../api/errors'
import { PAYMENT_METHOD_LABELS, PAYMENT_METHODS } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

export function RecordPaymentDrawer({
  open,
  onClose,
  invoiceId,
  studentId,
  studentName,
  balance,
}: {
  open: boolean
  onClose: () => void
  invoiceId: string
  studentId: string
  studentName: string
  balance: number
}) {
  const queryClient = useQueryClient()
  const [amount, setAmount] = useState('0')
  const [method, setMethod] = useState<(typeof PAYMENT_METHODS)[number]>('Cash')
  const [amountTendered, setAmountTendered] = useState('')
  const [excessHandling, setExcessHandling] = useState<'Change' | 'Credit'>('Change')
  const [paymentDate, setPaymentDate] = useState(new Date().toISOString().slice(0, 10))
  const [reference, setReference] = useState('')
  const [mobileMoneyNumber, setMobileMoneyNumber] = useState('')
  const [bankName, setBankName] = useState('')
  const [result, setResult] = useState<{ receiptNumber: string; changeDue: number; creditedToAccount: number } | null>(
    null,
  )
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: accountBalance } = useQuery({
    queryKey: ['account-balance', studentId],
    queryFn: () => fetchAccountBalance(studentId),
    enabled: open,
  })

  useEffect(() => {
    if (open) {
      setAmount(balance > 0 ? balance.toFixed(2) : '0')
      setMethod('Cash')
      setAmountTendered('')
      setExcessHandling('Change')
      setPaymentDate(new Date().toISOString().slice(0, 10))
      setReference('')
      setMobileMoneyNumber('')
      setBankName('')
      setResult(null)
      setError(null)
    }
    // Intentionally omits `balance` — a successful payment triggers a parent refetch
    // that updates `balance`, and re-running this reset while still open would wipe
    // the just-shown receipt and refill the form with the new balance.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const tenderedExcess = method === 'Cash' && amountTendered ? Number(amountTendered) - Number(amount) : 0
  const available = accountBalance?.balance ?? 0

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await recordPayment({
        invoiceId,
        amount: Number(amount),
        paymentMethod: method,
        paymentDate,
        transactionReference: reference || undefined,
        mobileMoneyNumber: method === 'MobileMoney' ? mobileMoneyNumber || undefined : undefined,
        bankName: method === 'BankTransfer' ? bankName || undefined : undefined,
        amountTendered:
          method === 'Cash' && amountTendered && Number(amountTendered) > Number(amount)
            ? Number(amountTendered)
            : undefined,
        excessHandling: method === 'Cash' ? excessHandling : undefined,
      })
      setResult({ receiptNumber: res.receiptNumber, changeDue: res.changeDue, creditedToAccount: res.creditedToAccount })
      await queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['finance-summary'] })
      await queryClient.invalidateQueries({ queryKey: ['account-balance', studentId] })
      await queryClient.invalidateQueries({ queryKey: ['account-transactions', studentId] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not record payment'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Record payment"
      description={`${studentName} · Balance due ${currency.format(balance)}`}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            {result ? 'Close' : 'Cancel'}
          </Button>
          {!result && (
            <Button type="submit" form="record-payment-form" loading={submitting}>
              Record payment
            </Button>
          )}
        </>
      }
    >
      <form id="record-payment-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Amount"
            type="number"
            min={0.01}
            step="0.01"
            max={method === 'AccountCredit' ? Math.min(available, balance) : balance}
            required
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
          <SelectField
            label="Method"
            required
            value={method}
            onChange={(e) => {
              setMethod(e.target.value as (typeof PAYMENT_METHODS)[number])
              setAmountTendered('')
            }}
          >
            {PAYMENT_METHODS.map((m) => (
              <option key={m} value={m}>
                {PAYMENT_METHOD_LABELS[m]}
              </option>
            ))}
          </SelectField>
        </div>

        {method === 'AccountCredit' && (
          <p className="text-xs text-slate-400">
            Available balance: <span className="font-medium text-slate-600 dark:text-slate-300">{currency.format(available)}</span>
            {available < Number(amount) && (
              <span className="ml-1 text-red-600 dark:text-red-400">— exceeds available balance</span>
            )}
          </p>
        )}

        <TextField
          label="Payment date"
          type="date"
          required
          value={paymentDate}
          onChange={(e) => setPaymentDate(e.target.value)}
        />

        {method === 'Cash' && (
          <div>
            <TextField
              label="Amount tendered"
              type="number"
              min={0}
              step="0.01"
              value={amountTendered}
              onChange={(e) => setAmountTendered(e.target.value)}
              hint="Optional — only if the payer handed over more than the amount above"
            />
            {tenderedExcess > 0 && (
              <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-900 dark:bg-amber-950/30">
                <p className="text-sm font-medium text-amber-800 dark:text-amber-300">
                  Excess of {currency.format(tenderedExcess)} — how should it be handled?
                </p>
                <div className="mt-2 flex gap-2">
                  <button
                    type="button"
                    onClick={() => setExcessHandling('Change')}
                    className={`flex-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors ${
                      excessHandling === 'Change'
                        ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-600 dark:bg-brand-950/40 dark:text-brand-300'
                        : 'border-slate-300 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300'
                    }`}
                  >
                    Give change
                  </button>
                  <button
                    type="button"
                    onClick={() => setExcessHandling('Credit')}
                    className={`flex-1 rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors ${
                      excessHandling === 'Credit'
                        ? 'border-brand-500 bg-brand-50 text-brand-700 dark:border-brand-600 dark:bg-brand-950/40 dark:text-brand-300'
                        : 'border-slate-300 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300'
                    }`}
                  >
                    Credit to account
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {method === 'MobileMoney' && (
          <TextField
            label="Mobile money number"
            value={mobileMoneyNumber}
            onChange={(e) => setMobileMoneyNumber(e.target.value)}
          />
        )}
        {method === 'BankTransfer' && (
          <TextField label="Bank name" value={bankName} onChange={(e) => setBankName(e.target.value)} />
        )}

        <TextField
          label="Transaction reference"
          value={reference}
          onChange={(e) => setReference(e.target.value)}
        />

        {result && (
          <div className="space-y-1 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
            <p>
              Payment recorded. Receipt: <span className="font-mono font-semibold">{result.receiptNumber}</span>
            </p>
            {result.changeDue > 0 && <p className="font-medium">Give change: {currency.format(result.changeDue)}</p>}
            {result.creditedToAccount > 0 && (
              <p className="font-medium">Credited to account: {currency.format(result.creditedToAccount)}</p>
            )}
          </div>
        )}

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
