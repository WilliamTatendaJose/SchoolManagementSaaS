import { useQueryClient } from '@tanstack/react-query'
import { Plus, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { createAcademicYear, updateAcademicYear } from '../../api/academicYears'
import { getErrorMessage } from '../../api/errors'
import type { AcademicYearDto, CreateAcademicYearTermRequest } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

function emptyTerm(termNumber: number): CreateAcademicYearTermRequest {
  return { name: `Term ${termNumber}`, termNumber, startDate: '', endDate: '' }
}

interface AcademicYearFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  academicYear?: AcademicYearDto
}

export function AcademicYearFormDrawer({ open, onClose, academicYear }: AcademicYearFormDrawerProps) {
  const isEdit = !!academicYear
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [year, setYear] = useState(String(new Date().getFullYear()))
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [setAsCurrent, setSetAsCurrent] = useState(false)
  const [terms, setTerms] = useState<CreateAcademicYearTermRequest[]>([])
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      if (academicYear) {
        setName(academicYear.name)
        setYear(String(academicYear.year))
        setStartDate(academicYear.startDate.slice(0, 10))
        setEndDate(academicYear.endDate.slice(0, 10))
        setSetAsCurrent(academicYear.isCurrent)
      } else {
        const y = new Date().getFullYear()
        setName(`${y} Academic Year`)
        setYear(String(y))
        setStartDate('')
        setEndDate('')
        setSetAsCurrent(false)
      }
      setTerms([])
      setError(null)
    }
  }, [open, academicYear])

  function updateTerm(index: number, patch: Partial<CreateAcademicYearTermRequest>) {
    setTerms((prev) => prev.map((t, i) => (i === index ? { ...t, ...patch } : t)))
  }

  function addTerm() {
    setTerms((prev) => [...prev, emptyTerm(prev.length + 1)])
  }

  function removeTerm(index: number) {
    setTerms((prev) => prev.filter((_, i) => i !== index))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (isEdit && academicYear) {
        await updateAcademicYear({
          id: academicYear.id,
          name,
          year: Number(year),
          startDate,
          endDate,
          setAsCurrent,
        })
      } else {
        await createAcademicYear({
          name,
          year: Number(year),
          startDate,
          endDate,
          setAsCurrent,
          terms,
        })
      }
      await queryClient.invalidateQueries({ queryKey: ['academic-years'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update academic year' : 'Could not create academic year'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit academic year' : 'Add academic year'}
      description="Academic years drive enrollment, promotion and results"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="academic-year-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create year'}
          </Button>
        </>
      }
    >
      <form id="academic-year-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Year" type="number" required value={year} onChange={(e) => setYear(e.target.value)} />
          <div className="flex items-end pb-2.5">
            <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={setAsCurrent}
                onChange={(e) => setSetAsCurrent(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              Set as current year
            </label>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Start date"
            type="date"
            required
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
          <TextField label="End date" type="date" required value={endDate} onChange={(e) => setEndDate(e.target.value)} />
        </div>

        {!isEdit && (
          <div>
            <div className="mb-1.5 flex items-center justify-between">
              <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Terms</span>
              <button
                type="button"
                onClick={addTerm}
                className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                <Plus className="h-3.5 w-3.5" strokeWidth={2.5} />
                Add term
              </button>
            </div>
            {terms.length === 0 ? (
              <p className="text-xs text-slate-400">No terms added. You can add them later if needed.</p>
            ) : (
              <div className="space-y-3">
                {terms.map((term, i) => (
                  <div key={i} className="rounded-lg border border-slate-200 p-3 dark:border-slate-800">
                    <div className="mb-2 flex items-center justify-between">
                      <input
                        value={term.name}
                        onChange={(e) => updateTerm(i, { name: e.target.value })}
                        className="w-40 rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                      />
                      <button
                        type="button"
                        onClick={() => removeTerm(i)}
                        className="text-slate-400 hover:text-red-600 dark:hover:text-red-400"
                      >
                        <X className="h-4 w-4" strokeWidth={2} />
                      </button>
                    </div>
                    <div className="grid grid-cols-2 gap-2">
                      <input
                        type="date"
                        required
                        value={term.startDate}
                        onChange={(e) => updateTerm(i, { startDate: e.target.value })}
                        className="rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                      />
                      <input
                        type="date"
                        required
                        value={term.endDate}
                        onChange={(e) => updateTerm(i, { endDate: e.target.value })}
                        className="rounded border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
        {isEdit && (
          <p className="text-xs text-slate-400">
            Terms aren't editable here yet — {academicYear?.termCount ?? 0} term
            {academicYear?.termCount === 1 ? '' : 's'} on this year stay as-is.
          </p>
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
