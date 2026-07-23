import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpenCheck, Download, FileText, Link2, Paperclip, Upload } from 'lucide-react'
import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { downloadStoredFile } from '../../api/files'
import { fetchChildAssignments, fetchChildMaterials, submitChildAssignment } from '../../api/portal'
import type { StudentAssignmentDto, StudentCourseMaterialDto } from '../../api/types'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'

const statusTone: Record<string, 'emerald' | 'brand' | 'amber' | 'red' | 'slate'> = {
  Graded: 'emerald',
  Submitted: 'brand',
  Late: 'amber',
}

export function PortalAssignmentsTab({ studentId }: { studentId: string }) {
  return (
    <div className="space-y-6">
      <MaterialsSection studentId={studentId} />
      <AssignmentsSection studentId={studentId} />
    </div>
  )
}

function SectionHeading({ children }: { children: string }) {
  return (
    <h3 className="mb-2 px-1 text-xs font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500">
      {children}
    </h3>
  )
}

function MaterialsSection({ studentId }: { studentId: string }) {
  const { data: materials, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-materials', studentId],
    queryFn: () => fetchChildMaterials(studentId),
  })

  if (isLoading) {
    return <div className="h-24 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
  }
  if (isError || !materials) {
    return <ErrorState description="Could not load course materials." onRetry={() => refetch()} />
  }
  if (materials.length === 0) {
    return null // Keep the Work tab focused on assignments when there are no materials.
  }

  return (
    <div>
      <SectionHeading>Course materials</SectionHeading>
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
          {materials.map((m) => (
            <MaterialRow key={m.id} material={m} />
          ))}
        </ul>
      </div>
    </div>
  )
}

function MaterialRow({ material: m }: { material: StudentCourseMaterialDto }) {
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${
          m.kind === 'File'
            ? 'bg-brand-50 text-brand-600 dark:bg-brand-900/30 dark:text-brand-300'
            : 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-300'
        }`}
      >
        {m.kind === 'File' ? <FileText className="h-4 w-4" /> : <Link2 className="h-4 w-4" />}
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-slate-900 dark:text-white">{m.title}</p>
        <p className="truncate text-xs text-slate-400">
          {m.subjectName}
          {m.termName ? ` · ${m.termName}` : ''}
        </p>
      </div>
      {m.link &&
        (m.kind === 'File' ? (
          <button
            type="button"
            onClick={() => downloadStoredFile(m.link!, m.fileName ?? undefined)}
            className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            <Download className="h-3.5 w-3.5" />
            Download
          </button>
        ) : (
          <a
            href={m.link}
            target="_blank"
            rel="noreferrer"
            className="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            <Link2 className="h-3.5 w-3.5" />
            Open
          </a>
        ))}
    </li>
  )
}

function AssignmentsSection({ studentId }: { studentId: string }) {
  const { data: assignments, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-assignments', studentId],
    queryFn: () => fetchChildAssignments(studentId),
  })

  if (isLoading) {
    return <div className="h-48 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
  }
  if (isError || !assignments) {
    return <ErrorState description="Could not load assignments." onRetry={() => refetch()} />
  }
  if (assignments.length === 0) {
    return (
      <EmptyState
        icon={BookOpenCheck}
        title="No assignments"
        description="Assignments set for your child's class will appear here."
      />
    )
  }

  return (
    <div>
      <SectionHeading>Assignments</SectionHeading>
      <div className="space-y-3">
        {assignments.map((a) => (
          <AssignmentCard key={a.assignmentId} studentId={studentId} assignment={a} />
        ))}
      </div>
    </div>
  )
}

function AssignmentCard({ studentId, assignment: a }: { studentId: string; assignment: StudentAssignmentDto }) {
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [comment, setComment] = useState('')

  const isGraded = a.submissionStatus === 'Graded'
  const overdue = !a.hasSubmitted && new Date(a.dueDate) < new Date()

  const submit = useMutation({
    mutationFn: () => submitChildAssignment(studentId, a.assignmentId, { comment: comment || undefined, attachment: file }),
    onSuccess: async () => {
      setOpen(false)
      setFile(null)
      setComment('')
      await queryClient.invalidateQueries({ queryKey: ['portal-assignments', studentId] })
    },
  })

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-medium text-slate-900 dark:text-white">{a.title}</p>
          <p className="text-xs text-slate-400">
            {a.subjectName} · Due{' '}
            <span className={overdue ? 'font-medium text-amber-600 dark:text-amber-400' : undefined}>
              {new Date(a.dueDate).toLocaleDateString()}
            </span>
          </p>
        </div>
        {a.submissionStatus ? (
          <Badge tone={statusTone[a.submissionStatus] ?? 'slate'}>{a.submissionStatus}</Badge>
        ) : (
          <Badge tone={overdue ? 'red' : 'slate'}>{overdue ? 'Overdue' : 'To do'}</Badge>
        )}
      </div>

      {a.description && <p className="mt-2 text-sm text-slate-600 dark:text-slate-300">{a.description}</p>}

      {isGraded && (
        <div className="mt-2 rounded-lg bg-emerald-50 px-3 py-2 text-sm dark:bg-emerald-950/30">
          <span className="font-semibold text-emerald-800 dark:text-emerald-300">Grade: {a.grade}</span>
          {a.feedback && <p className="mt-0.5 text-emerald-700 dark:text-emerald-300">{a.feedback}</p>}
        </div>
      )}

      <div className="mt-3 flex flex-wrap items-center gap-3">
        {a.attachmentUrl && (
          <button
            type="button"
            onClick={() => downloadStoredFile(a.attachmentUrl!, a.attachmentFileName ?? undefined)}
            className="flex items-center gap-1 text-sm text-brand-600 hover:underline dark:text-brand-300"
          >
            <Download className="h-3.5 w-3.5" strokeWidth={2} />
            Assignment file
          </button>
        )}
        {a.submissionAttachmentUrl && (
          <button
            type="button"
            onClick={() => downloadStoredFile(a.submissionAttachmentUrl!, a.submissionAttachmentFileName ?? undefined)}
            className="flex items-center gap-1 text-sm text-slate-500 hover:underline dark:text-slate-400"
          >
            <Paperclip className="h-3.5 w-3.5" strokeWidth={2} />
            {a.submissionAttachmentFileName ?? 'Your submission'}
          </button>
        )}
        {!isGraded && (
          <button
            onClick={() => setOpen((o) => !o)}
            className="ml-auto text-sm font-medium text-brand-600 hover:underline dark:text-brand-300"
          >
            {a.hasSubmitted ? 'Resubmit' : 'Submit work'}
          </button>
        )}
      </div>

      {open && !isGraded && (
        <div className="mt-3 space-y-2 rounded-lg border border-slate-200 bg-slate-50 p-3 dark:border-slate-800 dark:bg-slate-800/40">
          <input
            type="file"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="block w-full text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-white file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-slate-700 hover:file:bg-slate-100 dark:text-slate-300 dark:file:bg-slate-900 dark:file:text-slate-200"
          />
          <input
            type="text"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Optional note to the teacher"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
          />
          {submit.isError && (
            <p className="text-xs text-red-600 dark:text-red-400">{getErrorMessage(submit.error, 'Could not submit.')}</p>
          )}
          <div className="flex items-center justify-end gap-2">
            <Button variant="ghost" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button onClick={() => submit.mutate()} loading={submit.isPending} disabled={!file && !comment}>
              <Upload className="h-4 w-4" strokeWidth={2.5} />
              Submit
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
