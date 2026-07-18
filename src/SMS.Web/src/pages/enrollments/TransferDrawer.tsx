import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { transferStudent } from '../../api/enrollments'
import type { EnrollmentDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField } from '../../components/ui/Field'

export function TransferDrawer({
  enrollment,
  onClose,
}: {
  enrollment: EnrollmentDto | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [toClassId, setToClassId] = useState('')
  const [toStreamId, setToStreamId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: !!enrollment })
  const streams = classes?.find((c) => c.id === toClassId)?.streams ?? []

  useEffect(() => {
    if (enrollment) {
      setToClassId('')
      setToStreamId('')
      setError(null)
    }
  }, [enrollment])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!enrollment) return
    if (!toClassId) {
      setError('Select a destination class')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      await transferStudent({
        studentId: enrollment.studentId,
        academicYearId: enrollment.academicYearId,
        toClassId,
        toStreamId: toStreamId || null,
      })
      await queryClient.invalidateQueries({ queryKey: ['enrollments'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not transfer student'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={!!enrollment}
      onClose={onClose}
      title="Transfer student"
      description={enrollment ? `${enrollment.studentName} · currently in ${enrollment.className}` : undefined}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="transfer-form" loading={submitting}>
            Transfer
          </Button>
        </>
      }
    >
      <form id="transfer-form" onSubmit={handleSubmit} className="space-y-4">
        <SelectField
          label="New class"
          required
          value={toClassId}
          onChange={(e) => {
            setToClassId(e.target.value)
            setToStreamId('')
          }}
        >
          <option value="">Select a class…</option>
          {classes
            ?.filter((c) => c.id !== enrollment?.classId)
            .map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
        </SelectField>

        {streams.length > 0 && (
          <SelectField label="New stream" value={toStreamId} onChange={(e) => setToStreamId(e.target.value)}>
            <option value="">Not assigned</option>
            {streams.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </SelectField>
        )}

        <p className="text-xs text-slate-400">
          Stays within {enrollment?.academicYearName}. Use "Promote students" to move students into a new academic
          year.
        </p>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
