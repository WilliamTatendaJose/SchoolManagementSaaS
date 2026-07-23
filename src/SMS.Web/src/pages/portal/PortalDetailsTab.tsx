import { useQuery } from '@tanstack/react-query'
import { Heart, Phone, ShieldAlert } from 'lucide-react'
import { fetchChildProfile } from '../../api/portal'
import { ErrorState } from '../../components/ui/ErrorState'

function Row({ label, value }: { label: string; value?: string | null }) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
      <p className="mt-0.5 text-sm text-slate-700 dark:text-slate-200">{value?.trim() || '—'}</p>
    </div>
  )
}

export function PortalDetailsTab({ studentId }: { studentId: string }) {
  const { data: student, isLoading, isError, refetch } = useQuery({
    queryKey: ['portal-profile', studentId],
    queryFn: () => fetchChildProfile(studentId),
  })

  if (isLoading) {
    return <div className="h-56 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/60" />
  }
  if (isError || !student) {
    return <ErrorState description="Could not load your child's details." onRetry={() => refetch()} />
  }

  return (
    <div className="space-y-3">
      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Personal details</h3>
        <div className="mt-3 grid grid-cols-2 gap-x-4 gap-y-3">
          <Row label="Student number" value={student.studentNumber} />
          <Row label="Class" value={student.currentClassName} />
          <Row label="Date of birth" value={new Date(student.dateOfBirth).toLocaleDateString()} />
          <Row label="Gender" value={student.gender} />
          <Row label="Admission date" value={new Date(student.admissionDate).toLocaleDateString()} />
          <Row label="Nationality" value={student.nationality} />
          {student.houseName && <Row label="House" value={student.houseName} />}
          {student.dormitoryName && <Row label="Dormitory" value={student.dormitoryName} />}
        </div>
        {student.address && (
          <div className="mt-3">
            <Row label="Address" value={`${student.address}${student.city ? `, ${student.city}` : ''}`} />
          </div>
        )}
      </div>

      {(student.medicalNotes || student.specialNeeds) && (
        <div className="space-y-2">
          {student.medicalNotes && (
            <div className="flex gap-2 rounded-2xl border border-amber-200 bg-amber-50 p-3 dark:border-amber-900/50 dark:bg-amber-950/30">
              <Heart className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" strokeWidth={2} />
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-amber-700 dark:text-amber-400">Medical notes</p>
                <p className="mt-0.5 text-sm text-amber-900 dark:text-amber-200">{student.medicalNotes}</p>
              </div>
            </div>
          )}
          {student.specialNeeds && (
            <div className="flex gap-2 rounded-2xl border border-slate-200 bg-slate-50 p-3 dark:border-slate-800 dark:bg-slate-800/60">
              <ShieldAlert className="mt-0.5 h-4 w-4 shrink-0 text-slate-500" strokeWidth={2} />
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Special needs</p>
                <p className="mt-0.5 text-sm text-slate-700 dark:text-slate-200">{student.specialNeeds}</p>
              </div>
            </div>
          )}
        </div>
      )}

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Guardians on record</h3>
        {student.guardians.length === 0 ? (
          <p className="mt-2 text-sm text-slate-400">No guardians on record.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {student.guardians.map((g) => (
              <li key={g.id} className="rounded-xl border border-slate-100 p-3 dark:border-slate-800">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{g.fullName}</p>
                  <span className="text-xs text-slate-400">{g.relationship}</span>
                </div>
                {g.phone && (
                  <p className="mt-1 flex items-center gap-1 text-xs text-slate-500 dark:text-slate-400">
                    <Phone className="h-3 w-3" strokeWidth={2} />
                    {g.phone}
                  </p>
                )}
              </li>
            ))}
          </ul>
        )}
        <p className="mt-3 text-xs text-slate-400">
          If any of these details are wrong, please contact the school office to update them.
        </p>
      </div>
    </div>
  )
}
