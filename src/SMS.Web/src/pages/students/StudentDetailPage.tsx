import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BadgeCheck,
  CalendarDays,
  Download,
  GraduationCap,
  Heart,
  MapPin,
  Pencil,
  Phone,
  ShieldAlert,
  ShieldCheck,
  Trash2,
  Wallet,
} from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { fetchAcademicTerms } from '../../api/academicTerms'
import { getErrorMessage, isNotFoundError } from '../../api/errors'
import { downloadReportCard } from '../../api/reportCards'
import { deleteStudent, fetchStudent } from '../../api/students'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge, StatusBadge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { ErrorState } from '../../components/ui/ErrorState'
import { PageHeader } from '../../components/ui/PageHeader'
import { StudentFormDrawer } from './StudentFormDrawer'

const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
      <p className="mt-0.5 text-sm text-slate-700 dark:text-slate-200">{value?.trim() || '—'}</p>
    </div>
  )
}

function ReportCardCard({ studentId, studentNumber }: { studentId: string; studentNumber: string }) {
  const [termId, setTermId] = useState('')
  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const { data: terms } = useQuery({ queryKey: ['academic-terms'], queryFn: () => fetchAcademicTerms() })

  async function handleDownload() {
    if (!termId) return
    setDownloadError(null)
    setDownloading(true)
    try {
      await downloadReportCard(studentId, termId, studentNumber)
    } catch (err) {
      setDownloadError(getErrorMessage(err, 'Could not generate the report card for this term.'))
    } finally {
      setDownloading(false)
    }
  }

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center gap-2 text-slate-900 dark:text-white">
        <GraduationCap className="h-4 w-4" strokeWidth={2} />
        <h3 className="text-sm font-semibold">Report card</h3>
      </div>
      <div className="mt-3 flex items-center gap-2">
        <select
          value={termId}
          onChange={(e) => setTermId(e.target.value)}
          className="flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
        >
          <option value="">Select a term…</option>
          {terms?.map((t) => (
            <option key={t.id} value={t.id}>
              {t.academicYearName} · {t.name}
            </option>
          ))}
        </select>
        <Button variant="secondary" onClick={handleDownload} loading={downloading} disabled={!termId}>
          <Download className="h-4 w-4" strokeWidth={2} />
          PDF
        </Button>
      </div>
      {downloadError && <p className="mt-2 text-xs text-red-600 dark:text-red-400">{downloadError}</p>}
    </div>
  )
}

