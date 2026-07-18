import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { createSubject, updateSubject } from '../../api/subjects'
import type { SubjectDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextareaField, TextField } from '../../components/ui/Field'

interface SubjectFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  subject?: SubjectDto
}

export function SubjectFormDrawer({ open, onClose, subject }: SubjectFormDrawerProps) {
  const isEdit = !!subject
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [description, setDescription] = useState('')
  const [isCore, setIsCore] = useState(true)
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName(subject?.name ?? '')
      setCode(subject?.code ?? '')
      setDescription(subject?.description ?? '')
      setIsCore(subject?.isCore ?? true)
      setIsActive(subject?.isActive ?? true)
      setError(null)
    }
  }, [open, subject])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (isEdit && subject) {
        await updateSubject({ id: subject.id, name, code, description: description || undefined, isCore, isActive })
      } else {
        await createSubject({ name, code, description: description || undefined, isCore })
      }
      await queryClient.invalidateQueries({ queryKey: ['subjects'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update subject' : 'Could not create subject'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit subject' : 'Add subject'}
      description="Subjects can be assigned to teaching staff"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="subject-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create subject'}
          </Button>
        </>
      }
    >
      <form id="subject-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField
          label="Name"
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Mathematics"
        />
        <TextField label="Code" required value={code} onChange={(e) => setCode(e.target.value)} placeholder="MATH" />
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
              checked={isCore}
              onChange={(e) => setIsCore(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Core subject
          </label>
          {isEdit && (
            <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              Active
            </label>
          )}
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
