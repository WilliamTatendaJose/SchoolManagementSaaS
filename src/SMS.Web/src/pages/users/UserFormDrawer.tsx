import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { fetchRoles } from '../../api/roles'
import { registerUser } from '../../api/users'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'
import { GuardianPicker } from './GuardianPicker'

export function UserFormDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [password, setPassword] = useState('')
  const [roles, setRoles] = useState<Set<string>>(new Set())
  const [guardianId, setGuardianId] = useState<string | null>(null)
  const [guardianLabel, setGuardianLabel] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: availableRoles } = useQuery({ queryKey: ['roles'], queryFn: fetchRoles, enabled: open })

  useEffect(() => {
    if (open) {
      setFirstName('')
      setLastName('')
      setEmail('')
      setPhone('')
      setPassword('')
      setRoles(new Set())
      setGuardianId(null)
      setGuardianLabel('')
      setError(null)
    }
  }, [open])

  function toggleRole(name: string) {
    setRoles((prev) => {
      const next = new Set(prev)
      if (next.has(name)) next.delete(name)
      else next.add(name)
      return next
    })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const created = await registerUser({
        email,
        password,
        firstName,
        lastName,
        phone: phone || undefined,
        roles: Array.from(roles),
        guardianId: guardianId ?? undefined,
      })
      await queryClient.invalidateQueries({ queryKey: ['users'] })
      onClose()
      navigate(`/users/${created.id}`)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create user'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add user"
      description="Create a login for staff, a guardian, or an admin"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="user-form" loading={submitting}>
            Create user
          </Button>
        </>
      }
    >
      <form id="user-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField label="First name" required value={firstName} onChange={(e) => setFirstName(e.target.value)} />
          <TextField label="Last name" required value={lastName} onChange={(e) => setLastName(e.target.value)} />
        </div>
        <TextField
          label="Email"
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
          <TextField
            label="Temporary password"
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            hint="At least 8 characters"
          />
        </div>

        <div>
          <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Roles</span>
          <div className="max-h-48 overflow-y-auto rounded-lg border border-slate-200 dark:border-slate-800">
            {availableRoles?.map((r) => (
              <label
                key={r.id}
                className="flex cursor-pointer items-center gap-2.5 border-b border-slate-100 px-3 py-2 text-sm last:border-0 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800"
              >
                <input
                  type="checkbox"
                  checked={roles.has(r.name)}
                  onChange={() => toggleRole(r.name)}
                  className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                />
                <span className="text-slate-800 dark:text-slate-100">{r.name}</span>
                {r.description && <span className="text-xs text-slate-400">{r.description}</span>}
              </label>
            ))}
          </div>
        </div>

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
