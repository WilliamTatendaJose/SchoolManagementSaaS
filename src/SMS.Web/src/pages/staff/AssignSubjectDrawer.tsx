import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { assignSubjectToTeacher } from '../../api/staff'
import { fetchSubjects } from '../../api/subjects'
import type { TeacherSubjectDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField } from '../../components/ui/Field'

export function AssignSubjectDrawer({
  open,
  onClose,
  staffId,
  assignedSubjects,
}: {
  open: boolean
  onClose: () => void
  staffId: string
  assignedSubjects: TeacherSubjectDto[]
}) {
  const queryClient = useQueryClient()
  const [subjectId, setSubjectId] = useState('')
  const [isPrimary, setIsPrimary] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: subjects } = useQuery({ queryKey: ['subjects', true], queryFn: () => fetchSubjects(true), enabled: open })

  const assignedIds = new Set(assignedSubjects.map((s) => s.subjectId))
  const available = subjects?.filter((s) => !assignedIds.has(s.id)) ?? []

  useEffect(() => {
    if (open) {
      setSubjectId('')
      setIsPrimary(false)
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!subjectId) {
      setError('Select a subject')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await assignSubjectToTeacher({ staffId, subjectId, isPrimary })
      await queryClient.invalidateQueries({ queryKey: ['staff-member', staffId] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not assign subject'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Assign subject"
      description="Add a subject this teacher is qualified to teach"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} type="button">
            Cancel
          </Button>
          <Button type="submit" form="assign-subject-form" loading={submitting}>
            Assign
          </Button>
        </>
      }
    >
      <form id="assign-subject-form" onSubmit={handleSubmit} className="space-y-4">
        <SelectField
          label="Subject"
          required
          value={subjectId}
          onChange={(e) => setSubjectId(e.target.value)}
        >
          <option value="">Select a subject…</option>
          {available.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name} ({s.code})
            </option>
          ))}
        </SelectField>

        {subjects && subjects.length === 0 && (
          <p className="text-xs text-slate-400">No subjects have been set up yet.</p>
        )}
        {subjects && subjects.length > 0 && available.length === 0 && (
          <p className="text-xs text-slate-400">All active subjects are already assigned to this teacher.</p>
        )}

        <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPrimary}
            onChange={(e) => setIsPrimary(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
          />
          Primary subject
        </label>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
