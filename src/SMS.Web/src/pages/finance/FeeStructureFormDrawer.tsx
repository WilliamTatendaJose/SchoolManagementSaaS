import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAcademicYears } from '../../api/academicYears'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { createFeeStructure, updateFeeStructure } from '../../api/feeStructures'
import type { FeeStructureDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'

interface FeeStructureFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  feeStructure?: FeeStructureDto
}

export function FeeStructureFormDrawer({ open, onClose, feeStructure }: FeeStructureFormDrawerProps) {
  const isEdit = !!feeStructure
  const queryClient = useQueryClient()
  const [classId, setClassId] = useState('')
  const [academicYearId, setAcademicYearId] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('0')
  const [feeType, setFeeType] = useState('Tuition')
  const [isRecurring, setIsRecurring] = useState(true)
  const [isOptional, setIsOptional] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: years } = useQuery({ queryKey: ['academic-years'], queryFn: fetchAcademicYears, enabled: open })

  useEffect(() => {
    if (open) {
      setClassId(feeStructure?.classId ?? '')
      setAcademicYearId(feeStructure?.academicYearId ?? '')
      setName(feeStructure?.name ?? '')
      setDescription(feeStructure?.description ?? '')
      setAmount(String(feeStructure?.amount ?? 0))
      setFeeType(feeStructure?.feeType ?? 'Tuition')
      setIsRecurring(feeStructure?.isRecurring ?? true)
      setIsOptional(feeStructure?.isOptional ?? false)
      setError(null)
    }
  }, [open, feeStructure])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (isEdit && feeStructure) {
        await updateFeeStructure({
          id: feeStructure.id,
          name,
          description: description || undefined,
          amount: Number(amount),
          feeType,
          isRecurring,
          isOptional,
        })
      } else {
        await createFeeStructure({
          classId,
          academicYearId,
          name,
          description: description || undefined,
          amount: Number(amount),
          feeType,
          isRecurring,
          isOptional,
        })
      }
      await queryClient.invalidateQueries({ queryKey: ['fee-structures'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update fee structure' : 'Could not create fee structure'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit fee structure' : 'Add fee structure'}
      description={isEdit ? `${feeStructure?.className} · ${feeStructure?.academicYearName}` : 'Defines a fee owed by students in a class for an academic year'}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="fee-structure-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create fee'}
          </Button>
        </>
      }
    >
      <form id="fee-structure-form" onSubmit={handleSubmit} className="space-y-4">
        {!isEdit && (
          <div className="grid grid-cols-2 gap-4">
            <SelectField label="Class" required value={classId} onChange={(e) => setClassId(e.target.value)}>
              <option value="">Select a class…</option>
              {classes?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              label="Academic year"
              required
              value={academicYearId}
              onChange={(e) => setAcademicYearId(e.target.value)}
            >
              <option value="">Select a year…</option>
              {years?.map((y) => (
                <option key={y.id} value={y.id}>
                  {y.name}
                </option>
              ))}
            </SelectField>
          </div>
        )}

        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} placeholder="Tuition fee" />
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Amount"
            type="number"
            min={0.01}
            step="0.01"
            required
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
          <TextField
            label="Fee type"
            required
            value={feeType}
            onChange={(e) => setFeeType(e.target.value)}
            placeholder="Tuition, Transport, Boarding…"
          />
        </div>
        <TextareaField
          label="Description"
          rows={2}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        <div className="space-y-2.5">
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isRecurring}
              onChange={(e) => setIsRecurring(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Recurring (charged every term)
          </label>
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isOptional}
              onChange={(e) => setIsOptional(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Optional (excluded from bulk generation unless included)
          </label>
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
