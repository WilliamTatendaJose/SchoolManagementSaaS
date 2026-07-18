import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { createGuardian, updateGuardian } from '../../api/guardians'
import { GENDERS, type GuardianDetailDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

interface GuardianFormDrawerProps {
  open: boolean
  onClose: () => void
  guardian?: GuardianDetailDto
}

interface FormState {
  firstName: string
  lastName: string
  gender: string
  nationalId: string
  phone: string
  alternatePhone: string
  email: string
  address: string
  occupation: string
  employer: string
}

function emptyForm(): FormState {
  return {
    firstName: '',
    lastName: '',
    gender: 'Male',
    nationalId: '',
    phone: '',
    alternatePhone: '',
    email: '',
    address: '',
    occupation: '',
    employer: '',
  }
}

function formFromGuardian(g: GuardianDetailDto): FormState {
  return {
    firstName: g.firstName,
    lastName: g.lastName,
    gender: g.gender,
    nationalId: g.nationalId ?? '',
    phone: g.phone ?? '',
    alternatePhone: g.alternatePhone ?? '',
    email: g.email ?? '',
    address: g.address ?? '',
    occupation: g.occupation ?? '',
    employer: g.employer ?? '',
  }
}

export function GuardianFormDrawer({ open, onClose, guardian }: GuardianFormDrawerProps) {
  const isEdit = !!guardian
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(guardian ? formFromGuardian(guardian) : emptyForm())
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setForm(guardian ? formFromGuardian(guardian) : emptyForm())
      setError(null)
    }
  }, [open, guardian])

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)

    const payload = {
      firstName: form.firstName,
      lastName: form.lastName,
      gender: form.gender,
      nationalId: form.nationalId || undefined,
      phone: form.phone || undefined,
      alternatePhone: form.alternatePhone || undefined,
      email: form.email || undefined,
      address: form.address || undefined,
      occupation: form.occupation || undefined,
      employer: form.employer || undefined,
    }

    try {
      if (isEdit && guardian) {
        await updateGuardian({ id: guardian.id, ...payload })
        await queryClient.invalidateQueries({ queryKey: ['guardian', guardian.id] })
        await queryClient.invalidateQueries({ queryKey: ['guardians'] })
        onClose()
      } else {
        const created = await createGuardian(payload)
        await queryClient.invalidateQueries({ queryKey: ['guardians'] })
        onClose()
        navigate(`/guardians/${created.id}`)
      }
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update guardian' : 'Could not create guardian'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit guardian' : 'Add guardian'}
      description={isEdit ? guardian?.fullName : 'Create a new guardian record'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} type="button">
            Cancel
          </Button>
          <Button type="submit" form="guardian-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Create guardian'}
          </Button>
        </>
      }
    >
      <form id="guardian-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="First name"
            required
            value={form.firstName}
            onChange={(e) => set('firstName', e.target.value)}
          />
          <TextField
            label="Last name"
            required
            value={form.lastName}
            onChange={(e) => set('lastName', e.target.value)}
          />
        </div>

        <SelectField label="Gender" required value={form.gender} onChange={(e) => set('gender', e.target.value)}>
          {GENDERS.map((g) => (
            <option key={g} value={g}>
              {g}
            </option>
          ))}
        </SelectField>

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Phone"
            placeholder="+263 77 123 4567"
            value={form.phone}
            onChange={(e) => set('phone', e.target.value)}
          />
          <TextField
            label="Alternate phone"
            value={form.alternatePhone}
            onChange={(e) => set('alternatePhone', e.target.value)}
          />
        </div>

        <TextField
          label="Email"
          type="email"
          value={form.email}
          onChange={(e) => set('email', e.target.value)}
        />

        <TextField label="Address" value={form.address} onChange={(e) => set('address', e.target.value)} />

        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Occupation"
            value={form.occupation}
            onChange={(e) => set('occupation', e.target.value)}
          />
          <TextField label="Employer" value={form.employer} onChange={(e) => set('employer', e.target.value)} />
        </div>

        <TextField
          label="National ID"
          value={form.nationalId}
          onChange={(e) => set('nationalId', e.target.value)}
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
