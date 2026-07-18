import { useQuery, useQueryClient } from '@tanstack/react-query'
import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { sendMessage } from '../../api/messages'
import { getErrorMessage } from '../../api/errors'
import { fetchClasses } from '../../api/classes'
import { fetchStudents } from '../../api/students'
import { MESSAGE_AUDIENCE_LABELS, MESSAGE_AUDIENCES, MESSAGE_CHANNELS } from '../../api/types'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

export function SendMessageDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [channel, setChannel] = useState<(typeof MESSAGE_CHANNELS)[number]>('SMS')
  const [subject, setSubject] = useState('')
  const [content, setContent] = useState('')
  const [audience, setAudience] = useState<(typeof MESSAGE_AUDIENCES)[number]>('AllActiveStudents')
  const [classId, setClassId] = useState('')
  const [selectedStudents, setSelectedStudents] = useState<{ id: string; label: string }[]>([])
  const [studentSearch, setStudentSearch] = useState('')
  const [scheduled, setScheduled] = useState(false)
  const [scheduledAt, setScheduledAt] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<{ totalRecipients: number; delivered: number; failed: number } | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const debouncedSearch = useDebouncedValue(studentSearch, 300)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: searchResults, isFetching: searching } = useQuery({
    queryKey: ['students-search', debouncedSearch],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 6, searchTerm: debouncedSearch, status: 'Active' }),
    enabled: debouncedSearch.length >= 2 && audience === 'SpecificStudents',
  })

  useEffect(() => {
    if (open) {
      setChannel('SMS')
      setSubject('')
      setContent('')
      setAudience('AllActiveStudents')
      setClassId('')
      setSelectedStudents([])
      setStudentSearch('')
      setScheduled(false)
      setScheduledAt('')
      setError(null)
      setResult(null)
    }
  }, [open])

  const selectedIds = new Set(selectedStudents.map((s) => s.id))

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (audience === 'SpecificStudents' && selectedStudents.length === 0) {
      setError('Select at least one student')
      return
    }
    if (audience === 'Class' && !classId) {
      setError('Select a class')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const res = await sendMessage({
        channel,
        subject,
        content,
        audience,
        studentIds: audience === 'SpecificStudents' ? selectedStudents.map((s) => s.id) : undefined,
        classId: audience === 'Class' ? classId : undefined,
        scheduledAt: scheduled && scheduledAt ? new Date(scheduledAt).toISOString() : undefined,
      })
      setResult(res)
      await queryClient.invalidateQueries({ queryKey: ['messages'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not send message'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Compose message"
      description="Send an announcement to guardians"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            {result ? 'Close' : 'Cancel'}
          </Button>
          {!result && (
            <Button type="submit" form="send-message-form" loading={submitting}>
              {scheduled ? 'Schedule' : 'Send now'}
            </Button>
          )}
        </>
      }
    >
      <form id="send-message-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <SelectField label="Channel" value={channel} onChange={(e) => setChannel(e.target.value as typeof channel)}>
            {MESSAGE_CHANNELS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </SelectField>
          <SelectField label="Audience" value={audience} onChange={(e) => setAudience(e.target.value as typeof audience)}>
            {MESSAGE_AUDIENCES.map((a) => (
              <option key={a} value={a}>
                {MESSAGE_AUDIENCE_LABELS[a]}
              </option>
            ))}
          </SelectField>
        </div>

        {audience === 'Class' && (
          <SelectField label="Class" required value={classId} onChange={(e) => setClassId(e.target.value)}>
            <option value="">Select a class…</option>
            {classes?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </SelectField>
        )}

        {audience === 'SpecificStudents' && (
          <div>
            <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Students</span>
            {selectedStudents.length > 0 && (
              <div className="mb-2 flex flex-wrap gap-1.5">
                {selectedStudents.map((s) => (
                  <span
                    key={s.id}
                    className="flex items-center gap-1 rounded-full bg-brand-50 px-2.5 py-1 text-xs font-medium text-brand-700 dark:bg-brand-950/40 dark:text-brand-300"
                  >
                    {s.label}
                    <button
                      type="button"
                      onClick={() => setSelectedStudents((prev) => prev.filter((x) => x.id !== s.id))}
                      className="text-brand-500 hover:text-brand-700 dark:hover:text-brand-100"
                    >
                      <X className="h-3 w-3" strokeWidth={2.5} />
                    </button>
                  </span>
                ))}
              </div>
            )}
            <SearchInput
              placeholder="Search by name or student number…"
              value={studentSearch}
              onChange={(e) => setStudentSearch(e.target.value)}
            />
            {debouncedSearch.length >= 2 && (
              <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                {searching ? (
                  <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                ) : searchResults && searchResults.items.length > 0 ? (
                  searchResults.items.map((s) => (
                    <button
                      type="button"
                      key={s.id}
                      disabled={selectedIds.has(s.id)}
                      onClick={() => {
                        setSelectedStudents((prev) => [...prev, { id: s.id, label: s.fullName }])
                        setStudentSearch('')
                      }}
                      className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-800 dark:hover:bg-slate-800"
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
          </div>
        )}

        <TextField label="Subject" required value={subject} onChange={(e) => setSubject(e.target.value)} />
        <TextareaField
          label="Message"
          required
          rows={5}
          maxLength={2000}
          value={content}
          onChange={(e) => setContent(e.target.value)}
          hint={`${content.length}/2000`}
        />

        <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={scheduled}
            onChange={(e) => setScheduled(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
          />
          Schedule for later
        </label>
        {scheduled && (
          <TextField
            label="Send at"
            type="datetime-local"
            required
            value={scheduledAt}
            onChange={(e) => setScheduledAt(e.target.value)}
          />
        )}

        {result && (
          <div className="space-y-1 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
            {scheduled ? (
              <p>Message queued for {result.totalRecipients} recipients.</p>
            ) : (
              <p>
                Sent to {result.delivered} of {result.totalRecipients} recipients
                {result.failed > 0 ? ` (${result.failed} failed)` : ''}.
              </p>
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
