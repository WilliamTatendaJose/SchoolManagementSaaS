import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpenCheck, Bus, CalendarClock, CreditCard, Library, Lock, Mail, Save, School, ShieldCheck, Users } from 'lucide-react'
import { useEffect, useState } from 'react'
import { fetchSettings, updateSettings } from '../../api/settings'
import { getErrorMessage } from '../../api/errors'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'

export function SettingsPage() {
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('settings.manage'))
  const tenantName = useAuthStore((s) => s.tenantName)

  const { data: settings, isLoading } = useQuery({ queryKey: ['settings'], queryFn: fetchSettings })

  const [name, setName] = useState('')
  const [logo, setLogo] = useState('')
  const [phone, setPhone] = useState('')
  const [email, setEmail] = useState('')
  const [website, setWebsite] = useState('')
  const [address, setAddress] = useState('')
  const [city, setCity] = useState('')
  const [country, setCountry] = useState('')
  const [timeZone, setTimeZone] = useState('')
  const [currency, setCurrency] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (settings) {
      setName(settings.name)
      setLogo(settings.logo ?? '')
      setPhone(settings.phone ?? '')
      setEmail(settings.email ?? '')
      setWebsite(settings.website ?? '')
      setAddress(settings.address ?? '')
      setCity(settings.city ?? '')
      setCountry(settings.country ?? '')
      setTimeZone(settings.timeZone ?? '')
      setCurrency(settings.currency ?? '')
    }
  }, [settings])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSaved(false)
    setSaving(true)
    try {
      await updateSettings({
        name,
        logo: logo || undefined,
        phone: phone || undefined,
        email: email || undefined,
        website: website || undefined,
        address: address || undefined,
        city: city || undefined,
        country: country || undefined,
        timeZone: timeZone || undefined,
        currency: currency || undefined,
      })
      setSaved(true)
      await queryClient.invalidateQueries({ queryKey: ['settings'] })
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save settings'))
    } finally {
      setSaving(false)
    }
  }

  function requestUpgrade() {
    window.open(
      `mailto:?subject=${encodeURIComponent('Subscription upgrade request')}&body=${encodeURIComponent(
        `Hi,\n\nWe'd like to review our subscription plan and available modules for ${tenantName ?? 'our school'}.\n\nThanks.`,
      )}`,
    )
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!settings) {
    return <p className="text-sm text-slate-500">Could not load settings.</p>
  }

  const studentUsagePercent = settings.maxStudents > 0 ? Math.round((settings.currentStudentCount / settings.maxStudents) * 100) : 0

  return (
    <div>
      <PageHeader
        title="Settings"
        description="School profile, subscription and feature modules"
        actions={
          canManage && (
            <Button type="submit" form="settings-form" loading={saving}>
              <Save className="h-4 w-4" strokeWidth={2} />
              Save changes
            </Button>
          )
        }
      />

      {error && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {error}
        </p>
      )}
      {saved && (
        <p className="mb-4 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
          Settings saved.
        </p>
      )}

      <form id="settings-form" onSubmit={handleSubmit} className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
            <School className="h-4 w-4 text-slate-400" strokeWidth={2} />
            School profile
          </h3>

          <fieldset disabled={!canManage} className="mt-4 space-y-4 disabled:opacity-70">
            <TextField label="School name" required value={name} onChange={(e) => setName(e.target.value)} />
            <TextField
              label="Logo URL"
              value={logo}
              onChange={(e) => setLogo(e.target.value)}
              placeholder="https://…"
            />
            <div className="grid grid-cols-2 gap-4">
              <TextField label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
              <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <TextField label="Website" value={website} onChange={(e) => setWebsite(e.target.value)} placeholder="https://…" />
            <TextField label="Address" value={address} onChange={(e) => setAddress(e.target.value)} />
            <div className="grid grid-cols-2 gap-4">
              <TextField label="City" value={city} onChange={(e) => setCity(e.target.value)} />
              <TextField label="Country" value={country} onChange={(e) => setCountry(e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <TextField
                label="Time zone"
                value={timeZone}
                onChange={(e) => setTimeZone(e.target.value)}
                placeholder="Africa/Harare"
              />
              <TextField
                label="Currency"
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                placeholder="USD"
                maxLength={3}
              />
            </div>
          </fieldset>
        </div>

        <div className="space-y-4">
          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
              <CreditCard className="h-4 w-4 text-slate-400" strokeWidth={2} />
              Subscription
            </h3>
            <div className="mt-3 space-y-3 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-slate-500 dark:text-slate-400">Plan</span>
                <Badge tone="brand">{settings.subscriptionPlan}</Badge>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-500 dark:text-slate-400">Status</span>
                <Badge tone={settings.status === 'Active' ? 'emerald' : 'amber'}>{settings.status}</Badge>
              </div>
              {settings.subscriptionEndDate && (
                <div className="flex items-center justify-between">
                  <span className="text-slate-500 dark:text-slate-400">Renews</span>
                  <span className="text-slate-700 dark:text-slate-200">
                    {new Date(settings.subscriptionEndDate).toLocaleDateString()}
                  </span>
                </div>
              )}
              <div>
                <div className="flex items-center justify-between text-slate-500 dark:text-slate-400">
                  <span className="flex items-center gap-1">
                    <Users className="h-3.5 w-3.5" strokeWidth={2} />
                    Students
                  </span>
                  <span>
                    {settings.currentStudentCount} / {settings.maxStudents}
                  </span>
                </div>
                <div className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                  <div
                    className={`h-full rounded-full ${studentUsagePercent >= 90 ? 'bg-amber-500' : 'bg-brand-500'}`}
                    style={{ width: `${Math.min(100, studentUsagePercent)}%` }}
                  />
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-500 dark:text-slate-400">SMS credits</span>
                <span className="font-medium text-slate-800 dark:text-slate-100">{settings.smsCredits}</span>
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
            <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
              <ShieldCheck className="h-4 w-4 text-slate-400" strokeWidth={2} />
              Modules
            </h3>
            <div className="mt-3 space-y-2.5">
              <ModuleRow icon={BookOpenCheck} label="Learning management" enabled={settings.hasLmsModule} />
              <ModuleRow icon={Bus} label="Transport" enabled={settings.hasTransportModule} />
              <ModuleRow icon={CalendarClock} label="Boarding / hostel" enabled={settings.hasHostelModule} />
              <ModuleRow icon={Library} label="Library" enabled={settings.hasLibraryModule} />
            </div>
            <p className="mt-3 text-xs text-slate-400">
              Your plan and modules are managed by your platform administrator.
            </p>
            <Button variant="secondary" type="button" className="mt-3 w-full" onClick={requestUpgrade}>
              <Mail className="h-4 w-4" strokeWidth={2} />
              Request upgrade
            </Button>
          </div>
        </div>
      </form>
    </div>
  )
}

function ModuleRow({
  icon: Icon,
  label,
  enabled,
}: {
  icon: typeof BookOpenCheck
  label: string
  enabled: boolean
}) {
  return (
    <div className="flex items-center justify-between text-sm">
      <span className="flex items-center gap-2 text-slate-600 dark:text-slate-300">
        <Icon className="h-3.5 w-3.5 text-slate-400" strokeWidth={2} />
        {label}
      </span>
      {enabled ? (
        <Badge tone="emerald">Enabled</Badge>
      ) : (
        <span className="flex items-center gap-1 text-xs font-medium text-slate-400">
          <Lock className="h-3 w-3" strokeWidth={2} />
          Locked
        </span>
      )}
    </div>
  )
}
