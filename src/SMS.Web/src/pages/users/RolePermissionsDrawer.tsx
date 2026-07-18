import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { fetchPermissions, updateRolePermissions } from '../../api/roles'
import type { RoleDetailDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { PermissionChecklist } from './PermissionChecklist'

export function RolePermissionsDrawer({
  role,
  onClose,
}: {
  role: RoleDetailDto | null
  onClose: () => void
}) {
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: groups } = useQuery({
    queryKey: ['permissions'],
    queryFn: () => fetchPermissions(),
    enabled: !!role,
  })

  useEffect(() => {
    if (role) {
      setSelected(new Set(role.permissions.map((p) => p.code)))
      setError(null)
    }
  }, [role])

  function toggle(code: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(code)) next.delete(code)
      else next.add(code)
      return next
    })
  }

  function toggleModule(codes: string[]) {
    setSelected((prev) => {
      const next = new Set(prev)
      const allSelected = codes.every((c) => next.has(c))
      codes.forEach((c) => (allSelected ? next.delete(c) : next.add(c)))
      return next
    })
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!role) return
    setError(null)
    setSubmitting(true)
    try {
      await updateRolePermissions({ roleId: role.id, permissionCodes: Array.from(selected) })
      await queryClient.invalidateQueries({ queryKey: ['roles'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not update permissions'))
    } finally {
      setSubmitting(false)
    }
  }

  const isSystemRole = role?.isSystemRole ?? false

  return (
    <Drawer
      open={!!role}
      onClose={onClose}
      title={role ? `${role.name} permissions` : 'Permissions'}
      description={
        isSystemRole
          ? "Built-in roles' permissions are fixed and can't be changed"
          : role?.description || undefined
      }
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            {isSystemRole ? 'Close' : 'Cancel'}
          </Button>
          {!isSystemRole && (
            <Button type="submit" form="role-permissions-form" loading={submitting}>
              Save permissions
            </Button>
          )}
        </>
      }
    >
      <form id="role-permissions-form" onSubmit={handleSubmit} className="space-y-4">
        <fieldset disabled={isSystemRole} className="space-y-4 disabled:opacity-60">
          <PermissionChecklist groups={groups} selected={selected} onToggle={toggle} onToggleModule={toggleModule} />
        </fieldset>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
