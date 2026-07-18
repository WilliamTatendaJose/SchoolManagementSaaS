import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { generateInvoices } from '../../api/finance'
import type { InvoiceGenerationResultDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function GenerateInvoicesDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [termId, setTermId] = useState('')
  const [classId, setClassId] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [includeOptionalFees, setIncludeOptionalFees] = useState(false)
  const [siblingDiscountPercent, setSiblingDiscountPercent] = useState('0')
  const [carryForwardArrears, setCarryForwardArrears] = useState(true)
  const [result, setResult] = useState<InvoiceGenerationResultDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms(), enabled: open })
  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })

  useEffect(() => {
    if (open) {
      setTermId('')
      setClassId('')
      setDueDate('')
      setIncludeOptionalFees(false)
      setSiblingDiscountPercent('0')
      setCarryForwardArrears(true)
      setResult(null)
      setError(null)
    }
  }, [open])

  useEffect(() => {
    if (terms && terms.length > 0 && !termId) {
      setTermId(terms.find((t) => t.isCurrent)?.id ?? terms[0].id)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [terms])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await generateInvoices({
        academicTermId: termId,
        classId: classId || null,
        dueDate,
        includeOptionalFees,
        siblingDiscountPercent: Number(siblingDiscountPercent),
        carryForwardArrears,
      })
      setResult(res)
      await queryClient.invalidateQueries({ queryKey: ['invoices'] })
      await queryClient.invalidateQueries({ queryKey: ['finance-summary'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not generate invoices'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Generate invoices"
      description="Bulk-create term invoices from class fee structures for actively-enrolled students"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            {result ? 'Close' : 'Cancel'}
          </Button>
          <Button type="submit" form="generate-invoices-form" loading={submitting}>
            Generate
          </Button>
        </>
      }
    >
      <form id="generate-invoices-form" onSubmit={handleSubmit} className="space-y-4">
        <SelectField label="Academic term" required value={termId} onChange={(e) => setTermId(e.target.value)}>
          <option value="">Select a term…</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
              {t.isCurrent ? ' (current)' : ''}
            </option>
          ))}
        </SelectField>

        <SelectField label="Class" value={classId} onChange={(e) => setClassId(e.target.value)}>
          <option value="">All classes</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </SelectField>

        <div className="grid grid-cols-2 gap-4">
          <TextField label="Due date" type="date" required value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
          <TextField
            label="Sibling discount %"
            type="number"
            min={0}
            max={100}
            value={siblingDiscountPercent}
            onChange={(e) => setSiblingDiscountPercent(e.target.value)}
            hint="Applied to non-eldest siblings"
          />
        </div>

        <div className="space-y-2.5">
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={includeOptionalFees}
              onChange={(e) => setIncludeOptionalFees(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Include optional fees
          </label>
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={carryForwardArrears}
              onChange={(e) => setCarryForwardArrears(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Carry forward outstanding arrears
          </label>
        </div>

        {result && (
          <div className="space-y-1 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
            <p className="font-medium">{result.invoicesCreated} invoices created</p>
            <p>Total billed: ${result.totalBilled.toFixed(2)}</p>
            {result.totalDiscount > 0 && <p>Sibling discounts applied: ${result.totalDiscount.toFixed(2)}</p>}
            {result.totalArrearsCarriedForward > 0 && (
              <p>Arrears carried forward: ${result.totalArrearsCarriedForward.toFixed(2)}</p>
            )}
            {result.studentsSkipped > 0 && <p>{result.studentsSkipped} students already invoiced, skipped</p>}
            {result.studentsWithoutFees > 0 && (
              <p>{result.studentsWithoutFees} students had no matching fee structure</p>
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
