import { useQueryClient } from '@tanstack/react-query'
import { KeyRound, Save, ShieldCheck, User } from 'lucide-react'
import { useEffect, useState } from 'react'
import { changeMyPassword, updateMyProfile } from '../../api/users'
import { getErrorMessage } from '../../api/errors'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { TextField } from '../../components/ui/Field'
import { PageHeader } from '../../components/ui/PageHeader'

export function ProfilePage() {
  const queryClient = useQueryClient()
  const accessToken = useAuthStore((s) => s.accessToken)
  const profile = useAuthStore((s) => s.profile)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileSaved, setProfileSaved] = useState(false)
  const [savingProfile, setSavingProfile] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSaved, setPasswordSaved] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)

  useEffect(() => {
    if (profile) {
      setFirstName(profile.firstName)
      setLastName(profile.lastName)
      setPhone(profile.phone ?? '')
    }
  }, [profile])

  async function handleProfileSubmit(e: React.FormEvent) {
    e.preventDefault()
    setProfileError(null)
    setProfileSaved(false)
    setSavingProfile(true)
    try {
      await updateMyProfile({ firstName, lastName, phone: phone || undefined })
      setProfileSaved(true)
      await queryClient.invalidateQueries({ queryKey: ['me', accessToken] })
    } catch (err) {
      setProfileError(getErrorMessage(err, 'Could not save your profile'))
    } finally {
      setSavingProfile(false)
    }
  }

  async function handlePasswordSubmit(e: React.FormEvent) {
    e.preventDefault()
    setPasswordError(null)
    setPasswordSaved(false)
    if (newPassword !== confirmPassword) {
      setPasswordError('New password and confirmation do not match')
      return
    }
    setSavingPassword(true)
    try {
      await changeMyPassword({ currentPassword, newPassword })
      setPasswordSaved(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err) {
      setPasswordError(getErrorMessage(err, 'Could not change your password'))
    } finally {
      setSavingPassword(false)
    }
  }

  if (!profile) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  return (
    <div>
      <PageHeader title="My profile" description="Manage your account details and password" />

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-center gap-4">
          <Avatar name={profile.fullName} size="lg" />
          <div>
            <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{profile.fullName}</h2>
            <p className="text-sm text-slate-500 dark:text-slate-400">{profile.email}</p>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {profile.roles.map((r) => (
                <Badge key={r.id} tone="brand">
                  {r.name}
                </Badge>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
            <User className="h-4 w-4 text-slate-400" strokeWidth={2} />
            Profile details
          </h3>

          <form onSubmit={handleProfileSubmit} className="mt-4 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <TextField label="First name" required value={firstName} onChange={(e) => setFirstName(e.target.value)} />
              <TextField label="Last name" required value={lastName} onChange={(e) => setLastName(e.target.value)} />
            </div>
            <TextField label="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
            <TextField label="Email" value={profile.email} disabled hint="Contact your administrator to change your email." />

            {profileError && (
              <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
                {profileError}
              </p>
            )}
            {profileSaved && (
              <p className="rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
                Profile saved.
              </p>
            )}

            <div className="flex justify-end">
              <Button type="submit" loading={savingProfile}>
                <Save className="h-4 w-4" strokeWidth={2} />
                Save changes
              </Button>
            </div>
          </form>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
          <h3 className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
            <KeyRound className="h-4 w-4 text-slate-400" strokeWidth={2} />
            Change password
          </h3>

          <form onSubmit={handlePasswordSubmit} className="mt-4 space-y-4">
            <TextField
              label="Current password"
              type="password"
              autoComplete="current-password"
              required
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
            />
            <TextField
              label="New password"
              type="password"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              hint="At least 8 characters, with a letter and a digit."
            />
            <TextField
              label="Confirm new password"
              type="password"
              autoComplete="new-password"
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />

            {passwordError && (
              <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
                {passwordError}
              </p>
            )}
            {passwordSaved && (
              <p className="flex items-center gap-1.5 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
                <ShieldCheck className="h-4 w-4" strokeWidth={2} />
                Password changed.
              </p>
            )}

            <div className="flex justify-end">
              <Button type="submit" variant="secondary" loading={savingPassword}>
                <KeyRound className="h-4 w-4" strokeWidth={2} />
                Update password
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
