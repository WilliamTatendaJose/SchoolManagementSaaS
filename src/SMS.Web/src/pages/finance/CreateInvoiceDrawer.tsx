import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { getErrorMessage } from '../../api/errors'
import { createInvoice } from '../../api/finance'
import { fetchStudents } from '../../api/students'
import type { CreateInvoiceItemRequest } from '../../api/types'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

function emptyItem(): CreateInvoiceItemRequest {
  return { description: '', amount: 0, quantity: 1 }
}

export function CreateInvoiceDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [studentId, setStudentId] = useState<string | null>(null)
  const [studentLabel, setStudentLabel] = useState('')
  const [termId, setTermId] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [discountAmount, setDiscountAmount] = useState('0')
  const [items, setItems] = useState<CreateInvoiceItemRequest[]>([emptyItem()])
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: results, isFetching } = useQuery({
    queryKey: ['students-search', debouncedSearch],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 6, searchTerm: debouncedSearch }),
    enabled: debouncedSearch.length >= 2 && !studentId,
  })
  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms(), enabled: open })

  useEffect(() => {
    if (open) {
      setSearch('')
      setStudentId(null)
      setStudentLabel('')
      setTermId('')
      setDueDate('')
      setDiscountAmount('0')
      setItems([emptyItem()])
      setError(null)
    }
  }, [open])

  useEffect(() => {
    if (terms && terms.length > 0 && !termId) {
      setTermId(terms.find((t) => t.isCurrent)?.id ?? terms[0].id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [terms])

  function updateItem(index: number, patch: Partial<CreateInvoiceItemRequest>) {
    setItems((prev) => prev.map((it, i) => (i === index ? { ...it, ...patch } : it)))
  }

  const total = items.reduce((sum, it) => sum + it.amount * it.quantity, 0)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!studentId) {
      setError('Search for and select a student first')
      return
    }
    if (items.length === 0 || items.some((it) => !it.description || it.amount <= 0)) {
      setError('Every line item needs a description and an amount greater than zero')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const created = await createInvoice({
        studentId,
        academicTermId: termId,
        dueDate,
        discountAmount: Number(discountAmount) || undefined,
        items,
      })
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      onClose()
      navigate(`/finance/invoices/${created.id}`)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create invoice'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Create invoice"
      description="Manually invoice a student outside bulk generation"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-invoice-form" loading={submitting}>
            Create invoice
          </Button>
        </>
      }
    >
      <form id="create-invoice-form" onSubmit={handleSubmit} className="space-y-4">
        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Student <span className="text-red-500">*</span>
          </span>
          {studentId ? (
            <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 dark:border-brand-900 dark:bg-brand-950/40">
              <span className="text-sm font-medium text-brand-800 dark:text-brand-200">{studentLabel}</span>
              <button
                type="button"
                onClick={() => {
                  setStudentId(null)
                  setStudentLabel('')
                }}
                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                Change
              </button>
            </div>
          ) : (
            <>
              <SearchInput
                placeholder="Search by name or student number…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                autoFocus
              />
              {debouncedSearch.length >= 2 && (
                <div className="mt-2 max-h-40 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                  {isFetching ? (
                    <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                  ) : results && results.items.length > 0 ? (
                    results.items.map((s) => (
                      <button
                        type="button"
                        key={s.id}
                        onClick={() => {
                          setStudentId(s.id)
                          setStudentLabel(`${s.fullName} (${s.studentNumber})`)
                        }}
                        className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                      >
                        <Avatar name={s.fullName} size="sm" />
                        <div>
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.fullName}</p>
                          <p className="text-xs text-slate-400">{s.studentNumber}</p>
                        </div>
                      </button>
                    ))
                  ) : (
                    <p className="px-3 py-3 text-sm text-slate-400">No students found.</p>
                  )}
                </div>
              )}
            </>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <SelectField label="Academic term" required value={termId} onChange={(e) => setTermId(e.target.value)}>
            <option value="">Select a term…</option>
            {terms?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.academicYearName} · {t.name}
              </option>
            ))}
          </SelectField>
          <TextField
            label="Due date"
            type="date"
            required
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </div>

        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Line items</span>
            <button
              type="button"
              onClick={() => setItems((prev) => [...prev, emptyItem()])}
              className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
            >
              <Plus className="h-3.5 w-3.5" strokeWidth={2.5} />
              Add item
            </button>
          </div>
          <div className="space-y-2">
            {items.map((item, i) => (
              <div key={i} className="flex items-center gap-2">
                <input
                  placeholder="Description"
                  value={item.description}
                  onChange={(e) => updateItem(i, { description: e.target.value })}
                  className="flex-1 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
                <input
                  type="number"
                  min={0}
                  step="0.01"
                  placeholder="Amount"
                  value={item.amount || ''}
                  onChange={(e) => updateItem(i, { amount: Number(e.target.value) })}
                  className="w-24 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
                <input
                  type="number"
                  min={1}
                  value={item.quantity}
                  onChange={(e) => updateItem(i, { quantity: Number(e.target.value) })}
                  className="w-16 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                />
                <button
                  type="button"
                  onClick={() => setItems((prev) => prev.filter((_, idx) => idx !== i))}
                  disabled={items.length === 1}
                  className="text-slate-400 hover:text-red-600 disabled:opacity-30 dark:hover:text-red-400"
                >
                  <X className="h-4 w-4" strokeWidth={2} />
                </button>
              </div>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Discount"
            type="number"
            min={0}
            step="0.01"
            value={discountAmount}
            onChange={(e) => setDiscountAmount(e.target.value)}
          />
          <div className="flex flex-col justify-end">
            <p className="text-xs text-slate-400">Total</p>
            <p className="text-lg font-semibold text-slate-900 dark:text-white">
              ${(total - (Number(discountAmount) || 0)).toFixed(2)}
            </p>
          </div>
        </div>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
