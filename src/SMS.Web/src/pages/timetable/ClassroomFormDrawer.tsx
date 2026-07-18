import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { createClassroom, updateClassroom } from '../../api/timetable'
import type { ClassroomDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

interface ClassroomFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  classroom?: ClassroomDto
}

export function ClassroomFormDrawer({ open, onClose, classroom }: ClassroomFormDrawerProps) {
  const isEdit = !!classroom
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [building, setBuilding] = useState('')
  const [capacity, setCapacity] = useState('30')
  const [hasProjector, setHasProjector] = useState(false)
  const [hasWhiteboard, setHasWhiteboard] = useState(true)
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName(classroom?.name ?? '')
      setBuilding(classroom?.building ?? '')
      setCapacity(String(classroom?.capacity ?? 30))
      setHasProjector(classroom?.hasProjector ?? false)
      setHasWhiteboard(classroom?.hasWhiteboard ?? true)
      setIsActive(classroom?.isActive ?? true)
      setError(null)
    }
  }, [open, classroom])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      if (isEdit && classroom) {
        await updateClassroom({
          id: classroom.id,
          name,
          building: building || undefined,
          capacity: Number(capacity),
          hasProjector,
          hasWhiteboard,
          isActive,
        })
      } else {
        await createClassroom({
          name,
          building: building || undefined,
          capacity: Number(capacity),
          hasProjector,
          hasWhiteboard,
        })
      }
      await queryClient.invalidateQueries({ queryKey: ['classrooms'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update classroom' : 'Could not create classroom'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit classroom' : 'Add classroom'}
      description="Rooms available for scheduling lessons"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="classroom-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create classroom'}
          </Button>
        </>
      }
    >
      <form id="classroom-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} placeholder="Room 4" />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Building" value={building} onChange={(e) => setBuilding(e.target.value)} />
          <TextField
            label="Capacity"
            type="number"
            min={0}
            required
            value={capacity}
            onChange={(e) => setCapacity(e.target.value)}
          />
        </div>

        <div className="space-y-2.5">
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={hasProjector}
              onChange={(e) => setHasProjector(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Has projector
          </label>
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={hasWhiteboard}
              onChange={(e) => setHasWhiteboard(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Has whiteboard
          </label>
          {isEdit && (
            <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
              />
              Active
            </label>
          )}
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
