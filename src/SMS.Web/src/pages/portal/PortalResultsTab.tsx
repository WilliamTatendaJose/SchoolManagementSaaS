import { useQuery } from '@tanstack/react-query'
import { Download, GraduationCap } from 'lucide-react'
import { useState } from 'react'
import { downloadChildReportCard, fetchChildResults, fetchPortalTerms } from '../../api/portal'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'

const gradeTone: Record<string, 'emerald' | 'brand' | 'amber' | 'red' | 'slate'> = {
  'A*': 'emerald', A: 'emerald', B: 'brand', C: 'amber', D: 'amber', E: 'red', U: 'red',
}

export function PortalResultsTab({ studentId }: { studentId: string }) {
  const [termId, setTermId] = useState('')
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const { data: terms } = useQuery({ queryKey: ['portal-terms'], queryFn: fetchPortalTerms })

  const { data: results, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-results', studentId, termId],
    queryFn: () => fetchChildResults(studentId, termId || undefined),
  })

  async function handleDownload() {
    if (!termId || !results) return
    setDownloadError(null)
    setDownloading(true)
    try {
      await downloadChildReportCard(studentId, termId, results.studentNumber)
    } catch {
      setDownloadError('Could not download the report card. It may not be ready for this term yet.')
    } finally {
      setDownloading(false)
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <select
          value={termId}
          onChange={(e) => setTermId(e.target.value)}
          className="flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">All terms</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
            </option>
          ))}
        </select>
        <Button variant="secondary" onClick={handleDownload} loading={downloading} disabled={!termId}>
          <Download className="h-4 w-4" strokeWidth={2} />
          Report card
        </Button>
      </div>
      {!termId && (
        <p className="text-xs text-slate-400">Select a term to download that term's report card.</p>
      )}
      {downloadError && <p className="text-xs text-red-600 dark:text-red-400">{downloadError}</p>}

      {isLoading ? (
        <div className="h-40 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
      ) : isError ? (
        <ErrorState description="Could not load results." onRetry={() => refetch()} />
      ) : !results || results.subjectResults.length === 0 ? (
        <EmptyState icon={GraduationCap} title="No published results" description="Results will appear here once teachers publish them for this period." />
      ) : (
        <div className="space-y-3">
          <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs text-slate-400">Overall average</p>
                <p className="text-2xl font-semibold text-slate-900 dark:text-white">{results.overallAverage}%</p>
              </div>
              <Badge tone={gradeTone[results.overallGrade] ?? 'slate'}>{results.overallGrade}</Badge>
              {results.totalInClass > 0 && (
                <div className="text-right">
                  <p className="text-xs text-slate-400">Class rank</p>
                  <p className="text-sm font-semibold text-slate-700 dark:text-slate-200">
                    {results.classRank} / {results.totalInClass}
                  </p>
                </div>
              )}
            </div>
          </div>

          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                  <th className="px-4 py-2.5 font-medium">Subject</th>
                  <th className="px-4 py-2.5 text-right font-medium">%</th>
                  <th className="px-4 py-2.5 text-right font-medium">Grade</th>
                </tr>
              </thead>
              <tbody>
                {results.subjectResults.map((s) => (
                  <tr key={s.subjectId} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-4 py-2.5 font-medium text-slate-800 dark:text-slate-100">{s.subjectName}</td>
                    <td className="px-4 py-2.5 text-right text-slate-600 dark:text-slate-300">{s.averagePercentage}%</td>
                    <td className="px-4 py-2.5 text-right">
                      <Badge tone={gradeTone[s.grade] ?? 'slate'}>{s.grade}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
