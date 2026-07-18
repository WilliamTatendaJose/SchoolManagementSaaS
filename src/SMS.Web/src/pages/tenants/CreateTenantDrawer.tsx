import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createTenant } from '../../api/tenants'
import { getErrorMessage } from '../../api/errors'
import { SUBSCRIPTION_PLANS } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function CreateTenantDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')
  const [city, setCity] = useState('')
  const [country, setCountry] = useState('Zimbabwe')
  const [subscriptionPlan, setSubscriptionPlan] = useState<string>(SUBSCRIPTION_PLANS[0])
  const [maxStudents, setMaxStudents] = useState('100')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setName('')
      setCode('')
      setEmail('')
      setPhone('')
      setAddress('')
      setCity('')
      setCountry('Zimbabwe')
      setSubscriptionPlan(SUBSCRIPTION_PLANS[0])
      setMaxStudents('100')
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createTenant({
        name,
        code,
        email: email || undefined,
        phone: phone || undefined,
        address: address || undefined,
        city: city || undefined,
        country: country || undefined,
        subscriptionPlan,
        maxStudents: Number(maxStudents),
      })
      await queryClient.invalidateQueries({ queryKey: ['tenants'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create school'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add school"
      description="Onboard a new tenant onto the platform"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-tenant-form" loading={submitting}>
            Create school
          </Button>
        </>
      }
    >
      <form id="create-tenant-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <TextField label="School name" required value={name} onChange={(e) => setName(e.target.value)} />
          <TextField
            label="Code"
            required
            value={code}
            onChange={(e) => setCode(e.target.value.toUpperCase())}
            placeholder="SCHOOL01"
            maxLength={20}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <TextField label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
        </div>
        <TextField label="Address" value={address} onChange={(e) => setAddress(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="City" value={city} onChange={(e) => setCity(e.target.value)} />
          <TextField label="Country" value={country} onChange={(e) => setCountry(e.target.value)} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <SelectField
            label="Subscription plan"
            value={subscriptionPlan}
            onChange={(e) => setSubscriptionPlan(e.target.value)}
          >
            {SUBSCRIPTION_PLANS.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </SelectField>
          <TextField
            label="Max students"
            type="number"
            min={1}
            value={maxStudents}
            onChange={(e) => setMaxStudents(e.target.value)}
          />
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
