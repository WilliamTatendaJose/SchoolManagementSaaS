import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { recordLeaveDeparture } from '../../api/hostel'
import type { WeekendLeaveDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

export function RecordDepartureDrawer({ leave, onClose }: { leave: WeekendLeaveDto | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [collectedBy, setCollectedBy] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (leave) {
      setCollectedBy('')
      setError(null)
    }
  }, [leave])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!leave) return
    setError(null)
    setSubmitting(true)
    try {
      await recordLeaveDeparture({ leaveId: leave.id, collectedBy })
      await queryClient.invalidateQueries({ queryKey: ['weekend-leaves'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not record departure'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={!!leave}
      onClose={onClose}
      title="Record departure"
      description={leave ? `${leave.studentName} · ${leave.destination}` : undefined}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="record-departure-form" loading={submitting}>
            Record departure
          </Button>
        </>
      }
    >
      <form id="record-departure-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField
          label="Collected by"
          required
          value={collectedBy}
          onChange={(e) => setCollectedBy(e.target.value)}
          placeholder="Name of guardian/person collecting"
        />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
