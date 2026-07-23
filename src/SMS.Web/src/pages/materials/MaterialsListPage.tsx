import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Link2, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { fetchClasses } from '../../api/classes'
import { deleteCourseMaterial, fetchCourseMaterials } from '../../api/courseMaterials'
import { downloadStoredFile } from '../../api/files'
import { fetchSubjects } from '../../api/subjects'
import type { CourseMaterialDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { MaterialFormDrawer } from './MaterialFormDrawer'

export function MaterialsListPage() {
  const canManage = useAuthStore((s) => s.hasPermission('assignments.manage'))
  const queryClient = useQueryClient()
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

  const { data: materials, isLoading } = useQuery({
    queryKey: ['course-materials', { classId, subjectId, termId }],
    queryFn: () =>
      fetchCourseMaterials({
        classId: classId || undefined,
        subjectId: subjectId || undefined,
        termId: termId || undefined,
      }),
  })

  const removeMutation = useMutation({
    mutationFn: (id: string) => deleteCourseMaterial(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['course-materials'] }),
  })

  function handleDelete(m: CourseMaterialDto) {
    if (window.confirm(`Delete "${m.title}"? Students will no longer see it.`)) {
      removeMutation.mutate(m.id)
    }
  }

  const selectClass =
    'rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200'

  return (
    <div>
      <PageHeader
        title="Course materials"
        description={materials ? `${materials.length} materials` : 'Notes, worksheets and links shared with classes'}
        actions={
          canManage && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add material
            </Button>
          )
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <select value={classId} onChange={(e) => updateParam('classId', e.target.value)} className={selectClass}>
          <option value="">All classes</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <select value={subjectId} onChange={(e) => updateParam('subjectId', e.target.value)} className={selectClass}>
          <option value="">All subjects</option>
          {subjects?.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
        <select value={termId} onChange={(e) => updateParam('termId', e.target.value)} className={selectClass}>
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
        ) : materials && materials.length > 0 ? (
          <ul className="divide-y divide-slate-100 dark:divide-slate-800/60">
            {materials.map((m) => {
              const openClass =
                'inline-flex items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800'
              return (
                <li key={m.id} className="flex items-center gap-3 px-4 py-3">
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
                      {m.className} · {m.subjectName}
                      {m.termName ? ` · ${m.termName}` : ''}
                      {m.attachmentFileName ? ` · ${m.attachmentFileName}` : ''}
                    </p>
                  </div>
                  {!m.isPublished && <Badge tone="amber">Hidden</Badge>}
                  {m.kind === 'File' && m.downloadUrl ? (
                    <button
                      type="button"
                      onClick={() => downloadStoredFile(m.downloadUrl!, m.attachmentFileName ?? undefined)}
                      className={openClass}
                    >
                      <Download className="h-3.5 w-3.5" />
                      Download
                    </button>
                  ) : (
                    m.url && (
                      <a href={m.url} target="_blank" rel="noreferrer" className={openClass}>
                        <Link2 className="h-3.5 w-3.5" />
                        Open
                      </a>
                    )
                  )}
                  {canManage && (
                    <button
                      type="button"
                      onClick={() => handleDelete(m)}
                      title="Delete"
                      className="rounded-lg p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950/40"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  )}
                </li>
              )
            })}
          </ul>
        ) : (
          <EmptyState
            icon={FileText}
            title="No course materials yet"
            description={
              classId || subjectId || termId
                ? 'Try adjusting your filters.'
                : 'Share notes, worksheets or links with a class to get started.'
            }
            action={
              canManage &&
              !classId &&
              !subjectId &&
              !termId && (
                <Button onClick={() => setCreateOpen(true)}>
                  <Plus className="h-4 w-4" strokeWidth={2.5} />
                  Add material
                </Button>
              )
            }
          />
        )}
      </div>

      <MaterialFormDrawer open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  )
}
