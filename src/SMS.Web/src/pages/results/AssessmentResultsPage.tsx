import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Award, CheckCircle2, Users } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { fetchAssessmentResults, publishResults, recordResults } from '../../api/assessments'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { StatCard } from '../../components/ui/StatCard'

interface RowState {
  studentId: string
  studentNumber: string
  studentName: string
  score: string
  comment: string
  percentage?: number
  grade?: string
  rank?: number
}

export function AssessmentResultsPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const canRecord = useAuthStore((s) => s.hasPermission('results.record'))
  const canPublish = useAuthStore((s) => s.hasPermission('results.publish'))
  const [rows, setRows] = useState<RowState[]>([])
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [publishing, setPublishing] = useState(false)

  const { data: assessment, isLoading: loadingAssessment } = useQuery({
    queryKey: ['assessment-results', id],
    queryFn: () => fetchAssessmentResults(id!),
    enabled: !!id,
  })

  const { data: roster, isLoading: loadingRoster } = useQuery({
    queryKey: ['class-roster', assessment?.classId],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 200, classId: assessment!.classId }),
    enabled: !!assessment?.classId,
  })

  useEffect(() => {
    if (!roster || !assessment) return
    const existing = new Map(assessment.results.map((r) => [r.studentId, r]))
    setRows(
      roster.items.map((s) => {
        const r = existing.get(s.id)
        return {
          studentId: s.id,
          studentNumber: s.studentNumber,
          studentName: s.fullName,
          score: r ? String(r.score) : '',
          comment: r?.comment ?? '',
          percentage: r?.percentage,
          grade: r?.grade,
          rank: r?.rank,
        }
      }),
    )
  }, [roster, assessment])

  function updateRow(studentId: string, patch: Partial<RowState>) {
    setRows((prev) => prev.map((r) => (r.studentId === studentId ? { ...r, ...patch } : r)))
  }

  async function handleSave() {
    if (!id) return
    setError(null)
    setSaving(true)
    try {
      const toSubmit = rows
        .filter((r) => r.score.trim() !== '')
        .map((r) => ({ studentId: r.studentId, score: Number(r.score), comment: r.comment || undefined }))
      if (toSubmit.length === 0) {
        setError('Enter at least one score before saving')
        setSaving(false)
        return
      }
      await recordResults(id, toSubmit)
      await queryClient.invalidateQueries({ queryKey: ['assessment-results', id] })
      await queryClient.invalidateQueries({ queryKey: ['assessments'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save results'))
    } finally {
      setSaving(false)
    }
  }

  async function handlePublish() {
    if (!id || !assessment) return
    if (!window.confirm('Publish these results? Students and guardians will be able to see them, and this cannot be undone.'))
      return
    setError(null)
    setPublishing(true)
    try {
      await publishResults(id)
      await queryClient.invalidateQueries({ queryKey: ['assessment-results', id] })
      await queryClient.invalidateQueries({ queryKey: ['assessments'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not publish results'))
    } finally {
      setPublishing(false)
    }
  }

  if (loadingAssessment || loadingRoster) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!assessment) {
    return <p className="text-sm text-slate-500">Assessment not found.</p>
  }

  const stats = assessment.statistics

  return (
    <div>
      <PageHeader
        title={assessment.assessmentName}
        backTo="/results"
        actions={
          <>
            {canRecord && !assessment.isPublished && (
              <Button variant="secondary" onClick={handleSave} loading={saving}>
                Save results
              </Button>
            )}
            {canPublish && !assessment.isPublished && (
              <Button onClick={handlePublish} loading={publishing}>
                <CheckCircle2 className="h-4 w-4" strokeWidth={2} />
                Publish
              </Button>
            )}
          </>
        }
      />

      <div className="mb-4 flex items-center gap-2">
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {assessment.className} · {assessment.subjectName} · Max {assessment.maxScore} pts
        </p>
        <Badge tone={assessment.isPublished ? 'emerald' : 'amber'}>
          {assessment.isPublished ? 'Published' : 'Draft'}
        </Badge>
      </div>

      {error && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {error}
        </p>
      )}

      <div className="mb-4 grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="Recorded"
          value={`${stats.resultsRecorded}/${stats.totalStudents}`}
          icon={Users}
          tone="brand"
        />
        <StatCard
          label="Average"
          value={stats.averagePercentage != null ? `${stats.averagePercentage}%` : '—'}
          icon={Award}
          tone="slate"
          hint={stats.averageScore != null ? `${stats.averageScore} / ${assessment.maxScore}` : undefined}
        />
        <StatCard label="Pass rate" value={`${stats.passRate}%`} icon={CheckCircle2} tone="emerald" hint={`${stats.passCount} passed, ${stats.failCount} failed`} />
        <StatCard
          label="Range"
          value={stats.highestScore != null ? `${stats.lowestScore}–${stats.highestScore}` : '—'}
          icon={Award}
          tone="amber"
        />
      </div>

      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
              <th className="px-4 py-3 font-medium">Student</th>
              <th className="px-4 py-3 font-medium">Score</th>
              <th className="px-4 py-3 font-medium">%</th>
              <th className="px-4 py-3 font-medium">Grade</th>
              <th className="px-4 py-3 font-medium">Rank</th>
              <th className="px-4 py-3 font-medium">Comment</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.studentId} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                <td className="px-4 py-2.5">
                  <p className="font-medium text-slate-900 dark:text-white">{r.studentName}</p>
                  <p className="text-xs text-slate-400">{r.studentNumber}</p>
                </td>
                <td className="px-4 py-2.5">
                  {canRecord && !assessment.isPublished ? (
                    <input
                      type="number"
                      min={0}
                      max={assessment.maxScore}
                      step="0.01"
                      value={r.score}
                      onChange={(e) => updateRow(r.studentId, { score: e.target.value })}
                      className="w-20 rounded-lg border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                    />
                  ) : (
                    <span className="text-slate-700 dark:text-slate-200">{r.score || '—'}</span>
                  )}
                </td>
                <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">
                  {r.percentage != null ? `${r.percentage}%` : '—'}
                </td>
                <td className="px-4 py-2.5">{r.grade ? <Badge tone="slate">{r.grade}</Badge> : '—'}</td>
                <td className="px-4 py-2.5 text-slate-500 dark:text-slate-400">{r.rank ?? '—'}</td>
                <td className="px-4 py-2.5">
                  {canRecord && !assessment.isPublished ? (
                    <input
                      value={r.comment}
                      onChange={(e) => updateRow(r.studentId, { comment: e.target.value })}
                      className="w-full rounded-lg border border-slate-300 bg-white px-2 py-1 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-white"
                    />
                  ) : (
                    <span className="text-slate-500 dark:text-slate-400">{r.comment || '—'}</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
