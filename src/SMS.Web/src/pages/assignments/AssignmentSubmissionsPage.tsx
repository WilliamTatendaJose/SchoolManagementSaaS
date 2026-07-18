import { useQuery } from '@tanstack/react-query'
import { Paperclip, Plus } from 'lucide-react'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { fetchAssignments, fetchSubmissions } from '../../api/assignments'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import type { AssignmentSubmissionDto } from '../../api/types'
import { GradeSubmissionDrawer } from './GradeSubmissionDrawer'
import { RecordSubmissionDrawer } from './RecordSubmissionDrawer'

const statusTone: Record<string, 'emerald' | 'amber' | 'red' | 'slate' | 'brand'> = {
  Graded: 'emerald',
  Submitted: 'brand',
  Late: 'amber',
}

export function AssignmentSubmissionsPage() {
  const { id } = useParams<{ id: string }>()
  const canManage = useAuthStore((s) => s.hasPermission('assignments.manage'))
  const [recordOpen, setRecordOpen] = useState(false)
  const [gradingSubmission, setGradingSubmission] = useState<AssignmentSubmissionDto | null>(null)

  const { data: assignments, isLoading: loadingAssignment } = useQuery({
    queryKey: ['assignments', {}],
    queryFn: () => fetchAssignments(),
  })
  const assignment = assignments?.find((a) => a.id === id)

  const { data: submissions, isLoading: loadingSubmissions } = useQuery({
    queryKey: ['assignment-submissions', id],
    queryFn: () => fetchSubmissions(id!),
    enabled: !!id,
  })

  if (loadingAssignment || loadingSubmissions) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!assignment) {
    return <p className="text-sm text-slate-500">Assignment not found.</p>
  }

  return (
    <div>
      <PageHeader
        title={assignment.title}
        backTo="/assignments"
        actions={
          canManage && (
            <Button onClick={() => setRecordOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Record submission
            </Button>
          )
        }
      />

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {assignment.className} · {assignment.subjectName} · Due {new Date(assignment.dueDate).toLocaleDateString()}
        </p>
        {assignment.description && (
          <p className="mt-3 text-sm text-slate-700 dark:text-slate-200">{assignment.description}</p>
        )}
        {assignment.attachmentFileName && (
          <p className="mt-3 flex w-fit items-center gap-1.5 text-sm text-slate-500 dark:text-slate-400">
            <Paperclip className="h-3.5 w-3.5" strokeWidth={2} />
            {assignment.attachmentFileName}
          </p>
        )}
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {submissions && submissions.length > 0 ? (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="px-4 py-3 font-medium">Student</th>
                <th className="px-4 py-3 font-medium">Submitted</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Grade</th>
                <th className="px-4 py-3 font-medium">Comment</th>
                {canManage && <th className="w-20 px-2 py-3" />}
              </tr>
            </thead>
            <tbody>
              {submissions.map((s) => (
                <tr key={s.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                  <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{s.studentName}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                    {new Date(s.submittedAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <Badge tone={statusTone[s.status] ?? 'slate'}>{s.status}</Badge>
                  </td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{s.grade ?? '—'}</td>
                  <td className="px-4 py-3 text-slate-500 dark:text-slate-400">
                    <div className="flex items-center gap-2">
                      <span>{s.comment ?? '—'}</span>
                      {s.attachmentUrl && (
                        <a
                          href={s.attachmentUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="text-brand-600 hover:underline dark:text-brand-300"
                          title={s.attachmentFileName ?? 'Attachment'}
                        >
                          <Paperclip className="h-3.5 w-3.5" strokeWidth={2} />
                        </a>
                      )}
                    </div>
                  </td>
                  {canManage && (
                    <td className="px-2 py-3">
                      <button
                        onClick={() => setGradingSubmission(s)}
                        className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                      >
                        {s.status === 'Graded' ? 'Edit grade' : 'Grade'}
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p className="p-6 text-sm text-slate-400">No submissions recorded yet.</p>
        )}
      </div>

      {id && <RecordSubmissionDrawer open={recordOpen} onClose={() => setRecordOpen(false)} assignmentId={id} />}
      {id && (
        <GradeSubmissionDrawer
          submission={gradingSubmission}
          assignmentId={id}
          onClose={() => setGradingSubmission(null)}
        />
      )}
    </div>
  )
}
