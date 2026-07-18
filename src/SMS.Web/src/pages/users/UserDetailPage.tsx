import { useQuery } from '@tanstack/react-query'
import { KeyRound, Mail, Pencil, Phone, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { getErrorMessage } from '../../api/errors'
import { fetchUser, resetPassword } from '../../api/users'
import { useAuthStore } from '../../auth/authStore'
import { Avatar } from '../../components/ui/Avatar'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { AssignRolesDrawer } from './AssignRolesDrawer'
import { UpdateUserDrawer } from './UpdateUserDrawer'

export function UserDetailPage() {
  const { id } = useParams<{ id: string }>()
  const canEdit = useAuthStore((s) => s.hasPermission('users.edit'))
  const canManageRoles = useAuthStore((s) => s.hasPermission('roles.manage'))
  const [editOpen, setEditOpen] = useState(false)
  const [rolesOpen, setRolesOpen] = useState(false)
  const [resetting, setResetting] = useState(false)
  const [resetError, setResetError] = useState<string | null>(null)
  const [tempPassword, setTempPassword] = useState<string | null>(null)

  const { data: user, isLoading } = useQuery({
    queryKey: ['user', id],
    queryFn: () => fetchUser(id!),
    enabled: !!id,
  })

  async function handleResetPassword() {
    if (!user || !window.confirm(`Reset password for ${user.fullName}? A new temporary password will be generated.`))
      return
    setResetError(null)
    setResetting(true)
    try {
      const { temporaryPassword } = await resetPassword(user.id)
      setTempPassword(temporaryPassword)
    } catch (err) {
      setResetError(getErrorMessage(err, 'Could not reset password'))
    } finally {
      setResetting(false)
    }
  }

  if (isLoading) {
    return <div className="h-64 animate-pulse rounded-2xl bg-slate-100 dark:bg-slate-800/50" />
  }

  if (!user) {
    return <p className="text-sm text-slate-500">User not found.</p>
  }

  return (
    <div>
      <PageHeader
        title={user.fullName}
        backTo="/users"
        actions={
          <>
            {canEdit && (
              <Button variant="secondary" onClick={handleResetPassword} loading={resetting}>
                <KeyRound className="h-4 w-4" strokeWidth={2} />
                Reset password
              </Button>
            )}
            {canEdit && (
              <Button variant="secondary" onClick={() => setEditOpen(true)}>
                <Pencil className="h-4 w-4" strokeWidth={2} />
                Edit
              </Button>
            )}
          </>
        }
      />

      {resetError && (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
          {resetError}
        </p>
      )}
      {tempPassword && (
        <div className="mb-4 rounded-lg bg-emerald-50 px-3 py-2.5 text-sm text-emerald-800 dark:bg-emerald-950/40 dark:text-emerald-300">
          New temporary password: <span className="font-mono font-semibold">{tempPassword}</span> — share this with
          the user securely; it won't be shown again.
        </div>
      )}

      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-wrap items-center gap-4">
          <Avatar name={user.fullName} size="lg" />
          <div className="flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-semibold text-slate-900 dark:text-white">{user.fullName}</h2>
              <Badge tone={user.isActive ? 'emerald' : 'red'}>{user.isActive ? 'Active' : 'Inactive'}</Badge>
              {!user.emailConfirmed && <Badge tone="amber">Email unconfirmed</Badge>}
            </div>
            <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
              {user.staffNumber ? `Staff · ${user.staffNumber}` : user.guardianId ? 'Guardian account' : 'User account'}
            </p>
          </div>
        </div>
        <div className="mt-4 flex flex-wrap gap-6 text-sm text-slate-600 dark:text-slate-300">
          <span className="flex items-center gap-1.5">
            <Mail className="h-4 w-4 text-slate-400" strokeWidth={2} />
            {user.email}
          </span>
          {user.phone && (
            <span className="flex items-center gap-1.5">
              <Phone className="h-4 w-4 text-slate-400" strokeWidth={2} />
              {user.phone}
            </span>
          )}
          <span className="text-slate-400">
            {user.lastLoginAt ? `Last login ${new Date(user.lastLoginAt).toLocaleString()}` : 'Never logged in'}
          </span>
        </div>
      </div>

      <div className="mt-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-white">Roles &amp; permissions</h3>
          {canManageRoles && (
            <button
              onClick={() => setRolesOpen(true)}
              className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline dark:text-brand-300"
            >
              <ShieldCheck className="h-3.5 w-3.5" strokeWidth={2} />
              Manage roles
            </button>
          )}
        </div>

        {user.roles.length === 0 ? (
          <p className="mt-3 text-sm text-slate-400">No roles assigned.</p>
        ) : (
          <div className="mt-3 flex flex-wrap gap-1.5">
            {user.roles.map((r) => (
              <Badge key={r.id} tone="brand">
                {r.name}
              </Badge>
            ))}
          </div>
        )}

        <p className="mt-4 text-xs font-medium uppercase tracking-wide text-slate-400">
          {user.permissions.length} effective permission{user.permissions.length === 1 ? '' : 's'}
        </p>
      </div>

      <UpdateUserDrawer open={editOpen} onClose={() => setEditOpen(false)} user={user} />
      <AssignRolesDrawer open={rolesOpen} onClose={() => setRolesOpen(false)} user={user} />
    </div>
  )
}
