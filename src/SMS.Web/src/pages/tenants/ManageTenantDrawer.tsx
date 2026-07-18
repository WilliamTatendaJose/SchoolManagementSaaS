import { useQueryClient } from '@tanstack/react-query'
import { BookOpenCheck, Bus, CalendarClock, Library } from 'lucide-react'
import { useEffect, useState } from 'react'
import { updateTenantModules, updateTenantSubscription } from '../../api/tenants'
import { getErrorMessage } from '../../api/errors'
import { SUBSCRIPTION_PLANS, type TenantDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextField } from '../../components/ui/Field'

export function ManageTenantDrawer({ tenant, onClose }: { tenant: TenantDto | null; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [subscriptionPlan, setSubscriptionPlan] = useState<string>(SUBSCRIPTION_PLANS[0])
  const [maxStudents, setMaxStudents] = useState('100')
  const [subscriptionEndDate, setSubscriptionEndDate] = useState('')
  const [hasLmsModule, setHasLmsModule] = useState(false)
  const [hasTransportModule, setHasTransportModule] = useState(false)
  const [hasHostelModule, setHasHostelModule] = useState(false)
  const [hasLibraryModule, setHasLibraryModule] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (tenant) {
      setSubscriptionPlan(tenant.subscriptionPlan)
      setMaxStudents(String(tenant.maxStudents))
      setSubscriptionEndDate(tenant.subscriptionEndDate ? tenant.subscriptionEndDate.slice(0, 10) : '')
      setHasLmsModule(tenant.hasLmsModule)
      setHasTransportModule(tenant.hasTransportModule)
      setHasHostelModule(tenant.hasHostelModule)
      setHasLibraryModule(tenant.hasLibraryModule)
      setError(null)
    }
  }, [tenant])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!tenant) return
    setError(null)
    setSaving(true)
    try {
      await Promise.all([
        updateTenantSubscription(tenant.id, {
          subscriptionPlan,
          maxStudents: Number(maxStudents),
          subscriptionEndDate: subscriptionEndDate ? new Date(subscriptionEndDate).toISOString() : undefined,
        }),
        updateTenantModules(tenant.id, {
          hasLmsModule,
          hasTransportModule,
          hasHostelModule,
          hasLibraryModule,
        }),
      ])
      await queryClient.invalidateQueries({ queryKey: ['tenants'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save changes'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Drawer
      open={!!tenant}
      onClose={onClose}
      title="Manage subscription"
      description={tenant?.name}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="manage-tenant-form" loading={saving}>
            Save changes
          </Button>
        </>
      }
    >
      <form id="manage-tenant-form" onSubmit={handleSubmit} className="space-y-5">
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
            required
            value={maxStudents}
            onChange={(e) => setMaxStudents(e.target.value)}
          />
        </div>
        <TextField
          label="Subscription end date"
          type="date"
          value={subscriptionEndDate}
          onChange={(e) => setSubscriptionEndDate(e.target.value)}
        />

        <div>
          <span className="mb-2 block text-sm font-medium text-slate-700 dark:text-slate-300">Feature modules</span>
          <div className="space-y-2.5 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
            <ModuleCheckbox icon={BookOpenCheck} label="Learning management" checked={hasLmsModule} onChange={setHasLmsModule} />
            <ModuleCheckbox icon={Bus} label="Transport" checked={hasTransportModule} onChange={setHasTransportModule} />
            <ModuleCheckbox icon={CalendarClock} label="Boarding / hostel" checked={hasHostelModule} onChange={setHasHostelModule} />
            <ModuleCheckbox icon={Library} label="Library" checked={hasLibraryModule} onChange={setHasLibraryModule} />
          </div>
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

function ModuleCheckbox({
  icon: Icon,
  label,
  checked,
  onChange,
}: {
  icon: typeof BookOpenCheck
  label: string
  checked: boolean
  onChange: (value: boolean) => void
}) {
  return (
    <label className="flex cursor-pointer items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
      />
      <Icon className="h-3.5 w-3.5 text-slate-400" strokeWidth={2} />
      {label}
    </label>
  )
}
