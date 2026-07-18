import { useQuery } from '@tanstack/react-query'
import { BookOpenCheck, Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchAssignments } from '../../api/assignments'
import { fetchClasses } from '../../api/classes'
import { fetchSubjects } from '../../api/subjects'
import { useAuthStore } from '../../auth/authStore'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { AssignmentFormDrawer } from './AssignmentFormDrawer'

export function AssignmentsListPage() {
  const navigate = useNavigate()
  const canManage = useAuthStore((s) => s.hasPermission('assignments.manage'))
  const [params, setParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)

  const classId = params.get('classId') ?? ''
  const subjectId = params.get('subjectId') ?? ''
  const termId = params.get('termId') ?? ''

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })
  const { data: subjects } = useQuery({ queryKey: ['subjects'], queryFn: () => fetchSubjects(true) })
  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms() })

  const { data: assignments, isLoading } = useQuery({
    queryKey: ['assignments', { classId, subjectId, termId }],
    queryFn: () =>
      fetchAssignments({
        classId: classId || undefined,
        subjectId: subjectId || undefined,
        termId: termId || undefined,
      }),
  })

  return (
    <div>
      <PageHeader
        title="Assignments"
        description={assignments ? `${assignments.length} assignments` : undefined}
        actions={
          canManage && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add assignment
            </Button>
          )
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <select
          value={classId}
          onChange={(e) => updateParam('classId', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All classes</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <select
          value={subjectId}
          onChange={(e) => updateParam('subjectId', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All subjects</option>
          {subjects?.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <select
          value={termId}
          onChange={(e) => updateParam('termId', e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All terms</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
            ))}
          </div>
        ) : assignments && assignments.length > 0 ? (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="px-4 py-3 font-medium">Assignment</th>
                <th className="px-4 py-3 font-medium">Class</th>
                <th className="px-4 py-3 font-medium">Subject</th>
                <th className="px-4 py-3 font-medium">Due</th>
                <th className="px-4 py-3 font-medium text-right">Submissions</th>
              </tr>
            </thead>
            <tbody>
              {assignments.map((a) => (
                <tr
                  key={a.id}
                  onClick={() => navigate(`/assignments/${a.id}`)}
                  className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                >
                  <td className="px-4 py-3">
                    <p className="font-medium text-slate-900 dark:text-white">{a.title}</p>
                    {a.attachmentFileName && <p className="text-xs text-slate-400">{a.attachmentFileName}</p>}
                  </td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.className}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.subjectName}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                    {new Date(a.dueDate).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">{a.submissionCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <EmptyState
            icon={BookOpenCheck}
            title="No assignments found"
            description={
              classId || subjectId || termId ? 'Try adjusting your filters.' : 'Add your first assignment to get started.'
            }
            action={
              canManage &&
              !classId &&
              !subjectId &&
              !termId && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add assignment
                </Button>
              )
            }
          />
        )}
      </div>

      <AssignmentFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
