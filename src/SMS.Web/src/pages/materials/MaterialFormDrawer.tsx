import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchClasses } from '../../api/classes'
import { createCourseMaterial } from '../../api/courseMaterials'
import { getErrorMessage } from '../../api/errors'
import { fetchSubjects } from '../../api/subjects'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'

type Source = 'link' | 'file'

export function MaterialFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [classId, setClassId] = useState('')
  const [subjectId, setSubjectId] = useState('')
  const [termId, setTermId] = useState('')
  const [source, setSource] = useState<Source>('link')
  const [url, setUrl] = useState('')
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
      setSource('link')
      setUrl('')
      setAttachment(null)
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (source === 'link' && !url.trim()) {
      setError('Enter a link, or switch to a file upload.')
      return
    }
    if (source === 'file' && !attachment) {
      setError('Choose a file, or switch to a link.')
      return
    }

    setSubmitting(true)
    try {
      await createCourseMaterial({
        classId,
        subjectId,
        academicTermId: termId || undefined,
        title,
        description: description || undefined,
        url: source === 'link' ? url.trim() : undefined,
        attachment: source === 'file' ? attachment : undefined,
      })
      await queryClient.invalidateQueries({ queryKey: ['course-materials'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not add material'))
    } finally {
      setSubmitting(false)
    }
  }

  const tabClass = (active: boolean) =>
    `flex-1 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
      active
        ? 'bg-white text-slate-900 shadow-sm dark:bg-slate-700 dark:text-white'
        : 'text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200'
    }`

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add course material"
      description="Share notes, worksheets or a link with a class"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="material-form" loading={submitting}>
            Add material
          </Button>
        </>
      }
    >
      <form id="material-form" onSubmit={handleSubmit} className="space-y-4">
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

        <SelectField label="Term (optional)" value={termId} onChange={(e) => setTermId(e.target.value)}>
          <option value="">Any term</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
            </option>
          ))}
        </SelectField>

        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Material</span>
          <div className="mb-3 flex gap-1 rounded-lg bg-slate-100 p-1 dark:bg-slate-800">
            <button type="button" className={tabClass(source === 'link')} onClick={() => setSource('link')}>
              Link
            </button>
            <button type="button" className={tabClass(source === 'file')} onClick={() => setSource('file')}>
              File upload
            </button>
          </div>

          {source === 'link' ? (
            <TextField
              label="Link URL"
              type="url"
              placeholder="https://…"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
            />
          ) : (
            <input
              type="file"
              onChange={(e) => setAttachment(e.target.files?.[0] ?? null)}
              className="block w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-slate-700 hover:file:bg-slate-200 dark:text-slate-300 dark:file:bg-slate-800 dark:file:text-slate-200"
            />
          )}
        </div>

        <TextareaField
          label="Description"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
