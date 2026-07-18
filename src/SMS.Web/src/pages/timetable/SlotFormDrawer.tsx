import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { fetchClasses } from '../../api/classes'
import { getErrorMessage } from '../../api/errors'
import { fetchStaff } from '../../api/staff'
import { fetchSubjects } from '../../api/subjects'
import { createTimetableSlot, deleteTimetableSlot, fetchClassrooms, updateTimetableSlot } from '../../api/timetable'
import { DAYS_OF_WEEK, type TimetableSlotDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

interface SlotFormDrawerProps {
  open: boolean
  onClose: () => void
  academicTermId: string
  /** Present for edit mode; omitted for create. */
  slot?: TimetableSlotDto
  defaultClassId?: string
  defaultDayOfWeek?: number
}

export function SlotFormDrawer({
  open,
  onClose,
  academicTermId,
  slot,
  defaultClassId,
  defaultDayOfWeek,
}: SlotFormDrawerProps) {
  const isEdit = !!slot
  const queryClient = useQueryClient()
  const [classId, setClassId] = useState('')
  const [subjectId, setSubjectId] = useState('')
  const [teacherId, setTeacherId] = useState('')
  const [classroomId, setClassroomId] = useState('')
  const [dayOfWeek, setDayOfWeek] = useState('1')
  const [startTime, setStartTime] = useState('08:00')
  const [endTime, setEndTime] = useState('08:40')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const { data: classes } = useQuery({ queryKey: ['classes'], queryFn: fetchClasses, enabled: open })
  const { data: subjects } = useQuery({ queryKey: ['subjects'], queryFn: () => fetchSubjects(true), enabled: open })
  const { data: teachers } = useQuery({
    queryKey: ['staff-teachers'],
    queryFn: () => fetchStaff({ isTeacher: true, isActive: true, pageSize: 100 }),
    enabled: open,
  })
  const { data: classrooms } = useQuery({
    queryKey: ['classrooms'],
    queryFn: () => fetchClassrooms(true),
    enabled: open,
  })

  useEffect(() => {
    if (open) {
      setClassId(slot?.classId ?? defaultClassId ?? '')
      setSubjectId(slot?.subjectId ?? '')
      setTeacherId(slot?.teacherId ?? '')
      setClassroomId(slot?.classroomId ?? '')
      setDayOfWeek(String(slot ? dayNameToValue(slot.dayOfWeek) : (defaultDayOfWeek ?? 1)))
      setStartTime(slot?.startTime.slice(0, 5) ?? '08:00')
      setEndTime(slot?.endTime.slice(0, 5) ?? '08:40')
      setError(null)
    }
  }, [open, slot, defaultClassId, defaultDayOfWeek])

  function dayNameToValue(name: string) {
    return DAYS_OF_WEEK.find((d) => d.label === name)?.value ?? 1
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload = {
        classId,
        subjectId,
        teacherId,
        classroomId: classroomId || null,
        academicTermId,
        dayOfWeek: Number(dayOfWeek),
        startTime: `${startTime}:00`,
        endTime: `${endTime}:00`,
      }
      if (isEdit && slot) {
        await updateTimetableSlot({ id: slot.id, ...payload })
      } else {
        await createTimetableSlot(payload)
      }
      await queryClient.invalidateQueries({ queryKey: ['timetable'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update lesson' : 'Could not create lesson'))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete() {
    if (!slot || !window.confirm('Remove this lesson from the timetable?')) return
    setError(null)
    setDeleting(true)
    try {
      await deleteTimetableSlot(slot.id)
      await queryClient.invalidateQueries({ queryKey: ['timetable'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not remove lesson'))
      setDeleting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit lesson' : 'Add lesson'}
      description="Scheduling checks for class, teacher and room clashes automatically"
      footer={
        <>
          {isEdit && (
            <Button variant="danger" type="button" onClick={handleDelete} loading={deleting} className="mr-auto">
              Remove
            </Button>
          )}
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="slot-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Add lesson'}
          </Button>
        </>
      }
    >
      <form id="slot-form" onSubmit={handleSubmit} className="space-y-4">
        <SelectField label="Class" required value={classId} onChange={(e) => setClassId(e.target.value)}>
          <option value="">Select a class…</option>
          {classes?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </SelectField>

        <SelectField label="Subject" required value={subjectId} onChange={(e) => setSubjectId(e.target.value)}>
          <option value="">Select a subject…</option>
          {subjects?.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </SelectField>

        <SelectField label="Teacher" required value={teacherId} onChange={(e) => setTeacherId(e.target.value)}>
          <option value="">Select a teacher…</option>
          {teachers?.items.map((t) => (
            <option key={t.id} value={t.id}>
              {t.fullName}
            </option>
          ))}
        </SelectField>

        <SelectField label="Classroom" value={classroomId} onChange={(e) => setClassroomId(e.target.value)}>
          <option value="">Not assigned</option>
          {classrooms?.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
              {c.building ? ` (${c.building})` : ''}
            </option>
          ))}
        </SelectField>

        <SelectField label="Day" required value={dayOfWeek} onChange={(e) => setDayOfWeek(e.target.value)}>
          {DAYS_OF_WEEK.map((d) => (
            <option key={d.value} value={d.value}>
              {d.label}
            </option>
          ))}
        </SelectField>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Start time"
            type="time"
            required
            value={startTime}
            onChange={(e) => setStartTime(e.target.value)}
          />
          <TextField label="End time" type="time" required value={endTime} onChange={(e) => setEndTime(e.target.value)} />
        </div>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
