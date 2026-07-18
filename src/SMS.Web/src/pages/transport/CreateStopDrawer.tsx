import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createRouteStop } from '../../api/transport'
import { getErrorMessage } from '../../api/errors'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

export function CreateStopDrawer({
  open,
  onClose,
  routeId,
  nextSequence,
}: {
  open: boolean
  onClose: () => void
  routeId: string
  nextSequence: number
}) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [sequenceNumber, setSequenceNumber] = useState(String(nextSequence))
  const [pickupTime, setPickupTime] = useState('')
  const [dropoffTime, setDropoffTime] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName('')
      setSequenceNumber(String(nextSequence))
      setPickupTime('')
      setDropoffTime('')
      setError(null)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createRouteStop({
        transportRouteId: routeId,
        name,
        sequenceNumber: Number(sequenceNumber),
        pickupTime: pickupTime || undefined,
        dropoffTime: dropoffTime || undefined,
      })
      await queryClient.invalidateQueries({ queryKey: ['route-stops', routeId] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create stop'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add stop"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-stop-form" loading={submitting}>
            Add stop
          </Button>
        </>
      }
    >
      <form id="create-stop-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Stop name" required value={name} onChange={(e) => setName(e.target.value)} />
        <TextField
          label="Sequence number"
          type="number"
          min={1}
          required
          value={sequenceNumber}
          onChange={(e) => setSequenceNumber(e.target.value)}
          hint="Order along the route"
        />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Pickup time" type="time" value={pickupTime} onChange={(e) => setPickupTime(e.target.value)} />
          <TextField label="Dropoff time" type="time" value={dropoffTime} onChange={(e) => setDropoffTime(e.target.value)} />
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
