import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BellRing, CheckCircle2, Paperclip, Plus, Save } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { bulkGradeAssignment, fetchAssignmentRoster, remindNonSubmitters } from '../../api/assignments'
import { getErrorMessage } from '../../api/errors'
import type { AssignmentGradeEntry, AssignmentRosterRowDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { RecordSubmissionDrawer } from './RecordSubmissionDrawer'

const statusTone: Record<string, 'emerald' | 'amber' | 'red' | 'slate' | 'brand'> = {
  Graded: 'emerald',
  Submitted: 'brand',
  Late: 'amber',
  NotSubmitted: 'slate',
}

const statusLabel: Record<string, string> = {
  Graded: 'Graded',
  Submitted: 'Submitted',
  Late: 'Late',
  NotSubmitted: 'Not submitted',
}

interface RowEdit {
  grade: string
  feedback: string
}

export function AssignmentSubmissionsPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('assignments.manage'))

  const [recordOpen, setRecordOpen] = useState(false)
  const [edits, setEdits] = useState<Record<string, RowEdit>>({})
  const [dirty, setDirty] = useState<Set<string>>(new Set())
  const [channel, setChannel] = useState('SMS')
  const [banner, setBanner] = useState<{ tone: 'ok' | 'err'; text: string } | null>(null)

  const { data: roster, isLoading } = useQuery({
    queryKey: ['assignment-roster', id],
    queryFn: () => fetchAssignmentRoster(id!),
    enabled: !!id,
  })

  // Seed the editable grade/feedback state from the server rows whenever they change,
  // but keep any in-progress (dirty) edits the teacher hasn't saved yet.
  const seeded = useMemo(() => {
    const map: Record<string, RowEdit> = {}
    for (const row of roster?.rows ?? []) {
      map[row.studentId] = {
        grade: row.grade != null ? String(row.grade) : '',
        feedback: row.feedback ?? '',
      }
    }
    return map
  }, [roster])

  function editFor(row: AssignmentRosterRowDto): RowEdit {
    return edits[row.studentId] ?? seeded[row.studentId] ?? { grade: '', feedback: '' }
  }

  function updateEdit(studentId: string, patch: Partial<RowEdit>) {
    setEdits((prev) => ({ ...prev, [studentId]: { ...(prev[studentId] ?? seeded[studentId] ?? { grade: '', feedback: '' }), ...patch } }))
    setDirty((prev) => new Set(prev).add(studentId))
  }

  const saveMutation = useMutation({
    mutationFn: async () => {
      const entries: AssignmentGradeEntry[] = [...dirty]
        .map((studentId) => {
          const edit = edits[studentId]
          const grade = edit?.grade?.trim()
          return {
            studentId,
            grade: grade ? Number(grade) : null,
            feedback: edit?.feedback || null,
          }
        })
        .filter((e) => e.grade != null)
      if (entries.length === 0) throw new Error('Enter a grade for at least one student before saving.')
      await bulkGradeAssignment(id!, entries)
      return entries.length
    },
    onSuccess: async (count) => {
      setDirty(new Set())
      setEdits({})
      setBanner({ tone: 'ok', text: `Saved ${count} grade${count === 1 ? '' : 's'}.` })
      await queryClient.invalidateQueries({ queryKey: ['assignment-roster', id] })
      await queryClient.invalidateQueries({ queryKey: ['assignments'] })
    },
    onError: (err) => setBanner({ tone: 'err', text: getErrorMessage(err, 'Could not save grades.') }),
  })

  const remindMutation = useMutation({
    mutationFn: () => remindNonSubmitters(id!, channel),
    onSuccess: (result) =>
      setBanner({
        tone: 'ok',
        text: `Reminders sent: ${result.delivered} delivered${result.failed > 0 ? `, ${result.failed} failed` : ''} (${result.totalRecipients} guardian${result.totalRecipients === 1 ? '' : 's'}).`,
      }),
    onError: (err) => setBanner({ tone: 'err', text: getErrorMessage(err, 'Could not send reminders.') }),
  })

  function handleRemind() {
    if (!roster) return
    if (
      window.confirm(
        `Send a ${channel} reminder to the guardians of ${roster.missingCount} student${roster.missingCount === 1 ? '' : 's'} who haven't submitted?`,
      )
    ) {
      remindMutation.mutate()
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!roster) {
    return <p className="text-sm text-slate-500">Assignment not found.</p>
  }

  const dueDate = new Date(roster.dueDate)
  const isOverdue = dueDate < new Date()

  return (
    <div>
      <PageHeader
        title={roster.title}
        backTo="/assignments"
        actions={
          canManage && (
            <div className="flex items-center gap-2">
              <select
                value={channel}
                onChange={(e) => setChannel(e.target.value)}
                className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
              >
                <option value="SMS">SMS</option>
                <option value="WhatsApp">WhatsApp</option>
              </select>
              <Button
                variant="secondary"
                onClick={handleRemind}
                loading={remindMutation.isPending}
                disabled={roster.missingCount === 0}
                title={roster.missingCount === 0 ? 'Everyone has submitted' : undefined}
              >
                <BellRing className="h-4 w-4" strokeWidth={2.5} />
                Remind {roster.missingCount > 0 ? `(${roster.missingCount})` : ''}
              </Button>
              <Button onClick={() => setRecordOpen(true)}>
                <Plus className="h-4 w-4" strokeWidth={2.5} />
                Record submission
              </Button>
            </div>
          )
        }
      />

      {banner && (
        <div
          className={`mb-4 flex items-start gap-2 rounded-xl border p-3 text-sm ${
            banner.tone === 'ok'
              ? 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900/50 dark:bg-emerald-950/30 dark:text-emerald-300'
              : 'border-red-200 bg-red-50 text-red-700 dark:border-red-900/50 dark:bg-red-950/30 dark:text-red-300'
          }`}
        >
          <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" strokeWidth={2} />
          <span>{banner.text}</span>
        </div>
      )}

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {roster.className} · {roster.subjectName} ·{' '}
          <span className={isOverdue ? 'font-medium text-amber-600 dark:text-amber-400' : undefined}>
            Due {dueDate.toLocaleDateString()}
            {isOverdue ? ' (overdue)' : ''}
          </span>
        </p>
        {roster.description && <p className="mt-3 text-sm text-slate-700 dark:text-slate-200">{roster.description}</p>}
        {roster.attachmentFileName && (
          <a
            href={roster.attachmentUrl ?? '#'}
            target="_blank"
            rel="noreferrer"
            className="mt-3 flex w-fit items-center gap-1.5 text-sm text-brand-600 hover:underline dark:text-brand-300"
          >
            <Paperclip className="h-3.5 w-3.5" strokeWidth={2} />
            {roster.attachmentFileName}
          </a>
        )}
      </div>

      <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-4">
        <ProgressTile label="On roster" value={roster.rosterCount} tone="slate" />
        <ProgressTile label="Submitted" value={roster.submittedCount} tone="brand" />
        <ProgressTile label="Graded" value={roster.gradedCount} tone="emerald" />
        <ProgressTile label="Missing" value={roster.missingCount} tone={roster.missingCount > 0 ? 'amber' : 'slate'} />
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {roster.rows.length === 0 ? (
          <p className="p-6 text-sm text-slate-400">No students are currently placed in this class.</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Student</th>
                    <th className="px-4 py-3 font-medium">Status</th>
                    <th className="px-4 py-3 font-medium">Work</th>
                    {canManage && <th className="w-24 px-4 py-3 font-medium">Grade</th>}
                    {canManage && <th className="px-4 py-3 font-medium">Feedback</th>}
                  </tr>
                </thead>
                <tbody>
                  {roster.rows.map((row) => {
                    const edit = editFor(row)
                    return (
                      <tr key={row.studentId} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                        <td className="px-4 py-2.5">
                          <p className="font-medium text-slate-900 dark:text-white">{row.studentName}</p>
                          <p className="text-xs text-slate-400">{row.studentNumber}</p>
                        </td>
                        <td className="px-4 py-2.5">
                          <Badge tone={statusTone[row.status] ?? 'slate'}>{statusLabel[row.status] ?? row.status}</Badge>
                          {row.submittedAt && (
                            <p className="mt-0.5 text-xs text-slate-400">{new Date(row.submittedAt).toLocaleDateString()}</p>
                          )}
                        </td>
                        <td className="px-4 py-2.5">
                          {row.attachmentUrl ? (
                            <a
                              href={row.attachmentUrl}
                              target="_blank"
                              rel="noreferrer"
                              className="flex w-fit items-center gap-1 text-brand-600 hover:underline dark:text-brand-300"
                              title={row.attachmentFileName ?? 'Attachment'}
                            >
                              <Paperclip className="h-3.5 w-3.5" strokeWidth={2} />
                              <span className="max-w-[10rem] truncate">{row.attachmentFileName ?? 'File'}</span>
                            </a>
                          ) : row.comment ? (
                            <span className="text-slate-500 dark:text-slate-400">{row.comment}</span>
                          ) : (
                            <span className="text-slate-300 dark:text-slate-600">—</span>
                          )}
                        </td>
                        {canManage && (
                          <td className="px-4 py-2.5">
                            <input
                              type="number"
                              min={0}
                              step="0.01"
                              value={edit.grade}
                              onChange={(e) => updateEdit(row.studentId, { grade: e.target.value })}
                              placeholder="—"
                              className="w-20 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                            />
                          </td>
                        )}
                        {canManage && (
                          <td className="px-4 py-2.5">
                            <input
                              type="text"
                              value={edit.feedback}
                              onChange={(e) => updateEdit(row.studentId, { feedback: e.target.value })}
                              placeholder="Optional feedback"
                              className="w-full min-w-[10rem] rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                            />
                          </td>
                        )}
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
            {canManage && (
              <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3 dark:border-slate-800">
                <span className="text-xs text-slate-400">
                  {dirty.size > 0 ? `${dirty.size} unsaved change${dirty.size === 1 ? '' : 's'}` : 'Enter grades, then save the whole sheet at once.'}
                </span>
                <Button onClick={() => saveMutation.mutate()} loading={saveMutation.isPending} disabled={dirty.size === 0}>
                  <Save className="h-4 w-4" strokeWidth={2.5} />
                  Save grades
                </Button>
              </div>
            )}
          </>
        )}
      </div>

      {id && <RecordSubmissionDrawer open={recordOpen} onClose={() => setRecordOpen(false)} assignmentId={id} />}
    </div>
  )
}

function ProgressTile({
  label,
  value,
  tone,
}: {
  label: string
  value: number
  tone: 'slate' | 'emerald' | 'amber' | 'brand'
}) {
  const toneClasses: Record<typeof tone, string> = {
    slate: 'bg-slate-50 text-slate-700 dark:bg-slate-800/60 dark:text-slate-200',
    emerald: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
    amber: 'bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
    brand: 'bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300',
  }
  return (
    <div className={`rounded-xl px-4 py-3 ${toneClasses[tone]}`}>
      <p className="text-2xl font-semibold tracking-tight">{value}</p>
      <p className="text-xs font-medium opacity-80">{label}</p>
    </div>
  )
}