export function StudentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canEdit = useAuthStore((s) => s.hasPermission('students.edit'))
  const canDelete = useAuthStore((s) => s.hasPermission('students.delete'))
  const canViewResults = useAuthStore((s) => s.hasPermission('results.view'))
  const [editOpen, setEditOpen] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const {
    data: student,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['student', id],
    queryFn: () => fetchStudent(id!),
    enabled: !!id,
    retry: (failureCount, err) => !isNotFoundError(err) && failureCount < 2,
  })

  async function handleDelete() {
    if (!student || !window.confirm(`Delete ${student.fullName}? This cannot be undone.`)) return
    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteStudent(student.id)
      await queryClient.invalidateQueries({ queryKey: ['students'] })
      navigate('/students')
    } catch (err) {
      setDeleteError(getErrorMessage(err, 'Could not delete student'))
      setDeleting(false)
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (isError) {
    return isNotFoundError(error) ? (
      <ErrorState title="Student not found" description="It may have been deleted, or the link is out of date." />
    ) : (
      <ErrorState description={getErrorMessage(error, 'Could not load this student.')} onRetry={() => refetch()} />
    )
  }

  if (!student) {
    return null
  }

  return (
    <div>
      <PageHeader
        title={student.fullName}
        backTo="/students"
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
          <Avatar name={student.fullName} size="lg" />
          <div className="flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{student.fullName}</h2>
              <StatusBadge status={student.status} />
            </div>
            <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
              {student.studentNumber} · {student.currentClassName ?? 'No class assigned'}
              {student.houseName ? ` · ${student.houseName} House` : ''}
            </p>
          </div>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-2 gap-4 lg:grid-cols-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-2 text-slate-400">
            <Wallet className="h-4 w-4" strokeWidth={2} />
            <span className="text-xs font-medium uppercase tracking-wide">Balance</span>
          </div>
          <p className="mt-1.5 text-xl font-semibold text-slate-900 dark:text-white">
            {currency.format(student.outstandingBalance)}
          </p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-2 text-slate-400">
            <CalendarDays className="h-4 w-4" strokeWidth={2} />
            <span className="text-xs font-medium uppercase tracking-wide">Attendance</span>
          </div>
          <p className="mt-1.5 text-xl font-semibold text-slate-900 dark:text-white">
            {student.attendancePercentage}%
          </p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-2 text-slate-400">
            <ShieldCheck className="h-4 w-4" strokeWidth={2} />
            <span className="text-xs font-medium uppercase tracking-wide">Merits</span>
          </div>
          <p className="mt-1.5 text-xl font-semibold text-slate-900 dark:text-white">{student.totalMerits}</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <div className="flex items-center gap-2 text-slate-400">
            <ShieldAlert className="h-4 w-4" strokeWidth={2} />
            <span className="text-xs font-medium uppercase tracking-wide">Demerits</span>
          </div>
          <p className="mt-1.5 text-xl font-semibold text-slate-900 dark:text-white">{student.totalDemerits}</p>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Personal information</h3>
          <div className="mt-4 grid grid-cols-2 gap-x-4 gap-y-4 sm:grid-cols-3">
            <InfoRow label="Date of birth" value={new Date(student.dateOfBirth).toLocaleDateString()} />
            <InfoRow label="Gender" value={student.gender} />
            <InfoRow label="Admission date" value={new Date(student.admissionDate).toLocaleDateString()} />
            <InfoRow label="National ID" value={student.nationalId} />
            <InfoRow label="Birth certificate #" value={student.birthCertificateNumber} />
            <InfoRow label="Nationality" value={student.nationality} />
            <InfoRow label="Religion" value={student.religion} />
            <InfoRow label="City" value={student.city} />
            <InfoRow label="Dormitory" value={student.dormitoryName} />
          </div>
          <div className="mt-4">
            <InfoRow label="Address" value={student.address} />
          </div>
          {(student.medicalNotes || student.specialNeeds) && (
            <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              {student.medicalNotes && (
                <div className="flex gap-2 rounded-lg bg-amber-50 p-3 dark:bg-amber-900/20">
                  <Heart className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" strokeWidth={2} />
                  <div>
                    <p className="text-xs font-medium uppercase tracking-wide text-amber-700 dark:text-amber-400">
                      Medical notes
                    </p>
                    <p className="mt-0.5 text-sm text-amber-900 dark:text-amber-200">{student.medicalNotes}</p>
                  </div>
                </div>
              )}
              {student.specialNeeds && (
                <div className="flex gap-2 rounded-lg bg-slate-50 p-3 dark:bg-slate-800/60">
                  <BadgeCheck className="mt-0.5 h-4 w-4 shrink-0 text-slate-500" strokeWidth={2} />
                  <div>
                    <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Special needs</p>
                    <p className="mt-0.5 text-sm text-slate-700 dark:text-slate-200">{student.specialNeeds}</p>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Guardians</h3>
          {student.guardians.length === 0 ? (
            <p className="mt-3 text-sm text-slate-400">No guardians linked yet.</p>
          ) : (
            <ul className="mt-3 space-y-3">
              {student.guardians.map((g) => (
                <li
                  key={g.id}
                  onClick={() => navigate(`/guardians/${g.id}`)}
                  className="cursor-pointer rounded-xl border border-slate-100 p-3 transition-colors hover:border-brand-200 hover:bg-brand-50/50 dark:border-slate-800 dark:hover:border-brand-900 dark:hover:bg-brand-950/30"
                >
                  <div className="flex items-center justify-between">
                    <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{g.fullName}</p>
                    {g.isPrimaryContact && <Badge tone="brand">Primary</Badge>}
                  </div>
                  <p className="text-xs text-slate-400">{g.relationship}</p>
                  {g.phone && (
                    <p className="mt-1 flex items-center gap-1 text-xs text-slate-500 dark:text-slate-400">
                      <Phone className="h-3 w-3" strokeWidth={2} />
                      {g.phone}
                    </p>
                  )}
                  {g.isEmergencyContact && (
                    <p className="mt-1 flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
                      <MapPin className="h-3 w-3" strokeWidth={2} />
                      Emergency contact
                    </p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>

        {canViewResults && <ReportCardCard studentId={student.id} studentNumber={student.studentNumber} />}
      </div>

      <StudentFormDrawer open={editOpen} onClose={() => setEditOpen(false)} student={student} />
    </div>
  )
}
