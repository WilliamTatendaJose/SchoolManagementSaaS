import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { createDormitory } from '../../api/hostel'
import { fetchStaff } from '../../api/staff'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function DormitoryFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [capacity, setCapacity] = useState('20')
  const [gender, setGender] = useState('')
  const [wardenId, setWardenId] = useState('')
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
      setCapacity('20')
      setGender('')
      setWardenId('')
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createDormitory({
        name,
        capacity: Number(capacity),
        gender: gender || null,
        wardenId: wardenId || null,
      })
      await queryClient.invalidateQueries({ queryKey: ['dormitories'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create dormitory'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add dormitory"
      description="Boarding houses students are assigned to"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="dormitory-form" loading={submitting}>
            Create dormitory
          </Button>
        </>
      }
    >
      <form id="dormitory-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Capacity"
            type="number"
            min={1}
            required
            value={capacity}
            onChange={(e) => setCapacity(e.target.value)}
          />
          <SelectField label="Gender" value={gender} onChange={(e) => setGender(e.target.value)}>
            <option value="">Mixed / not set</option>
            <option value="Male">Male</option>
            <option value="Female">Female</option>
          </SelectField>
        </div>
        <SelectField label="Warden" value={wardenId} onChange={(e) => setWardenId(e.target.value)}>
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
