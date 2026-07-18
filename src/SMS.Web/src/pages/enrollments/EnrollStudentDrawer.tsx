import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAcademicYears } from '../../api/academicYears'
import { fetchClasses } from '../../api/classes'
import { enrollStudent } from '../../api/enrollments'
import { getErrorMessage } from '../../api/errors'
import { fetchStudents } from '../../api/students'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

export function EnrollStudentDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [studentId, setStudentId] = useState<string | null>(null)
  const [studentLabel, setStudentLabel] = useState('')
  const [classId, setClassId] = useState('')
  const [streamId, setStreamId] = useState('')
  const [academicYearId, setAcademicYearId] = useState('')
  const [enrollmentDate, setEnrollmentDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: results, isFetching } = useQuery({
    queryKey: ['students-search', debouncedSearch],
    queryFn: () => fetchStudents({ pageNumber: 1, pageSize: 6, searchTerm: debouncedSearch }),
    enabled: debouncedSearch.length >= 2 && !studentId,
  })
  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: years } = useQuery({ queryKey: ['academic-years'], queryFn: fetchAcademicYears, enabled: open })

  const streams = classes?.find((c) => c.id === classId)?.streams ?? []

  function reset() {
    setSearch('')
    setStudentId(null)
    setStudentLabel('')
    setClassId('')
    setStreamId('')
    setAcademicYearId('')
    setEnrollmentDate(new Date().toISOString().slice(0, 10))
    setError(null)
  }

  useEffect(() => {
    if (open) {
      reset()
      if (years && years.length > 0) {
        setAcademicYearId(years.find((y) => y.isCurrent)?.id ?? years[0].id)
      }
    }
    // Intentionally omits `years` — this should only reset when the drawer opens,
    // not whenever a background refetch of years resolves with a new array reference
    // (which would otherwise wipe out whatever the user has already filled in).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  function handleClose() {
    reset()
    onClose()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!studentId) {
      setError('Search for and select a student first')
      return
    }
    if (!classId || !academicYearId) {
      setError('Class and academic year are required')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await enrollStudent({
        studentId,
        classId,
        streamId: streamId || null,
        academicYearId,
        enrollmentDate,
      })
      await queryClient.invalidateQueries({ queryKey: ['enrollments'] })
      handleClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not enroll student'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={handleClose}
      title="Enroll student"
      description="Place a student into a class for an academic year"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={handleClose}>
            Cancel
          </Button>
          <Button type="submit" form="enroll-student-form" loading={submitting}>
            Enroll
          </Button>
        </>
      }
    >
      <form id="enroll-student-form" onSubmit={handleSubmit} className="space-y-4">
        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Student <span className="text-red-500">*</span>
          </span>
          {studentId ? (
            <div className="flex items-center justify-between rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 dark:border-brand-900 dark:bg-brand-950/40">
              <span className="text-sm font-medium text-brand-800 dark:text-brand-200">{studentLabel}</span>
              <button
                type="button"
                onClick={() => {
                  setStudentId(null)
                  setStudentLabel('')
                }}
                className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                Change
              </button>
            </div>
          ) : (
            <>
              <SearchInput
                placeholder="Search by name or student number…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                autoFocus
              />
              {debouncedSearch.length >= 2 && (
                <div className="mt-2 max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
                  {isFetching ? (
                    <p className="px-3 py-3 text-sm text-slate-400">Searching…</p>
                  ) : results && results.items.length > 0 ? (
                    results.items.map((s) => (
                      <button
                        type="button"
                        key={s.id}
                        onClick={() => {
                          setStudentId(s.id)
                          setStudentLabel(`${s.fullName} (${s.studentNumber})`)
                        }}
                        className="flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                      >
                        <Avatar name={s.fullName} size="sm" />
                        <div>
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{s.fullName}</p>
                          <p className="text-xs text-slate-400">{s.studentNumber}</p>
                        </div>
                      </button>
                    ))
                  ) : (
                    <p className="px-3 py-3 text-sm text-slate-400">No students found.</p>
                  )}
                </div>
              )}
            </>
          )}
        </div>

        <SelectField
          label="Class"
          required
          value={classId}
          onChange={(e) => {
            setClassId(e.target.value)
            setStreamId('')
          }}
        >
          <option value="">Select a class…</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </SelectField>

        {streams.length > 0 && (
          <SelectField label="Stream" value={streamId} onChange={(e) => setStreamId(e.target.value)}>
            <option value="">Not assigned</option>
            {streams.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </SelectField>
        )}

        <div className="grid grid-cols-2 gap-4">
          <SelectField
            label="Academic year"
            required
            value={academicYearId}
            onChange={(e) => setAcademicYearId(e.target.value)}
          >
            <option value="">Select a year…</option>
            {years?.map((y) => (
              <option key={y.id} value={y.id}>
                {y.name}
                {y.isCurrent ? ' (current)' : ''}
              </option>
            ))}
          </SelectField>
          <TextField
            label="Enrollment date"
            type="date"
            required
            value={enrollmentDate}
            onChange={(e) => setEnrollmentDate(e.target.value)}
          />
        </div>

        {years && years.length === 0 && (
          <p className="text-xs text-amber-600 dark:text-amber-400">
            No academic years exist yet. Create one before enrolling students.
          </p>
        )}

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
