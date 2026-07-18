import { useQuery } from '@tanstack/react-query'
import { GraduationCap, Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchAssessments } from '../../api/assessments'
import { fetchClasses } from '../../api/classes'
import { fetchSubjects } from '../../api/subjects'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { AssessmentFormDrawer } from './AssessmentFormDrawer'

export function AssessmentsListPage() {
  const navigate = useNavigate()
  const canCreate = useAuthStore((s) => s.hasPermission('assessments.create'))
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

  const { data: assessments, isLoading } = useQuery({
    queryKey: ['assessments', { classId, subjectId, termId }],
    queryFn: () =>
      fetchAssessments({
        classId: classId || undefined,
        subjectId: subjectId || undefined,
        termId: termId || undefined,
      }),
  })

  return (
    <div>
      <PageHeader
        title="Assessments & results"
        description={assessments ? `${assessments.length} assessments` : undefined}
        actions={
          canCreate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add assessment
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
        ) : assessments && assessments.length > 0 ? (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                <th className="px-4 py-3 font-medium">Assessment</th>
                <th className="px-4 py-3 font-medium">Class</th>
                <th className="px-4 py-3 font-medium">Subject</th>
                <th className="px-4 py-3 font-medium">Term</th>
                <th className="px-4 py-3 font-medium text-right">Results</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {assessments.map((a) => (
                <tr
                  key={a.id}
                  onClick={() => navigate(`/results/${a.id}`)}
                  className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50 dark:border-slate-800/60 dark:hover:bg-slate-800/40"
                >
                  <td className="px-4 py-3">
                    <p className="font-medium text-slate-900 dark:text-white">{a.name}</p>
                    <p className="text-xs text-slate-400">
                      {a.assessmentType} · {a.maxScore} pts · {a.weightPercentage}% weight
                    </p>
                  </td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.className}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.subjectName}</td>
                  <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{a.termName}</td>
                  <td className="px-4 py-3 text-right text-slate-600 dark:text-slate-300">{a.resultCount}</td>
                  <td className="px-4 py-3">
                    <Badge tone={a.isPublished ? 'emerald' : 'amber'}>
                      {a.isPublished ? 'Published' : 'Draft'}
                    </Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <EmptyState
            icon={GraduationCap}
            title="No assessments found"
            description={
              classId || subjectId || termId
                ? 'Try adjusting your filters.'
                : 'Add your first assessment to start recording results.'
            }
            action={
              canCreate &&
              !classId &&
              !subjectId &&
              !termId && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add assessment
                </Button>
              )
            }
          />
        )}
      </div>

      <AssessmentFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
