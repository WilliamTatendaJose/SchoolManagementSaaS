import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, CalendarRange, Layers, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { deleteAcademicYear, fetchAcademicYears } from '../../api/academicYears'
import { deleteClass, fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { deleteSubject, fetchSubjects } from '../../api/subjects'
import type { AcademicYearDto, ClassDto, SubjectDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { AcademicYearFormDrawer } from './AcademicYearFormDrawer'
import { ClassFormDrawer } from './ClassFormDrawer'
import { SubjectFormDrawer } from './SubjectFormDrawer'

function SectionCard({
  title,
  icon: Icon,
  canManage,
  onAdd,
  addLabel,
  children,
}: {
  title: string
  icon: typeof Layers
  canManage: boolean
  onAdd: () => void
  addLabel: string
  children: React.ReactNode
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4 dark:border-slate-800">
        <div className="flex items-center gap-2">
          <Icon className="h-4 w-4 text-slate-400" strokeWidth={2} />
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">{title}</h3>
        </div>
        {canManage && (
          <Button variant="secondary" onClick={onAdd} className="!px-2.5 !py-1.5 text-xs">
            <Plus className="h-3.5 w-3.5" strokeWidth={2.5} />
            {addLabel}
          </Button>
        )}
      </div>
      <div>{children}</div>
    </div>
  )
}

function RowActions({
  onEdit,
  onDelete,
  deleting,
}: {
  onEdit: () => void
  onDelete: () => void
  deleting: boolean
}) {
  return (
    <div className="flex items-center justify-end gap-1">
      <button
        onClick={onEdit}
        className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-300"
        aria-label="Edit"
      >
        <Pencil className="h-3.5 w-3.5" strokeWidth={2} />
      </button>
      <button
        onClick={onDelete}
        disabled={deleting}
        className="rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-50 dark:hover:bg-red-950/40 dark:hover:text-red-400"
        aria-label="Delete"
      >
        <Trash2 className="h-3.5 w-3.5" strokeWidth={2} />
      </button>
    </div>
  )
}

export function AcademicSetupPage() {
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('classes.manage'))
  const canManageSubjects = useAuthStore((s) => s.hasPermission('subjects.manage'))
  const [classOpen, setClassOpen] = useState(false)
  const [editingClass, setEditingClass] = useState<ClassDto | undefined>()
  const [subjectOpen, setSubjectOpen] = useState(false)
  const [editingSubject, setEditingSubject] = useState<SubjectDto | undefined>()
  const [yearOpen, setYearOpen] = useState(false)
  const [editingYear, setEditingYear] = useState<AcademicYearDto | undefined>()
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: classes, isLoading: loadingClasses } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses })
  const { data: subjects, isLoading: loadingSubjects } = useQuery({
    queryKey: ['subjects'],
    queryFn: () => fetchSubjects(),
  })
  const { data: years, isLoading: loadingYears } = useQuery({
    queryKey: ['academic-years'],
    queryFn: fetchAcademicYears,
  })

  function openAddClass() {
    setEditingClass(undefined)
    setClassOpen(true)
  }
  function openEditClass(c: ClassDto) {
    setEditingClass(c)
    setClassOpen(true)
  }
  async function handleDeleteClass(c: ClassDto) {
    if (!window.confirm(`Delete ${c.name}? This cannot be undone.`)) return
    setError(null)
    setDeletingId(c.id)
    try {
      await deleteClass(c.id)
      await queryClient.invalidateQueries({ queryKey: ['classes'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not delete class'))
    } finally {
      setDeletingId(null)
    }
  }

  function openAddSubject() {
    setEditingSubject(undefined)
    setSubjectOpen(true)
  }
  function openEditSubject(s: SubjectDto) {
    setEditingSubject(s)
    setSubjectOpen(true)
  }
  async function handleDeleteSubject(s: SubjectDto) {
    if (!window.confirm(`Delete ${s.name}? This cannot be undone.`)) return
    setError(null)
    setDeletingId(s.id)
    try {
      await deleteSubject(s.id)
      await queryClient.invalidateQueries({ queryKey: ['subjects'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not delete subject'))
    } finally {
      setDeletingId(null)
    }
  }

  function openAddYear() {
    setEditingYear(undefined)
    setYearOpen(true)
  }
  function openEditYear(y: AcademicYearDto) {
    setEditingYear(y)
    setYearOpen(true)
  }
  async function handleDeleteYear(y: AcademicYearDto) {
    if (!window.confirm(`Delete ${y.name}? This cannot be undone.`)) return
    setError(null)
    setDeletingId(y.id)
    try {
      await deleteAcademicYear(y.id)
      await queryClient.invalidateQueries({ queryKey: ['academic-years'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not delete academic year'))
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Classes & subjects"
        description="Foundational data used across enrollments, staff assignment and timetabling"
      />

      {error && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {error}
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <SectionCard title="Classes" icon={Layers} canManage={canManage} onAdd={openAddClass} addLabel="Add class">
          {loadingClasses ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : classes && classes.length > 0 ? (
            <table className="w-full text-left text-sm">
              <tbody>
                {classes.map((c) => (
                  <tr key={c.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-5 py-3">
                      <p className="font-medium text-slate-900 dark:text-white">{c.name}</p>
                      <p className="text-xs text-slate-400">
                        {c.code ?? '—'} · Level {c.level}
                        {c.streams.length > 0 ? ` · ${c.streams.length} stream${c.streams.length === 1 ? '' : 's'}` : ''}
                      </p>
                    </td>
                    <td className="px-5 py-3 text-right text-slate-500 dark:text-slate-400">
                      {c.studentCount}/{c.capacity}
                    </td>
                    {canManage && (
                      <td className="w-20 px-2 py-3">
                        <RowActions
                          onEdit={() => openEditClass(c)}
                          onDelete={() => handleDeleteClass(c)}
                          deleting={deletingId === c.id}
                        />
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={Layers}
              title="No classes yet"
              description="Add a class before enrolling students."
              action={
                canManage && (
                  <Button onClick={openAddClass}>
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add class
                  </Button>
                )
              }
            />
          )}
        </SectionCard>

        <SectionCard
          title="Subjects"
          icon={BookOpen}
          canManage={canManageSubjects}
          onAdd={openAddSubject}
          addLabel="Add subject"
        >
          {loadingSubjects ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : subjects && subjects.length > 0 ? (
            <table className="w-full text-left text-sm">
              <tbody>
                {subjects.map((s) => (
                  <tr key={s.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2">
                        <p className="font-medium text-slate-900 dark:text-white">{s.name}</p>
                        {s.isCore && <Badge tone="brand">Core</Badge>}
                        {!s.isActive && <Badge tone="slate">Inactive</Badge>}
                      </div>
                      <p className="text-xs text-slate-400">{s.code}</p>
                    </td>
                    <td className="px-5 py-3 text-right text-slate-500 dark:text-slate-400">
                      {s.teacherCount} teacher{s.teacherCount === 1 ? '' : 's'}
                    </td>
                    {canManageSubjects && (
                      <td className="w-20 px-2 py-3">
                        <RowActions
                          onEdit={() => openEditSubject(s)}
                          onDelete={() => handleDeleteSubject(s)}
                          deleting={deletingId === s.id}
                        />
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={BookOpen}
              title="No subjects yet"
              description="Add a subject before assigning it to teaching staff."
              action={
                canManageSubjects && (
                  <Button onClick={openAddSubject}>
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add subject
                  </Button>
                )
              }
            />
          )}
        </SectionCard>

        <SectionCard
          title="Academic years"
          icon={CalendarRange}
          canManage={canManage}
          onAdd={openAddYear}
          addLabel="Add year"
        >
          {loadingYears ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 2 }).map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
              ))}
            </div>
          ) : years && years.length > 0 ? (
            <table className="w-full text-left text-sm">
              <tbody>
                {years.map((y) => (
                  <tr key={y.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2">
                        <p className="font-medium text-slate-900 dark:text-white">{y.name}</p>
                        {y.isCurrent && <Badge tone="emerald">Current</Badge>}
                      </div>
                      <p className="text-xs text-slate-400">
                        {new Date(y.startDate).toLocaleDateString()} – {new Date(y.endDate).toLocaleDateString()}
                      </p>
                    </td>
                    <td className="px-5 py-3 text-right text-slate-500 dark:text-slate-400">
                      {y.termCount} term{y.termCount === 1 ? '' : 's'}
                    </td>
                    {canManage && (
                      <td className="w-20 px-2 py-3">
                        <RowActions
                          onEdit={() => openEditYear(y)}
                          onDelete={() => handleDeleteYear(y)}
                          deleting={deletingId === y.id}
                        />
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <EmptyState
              icon={CalendarRange}
              title="No academic years yet"
              description="Add one before enrolling or promoting students."
              action={
                canManage && (
                  <Button onClick={openAddYear}>
                    <Plus className="h-4 w-4" strokeWidth={2.5} />
                    Add year
                  </Button>
                )
              }
            />
          )}
        </SectionCard>
      </div>

      <ClassFormDrawer open={classOpen} onClose={() => setClassOpen(false)} classItem={editingClass} />
      <SubjectFormDrawer open={subjectOpen} onClose={() => setSubjectOpen(false)} subject={editingSubject} />
      <AcademicYearFormDrawer open={yearOpen} onClose={() => setYearOpen(false)} academicYear={editingYear} />
    </div>
  )
}
