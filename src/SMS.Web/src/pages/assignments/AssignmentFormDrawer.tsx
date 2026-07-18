import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { createAssignment } from '../../api/assignments'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { fetchSubjects } from '../../api/subjects'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'

export function AssignmentFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [classId, setClassId] = useState('')
  const [subjectId, setSubjectId] = useState('')
  const [termId, setTermId] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [attachment, setAttachment] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: subjects } = useQuery({ queryKey: ['subjects'], queryFn: () => fetchSubjects(true), enabled: open })
  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms(), enabled: open })

  useEffect(() => {
    if (open) {
      setTitle('')
      setDescription('')
      setClassId('')
      setSubjectId('')
      setTermId('')
      setDueDate('')
      setAttachment(null)
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
      const created = await createAssignment({
        classId,
        subjectId,
        academicTermId: termId,
        title,
        description: description || undefined,
        dueDate,
        attachment,
      })
      await queryClient.invalidateQueries({ queryKey: ['assignments'] })
      onClose()
      navigate(`/assignments/${created.id}`)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create assignment'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add assignment"
      description="Set homework for a class to submit and be graded"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="assignment-form" loading={submitting}>
            Create assignment
          </Button>
        </>
      }
    >
      <form id="assignment-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Title" required value={title} onChange={(e) => setTitle(e.target.value)} />

        <div className="grid grid-cols-2 gap-4">
          <SelectField label="Class" required value={classId} onChange={(e) => setClassId(e.target.value)}>
            <option value="">Select a class…</option>
            {classes?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
          <SelectField label="Subject" required value={subjectId} onChange={(e) => setSubjectId(e.target.value)}>
            <option value="">Select a subject…</option>
            {subjects?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </SelectField>
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

        <TextareaField
          label="Description"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Attachment</span>
          <input
            type="file"
            onChange={(e) => setAttachment(e.target.files?.[0] ?? null)}
            className="block w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-slate-700 hover:file:bg-slate-200 dark:text-slate-300 dark:file:bg-slate-800 dark:file:text-slate-200"
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
