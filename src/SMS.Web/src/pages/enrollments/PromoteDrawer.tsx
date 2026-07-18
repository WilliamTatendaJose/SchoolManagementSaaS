import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchAcademicYears } from '../../api/academicYears'
import { fetchClasses } from '../../api/classes'
import { fetchEnrollments, promoteStudents } from '../../api/enrollments'
import { getErrorMessage } from '../../api/errors'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function PromoteDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [fromClassId, setFromClassId] = useState('')
  const [fromYearId, setFromYearId] = useState('')
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [toClassId, setToClassId] = useState('')
  const [toStreamId, setToStreamId] = useState('')
  const [toYearId, setToYearId] = useState('')
  const [enrollmentDate, setEnrollmentDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<{ promotedCount: number; skippedCount: number } | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: years } = useQuery({ queryKey: ['academic-years'], queryFn: fetchAcademicYears, enabled: open })
  const toStreams = classes?.find((c) => c.id === toClassId)?.streams ?? []

  const { data: candidates, isFetching: loadingCandidates } = useQuery({
    queryKey: ['enrollments-promote-candidates', fromClassId, fromYearId],
    queryFn: () =>
      fetchEnrollments({ classId: fromClassId, academicYearId: fromYearId, isActive: true, pageSize: 200 }),
    enabled: open && !!fromClassId && !!fromYearId,
  })

  function reset() {
    setFromClassId('')
    setFromYearId('')
    setSelected(new Set())
    setToClassId('')
    setToStreamId('')
    setToYearId('')
    setEnrollmentDate(new Date().toISOString().slice(0, 10))
    setError(null)
    setResult(null)
  }

  useEffect(() => {
    if (open) {
      reset()
      if (years && years.length > 0) {
        setFromYearId(years.find((y) => y.isCurrent)?.id ?? years[0].id)
      }
    }
    // Intentionally omits `years` — only reset when the drawer opens, not whenever
    // a background refetch resolves with a new array reference mid-edit.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  useEffect(() => {
    setSelected(new Set())
  }, [fromClassId, fromYearId])

  function toggleStudent(studentId: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(studentId)) next.delete(studentId)
      else next.add(studentId)
      return next
    })
  }

  function toggleAll() {
    if (!candidates) return
    if (selected.size === candidates.items.length) {
      setSelected(new Set())
    } else {
      setSelected(new Set(candidates.items.map((c) => c.studentId)))
    }
  }

  function handleClose() {
    reset()
    onClose()
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (selected.size === 0) {
      setError('Select at least one student to promote')
      return
    }
    if (!toClassId || !toYearId) {
      setError('Destination class and academic year are required')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const res = await promoteStudents({
        toClassId,
        toStreamId: toStreamId || null,
        toAcademicYearId: toYearId,
        enrollmentDate,
        studentIds: Array.from(selected),
      })
      await queryClient.invalidateQueries({ queryKey: ['enrollments'] })
      setResult({ promotedCount: res.promotedCount, skippedCount: res.skippedStudentIds.length })
      setSelected(new Set())
    } catch (err) {
      setError(getErrorMessage(err, 'Could not promote students'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={handleClose}
      title="Promote students"
      description="Bulk-move students from one class into the next academic year"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={handleClose}>
            Close
          </Button>
          <Button type="submit" form="promote-form" loading={submitting}>
            Promote {selected.size > 0 ? `(${selected.size})` : ''}
          </Button>
        </>
      }
    >
      <form id="promote-form" onSubmit={handleSubmit} className="space-y-5">
        <div>
          <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-white">From</h3>
          <div className="grid grid-cols-2 gap-4">
            <SelectField label="Class" required value={fromClassId} onChange={(e) => setFromClassId(e.target.value)}>
              <option value="">Select a class…</option>
              {classes?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              label="Academic year"
              required
              value={fromYearId}
              onChange={(e) => setFromYearId(e.target.value)}
            >
              <option value="">Select a year…</option>
              {years?.map((y) => (
                <option key={y.id} value={y.id}>
                  {y.name}
                </option>
              ))}
            </SelectField>
          </div>
        </div>

        {fromClassId && fromYearId && (
          <div>
            <div className="mb-1.5 flex items-center justify-between">
              <span className="text-sm font-medium text-slate-700 dark:text-slate-300">Students</span>
              {candidates && candidates.items.length > 0 && (
                <button
                  type="button"
                  onClick={toggleAll}
                  className="text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
                >
                  {selected.size === candidates.items.length ? 'Deselect all' : 'Select all'}
                </button>
              )}
            </div>
            <div className="max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
              {loadingCandidates ? (
                <p className="px-3 py-3 text-sm text-slate-400">Loading…</p>
              ) : candidates && candidates.items.length > 0 ? (
                candidates.items.map((c) => (
                  <label
                    key={c.id}
                    className="flex cursor-pointer items-center gap-2.5 border-b border-slate-100 px-3 py-2 text-sm last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
                  >
                    <input
                      type="checkbox"
                      checked={selected.has(c.studentId)}
                      onChange={() => toggleStudent(c.studentId)}
                      className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                    />
                    <span className="text-slate-800 dark:text-slate-100">{c.studentName}</span>
                    <span className="text-xs text-slate-400">{c.studentNumber}</span>
                  </label>
                ))
              ) : (
                <p className="px-3 py-3 text-sm text-slate-400">No active students in this class/year.</p>
              )}
            </div>
          </div>
        )}

        <div>
          <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-white">To</h3>
          <div className="grid grid-cols-2 gap-4">
            <SelectField
              label="Class"
              required
              value={toClassId}
              onChange={(e) => {
                setToClassId(e.target.value)
                setToStreamId('')
              }}
            >
              <option value="">Select a class…</option>
              {classes?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </SelectField>
            <SelectField
              label="Academic year"
              required
              value={toYearId}
              onChange={(e) => setToYearId(e.target.value)}
            >
              <option value="">Select a year…</option>
              {years?.map((y) => (
                <option key={y.id} value={y.id}>
                  {y.name}
                  {y.isCurrent ? ' (current)' : ''}
                </option>
              ))}
            </SelectField>
          </div>
          {toStreams.length > 0 && (
            <div className="mt-4">
              <SelectField label="Stream" value={toStreamId} onChange={(e) => setToStreamId(e.target.value)}>
                <option value="">Not assigned</option>
                {toStreams.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </SelectField>
            </div>
          )}
          <div className="mt-4">
            <TextField
              label="Enrollment date"
              type="date"
              required
              value={enrollmentDate}
              onChange={(e) => setEnrollmentDate(e.target.value)}
            />
          </div>
        </div>

        {result && (
          <p className="rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300">
            Promoted {result.promotedCount} student{result.promotedCount === 1 ? '' : 's'}.
            {result.skippedCount > 0 &&
              ` ${result.skippedCount} skipped (already enrolled in the destination year).`}
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
