import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CircleDollarSign, Download, FileText, Pencil, Plus, Receipt, Trash2, TrendingUp, Wallet, Zap } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchClasses } from '../../api/classes'
import { deleteFeeStructure, fetchFeeStructures } from '../../api/feeStructures'
import { getErrorMessage } from '../../api/errors'
import { fetchFinanceSummary, fetchInvoices, fetchPayments } from '../../api/finance'
import { downloadReceipt } from '../../api/reports'
import type { FeeStructureDto } from '../../api/types'
import { PAYMENT_METHOD_LABELS } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { StatCard } from '../../components/ui/StatCard'
import { CreateInvoiceDrawer } from './CreateInvoiceDrawer'
import { FeeStructureFormDrawer } from './FeeStructureFormDrawer'
import { GenerateInvoicesDrawer } from './GenerateInvoicesDrawer'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const PAGE_SIZE = 15

export function FinancePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManageFees = useAuthStore((s) => s.hasPermission('feestructures.manage'))
  const canCreateInvoices = useAuthStore((s) => s.hasPermission('invoices.create'))
  const [params, setParams] = useSearchParams()
  const tabParam = params.get('tab')
  const tab = tabParam === 'fees' ? 'fees' : tabParam === 'payments' ? 'payments' : 'invoices'
  const [generateOpen, setGenerateOpen] = useState(false)
  const [createInvoiceOpen, setCreateInvoiceOpen] = useState(false)
  const [feeFormOpen, setFeeFormOpen] = useState(false)
  const [editingFee, setEditingFee] = useState<FeeStructureDto | undefined>()
  const [deletingFeeId, setDeletingFeeId] = useState<string | null>(null)
  const [feeError, setFeeError] = useState<string | null>(null)

  const page = Number(params.get('page') ?? '1')
  const termId = params.get('termId') ?? ''
  const unpaidOnly = params.get('unpaidOnly') === '1'
  const feeClassId = params.get('feeClassId') ?? ''

  function setTab(next: 'invoices' | 'fees' | 'payments') {
    const p = new URLSearchParams(params)
    if (next === 'invoices') p.delete('tab')
    else p.set('tab', next)
    p.delete('page')
    setParams(p, { replace: true })
  }

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms() })
  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: tab === 'fees' })

  useEffect(() => {
    if (!termId && terms && terms.length > 0) {
      const current = terms.find((t) => t.isCurrent) ?? terms[0]
      updateParam('termId', current.id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [termId, terms])

  const { data: summary } = useQuery({
    queryKey: ['finance-summary', termId],
    queryFn: () => fetchFinanceSummary(termId || undefined),
    enabled: !!termId,
  })

  const { data: invoices, isLoading: loadingInvoices } = useQuery({
    queryKey: ['invoices', { page, termId, unpaidOnly }],
    queryFn: () => fetchInvoices({ page, pageSize: PAGE_SIZE, termId: termId || undefined, unpaidOnly: unpaidOnly || undefined }),
    enabled: tab === 'invoices' && !!termId,
    placeholderData: (prev) => prev,
  })

  const { data: feeStructures, isLoading: loadingFees } = useQuery({
    queryKey: ['fee-structures', feeClassId],
    queryFn: () => fetchFeeStructures({ classId: feeClassId || undefined }),
    enabled: tab === 'fees',
  })

  const { data: payments, isLoading: loadingPayments } = useQuery({
    queryKey: ['payments', { page }],
    queryFn: () => fetchPayments({ page, pageSize: PAGE_SIZE }),
    enabled: tab === 'payments',
    placeholderData: (prev) => prev,
  })

  async function handleDeleteFee(fee: FeeStructureDto) {
    if (!window.confirm(`Delete "${fee.name}" for ${fee.className}?`)) return
    setFeeError(null)
    setDeletingFeeId(fee.id)
    try {
      await deleteFeeStructure(fee.id)
      await queryClient.invalidateQueries({ queryKey: ['fee-structures'] })
    } catch (err) {
      setFeeError(getErrorMessage(err, 'Could not delete fee structure'))
    } finally {
      setDeletingFeeId(null)
    }
  }

  const totalPages = invoices ? Math.max(1, Math.ceil(invoices.total / invoices.pageSize)) : 1
  const paymentsTotalPages = payments ? Math.max(1, Math.ceil(payments.total / payments.pageSize)) : 1

  return (
    <div>
      <PageHeader
        title="Fees & invoices"
        description="Billing, collections and fee structures"
        actions={
          tab === 'invoices' ? (
            <>
              {canCreateInvoices && (
                <Button variant="secondary" onClick={() => setGenerateOpen(true)}>
                  <Zap className="h-4 w-4" strokeWidth={2} />
                  Generate invoices
                </Button>
              )}
              {canCreateInvoices && (
                <Button onClick={() => setCreateInvoiceOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Create invoice
                </Button>
              )}
            </>
          ) : tab === 'fees' ? (
            canManageFees && (
              <Button
                onClick={() => {
                  setEditingFee(undefined)
                  setFeeFormOpen(true)
                }}
              >
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Add fee structure
              </Button>
            )
          ) : null
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('invoices')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'invoices'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <FileText className="h-4 w-4" strokeWidth={2} />
          Invoices
        </button>
        <button
          onClick={() => setTab('fees')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'fees'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <Wallet className="h-4 w-4" strokeWidth={2} />
          Fee structures
        </button>
        <button
          onClick={() => setTab('payments')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'payments'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <Receipt className="h-4 w-4" strokeWidth={2} />
          Payments
        </button>
      </div>

      {tab === 'invoices' ? (
        <>
          {summary && (
            <div className="mb-4 grid grid-cols-2 gap-4 lg:grid-cols-4">
              <StatCard label="Total billed" value={currency.format(summary.totalBilled)} icon={FileText} tone="brand" />
              <StatCard
                label="Collected"
                value={currency.format(summary.totalCollected)}
                icon={CircleDollarSign}
                tone="emerald"
              />
              <StatCard
                label="Outstanding"
                value={currency.format(summary.totalOutstanding)}
                icon={Wallet}
                tone="amber"
              />
              <StatCard
                label="Collection rate"
                value={`${summary.collectionRate}%`}
                icon={TrendingUp}
                tone="slate"
                hint={`${summary.paidInvoices}/${summary.totalInvoices} invoices paid`}
              />
            </div>
          )}

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <select
              value={termId}
              onChange={(e) => updateParam('termId', e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
            >
              <option value="">All terms</option>
              {terms?.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.academicYearName} · {t.name}
                </option>
              ))}
            </select>
            <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
              <input
                type="checkbox"
                checked={unpaidOnly}
                onChange={(e) => updateParam('unpaidOnly', e.target.checked ? '1' : '')}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              Unpaid only
            </label>
          </div>

          <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingInvoices ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : invoices && invoices.data.length > 0 ? (
              <>
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                      <th className="px-4 py-3 font-medium">Invoice</th>
                      <th className="px-4 py-3 font-medium">Student</th>
                      <th className="px-4 py-3 font-medium">Term</th>
                      <th className="px-4 py-3 font-medium">Due</th>
                      <th className="px-4 py-3 font-medium text-right">Total</th>
                      <th className="px-4 py-3 font-medium text-right">Balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {invoices.data.map((inv) => (
                      <tr
                        key={inv.id}
                        onClick={() => navigate(`/finance/invoices/${inv.id}`)}
                        className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                      >
                        <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{inv.invoiceNumber}</td>
                        <td className="px-4 py-3">
                          <p className="text-slate-700 dark:text-slate-200">{inv.studentName}</p>
                          <p className="text-xs text-slate-400">{inv.studentNumber}</p>
                        </td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{inv.termName}</td>
                        <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                          {new Date(inv.dueDate).toLocaleDateString()}
                        </td>
                        <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">
                          {currency.format(inv.totalAmount)}
                        </td>
                        <td className="px-4 py-3 text-right">
                          {inv.balance <= 0 ? (
                            <Badge tone="emerald">Paid</Badge>
                          ) : (
                            <span className="font-medium text-amber-700 dark:text-amber-400">
                              {currency.format(inv.balance)}
                            </span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination
                  pageNumber={invoices.page}
                  totalPages={totalPages}
                  totalCount={invoices.total}
                  pageSize={invoices.pageSize}
                  hasPreviousPage={invoices.page > 1}
                  hasNextPage={invoices.page * invoices.pageSize < invoices.total}
                  onPageChange={(p) => updateParam('page', String(p))}
                />
              </>
            ) : (
              <EmptyState
                icon={FileText}
                title="No invoices found"
                description={unpaidOnly ? 'Try clearing the unpaid filter.' : 'Generate or create an invoice to get started.'}
              />
            )}
          </div>
        </>
      ) : tab === 'fees' ? (
        <>
          {feeError && (
            <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
              {feeError}
            </p>
          )}
          <select
            value={feeClassId}
            onChange={(e) => updateParam('feeClassId', e.target.value)}
            className="mb-4 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
          >
            <option value="">All classes</option>
            {classes?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingFees ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : feeStructures && feeStructures.length > 0 ? (
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Fee</th>
                    <th className="px-4 py-3 font-medium">Class</th>
                    <th className="px-4 py-3 font-medium">Academic year</th>
                    <th className="px-4 py-3 font-medium text-right">Amount</th>
                    <th className="px-4 py-3 font-medium">Flags</th>
                    {canManageFees && <th className="w-20 px-2 py-3" />}
                  </tr>
                </thead>
                <tbody>
                  {feeStructures.map((f) => (
                    <tr key={f.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                      <td className="px-4 py-3">
                        <p className="font-medium text-slate-900 dark:text-white">{f.name}</p>
                        <p className="text-xs text-slate-400">{f.feeType}</p>
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{f.className}</td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{f.academicYearName}</td>
                      <td className="px-4 py-3 text-right font-medium text-slate-800 dark:text-slate-100">
                        {currency.format(f.amount)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex gap-1.5">
                          {f.isRecurring && <Badge tone="brand">Recurring</Badge>}
                          {f.isOptional && <Badge tone="slate">Optional</Badge>}
                        </div>
                      </td>
                      {canManageFees && (
                        <td className="px-2 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <button
                              onClick={() => {
                                setEditingFee(f)
                                setFeeFormOpen(true)
                              }}
                              className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                              aria-label="Edit"
                            >
                              <Pencil className="h-3.5 w-3.5" strokeWidth={2} />
                            </button>
                            <button
                              onClick={() => handleDeleteFee(f)}
                              disabled={deletingFeeId === f.id}
                              className="rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-50 dark:hover:bg-red-950/40 dark:hover:text-red-400"
                              aria-label="Delete"
                            >
                              <Trash2 className="h-3.5 w-3.5" strokeWidth={2} />
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <EmptyState
                icon={Wallet}
                title="No fee structures yet"
                description="Add one per class to enable bulk invoice generation."
                action={
                  canManageFees && (
                    <Button
                      onClick={() => {
                        setEditingFee(undefined)
                        setFeeFormOpen(true)
                      }}
                    >
                      <Plus className="h-4 w-4" strokeWidth={2.5} />
                      Add fee structure
                    </Button>
                  )
                }
              />
            )}
          </div>
        </>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
          {loadingPayments ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : payments && payments.data.length > 0 ? (
            <>
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Receipt</th>
                    <th className="px-4 py-3 font-medium">Student</th>
                    <th className="px-4 py-3 font-medium">Invoice</th>
                    <th className="px-4 py-3 font-medium">Method</th>
                    <th className="px-4 py-3 font-medium">Date</th>
                    <th className="px-4 py-3 font-medium text-right">Amount</th>
                    <th className="w-12 px-2 py-3" />
                  </tr>
                </thead>
                <tbody>
                  {payments.data.map((p) => (
                    <tr
                      key={p.id}
                      onClick={() => navigate(`/finance/invoices/${p.invoiceId}`)}
                      className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                    >
                      <td className="px-4 py-3 font-mono text-xs text-slate-600 dark:text-slate-300">
                        {p.receiptNumber}
                      </td>
                      <td className="px-4 py-3 text-slate-700 dark:text-slate-200">{p.studentName}</td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{p.invoiceNumber}</td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                        {PAYMENT_METHOD_LABELS[p.paymentMethod as keyof typeof PAYMENT_METHOD_LABELS] ?? p.paymentMethod}
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                        {new Date(p.paymentDate).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3 text-right font-medium text-slate-800 dark:text-slate-100">
                        {currency.format(p.amount)}
                      </td>
                      <td className="px-2 py-3 text-right">
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            downloadReceipt(p.id, p.receiptNumber)
                          }}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
                          aria-label="Download receipt"
                          title="Download receipt"
                        >
                          <Download className="h-3.5 w-3.5" strokeWidth={2} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination
                pageNumber={payments.page}
                totalPages={paymentsTotalPages}
                totalCount={payments.total}
                pageSize={payments.pageSize}
                hasPreviousPage={payments.page > 1}
                hasNextPage={payments.page * payments.pageSize < payments.total}
                onPageChange={(p) => updateParam('page', String(p))}
              />
            </>
          ) : (
            <EmptyState icon={Receipt} title="No payments recorded yet" description="Payments will appear here once recorded against an invoice." />
          )}
        </div>
      )}

      <GenerateInvoicesDrawer open={generateOpen} onClose={() => setGenerateOpen(false)} />
      <CreateInvoiceDrawer open={createInvoiceOpen} onClose={() => setCreateInvoiceOpen(false)} />
      <FeeStructureFormDrawer open={feeFormOpen} onClose={() => setFeeFormOpen(false)} feeStructure={editingFee} />
    </div>
  )
}
