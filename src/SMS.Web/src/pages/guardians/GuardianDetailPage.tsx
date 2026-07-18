import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Briefcase, Link2, Mail, Pencil, Phone, Trash2, Unlink, Users } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getErrorMessage, isNotFoundError } from '../../api/errors'
import { deleteGuardian, fetchGuardian, unlinkGuardianFromStudent } from '../../api/guardians'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { GuardianFormDrawer } from './GuardianFormDrawer'
import { LinkStudentDrawer } from './LinkStudentDrawer'

function InfoRow({ icon: Icon, label, value }: { icon: typeof Phone; label: string; value?: string | null }) {
  if (!value) return null
  return (
    <div className="flex items-center gap-2.5 text-sm text-slate-600 dark:text-slate-300">
      <Icon className="h-4 w-4 shrink-0 text-slate-400" strokeWidth={2} />
      <span>
        {value}
        <span className="ml-1.5 text-xs text-slate-400">({label})</span>
      </span>
    </div>
  )
}

export function GuardianDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canEdit = useAuthStore((s) => s.hasPermission('guardians.edit'))
  const canDelete = useAuthStore((s) => s.hasPermission('guardians.delete'))
  const [editOpen, setEditOpen] = useState(false)
  const [linkOpen, setLinkOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [unlinkingId, setUnlinkingId] = useState<string | null>(null)

  const {
    data: guardian,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['guardian', id],
    queryFn: () => fetchGuardian(id!),
    enabled: !!id,
    retry: (failureCount, err) => !isNotFoundError(err) && failureCount < 2,
  })

  async function handleDelete() {
    if (!guardian || !window.confirm(`Delete ${guardian.fullName}? This cannot be undone.`)) return
    setDeleting(true)
    setActionError(null)
    try {
      await deleteGuardian(guardian.id)
      await queryClient.invalidateQueries({ queryKey: ['guardians'] })
      navigate('/guardians')
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not delete guardian'))
      setDeleting(false)
    }
  }

  async function handleUnlink(studentId: string) {
    if (!guardian || !window.confirm('Remove this student link?')) return
    setUnlinkingId(studentId)
    setActionError(null)
    try {
      await unlinkGuardianFromStudent(guardian.id, studentId)
      await queryClient.invalidateQueries({ queryKey: ['guardian', guardian.id] })
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not unlink student'))
    } finally {
      setUnlinkingId(null)
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (isError) {
    return isNotFoundError(error) ? (
      <ErrorState title="Guardian not found" description="It may have been deleted, or the link is out of date." />
    ) : (
      <ErrorState description={getErrorMessage(error, 'Could not load this guardian.')} onRetry={() => refetch()} />
    )
  }

  if (!guardian) {
    return null
  }

  return (
    <div>
      <PageHeader
        title={guardian.fullName}
        backTo="/guardians"
        actions={
          <>
            {canEdit && (
              <Button variant="secondary" onClick={() => setEditOpen(true)}>
                <Pencil className="h-4 w-4" strokeWidth={2} />
                Edit
              </Button>
            )}
            {canDelete && (
              <Button variant="danger" onClick={handleDelete} loading={deleting}>
                <Trash2 className="h-4 w-4" strokeWidth={2} />
                Delete
              </Button>
            )}
          </>
        }
      />

      {actionError && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {actionError}
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-4">
            <Avatar name={guardian.fullName} size="lg" />
            <div>
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{guardian.fullName}</h2>
              <p className="text-sm text-slate-500 dark:text-slate-400">{guardian.gender}</p>
            </div>
          </div>

          <div className="mt-5 space-y-3">
            <InfoRow icon={Phone} label="phone" value={guardian.phone} />
            <InfoRow icon={Phone} label="alternate" value={guardian.alternatePhone} />
            <InfoRow icon={Mail} label="email" value={guardian.email} />
            <InfoRow icon={Briefcase} label={guardian.employer ?? 'occupation'} value={guardian.occupation} />
          </div>

          {guardian.address && (
            <p className="mt-4 border-t border-slate-100 pt-4 text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400">
              {guardian.address}
            </p>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Linked students</h3>
            <Button variant="secondary" onClick={() => setLinkOpen(true)}>
              <Link2 className="h-4 w-4" strokeWidth={2} />
              Link student
            </Button>
          </div>

          {guardian.students.length === 0 ? (
            <EmptyState icon={Users} title="No students linked" description="Link this guardian to a student." />
          ) : (
            <ul className="mt-4 space-y-2">
              {guardian.students.map((s) => (
                <li
                  key={s.studentId}
                  className="flex items-center justify-between rounded-xl border border-slate-100 p-3 dark:border-slate-800"
                >
                  <div
                    onClick={() => navigate(`/students/${s.studentId}`)}
                    className="flex flex-1 cursor-pointer items-center gap-3"
                  >
                    <Avatar name={s.fullName} size="sm" />
                    <div>
                      <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.fullName}</p>
                      <p className="text-xs text-slate-400">
                        {s.studentNumber} {s.className ? `· ${s.className}` : ''}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge tone="slate">{s.relationship}</Badge>
                    {s.isPrimaryContact && <Badge tone="brand">Primary</Badge>}
                    {s.isEmergencyContact && <Badge tone="amber">Emergency</Badge>}
                    <button
                      onClick={() => handleUnlink(s.studentId)}
                      disabled={unlinkingId === s.studentId}
                      title="Unlink student"
                      className="rounded-lg p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-50 dark:hover:bg-red-950/40"
                    >
                      <Unlink className="h-4 w-4" strokeWidth={2} />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <GuardianFormDrawer open={editOpen} onClose={() => setEditOpen(false)} guardian={guardian} />
      {id && <LinkStudentDrawer open={linkOpen} onClose={() => setLinkOpen(false)} guardianId={id} />}
    </div>
  )
}
