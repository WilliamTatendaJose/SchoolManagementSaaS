import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { fetchGuardian } from '../../api/guardians'
import type { UserDetailDto } from '../../api/types'
import { updateUser } from '../../api/users'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'
import { GuardianPicker } from './GuardianPicker'

export function UpdateUserDrawer({
  open,
  onClose,
  user,
}: {
  open: boolean
  onClose: () => void
  user: UserDetailDto
}) {
  const queryClient = useQueryClient()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [guardianId, setGuardianId] = useState<string | null>(null)
  const [guardianLabel, setGuardianLabel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Resolve the currently-linked guardian's name for display (the user DTO only carries
  // the id). Only fires when the user is already linked to a guardian.
  const { data: linkedGuardian } = useQuery({
    queryKey: ['guardian', user.guardianId],
    queryFn: () => fetchGuardian(user.guardianId!),
    enabled: open && !!user.guardianId,
  })

  useEffect(() => {
    if (open) {
      setFirstName(user.firstName)
      setLastName(user.lastName)
      setPhone(user.phone ?? '')
      setIsActive(user.isActive)
      setGuardianId(user.guardianId ?? null)
      setGuardianLabel('')
      setError(null)
    }
  }, [open, user])

  useEffect(() => {
    if (linkedGuardian && guardianId === linkedGuardian.id && !guardianLabel) {
      setGuardianLabel(linkedGuardian.phone ? `${linkedGuardian.fullName} · ${linkedGuardian.phone}` : linkedGuardian.fullName)
    }
  }, [linkedGuardian, guardianId, guardianLabel])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await updateUser({ id: user.id, firstName, lastName, phone: phone || undefined, isActive, guardianId })
      await queryClient.invalidateQueries({ queryKey: ['user', user.id] })
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not update user'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Edit user"
      description={user.email}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="update-user-form" loading={submitting}>
            Save changes
          </Button>
        </>
      }
    >
      <form id="update-user-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField label="First name" required value={firstName} onChange={(e) => setFirstName(e.target.value)} />
          <TextField label="Last name" required value={lastName} onChange={(e) => setLastName(e.target.value)} />
        </div>
        <TextField label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
        <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
          />
          Active
        </label>
        <p className="text-xs text-slate-400">Email can't be changed here. Roles are managed separately below.</p>

        <GuardianPicker
          selectedId={guardianId}
          selectedLabel={guardianLabel}
          onSelect={(id, label) => {
            setGuardianId(id)
            setGuardianLabel(label)
          }}
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
