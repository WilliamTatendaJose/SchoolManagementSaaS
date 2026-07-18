import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, CalendarClock, Mail, Pencil, Phone, Plus, Star, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { deleteStaff, fetchStaffMember, removeSubjectFromTeacher } from '../../api/staff'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { AssignSubjectDrawer } from './AssignSubjectDrawer'
import { StaffFormDrawer } from './StaffFormDrawer'

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
      <p className="mt-0.5 text-sm text-slate-700 dark:text-slate-200">{value?.trim() || '—'}</p>
    </div>
  )
}

export function StaffDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canEdit = useAuthStore((s) => s.hasPermission('staff.edit'))
  const canDelete = useAuthStore((s) => s.hasPermission('staff.delete'))
  const [editOpen, setEditOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [removingSubjectId, setRemovingSubjectId] = useState<string | null>(null)
  const [subjectError, setSubjectError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const { data: staff, isLoading } = useQuery({
    queryKey: ['staff-member', id],
    queryFn: () => fetchStaffMember(id!),
    enabled: !!id,
  })

  async function handleRemoveSubject(subjectId: string) {
    if (!staff) return
    setSubjectError(null)
    setRemovingSubjectId(subjectId)
    try {
      await removeSubjectFromTeacher(staff.id, subjectId)
      await queryClient.invalidateQueries({ queryKey: ['staff-member', staff.id] })
    } catch (err) {
      setSubjectError(getErrorMessage(err, 'Could not remove subject'))
    } finally {
      setRemovingSubjectId(null)
    }
  }

  async function handleDelete() {
    if (!staff || !window.confirm(`Delete ${staff.fullName}? This cannot be undone.`)) return
    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteStaff(staff.id)
      await queryClient.invalidateQueries({ queryKey: ['staff'] })
      navigate('/staff')
    } catch (err) {
      setDeleteError(getErrorMessage(err, 'Could not delete staff member'))
      setDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!staff) {
    return <p className="text-sm text-slate-500">Staff member not found.</p>
  }

  return (
    <div>
      <PageHeader
        title={staff.fullName}
        backTo="/staff"
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

      {deleteError && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {deleteError}
        </p>
      )}

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-wrap items-center gap-4">
          <Avatar name={staff.fullName} size="lg" />
          <div className="flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{staff.fullName}</h2>
              <Badge tone={staff.isActive ? 'emerald' : 'red'}>{staff.isActive ? 'Active' : 'Inactive'}</Badge>
              <Badge tone={staff.isTeacher ? 'brand' : 'slate'}>
                {staff.isTeacher ? 'Teaching' : 'Non-teaching'}
              </Badge>
            </div>
            <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
              {staff.staffNumber} · {staff.jobTitle ?? 'No title set'}
              {staff.department ? ` · ${staff.department}` : ''}
            </p>
          </div>
        </div>
        <div className="mt-4 flex flex-wrap gap-6 text-sm text-slate-600 dark:text-slate-300">
          <span className="flex items-center gap-1.5">
            <Mail className="h-4 w-4 text-slate-400" strokeWidth={2} />
            {staff.email}
          </span>
          {staff.phone && (
            <span className="flex items-center gap-1.5">
              <Phone className="h-4 w-4 text-slate-400" strokeWidth={2} />
              {staff.phone}
            </span>
          )}
          {staff.dateOfJoining && (
            <span className="flex items-center gap-1.5">
              <CalendarClock className="h-4 w-4 text-slate-400" strokeWidth={2} />
              Joined {new Date(staff.dateOfJoining).toLocaleDateString()}
            </span>
          )}
        </div>
      </div>

      {staff.pendingLeaveRequests > 0 && (
        <div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-300">
          {staff.pendingLeaveRequests} pending leave {staff.pendingLeaveRequests === 1 ? 'request' : 'requests'}
        </div>
      )}

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Profile</h3>
          <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-4 sm:grid-cols-3">
            <InfoRow label="Department" value={staff.department} />
            <InfoRow label="Job title" value={staff.jobTitle} />
            <InfoRow label="Specialization" value={staff.specialization} />
          </div>
          <div className="mt-4">
            <InfoRow label="Qualifications" value={staff.qualifications} />
          </div>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Subjects</h3>
            {staff.isTeacher && canEdit && (
              <button
                onClick={() => setAssignOpen(true)}
                className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                <Plus className="h-3.5 w-3.5" strokeWidth={2.5} />
                Assign
              </button>
            )}
          </div>

          {subjectError && (
            <p className="mt-2 rounded-lg bg-red-50 px-3 py-2 text-xs text-red-700 dark:bg-red-950/40 dark:text-red-300">
              {subjectError}
            </p>
          )}

          {!staff.isTeacher ? (
            <p className="mt-3 text-sm text-slate-400">Not a teaching staff member.</p>
          ) : staff.subjects.length === 0 ? (
            <p className="mt-3 text-sm text-slate-400">No subjects assigned yet.</p>
          ) : (
            <ul className="mt-3 space-y-2">
              {staff.subjects.map((sub) => (
                <li
                  key={sub.subjectId}
                  className="flex items-center justify-between rounded-xl border border-slate-100 px-3 py-2 dark:border-slate-800"
                >
                  <div className="flex items-center gap-2">
                    <BookOpen className="h-4 w-4 text-slate-400" strokeWidth={2} />
                    <span className="text-sm text-slate-700 dark:text-slate-200">{sub.subjectName}</span>
                    {sub.isPrimary && (
                      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400" strokeWidth={0} />
                    )}
                  </div>
                  {canEdit && (
                    <button
                      onClick={() => handleRemoveSubject(sub.subjectId)}
                      disabled={removingSubjectId === sub.subjectId}
                      className="text-slate-400 hover:text-red-600 disabled:opacity-50 dark:hover:text-red-400"
                      aria-label={`Remove ${sub.subjectName}`}
                    >
                      <X className="h-3.5 w-3.5" strokeWidth={2} />
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <StaffFormDrawer open={editOpen} onClose={() => setEditOpen(false)} staff={staff} />
      <AssignSubjectDrawer
        open={assignOpen}
        onClose={() => setAssignOpen(false)}
        staffId={staff.id}
        assignedSubjects={staff.subjects}
      />
    </div>
  )
}
