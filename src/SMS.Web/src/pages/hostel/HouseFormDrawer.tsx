import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { createHouse } from '../../api/hostel'
import { fetchStaff } from '../../api/staff'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function HouseFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [color, setColor] = useState('')
  const [houseMasterId, setHouseMasterId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: staff } = useQuery({
    queryKey: ['staff-all'],
    queryFn: () => fetchStaff({ isActive: true, pageSize: 100 }),
    enabled: open,
  })

  useEffect(() => {
    if (open) {
      setName('')
      setColor('')
      setHouseMasterId('')
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createHouse({ name, color: color || undefined, houseMasterId: houseMasterId || null })
      await queryClient.invalidateQueries({ queryKey: ['houses'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create house'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add house"
      description="Competition/pastoral houses students belong to"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="house-form" loading={submitting}>
            Create house
          </Button>
        </>
      }
    >
      <form id="house-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} placeholder="Red House" />
        <TextField label="Color" value={color} onChange={(e) => setColor(e.target.value)} placeholder="Red" />
        <SelectField label="House master" value={houseMasterId} onChange={(e) => setHouseMasterId(e.target.value)}>
          <option value="">Not assigned</option>
          {staff?.items.map((s) => (
            <option key={s.id} value={s.id}>
              {s.fullName}
            </option>
          ))}
        </SelectField>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
