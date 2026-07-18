import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createClass, updateClass } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import type { ClassDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

interface ClassFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  classItem?: ClassDto
}

export function ClassFormDrawer({ open, onClose, classItem }: ClassFormDrawerProps) {
  const isEdit = !!classItem
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [level, setLevel] = useState('1')
  const [capacity, setCapacity] = useState('40')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName(classItem?.name ?? '')
      setCode(classItem?.code ?? '')
      setLevel(String(classItem?.level ?? 1))
      setCapacity(String(classItem?.capacity ?? 40))
      setError(null)
    }
  }, [open, classItem])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (isEdit && classItem) {
        await updateClass({
          id: classItem.id,
          name,
          code: code || undefined,
          level: Number(level),
          capacity: Number(capacity),
        })
      } else {
        await createClass({
          name,
          code: code || undefined,
          level: Number(level),
          capacity: Number(capacity),
        })
      }
      await queryClient.invalidateQueries({ queryKey: ['classes'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update class' : 'Could not create class'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit class' : 'Add class'}
      description="Classes group students by grade/form"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="class-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create class'}
          </Button>
        </>
      }
    >
      <form id="class-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} placeholder="Form 1" />
        <TextField label="Code" value={code} onChange={(e) => setCode(e.target.value)} placeholder="F1" />
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Level"
            type="number"
            min={1}
            required
            value={level}
            onChange={(e) => setLevel(e.target.value)}
            hint="Grade order, used for sorting"
          />
          <TextField
            label="Capacity"
            type="number"
            min={1}
            required
            value={capacity}
            onChange={(e) => setCapacity(e.target.value)}
          />
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
