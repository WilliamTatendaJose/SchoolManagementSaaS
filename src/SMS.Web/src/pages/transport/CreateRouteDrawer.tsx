import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createTransportRoute } from '../../api/transport'
import { getErrorMessage } from '../../api/errors'
import { fetchStaff } from '../../api/staff'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function CreateRouteDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [vehicleRegistration, setVehicleRegistration] = useState('')
  const [driverId, setDriverId] = useState('')
  const [capacity, setCapacity] = useState('20')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: staff } = useQuery({
    queryKey: ['staff', { isActive: true }],
    queryFn: () => fetchStaff({ pageSize: 200, isActive: true }),
    enabled: open,
  })

  useEffect(() => {
    if (open) {
      setName('')
      setVehicleRegistration('')
      setDriverId('')
      setCapacity('20')
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createTransportRoute({
        name,
        vehicleRegistration: vehicleRegistration || undefined,
        driverId: driverId || undefined,
        capacity: Number(capacity),
      })
      await queryClient.invalidateQueries({ queryKey: ['transport-routes'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create route'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add transport route"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-route-form" loading={submitting}>
            Create route
          </Button>
        </>
      }
    >
      <form id="create-route-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Route name" required value={name} onChange={(e) => setName(e.target.value)} />
        <TextField
          label="Vehicle registration"
          value={vehicleRegistration}
          onChange={(e) => setVehicleRegistration(e.target.value)}
        />
        <SelectField label="Driver" value={driverId} onChange={(e) => setDriverId(e.target.value)}>
          <option value="">Unassigned</option>
          {staff?.items.map((s) => (
            <option key={s.id} value={s.id}>
              {s.fullName}
            </option>
          ))}
        </SelectField>
        <TextField
          label="Capacity"
          type="number"
          min={1}
          required
          value={capacity}
          onChange={(e) => setCapacity(e.target.value)}
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
